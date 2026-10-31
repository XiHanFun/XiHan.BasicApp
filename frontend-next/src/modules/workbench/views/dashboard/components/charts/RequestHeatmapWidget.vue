<script setup lang="ts">
import { XhHeatmapRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useWidgetData } from '../widget-data'
import { useRangeOptions, useWeekdayNames } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 访问时段：区间内请求按查看者本地的星期 × 小时汇总成矩阵，连续色阶着色，忙闲时段一眼能看出来。
 */
defineOptions({ name: 'RequestHeatmapWidget' })

const HOURS = Array.from({ length: 24 }, (_, hour) => String(hour).padStart(2, '0'))

const { t, locale } = useI18n()
const weekdays = useWeekdayNames()
const range = ref('30')
const rangeOptions = useRangeOptions([7, 30])
const { data, loading, error } = useWidgetData('auditing.request-heatmap', () => Number(range.value))

// 序列是星期一（1）到星期日（7），每条 24 个值对应 0–23 时
const cells = computed(() => (data.value?.series?.lines ?? []).flatMap(line => line.values.map((value, hour) => ({
  row: weekdays.value[Number(line.key) - 1] ?? line.key,
  column: HOURS[hour]!,
  value,
}))))
const empty = computed(() => cells.value.every(cell => cell.value === 0))
</script>

<template>
  <ChartWidget
    icon="lucide:calendar-clock"
    :title="t('workbench.widgets.request_heatmap.title')"
    :loading="loading"
    :error="error"
    :empty="empty"
  >
    <template #extra>
      <XSegmented v-model:value="range" :options="rangeOptions" />
    </template>
    <div class="heatmap-scroll">
      <XhHeatmapRoot
        :key="range"
        variant="matrix"
        :rows="weekdays"
        :columns="HOURS"
        :value="cells"
        continuous
        :locale="locale"
        :aria-label="t('workbench.widgets.request_heatmap.desc')"
      />
    </div>
  </ChartWidget>
</template>

<style scoped>
/* 窄的时候放不下就横向滚动，不压扁格子 */
.heatmap-scroll {
  overflow-x: auto;
}
</style>
