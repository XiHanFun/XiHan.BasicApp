<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useFormatters } from './charts/chart-helpers'
import { dailySales } from './charts/demo-data'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'StatsWidget' })

const { t } = useI18n()
const formatters = useFormatters()

// 今日统计：示例数据，与「经营指标」「销售趋势」同一套逐日数据的今天那一行
const today = dailySales(1, new Date())[0]!

const statCards = computed(() => [
  { key: 'sales', label: t('workbench.dashboard.stat_sales'), value: formatters.value.currency(today.sales), icon: 'lucide:banknote', color: '#3b82f6' },
  { key: 'orders', label: t('workbench.dashboard.stat_orders'), value: formatters.value.number(today.orders), icon: 'lucide:shopping-cart', color: '#22c55e' },
  { key: 'visitors', label: t('workbench.dashboard.stat_visitors'), value: formatters.value.number(today.visitors), icon: 'lucide:users', color: '#8b5cf6' },
  { key: 'conversion', label: t('workbench.dashboard.stat_conversion'), value: formatters.value.percent(today.orders / today.visitors), icon: 'lucide:target', color: '#f59e0b' },
])
</script>

<template>
  <WidgetCard icon="lucide:gauge" :title="t('workbench.widgets.stats.title')" demo>
    <!-- 按小组件自身宽度排布：极窄单列横排；中等两列、图标在上；宽到每格放得下横排时再横排，足够宽四列一行 -->
    <div class="grid grid-cols-1 gap-2 @[14rem]:grid-cols-2 @2xl:grid-cols-4 @2xl:gap-3">
      <div
        v-for="stat in statCards"
        :key="stat.key"
        class="flex min-w-0 items-center gap-3 rounded-xl border border-border/60 bg-background px-3 py-2.5 @[14rem]:flex-col @[14rem]:items-start @[14rem]:gap-2 @md:flex-row @md:items-center @md:gap-3"
      >
        <div
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl"
          :style="{ backgroundColor: `${stat.color}18` }"
        >
          <Icon :icon="stat.icon" width="20" height="20" :style="{ color: stat.color }" />
        </div>
        <div class="flex min-w-0 max-w-full flex-col gap-0.5">
          <span class="text-xl font-bold leading-tight text-foreground tabular-nums">{{ stat.value }}</span>
          <span class="truncate text-xs text-muted-foreground">{{ stat.label }}</span>
        </div>
      </div>
    </div>
  </WidgetCard>
</template>
