import type { NumberFormatSpec } from '@xihan-ui/headless'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

/** 图表 format 用的次数规格：紧凑记数（zh 得「1.2万」，en 得「12K」） */
export const COUNT_COMPACT: NumberFormatSpec = { notation: 'compact' }

/** 图表 format 用的毫秒规格 */
export const MILLISECONDS: NumberFormatSpec = { style: 'unit', unit: 'millisecond', precision: { type: 'fixed', digits: 0 } }

/** 字节数换成合适的单位：图表的值按它缩放，format 用它的单位 */
const BYTE_UNITS = [
  { unit: 'gigabyte', size: 1024 ** 3 },
  { unit: 'megabyte', size: 1024 ** 2 },
  { unit: 'kilobyte', size: 1024 },
  { unit: 'byte', size: 1 },
] as const

/** 按最大的值挑一个字节单位：返回缩放倍数与 format 规格 */
export function byteScale(max: number): { divisor: number, format: NumberFormatSpec } {
  const picked = BYTE_UNITS.find(entry => max >= entry.size) ?? BYTE_UNITS[BYTE_UNITS.length - 1]!
  return { divisor: picked.size, format: { style: 'unit', unit: picked.unit } }
}

/** 区间切换的选项：「n 天」 */
export function useRangeOptions(days: readonly number[]) {
  const { t } = useI18n()
  return computed(() => days.map(n => ({ value: String(n), label: t('workbench.charts.days', { n }) })))
}

/** 卡片与说明文字里直接用的数字格式，跟随当前语言 */
export function useFormatters() {
  const { locale } = useI18n()
  return computed(() => {
    const number = new Intl.NumberFormat(locale.value).format
    const unit = (value: number, name: string) => new Intl.NumberFormat(locale.value, { style: 'unit', unit: name, maximumFractionDigits: 1 }).format(value)
    return {
      number,
      /** 紧凑记数 */
      compact: new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format,
      /** 0–1 的比例写成百分数 */
      percent: new Intl.NumberFormat(locale.value, { style: 'percent', maximumFractionDigits: 1 }).format,
      /** 毫秒 */
      milliseconds: (value: number) => unit(Math.round(value), 'millisecond'),
      /** 字节数按大小换单位 */
      bytes: (value: number) => {
        const { divisor, format } = byteScale(value)
        return unit(value / divisor, String(format.unit))
      },
    }
  })
}

/** 周一到周日的短名，按当前语言（2024-01-01 是周一，只借它取星期名） */
export function useWeekdayNames() {
  const { locale } = useI18n()
  return computed(() => {
    const format = new Intl.DateTimeFormat(locale.value, { weekday: 'short', timeZone: 'UTC' })
    return Array.from({ length: 7 }, (_, index) => format.format(new Date(Date.UTC(2024, 0, 1 + index))))
  })
}

/** 本地日（yyyy-MM-dd）转成图表横轴用的日期（按本地零点） */
export function localDay(day: string): Date {
  const [year, month, date] = day.split('-').map(Number)
  return new Date(year!, month! - 1, date!)
}
