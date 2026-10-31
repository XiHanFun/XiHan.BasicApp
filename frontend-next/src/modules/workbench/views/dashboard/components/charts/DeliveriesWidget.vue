<script setup lang="ts">
import type { CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { lineOf, useWidgetData } from '../widget-data'
import { COUNT_COMPACT } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 消息发送：近 30 天邮件与短信（有短信发件记录的权限才有）按发送状态堆叠：成功、失败、待发（含发送中）、取消。
 */
defineOptions({ name: 'DeliveriesWidget' })

const DAYS = 30
const STATUSES = ['succeeded', 'failed', 'pending', 'cancelled'] as const

const { t, locale } = useI18n()
const { data, loading, error } = useWidgetData('notification.deliveries', DAYS)

const rows = computed(() => (data.value?.series?.categories ?? []).map((channel, index) => ({
  channel: t(`workbench.charts.channel.${channel}`),
  ...Object.fromEntries(STATUSES.map(status => [status, lineOf(data.value, status)[index] ?? 0])),
})))
const empty = computed(() => STATUSES.every(status => lineOf(data.value, status).every(value => value === 0)))

const series = computed<CartesianSeries[]>(() => STATUSES.map(status => ({
  id: status,
  mark: 'bar',
  x: 'channel',
  y: status,
  stack: 'messages',
  name: t(`workbench.charts.status.${status}`),
})))
const yAxis = { format: COUNT_COMPACT }
</script>

<template>
  <ChartWidget
    icon="lucide:send"
    :title="t('workbench.widgets.deliveries.title')"
    :loading="loading"
    :error="error"
    :empty="empty"
  >
    <XhCartesianChartRoot
      :data="rows"
      :series="series"
      :y-axis="yAxis"
      orientation="horizontal"
      totals
      :locale="locale"
      :aria-label="t('workbench.widgets.deliveries.desc')"
    />
  </ChartWidget>
</template>
