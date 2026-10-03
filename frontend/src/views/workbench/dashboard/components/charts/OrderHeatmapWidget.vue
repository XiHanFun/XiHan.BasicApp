<script setup lang="ts">
import { XhHeatmapRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useWeekdayNames } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { addDays, dailySales, isoDate, orderWeekHours, startOfDay } from './demo-data'

/**
 * 下单时段：
 * - 周内时段：一周七天 × 二十四小时的下单量矩阵，连续色阶着色，同一档里的格子也分得出深浅；
 * - 近一年：逐日下单量的日历热力，会员日的放量一眼能看出来。
 */
defineOptions({ name: 'OrderHeatmapWidget' })

type View = 'week' | 'year'

const YEAR_DAYS = 365
const HOURS = Array.from({ length: 24 }, (_, hour) => String(hour).padStart(2, '0'))

const { t, locale } = useI18n()
const weekdays = useWeekdayNames()

const view = ref<View>('week')
const viewOptions = computed(() => [
  { value: 'week' as const, label: t('workbench.charts.view_week') },
  { value: 'year' as const, label: t('workbench.charts.view_year') },
])

const today = new Date()
const weekHours = orderWeekHours(today)
const cells = computed(() => weekHours.map(cell => ({
  row: weekdays.value[cell.weekday]!,
  column: HOURS[cell.hour]!,
  value: cell.orders,
})))

const yearEnd = startOfDay(today)
const yearStart = addDays(yearEnd, 1 - YEAR_DAYS)
const days = dailySales(YEAR_DAYS, today).map(row => ({ date: isoDate(row.date), count: row.orders }))
</script>

<template>
  <ChartWidget icon="lucide:calendar-clock" :title="t('workbench.widgets.order_heatmap.title')">
    <template #extra>
      <XSegmented v-model:value="view" :options="viewOptions" />
    </template>
    <div class="heatmap-scroll">
      <XhHeatmapRoot
        v-if="view === 'week'"
        variant="matrix"
        :rows="weekdays"
        :columns="HOURS"
        :value="cells"
        continuous
        :locale="locale"
        :aria-label="t('workbench.widgets.order_heatmap.desc')"
      />
      <XhHeatmapRoot
        v-else
        variant="calendar"
        :start-date="isoDate(yearStart)"
        :end-date="isoDate(yearEnd)"
        :value="days"
        :first-day-of-week="1"
        palette="green"
        :locale="locale"
        :aria-label="t('workbench.widgets.order_heatmap.desc')"
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
