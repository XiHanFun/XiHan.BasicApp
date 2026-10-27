<script lang="ts" setup>
import type { Placement } from '@xihan-ui/core'
import type { VNodeArrayChildren, VNodeChild } from 'vue'
import { XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'
import { Comment, Fragment, isVNode, Text, useSlots } from 'vue'

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

const slots = useSlots()

/**
 * 插槽里还有没有东西可挂触发器：唯一的子节点被 v-if 收掉时插槽只剩注释占位，
 * asChild 拿到 0 个节点会直接抛错，这时连说明一起不装。
 * 口径与 asChild 相同（注释、空白文本、片段外壳不算），非空文本仍算一个，交给 asChild 报错。
 * 在渲染里调用插槽，插槽内的依赖记在本组件名下，v-if 翻转时这里跟着重判。
 */
function hasMountableChild(nodes: VNodeArrayChildren | VNodeChild | undefined): boolean {
  if (nodes == null || typeof nodes === 'boolean')
    return false
  if (Array.isArray(nodes))
    return nodes.some(node => hasMountableChild(node))
  if (!isVNode(nodes))
    return String(nodes).trim() !== ''
  if (nodes.type === Comment)
    return false
  if (nodes.type === Text)
    return String(nodes.children ?? '').trim() !== ''
  if (nodes.type === Fragment)
    return hasMountableChild(nodes.children as VNodeArrayChildren)
  return true
}
</script>

<template>
  <slot v-if="disabled || !content || !hasMountableChild(slots.default?.())" />
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
