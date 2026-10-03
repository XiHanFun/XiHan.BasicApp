<script setup lang="ts">
import { XhFunnelChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { conversionFunnel } from './demo-data'

/**
 * 转化漏斗：从曝光、访问、加购、下单、支付到复购的逐级人数，相邻两级之间写出相对上一级的转化率。
 * 梯形居中摆，条形左对齐，后者更容易比较各级的绝对长度。
 */
defineOptions({ name: 'ConversionFunnelWidget' })

type Shape = 'trapezoid' | 'bar'

const { t, locale } = useI18n()
const label = useDemoLabels()

const shape = ref<Shape>('trapezoid')
const shapeOptions = computed(() => [
  { value: 'trapezoid' as const, label: t('workbench.charts.shape_trapezoid') },
  { value: 'bar' as const, label: t('workbench.charts.shape_bar') },
])

const stages = conversionFunnel(new Date())
const rows = computed(() => stages.map(item => ({ stage: label.stage(item.stage), users: item.value })))
</script>

<template>
  <ChartWidget icon="lucide:filter" :title="t('workbench.widgets.conversion_funnel.title')">
    <template #extra>
      <XSegmented v-model:value="shape" :options="shapeOptions" />
    </template>
    <XhFunnelChartRoot
      :data="rows"
      name-field="stage"
      value-field="users"
      :shape="shape"
      :align="shape === 'bar' ? 'start' : 'center'"
      conversion="previous"
      :format="{ notation: 'compact' }"
      :locale="locale"
      :aria-label="t('workbench.widgets.conversion_funnel.desc')"
    />
  </ChartWidget>
</template>
