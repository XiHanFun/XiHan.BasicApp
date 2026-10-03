<script setup lang="ts">
import { XhPieChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { CURRENCY_COMPACT, useDemoLabels, useFormatters } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { CHANNELS, channelTotals, dailySales, paymentShare, regionShare } from './demo-data'

/**
 * 销售构成：近 30 天销售额按渠道、支付方式与收货地区的占比，三个维度各用一种饼图形态：
 * 渠道是环形、环心写合计，标签在外；支付方式是玫瑰图，扇区半径也随占比变，差距一眼拉开；
 * 地区是半环，只单列最多的五个，其余并进「其他」。
 */
defineOptions({ name: 'ChannelShareWidget' })

type Dimension = 'channel' | 'payment' | 'region'

const DAYS = 30
const MAX_REGIONS = 5

const { t, locale } = useI18n()
const label = useDemoLabels()
const formatters = useFormatters()

const dimension = ref<Dimension>('channel')
const dimensionOptions = computed(() => [
  { value: 'channel' as const, label: t('workbench.charts.view_channel') },
  { value: 'payment' as const, label: t('workbench.charts.view_payment') },
  { value: 'region' as const, label: t('workbench.charts.view_region') },
])

const today = new Date()
const totals = channelTotals(dailySales(DAYS, today))
const total = CHANNELS.reduce((sum, channel) => sum + totals[channel], 0)
const payments = paymentShare(today, total)
const regions = regionShare(today, total)

const rows = computed(() => {
  if (dimension.value === 'channel') {
    return CHANNELS.map(channel => ({ name: label.channel(channel), sales: totals[channel] }))
  }
  if (dimension.value === 'payment') {
    return payments.map(item => ({ name: label.payment(item.key), sales: item.value }))
  }
  return regions.map(item => ({ name: label.region(item.key), sales: item.value }))
})
</script>

<template>
  <ChartWidget icon="lucide:pie-chart" :title="t('workbench.widgets.channel_share.title')">
    <template #extra>
      <XSegmented v-model:value="dimension" :options="dimensionOptions" />
    </template>
    <XhPieChartRoot
      :key="dimension"
      :data="rows"
      name-field="name"
      value-field="sales"
      :variant="dimension === 'payment' ? 'pie' : 'donut'"
      :rose="dimension === 'payment'"
      :sweep="dimension === 'region' ? 'half' : 'full'"
      :sort="dimension === 'region' ? 'descending' : 'none'"
      :max-slices="dimension === 'region' ? MAX_REGIONS : undefined"
      :labels="dimension === 'payment' ? 'inside' : 'outside'"
      :label-content="dimension === 'payment' ? 'share' : 'name-share'"
      :format="CURRENCY_COMPACT"
      :locale="locale"
      :aria-label="t('workbench.widgets.channel_share.desc')"
    >
      <template v-if="dimension === 'channel'" #center>
        <span class="center-value">{{ formatters.currency(total) }}</span>
        <span class="center-label">{{ t('workbench.charts.days_total', { n: DAYS }) }}</span>
      </template>
    </XhPieChartRoot>
  </ChartWidget>
</template>

<style scoped>
.center-value {
  font-size: var(--xh-font-size-lg);
  font-weight: var(--xh-font-weight-semibold);
}

.center-label {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-secondary-size);
}
</style>
