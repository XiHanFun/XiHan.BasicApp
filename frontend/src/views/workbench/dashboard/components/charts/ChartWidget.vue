<script setup lang="ts">
import WidgetCard from '../WidgetCard.vue'

/**
 * 图表小组件外壳：标题行标出「示例数据」，右侧放范围、视图等切换（extra 插槽），正文放图表。
 * 图表高度经 --xh-chart-height 统一给：md 取中档视口高，lg 取高档视口高。
 */
defineOptions({ name: 'ChartWidget' })

withDefaults(defineProps<{
  icon: string
  title: string
  size?: 'md' | 'lg'
}>(), {
  size: 'md',
})
</script>

<template>
  <WidgetCard :icon="icon" :title="title" demo>
    <template #extra>
      <slot name="extra" />
    </template>
    <div class="chart-widget" :data-size="size">
      <slot />
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
</style>
