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
import { lineOf, useWidgetData } from '../widget-data'
import { useFormatters } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 访问概况：今天的请求数、失败次数、失败率与平均耗时，较昨日的涨跌，加近两周走势的迷你图。
 * 四张卡各用一种迷你图：面积标出最高最低、柱形、涨跌（每天较前一天升还是降）、折线带均值参考线。
 * 失败与耗时越低越好：它们的涨跌语气反过来。
 */
defineOptions({ name: 'RequestKpiWidget' })

const SPARK_DAYS = 14

const { t } = useI18n()
const formatters = useFormatters()
// 多取一天：迷你图只画最近两周，多的那天只用来算第一天的变化
const { data, loading, error } = useWidgetData('auditing.request-kpi', SPARK_DAYS + 1)

interface Card {
  key: string
  label: string
  values: number[]
  format: (value: number) => string
  variant: SparklineVariant
  markers?: SparklineMarkers
  reference?: 'mean'
  /** 涨了是坏事 */
  lowerIsBetter?: boolean
}

const cards = computed(() => {
  const { number, percent, milliseconds } = formatters.value
  const total = lineOf(data.value, 'total')
  const failed = lineOf(data.value, 'failed')
  const rate = total.map((count, index) => (count === 0 ? 0 : (failed[index] ?? 0) / count))
  const definitions: Card[] = [
    { key: 'total', label: t('workbench.charts.requests'), values: total, format: number, variant: 'area', markers: 'extremes' },
    { key: 'failed', label: t('workbench.charts.failed_requests'), values: failed, format: number, variant: 'bar', markers: 'last', lowerIsBetter: true },
    { key: 'rate', label: t('workbench.charts.failure_rate'), values: rate, format: percent, variant: 'win-loss', lowerIsBetter: true },
    { key: 'average', label: t('workbench.charts.average_duration'), values: lineOf(data.value, 'average_ms'), format: milliseconds, variant: 'line', reference: 'mean', lowerIsBetter: true },
  ]

  return definitions.map((card) => {
    const today = card.values.at(-1) ?? 0
    const yesterday = card.values.at(-2) ?? 0
    const trend: StatisticTrend = today > yesterday ? 'up' : today < yesterday ? 'down' : 'flat'
    const good = trend === 'flat' ? null : (trend === 'up') !== !!card.lowerIsBetter
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
        : yesterday === 0
          ? t('workbench.charts.vs_yesterday_new')
          : t('workbench.charts.vs_yesterday', { value: percent(Math.abs(today - yesterday) / yesterday) }),
      series,
      sparkLabel: t('workbench.charts.spark_label', { name: card.label, n: SPARK_DAYS }),
    }
  })
})
</script>

<template>
  <ChartWidget
    icon="lucide:activity"
    :title="t('workbench.widgets.request_kpi.title')"
    :loading="loading"
    :error="error"
  >
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
