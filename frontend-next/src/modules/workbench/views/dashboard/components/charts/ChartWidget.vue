<script setup lang="ts">
import { XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { Icon, XChartInView } from '~/components'
import WidgetCard from '../WidgetCard.vue'

/**
 * 图表小组件外壳：右侧放范围、视图等切换（extra 插槽），正文放图表。
 * 取数中、出错、没有数据各给一个占位，图表只在有数据时挂上。
 * 图表高度经 --xh-chart-height 统一给：md 取中档视口高，lg 取高档视口高。
 * 正文包在 XChartInView 里：偏好开了视口内动画时，卡片滚进视口才挂上图表、才播入场。
 */
defineOptions({ name: 'ChartWidget' })

withDefaults(defineProps<{
  icon: string
  title: string
  size?: 'md' | 'lg'
  loading?: boolean
  error?: string | null
  empty?: boolean
}>(), {
  size: 'md',
  loading: false,
  error: null,
  empty: false,
})

const { t } = useI18n()
</script>

<template>
  <WidgetCard :icon="icon" :title="title">
    <template #extra>
      <slot name="extra" />
    </template>
    <div class="chart-widget" :data-size="size">
      <div v-if="loading" class="chart-placeholder" role="status">
        {{ t('common.loading') }}
      </div>
      <XhEmptyStateRoot v-else-if="error || empty" size="sm" class="chart-placeholder">
        <XhEmptyStateIndicator>
          <Icon :icon="error ? 'lucide:circle-alert' : 'lucide:inbox'" width="24" height="24" />
        </XhEmptyStateIndicator>
        <XhEmptyStateTitle>{{ error ? t('common.messages.load_failed') : t('common.no_data') }}</XhEmptyStateTitle>
        <XhEmptyStateDescription v-if="error">
          {{ error }}
        </XhEmptyStateDescription>
      </XhEmptyStateRoot>
      <XChartInView v-else>
        <slot />
      </XChartInView>
    </div>
  </WidgetCard>
</template>

<style scoped>
/*
 * 不裁切：原先为挡住组件库图表根末尾那张读屏数据表撑出的滚动条加过 overflow: clip，组件库已修好；
 * 而 next 起图表里可聚焦的图元把聚焦环画在盒外，靠边的扇区、柱子的环会被这一层裁掉
 */
.chart-widget {
  --xh-chart-height: var(--xh-viewport-h-md);

  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  min-inline-size: 0;
}

.chart-widget[data-size='lg'] {
  --xh-chart-height: var(--xh-viewport-h-lg);
}

/* 占位与图表同高，取数前后看板不跳 */
.chart-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-block-size: var(--xh-chart-height);
  color: var(--xh-fg-muted);
}
</style>
