import dayjs from 'dayjs'
import timezone from 'dayjs/plugin/timezone'
import utc from 'dayjs/plugin/utc'
import { ref } from 'vue'

dayjs.extend(utc)
dayjs.extend(timezone)

/** 只有日历日（`YYYY-MM-DD`）：没有时刻也没有时区，显示时不换算 */
const CALENDAR_DATE = /^\d{4}-\d{2}-\d{2}$/

/** 是否是运行时认得的 IANA 时区名 */
export function isTimeZone(zone: string): boolean {
  if (!zone)
    return false
  try {
    // eslint-disable-next-line no-new
    new Intl.DateTimeFormat('en-US', { timeZone: zone })
    return true
  }
  catch {
    return false
  }
}

/** 浏览器所在的时区（IANA 名） */
export function browserTimeZone(): string {
  return new Intl.DateTimeFormat().resolvedOptions().timeZone
}

/**
 * 显示时间用的时区（IANA 名）
 *
 * 由偏好设置的应用时区写入（stores 的 appTimezone），写入之前是浏览器的时区。
 * formatDate 在渲染时读它，模板随时区切换重新渲染。
 */
const displayTimeZone = ref(browserTimeZone())

/** 显示时区的当前值；按时区算「今天」的请求（统计、看板）与显示用同一个时区 */
export function getDisplayTimeZone(): string {
  return displayTimeZone.value
}

/** 换显示时区；空串表示跟随浏览器，认不出的时区名抛出 RangeError */
export function setDisplayTimeZone(zone: string): void {
  if (zone === '') {
    displayTimeZone.value = browserTimeZone()
    return
  }
  if (!isTimeZone(zone))
    throw new RangeError(`Unknown time zone: ${zone}`)
  displayTimeZone.value = zone
}

/** 某个时区在某一时刻相对 UTC 的分钟数（东八区为 480，夏令时按那一刻算） */
export function utcOffsetMinutes(zone: string, at: Date = new Date()): number {
  return dayjs(at).tz(zone).utcOffset()
}

/**
 * 按指定时区格式化时间
 *
 * 后端的时间一律是带 `Z` 或偏移的 ISO 串（时刻），按 dayjs utc + timezone 换到 timeZone 再格式化，
 * 与浏览器所在的时区无关。只有日历日的串原样格式化（换算会把零点挪到前一天）。
 * 判空只认「没有值」（null / undefined / 空串）：数字 0 是 Unix 纪元，照常格式化；
 * NaN 与解析不出的串归到占位符，不把 'Invalid Date' 渲染到界面上。
 */
export function formatDateTime(
  value: string | Date | number | null | undefined,
  timeZone: string,
  format = 'YYYY-MM-DD HH:mm:ss',
): string {
  if (value === null || value === undefined || value === '')
    return '-'
  if (typeof value === 'string' && CALENDAR_DATE.test(value))
    return dayjs(value).format(format)
  const parsed = dayjs(value)
  if (!parsed.isValid())
    return '-'
  return parsed.tz(timeZone).format(format)
}

/** 按显示时区（偏好设置的应用时区）格式化时间，见 formatDateTime */
export function formatDate(
  date: string | Date | number | null | undefined,
  format = 'YYYY-MM-DD HH:mm:ss',
): string {
  return formatDateTime(date, displayTimeZone.value, format)
}

/**
 * 格式化文件大小
 *
 * 非有限数与负数返回 '-'，与 formatDate / getOptionLabel 的占位符口径一致：
 * 后端字段缺失被 Number() 化成 NaN 时，原实现会把 'NaN undefined' 直接渲染到文件列表与配额统计上。
 * 下标两侧都要夹：0~1 字节的小数得到 -1、≥1PB 得到 5，越界那一侧 sizes[i] 是 undefined，
 * 输出成 '512 undefined' / '1 undefined'。
 */
