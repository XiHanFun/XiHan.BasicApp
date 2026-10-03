<script setup lang="ts">
import type { CartesianAnnotation, CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import ChartWidget from './ChartWidget.vue'
import { materialPrices } from './demo-data'

/**
 * 原料行情：主要原料（铜）最近 60 个交易日的日 K，可在蜡烛图与美国线之间切换，另画 5 日均线。
 * 横轴可拖动缩放，默认停在最近 30 个交易日。
 */
defineOptions({ name: 'MaterialPriceWidget' })

type Style = 'candle' | 'ohlc'

const TRADING_DAYS = 60
const WINDOW_DAYS = 30
const MOVING_AVERAGE_DAYS = 5

const { t, locale } = useI18n()

const style = ref<Style>('candle')
const styleOptions = computed(() => [
  { value: 'candle' as const, label: t('workbench.charts.style_candle') },
  { value: 'ohlc' as const, label: t('workbench.charts.style_ohlc') },
])

const candles = materialPrices(TRADING_DAYS, new Date())
// 图表按字段名读行，要的是普通对象行，不收接口类型
const rows = candles.map(candle => ({ ...candle }))
const defaultWindow = { x: [candles.at(-WINDOW_DAYS)!.date, candles.at(-1)!.date] as const }

const series = computed<CartesianSeries[]>(() => [
  { id: 'copper', mark: 'candlestick', x: 'date', open: 'open', high: 'high', low: 'low', close: 'close', style: style.value, name: t('workbench.charts.copper') },
])

const annotations = computed<CartesianAnnotation[]>(() => [
  { kind: 'trend', series: 'copper', method: 'moving-average', window: MOVING_AVERAGE_DAYS, label: t('workbench.charts.moving_average', { n: MOVING_AVERAGE_DAYS }) },
])

const xAxis = { format: { month: 'numeric', day: 'numeric' } } as const
const yAxis = computed(() => ({ zero: false, title: t('workbench.charts.price_per_ton') }))
</script>

<template>
  <ChartWidget icon="lucide:chart-candlestick" :title="t('workbench.widgets.material_price.title')">
    <template #extra>
      <XSegmented v-model:value="style" :options="styleOptions" />
    </template>
    <XhCartesianChartRoot
      :data="rows"
      :series="series"
      :annotations="annotations"
      :x-axis="xAxis"
      :y-axis="yAxis"
      zoom="x"
      :default-window="defaultWindow"
      :locale="locale"
      :aria-label="t('workbench.widgets.material_price.desc')"
    />
  </ChartWidget>
</template>
