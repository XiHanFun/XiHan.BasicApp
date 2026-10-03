<script setup lang="ts">
import type { CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CURRENCY_COMPACT, useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { profitWaterfall } from './demo-data'

/**
 * 利润构成：本月从营业收入出发，逐项减去销售成本、营销、物流与人工费用，毛利与净利润是小计柱。
 * 瀑布图里每一步接在上一步的累计值上，增取涨色、减取跌色。
 */
defineOptions({ name: 'ProfitWaterfallWidget' })

const { t, locale } = useI18n()
const label = useDemoLabels()

const steps = profitWaterfall(new Date())
const rows = computed(() => steps.map(row => ({ step: label.step(row.step), value: row.value, total: row.total })))

const series = computed<CartesianSeries[]>(() => [
  { mark: 'bar', x: 'step', y: 'value', waterfall: { total: 'total' }, labels: 'end', name: t('workbench.charts.amount') },
])

const yAxis = { format: CURRENCY_COMPACT }
</script>

<template>
  <ChartWidget icon="lucide:chart-column-decreasing" :title="t('workbench.widgets.profit_waterfall.title')">
    <XhCartesianChartRoot
      :data="rows"
      :series="series"
      :y-axis="yAxis"
      :locale="locale"
      :aria-label="t('workbench.widgets.profit_waterfall.desc')"
    />
  </ChartWidget>
</template>
