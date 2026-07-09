<script setup lang="ts">
import type { LogDetailField, LogDetailFieldType } from '../_components/log-detail.types.ts'
import type { TracePreset } from '../_components/trace-nav'
import type { TraceTimelineItemDto, TraceTimelineResultDto } from '@/api'
import type { ListFieldSchema } from '~/components'
import { NCard, NEmpty, NSpin, NText, useMessage, useThemeVars } from 'naive-ui'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { logManagementApi, TraceDimension, TraceLogType } from '@/api'
import { SchemaSearchPanel } from '~/components'
import { formatDate } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { tracePreset } from '../_components/trace-nav'

defineOptions({ name: 'LogTracePage' })

const { t, te } = useI18n()
const message = useMessage()
const themeVars = useThemeVars()

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
// 中性灰度为主，仅异常/告警着色（对齐设计稿）
function statusColor(status: string): string {
  switch (status) {
    case 'error': return themeVars.value.errorColor
    case 'warning': return themeVars.value.warningColor
    default: return themeVars.value.textColor2
  }
}

function statusLabel(status: string): string {
  switch (status) {
    case 'success': return t('log.trace.status_success')
    case 'error': return t('log.trace.status_error')
    case 'warning': return t('log.trace.status_warning')
    case 'info': return t('log.trace.status_info')
    default: return '-'
  }
}

function logTypeLabel(type: string) {
  return logTypeLabelMap.value[type] ?? type
}

/** 状态文案：有 HTTP 码显码，否则显结果文案 */
function statusText(item: TraceTimelineItemDto): string {
  return item.statusCode != null ? String(item.statusCode) : statusLabel(item.status)
}

/** 路径/标题（method/path 为主，机器值等宽） */
function pathOf(item: TraceTimelineItemDto): string {
  return item.path || item.title || '-'
}

// formatDate 输出 "YYYY-MM-DD HH:MM:SS"，拆出时间/分钟
function timeText(value: string): string {
  const parts = formatDate(value).split(' ')
  return parts[1] ?? parts[0] ?? ''
}
function minuteKey(value: string): string {
  return formatDate(value).slice(0, 16)
}

/** 行数据：按分钟分组（相邻分钟变化时显示分钟分隔） */
const rows = computed(() => {
  let prev = ''
  return items.value.map((item) => {
    const key = minuteKey(item.time)
    const showMinute = key !== prev
    prev = key
    return { item, showMinute, minute: timeText(item.time).slice(0, 5) }
  })
})

/** 最大耗时（耗时条相对刻度） */
const maxDuration = computed(() => {
  let max = 1
  for (const item of items.value) {
    const v = Number(item.executionTime)
    if (Number.isFinite(v) && v > max)
      max = v
  }
  return max
})

function hasDuration(item: TraceTimelineItemDto): boolean {
  const v = Number(item.executionTime)
  return Number.isFinite(v) && v > 0
}
function barWidth(item: TraceTimelineItemDto): string {
  const v = Number(item.executionTime)
  const pct = Number.isFinite(v) ? (v / maxDuration.value) * 100 : 0
  return `${Math.max(6, Math.min(100, Math.round(pct)))}%`
}

