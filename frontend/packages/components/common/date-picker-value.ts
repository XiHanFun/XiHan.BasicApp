import type { DateSegmentType } from '@xihan-ui/headless'

/**
 * 日期选择封装的值换算与区间草稿。
 *
 * 组件库收发不带时区的 ISO 串（日期档 `YYYY-MM-DD`，带时刻档 `YYYY-MM-DDTHH:mm`），
 * 本应用上下游一律用时间戳（毫秒）。两边都按本地时区读写：toISOString 走 UTC，
 * 东八区零点会被挪到前一天，所以这里一律用本地分量拼、按本地分量解。
 */

function pad(part: number): string {
  return String(part).padStart(2, '0')
}

/** 时间戳 → ISO 串。withTime 为真时带上时:分（组件库 showTime 的缺省精度），否则只取日历日 */
export function timestampToIso(timestamp: number, withTime: boolean): string {
  const date = new Date(timestamp)
  const day = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
  return withTime ? `${day}T${pad(date.getHours())}:${pad(date.getMinutes())}` : day
}

const ISO_PATTERN = /^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2})(?::(\d{2}))?)?$/

/** ISO 串 → 本地时区的时间戳：只有日期段时取当日零点；空串或写坏的串按无值 */
export function isoToTimestamp(iso: string): number | null {
  const match = ISO_PATTERN.exec(iso)
  if (!match) {
    return null
  }
  const [, year, month, day, hour = '0', minute = '0', second = '0'] = match
  const date = new Date(Number(year), Number(month) - 1, Number(day), Number(hour), Number(minute), Number(second))
  return Number.isNaN(date.getTime()) ? null : date.getTime()
}

/** 段位前的字面分隔：日期段之间 `-`，日期与时刻之间、时刻与上下午之间留空格，时刻段之间 `:` */
export function segmentLiteralBefore(type: DateSegmentType): string {
  if (type === 'hour' || type === 'dayPeriod') {
    return ' '
  }
  if (type === 'minute' || type === 'second') {
    return ':'
  }
  return '-'
}

/** 区间受控值 → 交给组件库的两端草稿；无值是空数组。withTime 为真时两端带上时:分 */
export function rangeToDraft(value: readonly [number, number] | null | undefined, withTime = false): string[] {
  return value == null ? [] : value.map(ts => timestampToIso(ts, withTime))
}

/**
 * 组件库发来的区间两端 → 该往上抛什么。
 *
 * 组件库按位存放：只填了起点是 `[起]` 或 `[起, '']`，只填了终点是 `['', 止]`。
 * 两端齐备上抛 [起, 止]；两端皆空（清空）上抛 null；只有一端时返回 undefined——
 * 先不上抛，调用方拿到的区间要么两端齐备、要么为空，那一半留在封装的草稿里继续填。
 */
export function resolveRangeChange(next: readonly string[]): [number, number] | null | undefined {
  const start = isoToTimestamp(next[0] ?? '')
  const end = isoToTimestamp(next[1] ?? '')
  if (start != null && end != null) {
    return [start, end]
  }
  if (start == null && end == null) {
    return null
  }
  return undefined
}
