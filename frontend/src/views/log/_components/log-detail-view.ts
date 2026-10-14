/**
 * 日志详情的视图模型（纯函数，抽屉与测试共用）：把一份详情定义与一条记录排成抬头、分组与代码块。
 * 空值一律不出现；抬头用到的字段不在分组里重复。
 */
import type { Tone } from '@xihan-ui/core'
import type { LogDetailField, LogDetailGroup, LogDetailSchema } from './log-detail.types'

/** 分组的排版顺序 */
export const LOG_DETAIL_GROUP_ORDER: readonly LogDetailGroup[] = ['subject', 'request', 'client', 'runtime', 'handling', 'trace']

export interface LogDetailEntry {
  field: LogDetailField
  /** 原始值（复制、追踪用） */
  raw: string
  /** 展示文本（枚举标签、格式化后的时间等） */
  text: string
}

export interface LogDetailView {
  title: string
  status?: LogDetailEntry & { tone: Tone }
  badges: LogDetailEntry[]
  meta: LogDetailEntry[]
  groups: Array<{ group: LogDetailGroup, entries: LogDetailEntry[] }>
  blocks: LogDetailEntry[]
}

function isEmptyValue(value: unknown) {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '')
}

/**
 * 排出详情视图
 * @param schema 详情定义
 * @param record 日志记录
 * @param format 把字段值格式化成展示文本（枚举标签、时间、耗时等）
 */
export function buildLogDetailView(schema: LogDetailSchema, record: object, format: (field: LogDetailField, value: unknown) => string): LogDetailView {
  const values = record as Record<string, unknown>
  const fieldMap = new Map(schema.fields.map(field => [field.key, field]))

  function entryOf(key: string): LogDetailEntry | null {
    const field = fieldMap.get(key)
    const value = values[key]
    if (!field || isEmptyValue(value))
      return null
    return { field, raw: typeof value === 'object' ? JSON.stringify(value) : String(value), text: format(field, value) }
  }

  const pick = (keys: readonly string[] = []) => keys.map(entryOf).filter((entry): entry is LogDetailEntry => entry !== null)

  const { header } = schema
  const headerKeys = new Set([...header.title, ...(header.status ? [header.status] : []), ...(header.badges ?? []), ...header.meta])

  const statusEntry = header.status ? entryOf(header.status) : null
  const status = statusEntry?.field.tone
    ? { ...statusEntry, tone: statusEntry.field.tone(values[header.status!]) }
    : undefined

  const groups = LOG_DETAIL_GROUP_ORDER
    .map(group => ({
      group,
      entries: schema.fields
        .filter(field => field.type !== 'code' && (field.group ?? 'subject') === group && !headerKeys.has(field.key))
        .map(field => entryOf(field.key))
        .filter((entry): entry is LogDetailEntry => entry !== null),
    }))
    .filter(group => group.entries.length > 0)

  return {
    title: pick(header.title).map(entry => entry.text).join(' · '),
    status: status ?? undefined,
    // 没有语气的结果字段退回成普通标签
    badges: [...(statusEntry && !status ? [statusEntry] : []), ...pick(header.badges)],
    meta: pick(header.meta),
    groups,
    blocks: schema.fields
      .filter(field => field.type === 'code')
      .map(field => entryOf(field.key))
      .filter((entry): entry is LogDetailEntry => entry !== null),
  }
}
