<script setup lang="ts">
import type { CartesianAxis, CartesianSeries, ChartRow } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { CURRENCY_COMPACT, useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { orderAmounts, products } from './demo-data'

/**
 * 商品表现，四种看法：
 * - 排行：销售额前 10 的商品，棒棒糖条形，末端写数；
 * - 价量：横轴单价、纵轴销量（都是对数轴，跨了三个数量级），气泡大小是毛利额，颜色按毛利率走顺序色阶；
 * - 箱线 / 小提琴：各零售渠道单笔订单金额的分布，箱线标出离群的大单，小提琴画出分布的轮廓（门店是日常小单与家电大单的双峰）。
 */
defineOptions({ name: 'ProductPerformanceWidget' })

type View = 'rank' | 'bubble' | 'box' | 'violin'

const RANK_TOP = 10

const { t, locale } = useI18n()
const label = useDemoLabels()

const view = ref<View>('rank')
const viewOptions = computed(() => [
  { value: 'rank' as const, label: t('workbench.charts.view_rank') },
  { value: 'bubble' as const, label: t('workbench.charts.view_bubble') },
  { value: 'box' as const, label: t('workbench.charts.view_box') },
  { value: 'violin' as const, label: t('workbench.charts.view_violin') },
])

const today = new Date()
const productRows = products(today)
const amountRows = orderAmounts(today)

const rows = computed(() => productRows.map(row => ({
  name: `${label.item(row.item)} ${row.variant}`,
  price: row.price,
  units: row.units,
  sales: row.sales,
  profit: row.grossProfit,
  margin: Math.round((row.grossProfit / row.sales) * 1000) / 10,
})))

const rankRows = computed(() => [...rows.value].sort((a, b) => b.sales - a.sales).slice(0, RANK_TOP))
const distributionRows = computed(() => amountRows.map(row => ({ channel: label.channel(row.channel), amount: row.amount })))

const chart = computed<{ data: readonly ChartRow[], series: CartesianSeries[], xAxis?: CartesianAxis, yAxis?: CartesianAxis, horizontal?: boolean }>(() => {
  switch (view.value) {
    case 'rank':
      return {
        data: rankRows.value,
        series: [{ mark: 'bar', shape: 'lollipop', x: 'name', y: 'sales', labels: 'end', name: t('workbench.charts.sales') }],
        yAxis: { format: CURRENCY_COMPACT },
        horizontal: true,
      }
    case 'bubble':
      return {
        data: rows.value,
        series: [{ mark: 'scatter', x: 'price', y: 'units', size: 'profit', color: 'margin', datumId: 'name', name: t('workbench.charts.product') }],
        xAxis: { scale: 'log', title: t('workbench.charts.unit_price'), format: CURRENCY_COMPACT },
        yAxis: { scale: 'log', title: t('workbench.charts.units'), format: { notation: 'compact' } },
      }
    default:
      return {
        data: distributionRows.value,
        series: [{ mark: 'boxplot', x: 'channel', y: 'amount', style: view.value === 'violin' ? 'violin' : 'box', name: t('workbench.charts.order_amount') }],
        yAxis: { title: t('workbench.charts.order_amount'), format: CURRENCY_COMPACT },
      }
  }
})

const translations = computed(() => ({
  sizeLabel: t('workbench.charts.gross_profit'),
  colorLabel: t('workbench.charts.gross_margin'),
}))
</script>

<template>
  <ChartWidget icon="lucide:package" :title="t('workbench.widgets.product_performance.title')" size="lg">
    <template #extra>
      <XSegmented v-model:value="view" :options="viewOptions" />
    </template>
    <XhCartesianChartRoot
      :key="view"
      :data="chart.data"
      :series="chart.series"
      :x-axis="chart.xAxis"
      :y-axis="chart.yAxis"
      :orientation="chart.horizontal ? 'horizontal' : 'vertical'"
      :translations="translations"
      :locale="locale"
      :aria-label="t('workbench.widgets.product_performance.desc')"
    />
  </ChartWidget>
</template>
