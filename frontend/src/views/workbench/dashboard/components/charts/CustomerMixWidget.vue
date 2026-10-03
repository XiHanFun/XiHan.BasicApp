<script setup lang="ts">
import type { CartesianSeries } from '@xihan-ui/headless'
import { XhCartesianChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { customerMix, TIERS } from './demo-data'

/**
 * 会员构成：各大区的会员按等级堆叠成横条。
 * 「占比」把每条归一成 100%，比的是各区的等级结构；「人数」逐段累加，比的是各区的会员规模。
 */
defineOptions({ name: 'CustomerMixWidget' })

type Mode = 'share' | 'count'

const { t, locale } = useI18n()
const label = useDemoLabels()

const mode = ref<Mode>('share')
const modeOptions = computed(() => [
  { value: 'share' as const, label: t('workbench.charts.view_share') },
  { value: 'count' as const, label: t('workbench.charts.view_count') },
])

const mix = customerMix(new Date())
const rows = computed(() => mix.map(({ region, ...tiers }) => ({ region: label.region(region), ...tiers })))

const series = computed<CartesianSeries[]>(() => TIERS.map(tier => ({
  id: tier,
  mark: 'bar',
  x: 'region',
  y: tier,
  stack: 'tier',
  stackOffset: mode.value === 'share' ? 'expand' : 'none',
  name: label.tier(tier),
})))

const yAxis = computed(() => ({ format: mode.value === 'share' ? { style: 'percent' as const } : { notation: 'compact' as const } }))
</script>

<template>
  <ChartWidget icon="lucide:users" :title="t('workbench.widgets.customer_mix.title')">
    <template #extra>
      <XSegmented v-model:value="mode" :options="modeOptions" />
    </template>
    <XhCartesianChartRoot
      :key="mode"
      :data="rows"
      :series="series"
      :y-axis="yAxis"
      orientation="horizontal"
      :totals="mode === 'count'"
      :locale="locale"
      :aria-label="t('workbench.widgets.customer_mix.desc')"
    />
  </ChartWidget>
</template>