export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0)
    return '-'
  if (bytes === 0)
    return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(Math.max(Math.floor(Math.log(bytes) / Math.log(k)), 0), sizes.length - 1)
  return `${Number.parseFloat((bytes / k ** i).toFixed(2))} ${sizes[i]}`
}

/**
 * 防抖函数
 */
export function debounce<T extends (...args: never[]) => unknown>(fn: T, delay = 300): T {
  let timer: ReturnType<typeof setTimeout> | null = null
  return ((...args: Parameters<T>) => {
    if (timer)
      clearTimeout(timer)
    // 传参形式 setTimeout(fn, delay, ...args) 命中 DOM 重载、返回 number，与 timer 的 Timeout 类型不符
    // eslint-disable-next-line e18e/prefer-timer-args
    timer = setTimeout(() => fn(...args), delay)
  }) as T
}

/**
 * 节流函数
 */
export function throttle<T extends (...args: never[]) => unknown>(fn: T, delay = 300): T {
  let lastTime = 0
  return ((...args: Parameters<T>) => {
    const now = Date.now()
    if (now - lastTime >= delay) {
      lastTime = now
      return fn(...args)
    }
  }) as T
}

/**
 * 深拷贝
 */
export function deepClone<T>(obj: T): T {
  if (obj === null || typeof obj !== 'object')
    return obj
  if (obj instanceof Date)
    return new Date(obj.getTime()) as unknown as T
  if (Array.isArray(obj))
    return obj.map(item => deepClone(item)) as unknown as T
  const cloned = {} as T
  for (const key in obj) {
    if (Object.hasOwn(obj, key)) {
      cloned[key] = deepClone(obj[key])
    }
  }
  return cloned
}

/**
 * 判断是否为空值
 *
 * Date、Map、Set 都没有自有可枚举键，落到最后的 Object.keys 分支会一律被判为空——
 * 表单 / 查询条件用本函数裁参数时，用户选中的日期与 Map/Set 型集合会被静默丢掉，请求少带条件。
 * 因此在通用对象分支之前按各自的「有没有内容」口径短路：
 * 日期只有 Invalid Date（没选出有效时间）才算空，集合看 size。
 */
export function isEmpty(value: unknown): boolean {
  if (value === null || value === undefined)
    return true
  if (typeof value === 'string')
    return value.trim() === ''
  if (Array.isArray(value))
    return value.length === 0
  if (value instanceof Date)
    return Number.isNaN(value.getTime())
  if (value instanceof Map || value instanceof Set)
    return value.size === 0
  if (typeof value === 'object')
    return Object.keys(value).length === 0
  return false
}

/**
 * 生成随机字符串
 */
export function randomString(length = 8): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
  return Array.from({ length }, () => chars.charAt(Math.floor(Math.random() * chars.length))).join(
    '',
  )
}

/**
 * URL参数解析
 */
export function parseQuery(search: string): Record<string, string> {
  const params = new URLSearchParams(search)
  const result: Record<string, string> = {}
  params.forEach((value, key) => {
    result[key] = value
  })
  return result
}

/**
 * 复制到剪贴板
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text)
    return true
  }
  catch {
    const el = document.createElement('textarea')
    el.value = text
    document.body.appendChild(el)
    el.select()
    const success = document.execCommand('copy')
    document.body.removeChild(el)
    return success
  }
}

/**
 * 获取状态对应的语气档
 */
export function getStatusType(
  status: number,
): 'success' | 'warning' | 'danger' | 'info' | 'neutral' {
  const map: Record<number, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
    1: 'success',
    0: 'danger',
    2: 'warning',
  }
  return map[status] ?? 'neutral'
}

/**
 * 根据选项数组获取标签
 */
export function getOptionLabel(
  options: Array<{ label: string, value: number | string }>,
  value: number | string | null | undefined,
  fallback = '-',
) {
  const matched = options.find(item => item.value === value)
  return matched?.label ?? fallback
}
