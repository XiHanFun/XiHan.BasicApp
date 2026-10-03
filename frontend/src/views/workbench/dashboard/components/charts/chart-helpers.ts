import type { NumberFormatSpec } from '@xihan-ui/headless'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

/** 图表 format 用的金额规格：人民币紧凑记数（zh 得「1.2万」，en 得「¥12K」） */
export const CURRENCY_COMPACT: NumberFormatSpec = { style: 'currency', currency: 'CNY', notation: 'compact' }

/** 区间切换的选项：「n 天」 */
export function useRangeOptions(days: readonly number[]) {
  const { t } = useI18n()
  return computed(() => days.map(n => ({ value: String(n), label: t('workbench.charts.days', { n }) })))
}

/** 卡片与说明文字里直接用的数字格式，跟随当前语言 */
export function useFormatters() {
  const { locale } = useI18n()
  return computed(() => ({
    /** 金额，紧凑记数 */
    currency: new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'CNY', notation: 'compact', maximumFractionDigits: 1 }).format,
    /** 金额，完整到元 */
    currencyFull: new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'CNY', maximumFractionDigits: 0 }).format,
    number: new Intl.NumberFormat(locale.value).format,
    /** 0–1 的比例写成百分数 */
    percent: new Intl.NumberFormat(locale.value, { style: 'percent', maximumFractionDigits: 1 }).format,
  }))
}

/** 示例数据里的键 → 当前语言的显示名（workbench.charts.<分组>.<键>） */
export function useDemoLabels() {
  const { t } = useI18n()
  const group = (name: string) => (key: string) => t(`workbench.charts.${name}.${key}`)
  return {
    channel: group('channel'),
    payment: group('payment'),
    region: group('region'),
    tier: group('tier'),
    stage: group('stage'),
    store: group('store'),
    score: group('score'),
    category: group('category'),
    item: group('item'),
    fulfillment: group('fulfillment'),
    step: group('step'),
  }
}

/** 周一到周日的短名，按当前语言（2024-01-01 是周一，只借它取星期名） */
export function useWeekdayNames() {
  const { locale } = useI18n()
  return computed(() => {
    const format = new Intl.DateTimeFormat(locale.value, { weekday: 'short', timeZone: 'UTC' })
    return Array.from({ length: 7 }, (_, index) => format.format(new Date(Date.UTC(2024, 0, 1 + index))))
  })
}
