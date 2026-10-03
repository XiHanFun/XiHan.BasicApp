<script setup lang="ts">
import type { SparklineMarkers, SparklineVariant, StatisticTrend } from '@xihan-ui/headless'
import {
  XhSparkline,
  XhStatisticLabel,
  XhStatisticRoot,
  XhStatisticTrend,
  XhStatisticValue,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { dailySales } from './demo-data'

/**
 * 经营指标：今日销售额、订单量、客单价与退款率，较昨日的涨跌，加近两周走势的迷你图。
 * 四张卡各用一种迷你图：面积标出最高最低、柱形、折线带正常区间与均值参考线、涨跌（每天较前一天升还是降）。
 * 退款率越低越好：它的涨跌语气反过来。
 */
defineOptions({ name: 'SalesKpiWidget' })

const SPARK_DAYS = 14

const { t } = useI18n()
const formatters = useFormatters()
const rows = dailySales(SPARK_DAYS + 1, new Date())

interface Card {
  key: string
  label: string
  values: number[]
  format: (value: number) => string
  variant: SparklineVariant
  markers?: SparklineMarkers
  band?: readonly [number, number]
  reference?: 'mean'
  /** 涨了是坏事 */
  lowerIsBetter?: boolean
}

const cards = computed(() => {
  const { currency, number, percent } = formatters.value
  const definitions: Card[] = [
    { key: 'sales', label: t('workbench.charts.sales'), values: rows.map(row => row.sales), format: currency, variant: 'area', markers: 'extremes' },
    { key: 'orders', label: t('workbench.charts.orders'), values: rows.map(row => row.orders), format: number, variant: 'bar', markers: 'last' },
    {
      key: 'aov',
      label: t('workbench.charts.aov'),
      values: rows.map(row => Math.round(row.sales / row.orders)),
      format: value => formatters.value.currencyFull(value),
      variant: 'line',
      band: [215, 245],
      reference: 'mean',
    },
    { key: 'refund', label: t('workbench.charts.refund_rate'), values: rows.map(row => row.refundRate), format: percent, variant: 'win-loss', lowerIsBetter: true },
  ]

  return definitions.map((card) => {
    const today = card.values.at(-1) ?? 0
    const yesterday = card.values.at(-2) ?? 0
    const trend: StatisticTrend = today > yesterday ? 'up' : today < yesterday ? 'down' : 'flat'
    const good = trend === 'flat' ? null : (trend === 'up') !== !!card.lowerIsBetter
    // 涨跌形态画的是每天较前一天的变化，其余画原值；迷你图只画最近两周，多取的一天只用来算第一天的变化
    const series = card.variant === 'win-loss'
      ? card.values.slice(1).map((value, index) => value - card.values[index]!)
      : card.values.slice(1)
    return {
      ...card,
      value: card.format(today),
      trend,
      tone: good === null ? undefined : good ? 'success' as const : 'danger' as const,
      note: trend === 'flat'
        ? t('workbench.charts.vs_yesterday_flat')
        : t('workbench.charts.vs_yesterday', { value: percent(Math.abs(today - yesterday) / yesterday) }),
      series,
      sparkLabel: t('workbench.charts.spark_label', { name: card.label, n: SPARK_DAYS }),
    }
  })
})
</script>

<template>
  <ChartWidget icon="lucide:activity" :title="t('workbench.widgets.sales_kpi.title')">
    <div class="kpi-grid">
      <div v-for="card in cards" :key="card.key" class="kpi-card">
        <XhStatisticRoot :trend="card.trend" :tone="card.tone" size="sm">
          <XhStatisticLabel>{{ card.label }}</XhStatisticLabel>
          <XhStatisticValue>{{ card.value }}</XhStatisticValue>
          <XhStatisticTrend>{{ card.note }}</XhStatisticTrend>
        </XhStatisticRoot>
        <XhSparkline
          :data="card.series"
          :variant="card.variant"
          :markers="card.markers"
          :band="card.band"
          :reference="card.reference"
          :aria-label="card.sparkLabel"
          class="kpi-spark"
        />
      </div>
    </div>
  </ChartWidget>
</template>

<style scoped>
/* 按小组件自身宽度排布：窄时单列，放得下两张卡时两列 */
.kpi-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--xh-space-3);
}

@container (min-width: 22rem) {
  .kpi-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

.kpi-card {
  display: grid;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: var(--xh-stroke-thin) solid var(--xh-border-subtle);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-surface);
}

/* 迷你图铺满卡片宽度，高度取一个控件高 */
.kpi-spark {
  --xh-sparkline-width: 100%;
  --xh-sparkline-height: var(--xh-control-h-md);
}
</style>
