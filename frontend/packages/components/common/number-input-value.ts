/**
 * 数字输入的值换算：组件库那侧收发原始输入串，业务上下游一律是 `number | null`。
 *
 * 固定小数位（precision）收在这里：上抛的值按位数回舍，显示串按位数补齐，
 * 步进、失焦规范化与外部写值三处显示一致，整数字段不会提交出 1.5。
 */

/** 小数位数上限：双精度在 20 位以后已无意义，toFixed 也只认到这里以内的整数 */
const MAX_PRECISION = 20

/** precision 只收 0–20 的整数；写错是调用方的配置错误，直接报出来，不夹取 */
export function assertPrecision(precision: number | undefined): void {
  if (precision === undefined)
    return
  if (!Number.isInteger(precision) || precision < 0 || precision > MAX_PRECISION)
    throw new RangeError(`[XNumberInput] precision 必须是 0–${MAX_PRECISION} 的整数，收到 ${precision}`)
}

/** 按小数位数回舍；不给位数时原样。-0 一律收成 0，免得模型里出现带符号的零 */
export function roundToPrecision(value: number, precision?: number): number {
  const rounded = precision === undefined ? value : Number(value.toFixed(precision))
  return rounded === 0 ? 0 : rounded
}

/** 数 → 显示串：给了位数时补齐到固定小数位（12.5 → 12.50） */
export function formatNumber(value: number, precision?: number): string {
  return precision === undefined ? String(value) : value.toFixed(precision)
}

/** 显示串 → 数：与组件库缺省同口径，严格 Number()，空串与纯空白为 NaN */
export function parseNumber(text: string): number {
  const trimmed = text.trim()
  return trimmed === '' ? Number.NaN : Number(trimmed)
}

/** 不写 step 时的缺省步长：给了位数就按最小一位走（2 位 → 0.01，0 位 → 1） */
export function defaultStep(precision?: number): number | undefined {
  return precision === undefined ? undefined : Number(`1e-${precision}`)
}

/**
 * 原始输入串 → 上抛的值。
 * 空串为 null；'-'、'1e' 这类还不成数的中间态为 undefined，表示这一次不上抛。
 */
export function toModelValue(text: string, precision?: number): number | null | undefined {
  if (text.trim() === '')
    return null
  const parsed = parseNumber(text)
  return Number.isFinite(parsed) ? roundToPrecision(parsed, precision) : undefined
}

/** 用户正在打的那一串，以及它对应的模型值 */
export interface NumberDraft {
  text: string
  value: number | null
}

/**
 * 框里该显示的串。
 *
 * 草稿仍对应当前值时沿用草稿：'-0'、'1.'、'1.50' 这类输入中途的写法按值格式化会被改掉，
 * 光标也跟着跳；值被外部改成别的数时草稿作废，按值格式化。
 */
export function resolveDisplayText(value: number | null, draft: NumberDraft | null, precision?: number): string {
  if (draft && draft.value === value)
    return draft.text
  return value == null ? '' : formatNumber(value, precision)
}
