<script lang="ts" setup>
import type { TreeNode } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import { XhTreeRoot, XhTreeTree } from '@xihan-ui/vue'
import { computed } from 'vue'
import XTreeNodes from './XTreeNodes.vue'

/** 层级列表：分支可展开，叶子可选。选中集合与展开集合都双向绑定 */
defineOptions({ name: 'XTree' })

const props = withDefaults(defineProps<{
  /** 节点除键与文本外可挂业务字段，渲染标签时原样拿得到 */
  data: ReadonlyArray<{ value: string | number, label?: string, children?: readonly unknown[] }>
  /** single 单选；multiple 多选（带勾选指示，对应旧的 checkable） */
  selectionMode?: 'single' | 'multiple'
  /** 逐节点自定义标签 */
  renderLabel?: (node: Record<string, unknown>) => VNodeChild
  /** 点分支文字即展开，不必点把手 */
  expandOnClick?: boolean
}>(), {
  selectionMode: 'single',
  renderLabel: undefined,
  expandOnClick: true,
})

const selectedKeys = defineModel<string[]>('selectedKeys', { default: () => [] })
const expandedKeys = defineModel<string[]>('expandedKeys', { default: () => [] })

/** 机器的节点只认字符串键，逐层归一；业务字段随节点一起带下去 */
function toNodes(options: ReadonlyArray<unknown>): TreeNode[] {
  return options.map((raw) => {
    const option = raw as Record<string, unknown>
    const children = option.children as unknown[] | undefined
    return {
      ...option,
      value: String(option.value),
      ...(children?.length ? { children: toNodes(children) } : {}),
    } as TreeNode
  })
}

const collection = computed(() => toNodes(props.data))
</script>

<template>
  <XhTreeRoot
    class="x-tree"
    :collection="collection"
    :selection-mode="selectionMode"
    :selected-value="selectedKeys"
    :expanded-value="expandedKeys"
    :expand-on-click="expandOnClick"
    @update:selected-value="(value: string[]) => (selectedKeys = value)"
    @update:expanded-value="(value: string[]) => (expandedKeys = value)"
  >
    <XhTreeTree>
      <XTreeNodes :nodes="collection" :render-label="renderLabel" />
    </XhTreeTree>
  </XhTreeRoot>
</template>