/** 元信息（机器值等宽，用户/IP + 链路/会话次之） */
function metaParts(item: TraceTimelineItemDto): { key: string, label: string, value: string }[] {
  const parts: { key: string, label: string, value: string }[] = []
  if (item.userName)
    parts.push({ key: 'user', label: t('log.common.user_name'), value: item.userId != null ? `${item.userName} (#${item.userId})` : item.userName })
  else if (item.userId != null)
    parts.push({ key: 'user', label: t('log.common.user_id'), value: String(item.userId) })
  if (item.ip)
    parts.push({ key: 'ip', label: 'IP', value: item.location ? `${item.ip} · ${item.location}` : item.ip })
  if (item.traceId)
    parts.push({ key: 'trace', label: t('log.common.trace_id'), value: item.traceId })
  if (item.sessionId)
    parts.push({ key: 'session', label: t('log.common.session_id'), value: item.sessionId })
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
</script>

<template>
  <div class="trace-page">
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
      :content-style="{ height: '100%', display: 'flex', flexDirection: 'column', minHeight: '0', padding: '0' }"
    >
      <NSpin :show="loading" class="trace-scroll">
        <div v-if="result" class="trace-panel">
          <div class="trace-panel__header">
            <div class="trace-panel__titlerow">
              <span class="trace-panel__title">{{ t('log.trace.page_name') }}</span>
              <span class="trace-panel__count">{{ t('log.trace.summary_total', { total: result.totalCount }) }}</span>
              <span class="trace-panel__grow" />
              <span
                v-for="(count, type) in result.typeCounts"
                :key="type"
                class="trace-chip"
                :class="{ 'is-error': type === 'Exception' }"
              >
                {{ logTypeLabel(type) }}<i>·</i><b>{{ count }}</b>
              </span>
            </div>
            <div v-if="result.truncated" class="trace-panel__warn">
              <span class="trace-panel__warndot" />
              {{ t('log.trace.truncated') }}
            </div>
          </div>

          <div v-if="rows.length" class="trace-panel__list">
            <template v-for="row in rows" :key="`${row.item.logType}-${row.item.basicId}`">
              <div v-if="row.showMinute" class="trace-min">
                <span class="trace-min__label">{{ row.minute }}</span>
                <span class="trace-min__gap" />
                <span class="trace-min__line" />
              </div>
              <div class="trace-row" @click="openDetail(row.item)">
                <div class="trace-row__time">
                  {{ timeText(row.item.time) }}
                </div>
                <div class="trace-row__rail">
                  <span class="trace-row__line" />
                  <span class="trace-row__dot" :style="{ borderColor: statusColor(row.item.status) }" />
                </div>
                <div class="trace-row__body">
                  <div class="trace-row__main">
                    <span class="trace-row__chip">{{ row.item.method || logTypeLabel(row.item.logType) }}</span>
                    <span class="trace-row__path">{{ pathOf(row.item) }}</span>
                    <span class="trace-row__grow" />
                    <span v-if="hasDuration(row.item)" class="trace-row__dur">
                      <span class="trace-row__bar">
                        <span class="trace-row__barfill" :style="{ width: barWidth(row.item), background: statusColor(row.item.status) }" />
                      </span>
                      <span class="trace-row__durlabel">{{ row.item.executionTime }}ms</span>
                    </span>
                    <span class="trace-row__status" :style="{ color: statusColor(row.item.status) }">
                      <span class="trace-row__sdot" :style="{ background: statusColor(row.item.status) }" />
                      {{ statusText(row.item) }}
                    </span>
                  </div>
                  <div v-if="row.item.summary || metaParts(row.item).length" class="trace-row__meta">
                    <span v-if="row.item.summary" class="trace-row__handler">{{ row.item.summary }}</span>
                    <template v-for="p in metaParts(row.item)" :key="p.key">
                      <i class="trace-row__sep">·</i>
                      <span>{{ p.label }} <b>{{ p.value }}</b></span>
                    </template>
                  </div>
                </div>
              </div>
            </template>
          </div>

          <NEmpty v-else :description="emptyDescription" class="trace-empty" />
        </div>

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
  height: 100%;
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

/* ── 时间线轨道（内嵌于外层 NCard，中性灰度 + 等宽机器值；不再套第二层边框） ── */
.trace-panel {
  --trace-mono: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

/* 头部吸顶：随内容滚动固定在结果卡顶部 */
.trace-panel__header {
  position: sticky;
  top: 0;
  z-index: 1;
  padding: 14px 20px 12px;
  background: v-bind('themeVars.cardColor');
  border-bottom: 1px solid v-bind('themeVars.dividerColor');
}

.trace-panel__titlerow {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 8px 10px;
}

.trace-panel__title {
  font-size: 15px;
  font-weight: 600;
  letter-spacing: -0.01em;
  color: v-bind('themeVars.textColor1');
}

.trace-panel__count {
  font-size: 12px;
  color: v-bind('themeVars.textColor3');
}

.trace-panel__grow {
  flex: 1;
}

.trace-chip {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
  padding: 3px 8px;
  border-radius: 5px;
  background: v-bind('themeVars.actionColor');
  color: v-bind('themeVars.textColor3');
  font-family: var(--trace-mono);
  font-size: 11px;
}

.trace-chip i {
  font-style: normal;
  opacity: 0.5;
}

.trace-chip b {
  font-weight: 500;
  color: v-bind('themeVars.textColor2');
}

.trace-chip.is-error,
.trace-chip.is-error b {
  color: v-bind('themeVars.errorColor');
}

.trace-panel__warn {
  display: flex;
  align-items: center;
  gap: 7px;
  margin-top: 9px;
  font-size: 11px;
  color: v-bind('themeVars.warningColor');
}

.trace-panel__warndot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: v-bind('themeVars.warningColor');
}

.trace-panel__list {
  padding: 2px 20px 12px;
}

/* 分钟分隔 */
.trace-min {
  display: flex;
  align-items: center;
  gap: 11px;
  padding: 14px 0 6px;
}

.trace-min__label {
  width: 56px;
  flex: none;
  text-align: right;
  font-family: var(--trace-mono);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: v-bind('themeVars.textColor2');
}

.trace-min__gap {
  width: 34px;
  flex: none;
}

.trace-min__line {
  flex: 1;
  height: 1px;
  background: v-bind('themeVars.dividerColor');
}

/* 单行 */
.trace-row {
  display: flex;
  align-items: flex-start;
  border-bottom: 1px solid v-bind('themeVars.dividerColor');
  cursor: pointer;
  transition: background-color 0.12s ease;
}

.trace-row:hover {
  background: v-bind('themeVars.hoverColor');
}

.trace-row__time {
  width: 56px;
  flex: none;
  text-align: right;
  padding-top: 14px;
  font-family: var(--trace-mono);
  font-size: 11.5px;
  color: v-bind('themeVars.textColor3');
}

.trace-row__rail {
  position: relative;
  width: 34px;
  flex: none;
  align-self: stretch;
}

.trace-row__line {
  position: absolute;
  left: 16px;
  top: 0;
  bottom: 0;
  width: 1px;
  background: v-bind('themeVars.borderColor');
}

.trace-row__dot {
  position: absolute;
  left: 11px;
  top: 14px;
  width: 9px;
  height: 9px;
  border-radius: 50%;
  background: v-bind('themeVars.cardColor');
  border: 2px solid;
  box-shadow: 0 0 0 3px v-bind('themeVars.cardColor');
}

.trace-row__body {
  flex: 1;
  min-width: 0;
  padding: 11px 0 12px;
}

.trace-row__main {
  display: flex;
  align-items: center;
  gap: 9px;
}

.trace-row__chip {
  flex: none;
  padding: 2px 6px;
  border-radius: 5px;
  border: 1px solid v-bind('themeVars.borderColor');
  background: v-bind('themeVars.actionColor');
  color: v-bind('themeVars.textColor3');
  font-family: var(--trace-mono);
  font-size: 10.5px;
  font-weight: 500;
  letter-spacing: 0.03em;
  line-height: 1.3;
}

.trace-row__path {
  min-width: 0;
  font-family: var(--trace-mono);
  font-size: 13.5px;
  font-weight: 500;
  letter-spacing: -0.01em;
  color: v-bind('themeVars.textColor1');
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.trace-row__grow {
  flex: 1;
}

.trace-row__dur {
  display: inline-flex;
  align-items: center;
  gap: 9px;
  flex: none;
}

.trace-row__bar {
  width: 60px;
  height: 5px;
  border-radius: 3px;
  background: v-bind('themeVars.actionColor');
  overflow: hidden;
}

.trace-row__barfill {
  display: block;
  height: 100%;
  border-radius: 3px;
}

.trace-row__durlabel {
  min-width: 46px;
  text-align: right;
  font-family: var(--trace-mono);
  font-size: 12px;
  color: v-bind('themeVars.textColor2');
}

.trace-row__status {
  display: inline-flex;
  align-items: center;
  justify-content: flex-end;
  gap: 5px;
  flex: none;
  min-width: 52px;
  font-family: var(--trace-mono);
  font-size: 12px;
  font-weight: 500;
}

.trace-row__sdot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.trace-row__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
  margin-top: 7px;
  font-family: var(--trace-mono);
  font-size: 11.5px;
  color: v-bind('themeVars.textColor3');
}

.trace-row__meta b {
  font-weight: 400;
  color: v-bind('themeVars.textColor2');
  word-break: break-all;
}

.trace-row__handler {
  color: v-bind('themeVars.textColor2');
}

.trace-row__sep {
  font-style: normal;
  opacity: 0.4;
}

.trace-empty {
  padding: 48px 0;
}
</style>
