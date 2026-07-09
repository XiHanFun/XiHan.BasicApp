<script setup lang="ts">
import type { LogDetailField, LogDetailFieldType } from '../_components/log-detail.types.ts'
import type { TracePreset } from '../_components/trace-nav'
import type { TraceTimelineItemDto, TraceTimelineResultDto } from '@/api'
import type { ListFieldSchema } from '~/components'
import { NCard, NEmpty, NSpin, NTag, NText, NTimeline, NTimelineItem, useMessage } from 'naive-ui'
import { computed, nextTick, onActivated, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { logManagementApi, TraceDimension, TraceLogType } from '@/api'
import { SchemaSearchPanel } from '~/components'
import { formatDate } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { tracePreset } from '../_components/trace-nav'

defineOptions({ name: 'LogTracePage' })

const { t, te } = useI18n()
const message = useMessage()

const DAY_MS = 24 * 60 * 60 * 1000

const ALL_LOG_TYPES: TraceLogType[] = [
  TraceLogType.Access,
  TraceLogType.Api,
  TraceLogType.Operation,
  TraceLogType.Login,
  TraceLogType.Exception,
  TraceLogType.Diff,
  TraceLogType.PermissionChange,
]

/** 通用搜索组件无高级区 */
const EMPTY_FIELDS: ListFieldSchema[] = []

function defaultRange(): [number, number] {
  const now = Date.now()
  return [now - DAY_MS, now]
}

// 搜索模型（供通用搜索组件 SchemaSearchPanel 双向绑定）
const filters = reactive<Record<string, unknown>>({
  dimension: TraceDimension.TraceId,
  value: '',
  logTypes: [...ALL_LOG_TYPES],
  timeRange: defaultRange(),
})

// 结果状态
const loading = ref(false)
const hasQueried = ref(false)
const result = ref<TraceTimelineResultDto | null>(null)

// 详情抽屉
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<Record<string, unknown> | null>(null)
const detailLogType = ref<TraceLogType | null>(null)

// 视口内定高：布局内容区高度链不确定（min-h-full 断链、实际在 body 滚动），
// 故自算可用高度，让结果区内部滚动、搜索区固定在顶部
const pageRef = ref<HTMLElement>()
const pageHeight = ref('')

// ── 选项 ─────────────────────────────────────────────────────────
const dimensionOptions = computed(() => [
  { label: t('log.trace.dimension_trace_id'), value: TraceDimension.TraceId },
  { label: t('log.trace.dimension_session_id'), value: TraceDimension.SessionId },
  { label: t('log.trace.dimension_user_name'), value: TraceDimension.UserName },
  { label: t('log.trace.dimension_ip'), value: TraceDimension.Ip },
  { label: t('log.trace.dimension_user_id'), value: TraceDimension.UserId },
])

const logTypeLabelMap = computed<Record<string, string>>(() => ({
  Access: t('log.trace.log_type_access'),
  Api: t('log.trace.log_type_api'),
  Operation: t('log.trace.log_type_operation'),
  Login: t('log.trace.log_type_login'),
  Exception: t('log.trace.log_type_exception'),
  Diff: t('log.trace.log_type_diff'),
  PermissionChange: t('log.trace.log_type_permission_change'),
}))

const logTypeOptions = computed(() => ALL_LOG_TYPES.map(value => ({ label: logTypeLabelMap.value[value] ?? value, value })))

// 权限变更日志无用户名/会话维度
const disabledTypes = computed<TraceLogType[]>(() =>
  filters.dimension === TraceDimension.UserName || filters.dimension === TraceDimension.SessionId
    ? [TraceLogType.PermissionChange]
    : [])

const showUnsupportedHint = computed(() => disabledTypes.value.length > 0)

const valuePlaceholder = computed(() => {
  switch (filters.dimension) {
    case TraceDimension.TraceId: return t('log.trace.placeholder_trace_id')
    case TraceDimension.SessionId: return t('log.trace.placeholder_session_id')
    case TraceDimension.UserName: return t('log.trace.placeholder_user_name')
    case TraceDimension.Ip: return t('log.trace.placeholder_ip')
    case TraceDimension.UserId: return t('log.trace.placeholder_user_id')
    default: return t('log.trace.value')
  }
})

// 通用搜索组件字段（单一事实源）：维度 / 值 / 日志类型 / 时间范围
const searchFields = computed<ListFieldSchema[]>(() => [
  { key: 'dimension', title: t('log.trace.dimension'), dataType: 'enum', searchable: true, options: dimensionOptions.value, searchPlaceholder: t('log.trace.dimension') },
  { key: 'value', title: t('log.trace.value'), dataType: 'string', searchable: true, searchPlaceholder: valuePlaceholder.value },
  { key: 'logTypes', title: t('log.trace.log_types'), dataType: 'enum', searchable: true, searchMultiple: true, options: logTypeOptions.value, searchPlaceholder: t('log.trace.log_types') },
  { key: 'timeRange', title: t('log.trace.time_range'), dataType: 'datetime', searchable: true, searchRange: true },
])

// 维度切到用户名/会话时，剔除已选的不适用类型（如权限变更）
watch(() => filters.dimension, () => {
  const disabled = disabledTypes.value
  if (disabled.length)
    filters.logTypes = (filters.logTypes as TraceLogType[]).filter(type => !disabled.includes(type))
})

const items = computed(() => result.value?.items ?? [])

const emptyDescription = computed(() =>
  hasQueried.value ? t('log.trace.empty_result') : t('log.trace.empty_initial'))

const detailTitle = computed(() =>
  detailLogType.value
    ? `${logTypeLabelMap.value[detailLogType.value]} · ${t('log.trace.detail_title')}`
    : t('log.trace.detail_title'))

// ── 查询 ─────────────────────────────────────────────────────────
async function runQuery() {
  const dimension = filters.dimension as TraceDimension | null | undefined
  if (!dimension) {
    message.warning(t('log.trace.dimension_required'))
    return
  }

  const value = String(filters.value ?? '').trim()
  if (!value) {
    message.warning(t('log.trace.value_required'))
    return
  }

  const range = filters.timeRange as [number, number] | null
  if (!range) {
    message.warning(t('log.trace.range_required'))
    return
  }

  const disabled = disabledTypes.value
  const effectiveTypes = ((filters.logTypes as TraceLogType[] | undefined) ?? []).filter(type => !disabled.includes(type))
  if (effectiveTypes.length === 0) {
    message.warning(t('log.trace.type_required'))
    return
  }

  loading.value = true
  try {
    result.value = await logManagementApi.trace.timeline({
      dimension,
      value,
      startTime: new Date(range[0]).toISOString(),
      endTime: new Date(range[1]).toISOString(),
      logTypes: effectiveTypes.length === ALL_LOG_TYPES.length ? null : effectiveTypes,
      maxPerType: 200,
    })
    hasQueried.value = true
  }
  catch {
    message.error(t('log.trace.query_failed'))
  }
  finally {
    loading.value = false
  }
}

function reset() {
  filters.dimension = TraceDimension.TraceId
  filters.value = ''
  filters.logTypes = [...ALL_LOG_TYPES]
  filters.timeRange = defaultRange()
  result.value = null
  hasQueried.value = false
}

// ── 展示辅助 ─────────────────────────────────────────────────────
function timelineType(status: string): 'default' | 'error' | 'info' | 'success' | 'warning' {
  switch (status) {
    case 'success': return 'success'
    case 'error': return 'error'
    case 'warning': return 'warning'
    case 'info': return 'info'
    default: return 'default'
  }
}

function logTypeTagType(type: TraceLogType): 'default' | 'error' | 'info' | 'primary' | 'success' | 'warning' {
  switch (type) {
    case TraceLogType.Access: return 'info'
    case TraceLogType.Api: return 'primary'
    case TraceLogType.Operation: return 'success'
    case TraceLogType.Login: return 'warning'
    case TraceLogType.Exception: return 'error'
    case TraceLogType.Diff: return 'info'
    default: return 'default'
  }
}

function logTypeLabel(type: string) {
  return logTypeLabelMap.value[type] ?? type
}

function metaParts(item: TraceTimelineItemDto): { key: string, label: string, value: string }[] {
  const parts: { key: string, label: string, value: string }[] = []
  if (item.userName)
    parts.push({ key: 'user', label: t('log.common.user_name'), value: item.userName })
  if (item.ip)
    parts.push({ key: 'ip', label: 'IP', value: item.ip })
  if (item.method)
    parts.push({ key: 'method', label: t('log.common.method'), value: item.method })
  if (item.executionTime != null && item.executionTime !== '')
    parts.push({ key: 'cost', label: t('log.common.execution_time'), value: `${item.executionTime}ms` })
  if (item.sessionId)
    parts.push({ key: 'session', label: t('log.common.session_id'), value: item.sessionId })
  if (item.traceId)
    parts.push({ key: 'trace', label: t('log.common.trace_id'), value: item.traceId })
  return parts
}

// ── 详情 ─────────────────────────────────────────────────────────
const detailFetchers: Record<TraceLogType, (id: string) => Promise<unknown>> = {
  [TraceLogType.Access]: id => logManagementApi.access.detail(id),
  [TraceLogType.Api]: id => logManagementApi.api.detail(id),
  [TraceLogType.Operation]: id => logManagementApi.operation.detail(id),
  [TraceLogType.Login]: id => logManagementApi.login.detail(id),
  [TraceLogType.Exception]: id => logManagementApi.exception.detail(id),
  [TraceLogType.Diff]: id => logManagementApi.diff.detail(id),
  [TraceLogType.PermissionChange]: id => logManagementApi.permissionChanges.detail(id),
}

async function openDetail(item: TraceTimelineItemDto) {
  detailLogType.value = item.logType
  detailVisible.value = true
  detailLoading.value = true
  try {
    const record = await detailFetchers[item.logType](item.basicId)
    detailData.value = (record as Record<string, unknown> | null) ?? (item as unknown as Record<string, unknown>)
  }
  catch {
    detailData.value = item as unknown as Record<string, unknown>
    message.error(t('log.trace.detail_load_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

const CODE_KEYS = new Set([
  'extendData',
  'requestBody',
  'responseBody',
  'requestParams',
  'requestHeaders',
  'responseHeaders',
  'exceptionStackTrace',
  'stackTrace',
  'changedFields',
  'beforeData',
  'afterData',
  'userAgent',
  'errorMessage',
  'exceptionMessage',
  'handledRemark',
])

function fieldType(key: string): LogDetailFieldType | undefined {
  if (key === 'executionTime')
    return 'duration'
  if (key === 'requestSize' || key === 'responseSize')
    return 'bytes'
  if (CODE_KEYS.has(key))
    return 'code'
  if (/Time$/.test(key))
    return 'date'
  return undefined
}

function fieldLabel(key: string): string {
  const snake = key.replace(/([A-Z])/g, '_$1').toLowerCase()
  const commonKey = `log.common.${snake}`
  if (te(commonKey))
    return t(commonKey)
  const humanized = key.replace(/([A-Z])/g, ' $1').trim()
  return humanized.charAt(0).toUpperCase() + humanized.slice(1)
}

const detailFields = computed<LogDetailField[]>(() => {
  const record = detailData.value
  if (!record)
    return []
  return Object.keys(record)
    .filter((key) => {
      const v = record[key]
      return v !== null && v !== undefined && v !== ''
    })
    .map(key => ({ key, label: fieldLabel(key), type: fieldType(key), span: fieldType(key) === 'code' ? 2 : 1 } as LogDetailField))
})

// ── 深链预填（从各日志页「追踪」跳转，经共享预设复用同一标签页） ──
function applyPreset(preset: TracePreset) {
  if ((Object.values(TraceDimension) as string[]).includes(preset.dimension))
    filters.dimension = preset.dimension as TraceDimension
  filters.value = preset.value
  filters.timeRange = [preset.start, preset.end]
  filters.logTypes = [...ALL_LOG_TYPES]
  void runQuery()
}

function consumePreset() {
  const preset = tracePreset.value
  if (!preset)
    return
  tracePreset.value = null
  applyPreset(preset)
}

// 首次进入（新开标签）由 onMounted 消费；标签已存在（keep-alive）时由 watch 消费
onMounted(consumePreset)
watch(tracePreset, (preset) => {
  if (preset)
    consumePreset()
})

// ── 视口内定高：令搜索区固定、仅结果区滚动（规避布局在 body 级滚动） ──
function updatePageHeight() {
  const el = pageRef.value
  if (!el)
    return
  const top = el.getBoundingClientRect().top
  const footer = document.querySelector<HTMLElement>('.footer-bar')
  const bottom = footer && getComputedStyle(footer).position === 'fixed'
    ? footer.getBoundingClientRect().top
    : window.innerHeight
  pageHeight.value = `${Math.max(240, Math.floor(bottom - top))}px`
}

onMounted(() => {
  void nextTick(updatePageHeight)
  window.addEventListener('resize', updatePageHeight)
})
onActivated(() => {
  void nextTick(updatePageHeight)
})
onBeforeUnmount(() => {
  window.removeEventListener('resize', updatePageHeight)
})
</script>

<template>
  <div ref="pageRef" class="trace-page" :style="{ height: pageHeight }">
    <NCard size="small" :content-style="{ padding: '12px 16px' }" :style="{ overflow: 'visible' }">
      <SchemaSearchPanel
        :advanced-fields="EMPTY_FIELDS"
        :common-fields="searchFields"
        :model="filters"
        @reset="reset"
        @search="runQuery"
      />
      <NText v-if="showUnsupportedHint" depth="3" class="trace-hint">
        {{ t('log.trace.unsupported_hint') }}
      </NText>
    </NCard>

    <NCard
      size="small"
      class="flex-1"
      style="height: 0"
      :content-style="{ height: '100%', display: 'flex', flexDirection: 'column', minHeight: '0', padding: '12px 16px' }"
    >
      <NSpin :show="loading" class="trace-scroll">
        <div v-if="result" class="trace-summary">
          <NText strong>
            {{ t('log.trace.summary_total', { total: result.totalCount }) }}
          </NText>
          <NTag
            v-for="(count, type) in result.typeCounts"
            :key="type"
            size="small"
            round
            :bordered="false"
            :type="logTypeTagType(type as TraceLogType)"
          >
            {{ logTypeLabel(type) }} · {{ count }}
          </NTag>
          <NTag v-if="result.truncated" size="small" type="warning" :bordered="false">
            {{ t('log.trace.truncated') }}
          </NTag>
        </div>

        <NTimeline v-if="items.length" class="trace-timeline">
          <NTimelineItem
            v-for="item in items"
            :key="`${item.logType}-${item.basicId}`"
            :type="timelineType(item.status)"
            :time="formatDate(item.time)"
          >
            <div class="trace-item" @click="openDetail(item)">
              <div class="trace-item__head">
                <NTag size="small" round :bordered="false" :type="logTypeTagType(item.logType)">
                  {{ logTypeLabel(item.logType) }}
                </NTag>
                <span class="trace-item__title">{{ item.title || '-' }}</span>
                <NTag v-if="item.statusCode != null" size="tiny" :bordered="false">
                  {{ item.statusCode }}
                </NTag>
              </div>
              <div v-if="item.summary" class="trace-item__summary">
                {{ item.summary }}
              </div>
              <div v-if="metaParts(item).length" class="trace-item__meta">
                <span v-for="p in metaParts(item)" :key="p.key" class="trace-chip">
                  <span class="trace-chip__k">{{ p.label }}</span>{{ p.value }}
                </span>
              </div>
            </div>
          </NTimelineItem>
        </NTimeline>

        <NEmpty v-else :description="emptyDescription" class="trace-empty" />
      </NSpin>
    </NCard>

    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      :title="detailTitle"
    />
  </div>
</template>

<style scoped>
.trace-page {
  display: flex;
  flex-direction: column;
  gap: 8px;
  box-sizing: border-box;
  padding: 12px;
  overflow: hidden;
}

/* 结果区内部滚动：页面已在视口内定高，故 flex-1 + min-height:0 得到确定高度，滚动只发生在时间线内部 */
.trace-scroll {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

.trace-hint {
  display: block;
  margin-top: 8px;
  font-size: 12px;
}

.trace-summary {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
}

.trace-timeline {
  padding: 4px 4px 4px 8px;
}

.trace-item {
  cursor: pointer;
  padding: 6px 8px;
  margin: -6px -8px;
  border-radius: 6px;
  transition: background-color 0.15s ease;
}

.trace-item:hover {
  background: var(--code-bg, rgba(128, 128, 128, 0.08));
}

.trace-item__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.trace-item__title {
  font-weight: 500;
  word-break: break-all;
}

.trace-item__summary {
  margin-top: 4px;
  color: var(--text-secondary);
  font-size: 13px;
  word-break: break-word;
}

.trace-item__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 14px;
  margin-top: 6px;
  font-size: 12px;
  color: var(--text-secondary);
}

.trace-chip__k {
  margin-right: 4px;
  opacity: 0.7;
}

.trace-empty {
  padding: 48px 0;
}
</style>
