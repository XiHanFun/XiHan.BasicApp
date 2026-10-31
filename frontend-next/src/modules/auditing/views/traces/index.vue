<script setup lang="ts">
import type { LogDetailSchema } from '../../components/log-detail.types'
import type { Translate } from '../../components/log-options'
import type { TracePreset } from '../../components/trace-nav'
import type { TraceItem, TraceResult } from './trace-analysis'
import type { ListFieldSchema } from '~/components'
import type { LoginEvent, PermissionChangeType, ResponseOf, TraceDimension, TraceLogType } from '~/contracts'
import { XhButton, XhCardContent, XhCardRoot, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { SchemaSearchPanel } from '~/components'
import { toast } from '~/composables'
import { accessLogs, conditionHits, diffLogs, exceptionLogs, LoginEvent as LOGIN_EVENTS, loginLogs, openApiLogs, operationLogs, PermissionChangeType as PERMISSION_CHANGE_TYPES, permissionChangeLogs, TraceLogType as TRACE_LOG_TYPES, traces } from '~/contracts'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { formatDate } from '~/utils'
import {
  accessLogDetailSchema,
  conditionHitDetailSchema,
  diffLogDetailSchema,
  exceptionLogDetailSchema,
  loginLogDetailSchema,
  openApiLogDetailSchema,
  operationLogDetailSchema,
  permissionChangeLogDetailSchema,
} from '../../components/log-detail-fields'
import { loginEventLabel, permissionChangeLabel, traceDimensionOptions, traceLogTypeLabel, traceLogTypeOptions, traceStatusLabel } from '../../components/log-options'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { tracePreset } from '../../components/trace-nav'
import TraceAnalysisDialog from './components/TraceAnalysisDialog.vue'

defineOptions({ name: 'AuditingTracesView' })

const { t } = useI18n()

const DAY_MS = 24 * 60 * 60 * 1000

const ALL_LOG_TYPES: readonly TraceLogType[] = TRACE_LOG_TYPES

/** 通用搜索组件无高级区 */
const EMPTY_FIELDS: ListFieldSchema[] = []

function defaultRange(): [number, number] {
  const now = Date.now()
  return [now - DAY_MS, now]
}

// 搜索模型（供通用搜索组件 SchemaSearchPanel 双向绑定）
const filters = reactive<Record<string, unknown>>({
  dimension: 'TraceId' satisfies TraceDimension,
  value: '',
  logTypes: [...ALL_LOG_TYPES],
  timeRange: defaultRange(),
})

type TraceResponse = ResponseOf<'traces.timeline'>

// 结果状态
const loading = ref(false)
const hasQueried = ref(false)
const result = ref<TraceResponse | null>(null)

// 链路分析弹窗
const analysisVisible = ref(false)

// 详情抽屉
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<object | null>(null)
const detailLogType = ref<TraceLogType | null>(null)

// ── 选项 ─────────────────────────────────────────────────────────
const logTypeLabel = (type: TraceLogType) => traceLogTypeLabel(t, type)
const resultLabel = (status: TraceResult) => traceStatusLabel(t, status)

const valuePlaceholder = computed(() => {
  switch (filters.dimension as TraceDimension) {
    case 'TraceId': return t('auditing.traces.placeholders.trace_id')
    case 'SessionId': return t('auditing.traces.placeholders.session_id')
    case 'UserName': return t('auditing.traces.placeholders.user_name')
    case 'ClientIp': return t('auditing.traces.placeholders.client_ip')
    case 'UserId': return t('auditing.traces.placeholders.user_id')
    default: return t('auditing.traces.value')
  }
})

// 通用搜索组件字段（单一事实源）：维度 / 值 / 日志类型 / 时间范围
const searchFields = computed<ListFieldSchema[]>(() => [
  { key: 'dimension', title: t('auditing.traces.dimension'), dataType: 'enum', searchable: true, options: traceDimensionOptions(t), searchPlaceholder: t('auditing.traces.dimension') },
  { key: 'value', title: t('auditing.traces.value'), dataType: 'string', searchable: true, searchPlaceholder: valuePlaceholder.value },
  { key: 'logTypes', title: t('auditing.traces.log_types_label'), dataType: 'enum', searchable: true, searchMultiple: true, options: traceLogTypeOptions(t), searchPlaceholder: t('auditing.traces.log_types_label') },
  { key: 'timeRange', title: t('auditing.traces.time_range'), dataType: 'datetime', searchable: true, searchRange: true },
])

const items = computed(() => result.value?.items ?? [])

const emptyDescription = computed(() =>
  hasQueried.value ? t('auditing.traces.empty_result') : t('auditing.traces.empty_initial'))

const detailTitle = computed(() =>
  detailLogType.value
    ? `${logTypeLabel(detailLogType.value)} · ${t('auditing.traces.detail_title')}`
    : t('auditing.traces.detail_title'))

// ── 查询 ─────────────────────────────────────────────────────────
async function runQuery() {
  const dimension = filters.dimension as TraceDimension | null | undefined
  if (!dimension) {
    toast.warning(t('auditing.traces.dimension_required'))
    return
  }

  const value = String(filters.value ?? '').trim()
  if (!value) {
    toast.warning(t('auditing.traces.value_required'))
    return
  }

  const range = filters.timeRange as [number, number] | null
  if (!range) {
    toast.warning(t('auditing.traces.range_required'))
    return
  }

  const types = (filters.logTypes as TraceLogType[] | undefined) ?? []
  if (types.length === 0) {
    toast.warning(t('auditing.traces.type_required'))
    return
  }

  loading.value = true
  try {
    result.value = await traces.timeline({
      dimension,
      value,
      from: new Date(range[0]).toISOString(),
      to: new Date(range[1]).toISOString(),
      logTypes: types.length === ALL_LOG_TYPES.length ? null : types,
      maxPerType: 200,
    })
    hasQueried.value = true
  }
  catch (error) {
    toast.danger(error instanceof ApiError ? error.message : t('auditing.traces.query_failed'))
  }
  finally {
    loading.value = false
  }
}

function reset() {
  filters.dimension = 'TraceId' satisfies TraceDimension
  filters.value = ''
  filters.logTypes = [...ALL_LOG_TYPES]
  filters.timeRange = defaultRange()
  result.value = null
  hasQueried.value = false
}

// ── 展示辅助 ─────────────────────────────────────────────────────
type StatusKind = 'success' | 'error' | 'warning' | 'info'

/**
 * 状态归一：以后端归并的 item.status 为权威来源（访问结果、成败、是否拦下等已折算成同一口径）。
 * 不按原始 HTTP 码判色；HTTP 码仅用于状态标签文案（statusText）。
 */
function statusKind(item: TraceItem): StatusKind {
  switch (item.status) {
    case 'Failure': return 'error'
    case 'Warning': return 'warning'
    case 'Success': return 'success'
    default: return 'info'
  }
}

/** 请求方式配色：按 HTTP 动词着色；非 HTTP（日志类型标签）保持中性 */
function methodClass(item: TraceItem): string {
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
function logTypeClass(item: TraceItem): string {
  switch (item.logType) {
    case 'Login': return 't-success'
    case 'Exception': return 't-error'
    case 'Diff':
    case 'ConditionHit': return 't-warning'
    case 'Operation':
    case 'PermissionChange': return 't-primary'
    default: return 't-info'
  }
}

/** 状态文案：有正整数 HTTP 码显码，否则显结果文案 */
function statusText(item: TraceItem): string {
  const code = Number(item.statusCode)
  return Number.isFinite(code) && code > 0 ? String(code) : resultLabel(item.status)
}

/** 路径 / 标题（method + path 为主，没有路径显标题，如操作 ID、异常类型）；登录事件与权限变更类型是契约字面量，换成文案 */
function pathOf(item: TraceItem): string {
  if (item.path)
    return item.path
  if (item.logType === 'Login' && (LOGIN_EVENTS as readonly string[]).includes(item.title))
    return loginEventLabel(t, item.title as LoginEvent)
  if (item.logType === 'PermissionChange' && (PERMISSION_CHANGE_TYPES as readonly string[]).includes(item.title))
    return permissionChangeLabel(t, item.title as PermissionChangeType)
  return item.title || '-'
}

// formatDate 输出 "YYYY-MM-DD HH:MM:SS"，拆出时间 / 分钟
function timeText(value: string): string {
  const parts = formatDate(value).split(' ')
  return parts[1] ?? parts[0] ?? ''
}
function minuteKey(value: string): string {
  return formatDate(value).slice(0, 16)
}

/** 分钟分组：外层时间线按分钟连接，分钟内为卡片；datetime 为 <time> 的机读值 */
const groups = computed(() => {
  const out: { key: string, datetime: string, minute: string, items: TraceItem[] }[] = []
  let prev = ''
  for (const item of items.value) {
    const key = minuteKey(item.occurredAt)
    if (key !== prev) {
      out.push({ key, datetime: key.replace(' ', 'T'), minute: timeText(item.occurredAt).slice(0, 5), items: [] })
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
    const v = Number(item.durationMs)
    if (Number.isFinite(v) && v > max)
      max = v
  }
  return max
})

function hasDuration(item: TraceItem): boolean {
  const v = Number(item.durationMs)
  return item.durationMs != null && Number.isFinite(v) && v > 0
}
function barWidth(item: TraceItem): string {
  const v = Number(item.durationMs)
  const pct = Number.isFinite(v) ? (v / maxDuration.value) * 100 : 0
  return `${Math.max(6, Math.min(100, Math.round(pct)))}%`
}

/** 耗时分级（独立于状态配色）：<500ms 快 / <1500ms 一般 / 更久 慢 */
function durationLevel(item: TraceItem): 'fast' | 'mid' | 'slow' {
  const v = Number(item.durationMs)
  if (!Number.isFinite(v) || v < 500)
    return 'fast'
  if (v < 1500)
    return 'mid'
  return 'slow'
}

/** 元信息（机器值等宽，用户 / 地址 + 链路 / 会话次之） */
function metaParts(item: TraceItem): { key: string, label: string, value: string }[] {
  const parts: { key: string, label: string, value: string }[] = []
  if (item.userName)
    parts.push({ key: 'user', label: t('auditing.common.user_name'), value: item.userName })
  else if (item.userId != null)
    parts.push({ key: 'user', label: t('auditing.common.user_id'), value: item.userId })
  if (item.clientIp)
    parts.push({ key: 'ip', label: t('auditing.common.client_ip'), value: item.clientIp })
  if (item.traceId)
    parts.push({ key: 'trace', label: t('auditing.common.trace_id'), value: item.traceId })
  if (item.sessionId)
    parts.push({ key: 'session', label: t('auditing.common.session_id'), value: item.sessionId })
  return parts
}

/** 各类日志取到的条数（工具栏的小标签） */
const typeCounts = computed(() => result.value?.counts ?? [])

// ── 详情 ─────────────────────────────────────────────────────────
const detailFetchers: Record<TraceLogType, (id: string) => Promise<object>> = {
  Access: id => accessLogs.get({ id }),
  OpenApi: id => openApiLogs.get({ id }),
  Operation: id => operationLogs.get({ id }),
  Login: id => loginLogs.get({ id }),
  Exception: id => exceptionLogs.get({ id }),
  Diff: id => diffLogs.get({ id }),
  PermissionChange: id => permissionChangeLogs.get({ id }),
  ConditionHit: id => conditionHits.get({ id }),
}

async function openDetail(item: TraceItem) {
  detailLogType.value = item.logType
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = item
  try {
    detailData.value = await detailFetchers[item.logType](item.id)
  }
  catch (error) {
    toast.danger(error instanceof ApiError ? error.message : t('auditing.common.detail_load_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

// 详情定义：直接复用各日志页的抬头、分组、标签与枚举文案（单一事实源 log-detail-fields.ts），不在链路追踪里另建映射
const DETAIL_SCHEMA_BY_TYPE: Record<TraceLogType, (t: Translate) => LogDetailSchema> = {
  Access: accessLogDetailSchema,
  OpenApi: openApiLogDetailSchema,
  Operation: operationLogDetailSchema,
  Login: loginLogDetailSchema,
  Exception: exceptionLogDetailSchema,
  Diff: diffLogDetailSchema,
  PermissionChange: permissionChangeLogDetailSchema,
  ConditionHit: conditionHitDetailSchema,
}

const detailSchema = computed<LogDetailSchema>(() =>
  detailLogType.value != null ? DETAIL_SCHEMA_BY_TYPE[detailLogType.value](t) : { header: { title: [], meta: [] }, fields: [] })

// ── 深链预填（从各日志页「追踪」跳转，经共享预设复用同一标签页） ──
function applyPreset(preset: TracePreset) {
  if ((['TraceId', 'UserId', 'UserName', 'SessionId', 'ClientIp'] as const).includes(preset.dimension as TraceDimension))
    filters.dimension = preset.dimension
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
    <XhCardRoot variant="ghost">
      <XhCardContent>
        <SchemaSearchPanel
          :advanced-fields="EMPTY_FIELDS"
          :common-fields="searchFields"
          :model="filters"
          @reset="reset"
          @search="runQuery"
        />
      </XhCardContent>
    </XhCardRoot>

    <XhCardRoot class="flex-1" style="height: 0">
      <XhCardContent style="height: 100%; display: flex; flex-direction: column; min-height: 0; padding: 0">
        <div class="xh-loading-stage trace-stage" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <div v-if="result" class="trace-panel trace-scroll">
            <div class="trace-panel__header">
              <div class="trace-panel__titlerow">
                <span class="trace-panel__heading">
                  <span class="trace-panel__title">{{ t('auditing.traces.title') }}</span>
                  <span class="trace-panel__count">{{ t('auditing.traces.summary_total', { total: result.total }) }}</span>
                </span>
                <span class="trace-panel__grow" />
                <span class="trace-panel__tools">
                  <XhButton
                    size="sm"
                    variant="subtle"
                    tone="brand"
                    :disabled="items.length === 0"
                    @click="analysisVisible = true"
                  >
                    <Icon icon="lucide:chart-network" width="14" height="14" />
                    {{ t('auditing.traces.analysis_action') }}
                  </XhButton>
                  <span
                    v-for="count in typeCounts"
                    :key="count.logType"
                    class="trace-chip"
                    :class="{ 'is-error': count.logType === 'Exception' }"
                  >
                    {{ logTypeLabel(count.logType) }}<i>·</i><b>{{ count.count }}</b>
                  </span>
                </span>
              </div>
              <div v-if="result.truncated" class="trace-panel__warn">
                <span class="trace-panel__warndot" />
                {{ t('auditing.traces.truncated') }}
              </div>
            </div>

            <XhTimelineRoot v-if="groups.length" class="trace-panel__list">
              <XhTimelineItem v-for="grp in groups" :key="grp.key" tone="brand">
                <XhTimelineIndicator />
                <XhTimelineConnector />
                <XhTimelineContent>
                  <XhTimelineTitle class="trace-grp__minute">
                    <XhTimelineTime :datetime="grp.datetime">
                      {{ grp.minute }}
                    </XhTimelineTime>
                    <span class="trace-grp__count">{{ grp.items.length }}</span>
                  </XhTimelineTitle>
                  <div class="trace-grp__cards">
                    <div
                      v-for="item in grp.items"
                      :key="`${item.logType}-${item.id}`"
                      class="trace-card"
                      :class="`is-${statusKind(item)}`"
                      @click="openDetail(item)"
                    >
                      <div class="trace-card__head">
                        <span class="trace-card__time">{{ timeText(item.occurredAt) }}</span>
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
                        <span class="trace-card__durlabel">{{ item.durationMs }}ms</span>
                      </div>
                      <div v-if="metaParts(item).length" class="trace-card__meta">
                        <div v-for="p in metaParts(item)" :key="p.key" class="trace-card__metarow">
                          <span class="trace-card__metak">{{ p.label }}</span>
                          <span class="trace-card__metav">{{ p.value }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </XhTimelineContent>
              </XhTimelineItem>
            </XhTimelineRoot>

            <XhEmptyStateRoot v-else class="trace-empty">
              <XhEmptyStateIndicator>
                <Icon height="28" icon="lucide:search-x" width="28" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ emptyDescription }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
          </div>

          <XhEmptyStateRoot v-else class="trace-empty">
            <XhEmptyStateIndicator>
              <Icon height="28" icon="lucide:search-x" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ emptyDescription }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>
      </XhCardContent>
    </XhCardRoot>

    <TraceAnalysisDialog
      v-model:show="analysisVisible"
      :items="items"
      :truncated="result?.truncated ?? false"
      :log-type-label="logTypeLabel"
      :result-label="resultLabel"
    />

    <LogDetailDrawer
      v-model:show="detailVisible"
      :schema="detailSchema"
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

/* 加载罩层所在的这一层要吃掉卡片体的剩余高度，下面的结果面板才有确定高度可滚；
   罩层是它的绝对定位子元素，留在这一层就始终盖住整块结果区，不随内容滚走 */
.trace-stage {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
}

/* 结果区内部滚动：上面那层已给出确定高度，故 flex-1 + min-height:0 得到可滚高度，
   滚动只发生在时间线内部，页头的 sticky 也据此吸顶 */
.trace-scroll {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

/* ── 时间线轨道（内嵌于外层卡片，等宽机器值；卡片按状态浅色着色） ── */
.trace-panel {
  --trace-mono: var(--xh-font-family-mono);
  /* 状态色 / 耗时色 / 请求方式色 + 底色（卡片用 color-mix 调浅色） */
  --t-card: var(--xh-bg-surface);
  --t-action: var(--xh-bg-subtle);
  --t-border: var(--xh-border-default);
  --t-succ: var(--xh-color-success-600);
  --t-err: var(--xh-color-danger-600);
  --t-warn: var(--xh-color-warning-600);
  --t-info: var(--xh-color-info-600);
  --t-primary: var(--xh-color-brand-600);
  --t-text: var(--xh-fg-default);
}

/* 头部吸顶：随内容滚动固定在结果卡顶部 */
.trace-panel__header {
  position: sticky;
  top: 0;
  z-index: 1;
  padding: 14px 20px 12px;
  background: var(--xh-bg-surface);
  border-bottom: 1px solid var(--xh-border-subtle);
}

/* 标题行两组各自对齐：标题与条数按文字基线，按钮与类型统计按中线（按钮比统计标签高） */
.trace-panel__titlerow {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 10px;
}

.trace-panel__heading {
  display: inline-flex;
  align-items: baseline;
  gap: var(--xh-space-2);
}

.trace-panel__tools {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.trace-panel__title {
  font-size: 15px;
  font-weight: 600;
  letter-spacing: -0.01em;
  color: var(--xh-fg-default);
}

.trace-panel__count {
  font-size: 12px;
  color: var(--xh-fg-subtle);
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
  background: var(--xh-bg-subtle);
  color: var(--xh-fg-subtle);
  font-family: var(--trace-mono);
  font-size: 11px;
}

.trace-chip i {
  font-style: normal;
  opacity: 0.5;
}

.trace-chip b {
  font-weight: 500;
  color: var(--xh-fg-muted);
}

.trace-chip.is-error,
.trace-chip.is-error b {
  color: var(--xh-color-danger-600);
}

.trace-panel__warn {
  display: flex;
  align-items: center;
  gap: 7px;
  margin-top: 9px;
  font-size: 11px;
  color: var(--xh-color-warning-600);
}

.trace-panel__warndot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: var(--xh-color-warning-600);
}

/* ── 外层时间线（组件库 timeline）：圆点、连线、间距由皮肤负责，这里只改令牌 ── */
.trace-panel__list {
  --xh-timeline-connector-bg: var(--xh-border-subtle);
  --xh-timeline-title-font-size: 12px;
  --xh-timeline-time-font-size: 12px;
  --xh-timeline-time-fg: var(--xh-fg-default);

  padding: 12px 20px 16px;
}

/* 分钟标题行：时刻 + 该分钟内条数 */
.trace-grp__minute {
  display: flex;
  align-items: center;
  gap: 9px;
  font-family: var(--trace-mono);
  font-weight: 600;
  letter-spacing: 0.02em;
}

.trace-grp__count {
  padding: 1px 6px;
  border-radius: 8px;
  background: var(--xh-bg-subtle);
  color: var(--xh-fg-subtle);
  font-family: var(--trace-mono);
  font-size: 10.5px;
  font-weight: 400;
  letter-spacing: normal;
  line-height: 1.5;
}

/* ── 分钟内：卡片横向排列（自适应换行），信息竖向堆叠完整展示 ── */
.trace-grp__cards {
  margin-top: 4px;
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
    border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    box-shadow var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
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
  box-shadow: var(--xh-elevation-lifted);
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
  border: 1px solid var(--xh-border-default);
  background: var(--xh-bg-subtle);
  color: var(--xh-fg-subtle);
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
  color: var(--xh-fg-subtle);
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
  color: var(--xh-fg-default);
  word-break: break-all;
}

.trace-card__summary {
  font-size: 12px;
  line-height: 1.45;
  color: var(--xh-fg-muted);
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
  border-top: 1px dashed var(--xh-border-subtle);
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
  color: var(--xh-fg-subtle);
}

.trace-card__metav {
  color: var(--xh-fg-muted);
  word-break: break-all;
}

.trace-empty {
  padding: 48px 0;
}
</style>
