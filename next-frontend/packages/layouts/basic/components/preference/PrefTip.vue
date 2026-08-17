<script setup lang="ts">
import { XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'PrefTip' })
defineProps<{ content: string }>()
</script>

<template>
  <XhTooltipRoot placement="top">
    <XhTooltipTrigger class="tip-icon" :aria-label="content">
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
/* 提示浮层限宽，长文案折行而不是拉成一条 */
.tip-content {
  max-inline-size: 240px;
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
  transition: opacity 0.15s ease;
}

.tip-icon:hover {
  opacity: 1;
}
</style>
