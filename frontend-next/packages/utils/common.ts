import dayjs from 'dayjs'

/**
 * 格式化日期时间
 *
 * 判空只认「没有值」（null / undefined / 空串）：原来的 `!date` 会把数字 0 一并吞掉，
 * 以时间戳传参时 Unix 纪元时刻格式化不出来。NaN 不是有效时间，仍归到占位符，
 * 否则会把 'Invalid Date' 渲染到界面上。
 */
export function formatDate(
  date: string | Date | number | null | undefined,
  format = 'YYYY-MM-DD HH:mm:ss',
): string {
  if (date === null || date === undefined || date === '')
    return '-'
  if (typeof date === 'number' && Number.isNaN(date))
    return '-'
  return dayjs(date).format(format)
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
