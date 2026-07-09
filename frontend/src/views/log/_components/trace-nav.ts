import type { Router } from 'vue-router'
import type { ListFieldSchema } from '~/components'
import { NButton } from 'naive-ui'
import { h, ref } from 'vue'

const DAY_MS = 24 * 60 * 60 * 1000
const FALLBACK_WINDOW_MS = 7 * DAY_MS

/** 链路追踪深链预设（跨标签共享，避免 query 造成重复标签页） */
export interface TracePreset {
  dimension: string
  end: number
  start: number
  value: string
}

/** 待消费的追踪预设；由日志页发起、追踪页消费（每次赋新对象以触发 watch） */
export const tracePreset = ref<TracePreset | null>(null)

/** 列键 → 追踪维度（与后端 TraceDimension 序列化值一致） */
const DIMENSION_BY_KEY: Record<string, string> = {
  userName: 'UserName',
  sessionId: 'SessionId',
  traceId: 'TraceId',
}

/** 可发起链路追踪的行（各日志列表项的公共维度字段） */
export interface TraceNavRow {
  sessionId?: string | null
  traceId?: string | null
  userName?: string | null
}

export interface TraceFieldOptions {
  /** 额外的「列键 → 追踪维度」映射（页面特有列，如权限变更的 operatorUserName → UserName） */
  extraDimensions?: Record<string, string>
  /** IP 列的字段键（各日志列名不同：accessIp/requestIp/operationIp/loginIp） */
  ipKey?: string
  /** 行时间字段名，用于把追踪时间窗口居中到该行 ±1 天 */
  timeField: string
}

/**
 * 按指定维度深链到链路追踪页。
 * 时间窗口以给定时间为中心 ±1 天（无时间则回退近 7 天）。
 * @returns 值为空时返回 false，不跳转
 */
export function gotoTraceDimension(router: Router, dimension: string, value: string | number | null | undefined, time?: string | null): boolean {
  if (value == null || value === '')
    return false

  const at = time ? new Date(time).getTime() : Number.NaN
  const hasAt = Number.isFinite(at)
  const now = Date.now()
  const start = hasAt ? at - DAY_MS : now - FALLBACK_WINDOW_MS
  const end = hasAt ? at + DAY_MS : now

  // 经共享预设传参并跳转到无 query 的固定路径，保证链路追踪始终复用同一标签页
  tracePreset.value = { dimension, value: String(value), start, end }
  void router.push('/log/trace')
  return true
}

/**
 * 从某条日志行深链到链路追踪页（维度优先级：TraceId → 会话标识 → 用户名）。
 * @returns 三个维度值都为空时返回 false
 */
export function gotoTrace(router: Router, row: TraceNavRow, time?: string | null): boolean {
  const value = row.traceId || row.sessionId || row.userName
  if (!value)
    return false

  const dimension = row.traceId ? 'TraceId' : row.sessionId ? 'SessionId' : 'UserName'
  return gotoTraceDimension(router, dimension, value, time)
}

/**
 * 给日志列表的关键维度列（用户名/会话标识/TraceId/IP）注入「点击跳转链路追踪」链接渲染。
 * 已带自定义 render 的列保持不变。
 */
export function decorateTraceFields(fields: ListFieldSchema[], router: Router, options: TraceFieldOptions): ListFieldSchema[] {
  return fields.map((field) => {
    const dimension = field.key === options.ipKey
      ? 'Ip'
      : options.extraDimensions?.[field.key] ?? DIMENSION_BY_KEY[field.key]
    if (!dimension || field.render)
      return field

    return {
      ...field,
      render: (row: Record<string, unknown>) =>
        traceLink(router, dimension, row[field.key], row[options.timeField] as string | null | undefined),
    }
  })
}

/** 渲染一个跳转链路追踪的链接单元格 */
function traceLink(router: Router, dimension: string, value: unknown, time: string | null | undefined) {
  if (value == null || value === '')
    return '-'

  return h(
    NButton,
    {
      text: true,
      type: 'primary',
      onClick: () => gotoTraceDimension(router, dimension, value as string, time),
    },
    () => String(value),
  )
}
