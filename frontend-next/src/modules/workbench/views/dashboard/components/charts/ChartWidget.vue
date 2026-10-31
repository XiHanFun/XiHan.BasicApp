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
 * 图表根末尾追加的读屏数据表是绝对定位的视觉隐藏表格，但表格的 block-size 只当最小高度、overflow 对表格也不生效，
 * 它实际有几百上千像素高，照样撑出小组件内容区的滚动条。这里把图表之外的溢出裁掉：
 * 提示框靠边会翻转、始终在图表之内，焦点环向内描（ring-offset 为负），都裁不到。根因在组件库，修好后可去掉 overflow。
 */
.chart-widget {
  --xh-chart-height: var(--xh-viewport-h-md);

  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  min-inline-size: 0;
  overflow: clip;
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
