<script setup lang="ts">
import type { CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useWidgetData } from '../widget-data'
import { COUNT_COMPACT } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 登录失败原因：近 30 天失败的登录按错误码排行（显示名取该错误码的失败原因），棒棒糖条形、末端写数；
 * 只列最多的几种，其余并成「其他」。
 */
defineOptions({ name: 'LoginFailuresWidget' })

const DAYS = 30

const { t, locale } = useI18n()
const { data, loading, error } = useWidgetData('auditing.login-failures', DAYS)

const rows = computed(() => (data.value?.slices ?? []).map(slice => ({
  reason: slice.key === 'others' ? t('workbench.charts.others') : (slice.label || slice.key),
  count: slice.value,
})))

const series = computed<CartesianSeries[]>(() => [
  { mark: 'bar', shape: 'lollipop', x: 'reason', y: 'count', labels: 'end', name: t('workbench.charts.failed') },
])
const yAxis = { format: COUNT_COMPACT }
</script>

<template>
  <ChartWidget
    icon="lucide:shield-alert"
    :title="t('workbench.widgets.login_failures.title')"
    :loading="loading"
    :error="error"
    :empty="!rows.length"
  >
    <XhCartesianChartRoot
      :data="rows"
      :series="series"
      :y-axis="yAxis"
      orientation="horizontal"
      :locale="locale"
      :aria-label="t('workbench.widgets.login_failures.desc')"
    />
  </ChartWidget>
</template>
