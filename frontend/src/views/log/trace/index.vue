<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { TracePreset } from '../_components/trace-nav'
import type { TraceTimelineItemDto, TraceTimelineResultDto } from '@/api'
import type { ListFieldSchema } from '~/components'
import { NCard, NEmpty, NSpin, NText, useMessage, useThemeVars } from 'naive-ui'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { logManagementApi, TraceDimension, TraceLogType } from '@/api'
import { SchemaSearchPanel } from '~/components'
import { formatDate } from '~/utils'
import {
  accessLogDetailFields,
  apiLogDetailFields,
  diffLogDetailFields,
  exceptionLogDetailFields,
  loginLogDetailFields,
  operationLogDetailFields,
  permissionChangeLogDetailFields,
} from '../_components/log-detail-fields'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { tracePreset } from '../_components/trace-nav'

defineOptions({ name: 'LogTracePage' })

const { t } = useI18n()
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

// 大小写无关索引：后端统计 typeCounts 的字典键经 JSON camelCase 序列化为 access/operation/…，
// 与枚举 PascalCase 不一致；用小写归一化同时兼容 item.logType(PascalCase) 与 typeCounts 键(camelCase)。
const logTypeLabelByLower = computed<Record<string, string>>(() => {
  const map: Record<string, string> = {}
  for (const [key, label] of Object.entries(logTypeLabelMap.value))
    map[key.toLowerCase()] = label
  return map
})

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
type StatusKind = 'success' | 'error' | 'warning' | 'info'

/**
 * 状态归一：以后端已折算的 item.status 为权威来源（后端已综合 IsSuccess/AccessResult/
 * 严重级别等得出统一结果，7 类日志一致）。不按原始 HTTP 码判色——本框架业务失败常包成
 * HTTP 200，若按码判定会把失败误染成「成功(绿)」。HTTP 码仅用于状态标签文案(statusText)。
 * 颜色统一后，「200」与「成功」这类不同文案也呈现一致的状态视觉。
 */
