<script setup lang="ts">
import type { CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { lineOf, useWidgetData } from '../widget-data'
import { COUNT_COMPACT, MILLISECONDS, useRangeOptions } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 访问最多的操作：区间内次数最多的前 10 个操作（操作 ID），两种看法：
 * - 次数：成功与失败两段堆叠成横条，末端写合计；
 * - 耗时：同样这些操作的平均耗时，棒棒糖条形、末端写数，慢在哪一眼看出来。
 */
defineOptions({ name: 'TopOperationsWidget' })

type View = 'count' | 'duration'

const { t, locale } = useI18n()
const range = ref('7')
const rangeOptions = useRangeOptions([7, 30])
const view = ref<View>('count')
const viewOptions = computed(() => [
  { value: 'count' as const, label: t('workbench.charts.view_count') },
  { value: 'duration' as const, label: t('workbench.charts.view_duration') },
])
const { data, loading, error } = useWidgetData('auditing.top-operations', () => Number(range.value))

const rows = computed(() => {
  const count = lineOf(data.value, 'count')
  const failed = lineOf(data.value, 'failed')
  const average = lineOf(data.value, 'average_ms')
  return (data.value?.series?.categories ?? []).map((operation, index) => ({
    operation,
    succeeded: (count[index] ?? 0) - (failed[index] ?? 0),
    failed: failed[index] ?? 0,
    average: average[index] ?? 0,
  }))
})

const chart = computed<{ series: CartesianSeries[], format: typeof COUNT_COMPACT }>(() => view.value === 'count'
  ? {
      series: [
        { id: 'succeeded', mark: 'bar', x: 'operation', y: 'succeeded', stack: 'calls', name: t('workbench.charts.succeeded') },
        { id: 'failed', mark: 'bar', x: 'operation', y: 'failed', stack: 'calls', name: t('workbench.charts.failed') },
      ],
      format: COUNT_COMPACT,
    }
  : {
      series: [{ mark: 'bar', shape: 'lollipop', x: 'operation', y: 'average', labels: 'end', name: t('workbench.charts.average_duration') }],
      format: MILLISECONDS,
    })
</script>

<template>
  <ChartWidget
    icon="lucide:list-ordered"
    :title="t('workbench.widgets.top_operations.title')"
    size="lg"
    :loading="loading"
    :error="error"
    :empty="!rows.length"
  >
    <template #extra>
      <XSegmented v-model:value="view" :options="viewOptions" />
      <XSegmented v-model:value="range" :options="rangeOptions" />
    </template>
    <XhCartesianChartRoot
      :key="`${view}-${range}`"
      :data="rows"
      :series="chart.series"
      :y-axis="{ format: chart.format }"
      orientation="horizontal"
      :totals="view === 'count'"
      :locale="locale"
      :aria-label="t('workbench.widgets.top_operations.desc')"
    />
  </ChartWidget>
</template>
