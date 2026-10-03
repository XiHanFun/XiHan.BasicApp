<script setup lang="ts">
import { XhSankeyChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { CATEGORIES, CHANNELS, FULFILLMENTS, orderFlow } from './demo-data'

/**
 * 订单流向：近 30 天的订单从渠道流向品类、再流向履约结果（签收、退货、取消），流带宽度是订单数，
 * 颜色从起点节点渐变到终点节点。可在横向与纵向排布之间切换。
 */
defineOptions({ name: 'OrderFlowWidget' })

type Orientation = 'horizontal' | 'vertical'

const { t, locale } = useI18n()
const label = useDemoLabels()

const orientation = ref<Orientation>('horizontal')
const orientationOptions = computed(() => [
  { value: 'horizontal' as const, label: t('workbench.charts.orientation_horizontal') },
  { value: 'vertical' as const, label: t('workbench.charts.orientation_vertical') },
])

const flow = orderFlow(new Date())

const nodes = computed(() => [
  ...CHANNELS.map(channel => ({ id: `channel:${channel}`, name: label.channel(channel), group: t('workbench.charts.group_channel') })),
  ...CATEGORIES.map(category => ({ id: `category:${category}`, name: label.category(category), group: t('workbench.charts.group_category') })),
  ...FULFILLMENTS.map(fulfillment => ({ id: `fulfillment:${fulfillment}`, name: label.fulfillment(fulfillment), group: t('workbench.charts.group_fulfillment') })),
])

const links = [
  ...flow.channelCategory.map(row => ({ source: `channel:${row.channel}`, target: `category:${row.category}`, value: row.orders })),
  ...flow.categoryFulfillment.map(row => ({ source: `category:${row.category}`, target: `fulfillment:${row.fulfillment}`, value: row.orders })),
]
</script>

<template>
  <ChartWidget icon="lucide:waypoints" :title="t('workbench.widgets.order_flow.title')" size="lg">
    <template #extra>
      <XSegmented v-model:value="orientation" :options="orientationOptions" />
    </template>
    <XhSankeyChartRoot
      :key="orientation"
      :nodes="nodes"
      :links="links"
      :orientation="orientation"
      link-color="gradient"
      :format="{ notation: 'compact' }"
      :locale="locale"
      :aria-label="t('workbench.widgets.order_flow.desc')"
    />
  </ChartWidget>
</template>