function statusKind(item: TraceTimelineItemDto): StatusKind {
  switch (item.status) {
    case 'error': return 'error'
    case 'warning': return 'warning'
    case 'success': return 'success'
    default: return 'info'
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
  return logTypeLabelByLower.value[type.toLowerCase()] ?? type
}

/** 请求方式配色：按 HTTP 动词着色；非 HTTP（日志类型标签）保持中性 */
function methodClass(item: TraceTimelineItemDto): string {
  switch ((item.method ?? '').toUpperCase()) {
    case 'GET': return 'm-get'
    case 'POST': return 'm-post'
    case 'PUT': return 'm-put'
    case 'PATCH': return 'm-patch'
    case 'DELETE': return 'm-delete'
    default: return ''
  }
}

/** 日志类型配色：与请求方式 chip 同款样式，按日志类型着色 */
function logTypeClass(item: TraceTimelineItemDto): string {
  switch (item.logType) {
    case TraceLogType.Login: return 't-success'
    case TraceLogType.Exception: return 't-error'
    case TraceLogType.Diff: return 't-warning'
    case TraceLogType.Operation:
    case TraceLogType.PermissionChange: return 't-primary'
    case TraceLogType.Access:
    case TraceLogType.Api: return 't-info'
    default: return 't-info'
  }
}

/** 状态文案：有正整数 HTTP 码显码，否则显结果文案（与 statusKind 判定口径一致，避免出现无意义的「0」） */
function statusText(item: TraceTimelineItemDto): string {
  const code = Number(item.statusCode)
  return Number.isFinite(code) && code > 0 ? String(code) : statusLabel(item.status)
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

/** 分钟分组：外层时间线按分钟连接，分钟内为卡片子时间线 */
const groups = computed(() => {
  const out: { key: string, minute: string, items: TraceTimelineItemDto[] }[] = []
  let prev = ''
  for (const item of items.value) {
    const key = minuteKey(item.time)
    if (key !== prev) {
      out.push({ key, minute: timeText(item.time).slice(0, 5), items: [] })
      prev = key
    }
    out[out.length - 1]!.items.push(item)
  }
  return out
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

/** 耗时分级（独立于状态配色）：<500ms 快 / <1500ms 一般 / 更久 慢 */
function durationLevel(item: TraceTimelineItemDto): 'fast' | 'mid' | 'slow' {
  const v = Number(item.executionTime)
  if (!Number.isFinite(v) || v < 500)
    return 'fast'
  if (v < 1500)
    return 'mid'
  return 'slow'
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

// 详情字段：直接复用各日志类型页已定义的字段/标签/枚举国际化（单一事实源 log-detail-fields.ts），
// 不在链路追踪里另建映射，避免漏国际化。
const DETAIL_FIELDS_BY_TYPE: Record<TraceLogType, (t: (key: string) => string) => LogDetailField[]> = {
  [TraceLogType.Access]: accessLogDetailFields,
  [TraceLogType.Api]: apiLogDetailFields,
  [TraceLogType.Operation]: operationLogDetailFields,
  [TraceLogType.Login]: loginLogDetailFields,
  [TraceLogType.Exception]: exceptionLogDetailFields,
  [TraceLogType.Diff]: diffLogDetailFields,
  [TraceLogType.PermissionChange]: permissionChangeLogDetailFields,
}

const detailFields = computed<LogDetailField[]>(() =>
  detailLogType.value != null ? DETAIL_FIELDS_BY_TYPE[detailLogType.value](t) : [])

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
                :class="{ 'is-error': String(type).toLowerCase() === 'exception' }"
              >
                {{ logTypeLabel(type) }}<i>·</i><b>{{ count }}</b>
              </span>
            </div>
            <div v-if="result.truncated" class="trace-panel__warn">
              <span class="trace-panel__warndot" />
              {{ t('log.trace.truncated') }}
            </div>
          </div>

          <div v-if="groups.length" class="trace-panel__list">
            <div v-for="grp in groups" :key="grp.key" class="trace-grp">
              <div class="trace-grp__minute">
                <span class="trace-grp__node" />
                <span class="trace-grp__label">{{ grp.minute }}</span>
                <span class="trace-grp__count">{{ grp.items.length }}</span>
              </div>
              <div class="trace-grp__cards">
                <div
                  v-for="item in grp.items"
                  :key="`${item.logType}-${item.basicId}`"
                  class="trace-card"
                  :class="`is-${statusKind(item)}`"
                  @click="openDetail(item)"
                >
                  <div class="trace-card__head">
                    <span class="trace-card__time">{{ timeText(item.time) }}</span>
                    <span class="trace-card__chip" :class="logTypeClass(item)">{{ logTypeLabel(item.logType) }}</span>
                    <span v-if="item.method" class="trace-card__chip" :class="methodClass(item)">{{ item.method }}</span>
                    <span class="trace-card__grow" />
                    <span class="trace-card__status">{{ statusText(item) }}</span>
                  </div>
                  <div class="trace-card__path">
                    {{ pathOf(item) }}
                  </div>
                  <div v-if="item.summary" class="trace-card__summary">
                    {{ item.summary }}
                  </div>
                  <div v-if="hasDuration(item)" class="trace-card__dur" :class="`dur-${durationLevel(item)}`">
                    <span class="trace-card__bar">
                      <span class="trace-card__barfill" :style="{ width: barWidth(item) }" />
                    </span>
                    <span class="trace-card__durlabel">{{ item.executionTime }}ms</span>
                  </div>
                  <div v-if="metaParts(item).length" class="trace-card__meta">
                    <div v-for="p in metaParts(item)" :key="p.key" class="trace-card__metarow">
                      <span class="trace-card__metak">{{ p.label }}</span>
                      <span class="trace-card__metav">{{ p.value }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
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

/* ── 时间线轨道（内嵌于外层 NCard，等宽机器值；卡片按状态浅色着色） ── */
.trace-panel {
  --trace-mono: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  /* 状态色 / 耗时色 / 请求方式色 + 底色（卡片用 color-mix 调浅色） */
  --t-card: v-bind('themeVars.cardColor');
  --t-action: v-bind('themeVars.actionColor');
  --t-border: v-bind('themeVars.borderColor');
  --t-succ: v-bind('themeVars.successColor');
  --t-err: v-bind('themeVars.errorColor');
  --t-warn: v-bind('themeVars.warningColor');
  --t-info: v-bind('themeVars.infoColor');
  --t-primary: v-bind('themeVars.primaryColor');
  --t-text: v-bind('themeVars.textColor1');
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
  padding: 6px 20px 16px;
}

/* ── 外层时间线：按分钟连接 ── */
.trace-grp {
  position: relative;
}

/* 贯穿分钟节点的竖直主线（相邻分组无间距 → 连续） */
.trace-grp::before {
  content: '';
  position: absolute;
  left: 6px;
  top: 0;
  bottom: 0;
  width: 2px;
  background: v-bind('themeVars.dividerColor');
}

.trace-grp:first-child::before {
  top: 22px;
}

.trace-grp:last-child::before {
  bottom: auto;
  height: 24px;
}

/* 分钟节点 */
.trace-grp__minute {
  position: relative;
  display: flex;
  align-items: center;
  gap: 9px;
  min-height: 20px;
  padding: 12px 0 10px 26px;
}

.trace-grp__node {
  position: absolute;
  left: 1px;
  top: 50%;
  transform: translateY(-50%);
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: v-bind('themeVars.primaryColor');
  box-shadow: 0 0 0 3px v-bind('themeVars.cardColor');
}

.trace-grp__label {
  font-family: var(--trace-mono);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: v-bind('themeVars.textColor1');
}

.trace-grp__count {
  padding: 1px 6px;
  border-radius: 8px;
  background: v-bind('themeVars.actionColor');
  color: v-bind('themeVars.textColor3');
  font-family: var(--trace-mono);
  font-size: 10.5px;
  line-height: 1.5;
}

/* ── 分钟内：卡片横向排列（自适应换行），信息竖向堆叠完整展示 ── */
.trace-grp__cards {
  margin-left: 27px;
  padding-bottom: 16px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 10px;
  align-items: start;
}

/* 单条日志卡片：纵向堆叠 + 按状态(--k)浅色着色 */
.trace-card {
  --k: var(--t-info); /* 状态色，由 is-* 覆盖 */

  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 11px 13px 12px;
  border: 1px solid color-mix(in srgb, var(--k) 20%, var(--t-border));
  border-radius: 8px;
  /* 浅色渐变背景：状态色微染 → 卡片底色 */
  background: linear-gradient(135deg, color-mix(in srgb, var(--k) 9%, var(--t-card)) 0%, var(--t-card) 62%);
  cursor: pointer;
  transition:
    border-color 0.14s ease,
    box-shadow 0.14s ease,
    background 0.14s ease;
}

/* 状态 → 状态色（见 statusKind，来源于后端权威结果） */
.trace-card.is-success {
  /* 成功是绝大多数：染色更淡，让异常(告警/错误)更跳出 */
  --k: var(--t-succ);

  background: linear-gradient(135deg, color-mix(in srgb, var(--k) 5%, var(--t-card)) 0%, var(--t-card) 66%);
}

.trace-card.is-info {
  --k: var(--t-info);
}

.trace-card.is-warning {
  --k: var(--t-warn);
}

.trace-card.is-error {
  --k: var(--t-err);
}

.trace-card:hover {
  /* 悬停反馈以 边框加强 + 背景微抬升 为主（明暗主题都可见，不依赖纯黑阴影） */
  border-color: color-mix(in srgb, var(--k) 45%, var(--t-border));
  background: linear-gradient(135deg, color-mix(in srgb, var(--k) 14%, var(--t-card)) 0%, var(--t-card) 56%);
  box-shadow: 0 2px 12px rgb(0 0 0 / 0.1);
}

/* 卡片头部：状态点 + 方法 + 时间 … 状态标签 */
.trace-card__head {
  display: flex;
  align-items: center;
  gap: 8px;
}

.trace-card__chip {
  --m: var(--t-info); /* 请求方式色，由 m-* 覆盖 */

  flex: none;
  padding: 2px 7px;
  border-radius: 5px;
  border: 1px solid v-bind('themeVars.borderColor');
  background: v-bind('themeVars.actionColor');
  color: v-bind('themeVars.textColor3');
  font-family: var(--trace-mono);
  font-size: 10.5px;
  font-weight: 600;
  letter-spacing: 0.04em;
  line-height: 1.3;
}

/* 彩色 chip：HTTP 动词（m-*）与日志类型（t-*）同款着色 */
.trace-card__chip.m-get,
.trace-card__chip.m-post,
.trace-card__chip.m-put,
.trace-card__chip.m-patch,
.trace-card__chip.m-delete,
.trace-card__chip.t-info,
.trace-card__chip.t-primary,
.trace-card__chip.t-success,
.trace-card__chip.t-warning,
.trace-card__chip.t-error {
  /* 文字向 ink 混以保证对比度（浅底上纯色调过淡） */
  color: color-mix(in srgb, var(--m) 50%, var(--t-text));
  background: color-mix(in srgb, var(--m) 13%, var(--t-card));
  border-color: color-mix(in srgb, var(--m) 30%, var(--t-border));
}

.trace-card__chip.m-get,
.trace-card__chip.t-info {
  --m: var(--t-info);
}

.trace-card__chip.m-post,
.trace-card__chip.t-success {
  --m: var(--t-succ);
}

.trace-card__chip.m-put,
.trace-card__chip.t-warning {
  --m: var(--t-warn);
}

.trace-card__chip.m-patch,
.trace-card__chip.t-primary {
  --m: var(--t-primary);
}

.trace-card__chip.m-delete,
.trace-card__chip.t-error {
  --m: var(--t-err);
}

.trace-card__time {
  flex: none;
  font-family: var(--trace-mono);
  font-size: 11.5px;
  color: v-bind('themeVars.textColor3');
}

.trace-card__grow {
  flex: 1;
}

/* 状态标签：状态色文字(向 ink 混以保证对比度) + 浅色底，统一「200 / 成功」视觉 */
.trace-card__status {
  flex: none;
  padding: 1px 8px;
  border-radius: 999px;
  font-family: var(--trace-mono);
  font-size: 11px;
  font-weight: 600;
  color: color-mix(in srgb, var(--k) 45%, var(--t-text));
  background: color-mix(in srgb, var(--k) 16%, var(--t-card));
}

/* 路径：完整展示、允许换行（不截断） */
.trace-card__path {
  font-family: var(--trace-mono);
  font-size: 13px;
  font-weight: 500;
  letter-spacing: -0.01em;
  line-height: 1.45;
  color: v-bind('themeVars.textColor1');
  word-break: break-all;
}

.trace-card__summary {
  font-size: 12px;
  line-height: 1.45;
  color: v-bind('themeVars.textColor2');
  word-break: break-word;
}

/* 耗时按其快慢单独着色(--d)，与状态色解耦 */
.trace-card__dur {
  --d: var(--t-info); /* 耗时色，由 dur-* 覆盖 */

  display: flex;
  align-items: center;
  gap: 9px;
}

.trace-card__dur.dur-fast {
  --d: var(--t-succ);
}

.trace-card__dur.dur-mid {
  --d: var(--t-warn);
}

.trace-card__dur.dur-slow {
  --d: var(--t-err);
}

.trace-card__bar {
  flex: 1;
  height: 5px;
  border-radius: 3px;
  /* 轨道混入不透明卡片底色（避免暗色主题下 actionColor 半透明导致轨道消隐） */
  background: color-mix(in srgb, var(--d) 10%, var(--t-card));
  overflow: hidden;
}

/* 耗时条填充：耗时色淡化（仍偏浅，但与轨道拉开足够对比） */
.trace-card__barfill {
  display: block;
  height: 100%;
  border-radius: 3px;
  background: color-mix(in srgb, var(--d) 66%, var(--t-card));
}

.trace-card__durlabel {
  flex: none;
  min-width: 46px;
  text-align: right;
  font-family: var(--trace-mono);
  font-size: 12px;
  /* 以 ink 为主、耗时色为辅：既清晰可读又带快慢暗示 */
  color: color-mix(in srgb, var(--d) 32%, var(--t-text));
}

/* 元信息：逐行竖向展示（用户/IP/链路/会话），长值完整换行 */
.trace-card__meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-top: 2px;
  padding-top: 9px;
  border-top: 1px dashed v-bind('themeVars.dividerColor');
}

.trace-card__metarow {
  display: flex;
  gap: 8px;
  font-family: var(--trace-mono);
  font-size: 11px;
  line-height: 1.4;
}

.trace-card__metak {
  flex: none;
  min-width: 58px;
  color: v-bind('themeVars.textColor3');
}

.trace-card__metav {
  color: v-bind('themeVars.textColor2');
  word-break: break-all;
}

.trace-empty {
  padding: 48px 0;
}
</style>
