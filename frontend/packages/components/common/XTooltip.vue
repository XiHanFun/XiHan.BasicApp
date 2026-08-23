<script lang="ts" setup>
import type { Placement } from '@xihan-ui/kernel'
import { XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'

/** 悬停说明：包住任意一个元素或组件（按钮、开关、图标），它自己就是触发器 */
defineOptions({ name: 'XTooltip' })

withDefaults(defineProps<{
  /** 说明文案 */
  content: string
  placement?: Placement
  /** 文案为空时不装触发器，被包的元素照常渲染 */
  disabled?: boolean
}>(), {
  placement: undefined,
  disabled: false,
})
</script>

<template>
  <slot v-if="disabled || !content" />
  <XhTooltipRoot v-else :placement="placement">
    <XhTooltipTrigger as-child>
      <slot />
    </XhTooltipTrigger>
    <XhTooltipPositioner>
      <XhTooltipContent>
        {{ content }}
        <XhTooltipArrow />
      </XhTooltipContent>
    </XhTooltipPositioner>
  </XhTooltipRoot>
</template>
