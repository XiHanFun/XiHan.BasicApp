<script setup lang="ts">
import { XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'
import { Icon } from '~/iconify'

/**
 * 问号提示：一段说明收在一枚问号图标里，悬停或聚焦时浮出（偏好设置的各项、弹窗里字段名后面的说明都用它）
 * 提示文案是这段说明的唯一载体，所以触发器保持可聚焦
 * 代价是它进 tab 序列——不要把它放在浮层内容的最前面，组件库把程序化聚焦
 * 也当成展开信号，浮层一打开就会自动弹出提示盖住内容
 * 触发器带 data-help-tip：编辑弹窗选初始焦点时按它越过问号（见 edit-modal-focus）
 */
defineOptions({ name: 'XHelpTip' })
defineProps<{ content: string }>()
</script>

<template>
  <XhTooltipRoot placement="top">
    <XhTooltipTrigger class="tip-icon" data-help-tip :aria-label="content">
      <Icon icon="lucide:circle-help" :width="13" />
    </XhTooltipTrigger>
    <XhTooltipPositioner>
      <XhTooltipContent class="tip-content">
        <span class="whitespace-pre-line text-xs leading-relaxed">{{ content }}</span>
        <XhTooltipArrow />
      </XhTooltipContent>
    </XhTooltipPositioner>
  </XhTooltipRoot>
</template>

<style scoped>
/* 提示浮层限宽（7.5 个 8 号间距，合 240px），长文案折行而不是拉成一条 */
.tip-content {
  max-inline-size: calc(var(--xh-space-8) * 7.5);
}

.tip-icon {
  border: 0;
  background: transparent;
  display: inline-flex;
  align-items: center;
  color: hsl(var(--muted-foreground));
  opacity: 0.6;
  flex-shrink: 0;
  cursor: help;
  transition: opacity var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.tip-icon:hover {
  opacity: 1;
}
</style>
