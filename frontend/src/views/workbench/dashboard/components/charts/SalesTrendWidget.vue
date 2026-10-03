<script setup lang="ts">
import type { CartesianAnnotation, CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { CURRENCY_COMPACT, useDemoLabels, useRangeOptions } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { dailySales, isMemberDay } from './demo-data'

/**
 * 销售趋势：逐日销售额按线上（官网、小程序、直播）、门店、分销三段堆叠，阶梯线是按周定的销售目标。
 * 会员日画成参考带，线上销售额另画 7 日均线与峰值点；横轴可拖动缩放，看长区间时拉近某一段。
 */
defineOptions({ name: 'SalesTrendWidget' })

const MOVING_AVERAGE_DAYS = 7

const { t, locale } = useI18n()
const label = useDemoLabels()

const range = ref('30')
const rangeOptions = useRangeOptions([30, 90])

const sales = computed(() => dailySales(Number(range.value), new Date()))

const rows = computed(() => sales.value.map(row => ({
  date: row.date,
  online: row.channels.web + row.channels.mini + row.channels.live,
  store: row.channels.store,
  distribution: row.channels.distribution,
  target: row.target,
})))

const series = computed<CartesianSeries[]>(() => [
  { id: 'online', mark: 'bar', x: 'date', y: 'online', stack: 'sales', name: t('workbench.charts.online') },
  { id: 'store', mark: 'bar', x: 'date', y: 'store', stack: 'sales', name: label.channel('store') },
  { id: 'distribution', mark: 'bar', x: 'date', y: 'distribution', stack: 'sales', name: label.channel('distribution') },
  { id: 'target', mark: 'line', x: 'date', y: 'target', curve: 'step-after', symbols: 'none', name: t('workbench.charts.target') },
])

/** 会员日连着的几天合成一条参考带 */
const memberDayBands = computed<CartesianAnnotation[]>(() => {
  const bands: CartesianAnnotation[] = []
  let start: Date | null = null
  sales.value.forEach((row, index) => {
    const member = isMemberDay(row.date)
    if (member && start === null) {
      start = row.date
    }
    const next = sales.value[index + 1]
    if (member && start !== null && (!next || !isMemberDay(next.date))) {
      bands.push({ kind: 'band', axis: 'x', from: start, to: row.date, label: t('workbench.charts.member_day') })
      start = null
    }
  })
  return bands
})

const annotations = computed<CartesianAnnotation[]>(() => [
  ...memberDayBands.value,
  { kind: 'trend', series: 'online', method: 'moving-average', window: MOVING_AVERAGE_DAYS, label: t('workbench.charts.moving_average', { n: MOVING_AVERAGE_DAYS }) },
  { kind: 'point', series: 'online', at: 'max', label: t('workbench.charts.peak') },
])

const xAxis = { format: { month: 'numeric', day: 'numeric' } } as const
const yAxis = { format: CURRENCY_COMPACT }
</script>

<template>
  <ChartWidget icon="lucide:trending-up" :title="t('workbench.widgets.sales_trend.title')" size="lg">
    <template #extra>
      <XSegmented v-model:value="range" :options="rangeOptions" />
    </template>
    <XhCartesianChartRoot
      :key="range"
      :data="rows"
      :series="series"
      :annotations="annotations"
      :x-axis="xAxis"
      :y-axis="yAxis"
      zoom="x"
      totals
      :locale="locale"
      :aria-label="t('workbench.widgets.sales_trend.desc')"
    />
  </ChartWidget>
</template>
