<script setup lang="ts">
import type { CartesianAnnotation, CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { lineOf, useWidgetData } from '../widget-data'
import { COUNT_COMPACT, localDay, useRangeOptions } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 登录趋势：逐日登录按成功、失败两段堆叠，成功次数另画 7 日均线与峰值点；横轴可拖动缩放，看长区间时拉近某一段。
 * 按查看者的本地日分桶（后端按偏移换算）。
 */
defineOptions({ name: 'LoginTrendWidget' })

const MOVING_AVERAGE_DAYS = 7

const { t, locale } = useI18n()
const range = ref('30')
const rangeOptions = useRangeOptions([7, 30, 90])
const { data, loading, error } = useWidgetData('auditing.login-trend', () => Number(range.value))

const rows = computed(() => {
  const succeeded = lineOf(data.value, 'succeeded')
  const failed = lineOf(data.value, 'failed')
  return (data.value?.series?.categories ?? []).map((day, index) => ({
    date: localDay(day),
    succeeded: succeeded[index] ?? 0,
    failed: failed[index] ?? 0,
  }))
})
const empty = computed(() => rows.value.every(row => row.succeeded === 0 && row.failed === 0))

const series = computed<CartesianSeries[]>(() => [
  { id: 'succeeded', mark: 'bar', x: 'date', y: 'succeeded', stack: 'logins', name: t('workbench.charts.succeeded') },
  { id: 'failed', mark: 'bar', x: 'date', y: 'failed', stack: 'logins', name: t('workbench.charts.failed') },
])

/** 均线要够长的区间才有意义：7 天就只标峰值 */
const annotations = computed<CartesianAnnotation[]>(() => [
  ...(Number(range.value) > MOVING_AVERAGE_DAYS
    ? [{ kind: 'trend' as const, series: 'succeeded', method: 'moving-average' as const, window: MOVING_AVERAGE_DAYS, label: t('workbench.charts.moving_average', { n: MOVING_AVERAGE_DAYS }) }]
    : []),
  { kind: 'point', series: 'succeeded', at: 'max', label: t('workbench.charts.peak') },
])

const xAxis = { format: { month: 'numeric', day: 'numeric' } } as const
const yAxis = { format: COUNT_COMPACT }
</script>

<template>
  <ChartWidget
    icon="lucide:trending-up"
    :title="t('workbench.widgets.login_trend.title')"
    size="lg"
    :loading="loading"
    :error="error"
    :empty="empty"
  >
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
      :aria-label="t('workbench.widgets.login_trend.desc')"
    />
  </ChartWidget>
</template>
