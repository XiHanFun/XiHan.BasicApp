<script setup lang="ts">
import type { CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { lineOf, useWidgetData } from '../widget-data'
import { COUNT_COMPACT, localDay, useRangeOptions } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 作业执行：逐日作业运行按成功、失败（含节点掉线丢失的）两段堆叠，按计划时刻归到本地日。只在平台一侧。
 */
defineOptions({ name: 'JobRunsWidget' })

const { t, locale } = useI18n()
const range = ref('14')
const rangeOptions = useRangeOptions([14, 30])
const { data, loading, error } = useWidgetData('maintenance.job-runs', () => Number(range.value))

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
  { id: 'succeeded', mark: 'bar', x: 'date', y: 'succeeded', stack: 'runs', name: t('workbench.charts.succeeded') },
  { id: 'failed', mark: 'bar', x: 'date', y: 'failed', stack: 'runs', name: t('workbench.charts.failed') },
])
const xAxis = { format: { month: 'numeric', day: 'numeric' } } as const
const yAxis = { format: COUNT_COMPACT }
</script>

<template>
  <ChartWidget
    icon="lucide:timer"
    :title="t('workbench.widgets.job_runs.title')"
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
      :x-axis="xAxis"
      :y-axis="yAxis"
      :locale="locale"
      :aria-label="t('workbench.widgets.job_runs.desc')"
    />
  </ChartWidget>
</template>
