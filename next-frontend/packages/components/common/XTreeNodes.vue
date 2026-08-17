<script lang="ts" setup>
import type { TreeNode } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import {
  XhTreeBranch,
  XhTreeBranchContent,
  XhTreeBranchControl,
  XhTreeBranchIndicator,
  XhTreeBranchText,
  XhTreeItem,
  XhTreeItemIndicator,
  XhTreeItemText,
} from '@xihan-ui/vue'
import { VNodeRender } from './VNodeRender'

/** 树节点的递归渲染：分支自带展开把手，叶子只报选中指示 */
defineOptions({ name: 'XTreeNodes' })

defineProps<{
  nodes: ReadonlyArray<TreeNode>
  /** 逐节点自定义标签；不给就用 label */
  renderLabel?: (node: Record<string, unknown>) => VNodeChild
}>()
</script>

<template>
  <template v-for="node in nodes" :key="node.value">
    <XhTreeBranch v-if="node.children?.length" :value="String(node.value)">
      <XhTreeBranchControl>
        <XhTreeBranchIndicator>▸</XhTreeBranchIndicator>
        <XhTreeBranchTrigger>
          <XhTreeBranchText>
            <VNodeRender v-if="renderLabel" :content="renderLabel(node as unknown as Record<string, unknown>)" />
            <template v-else>
              {{ node.label }}
            </template>
          </XhTreeBranchText>
        </XhTreeBranchTrigger>
      </XhTreeBranchControl>
      <XhTreeBranchContent>
        <XTreeNodes :nodes="node.children" :render-label="renderLabel" />
      </XhTreeBranchContent>
    </XhTreeBranch>

    <XhTreeItem v-else :value="String(node.value)" :disabled="node.disabled">
      <XhTreeItemIndicator>✓</XhTreeItemIndicator>
      <XhTreeItemText>
        <VNodeRender v-if="renderLabel" :content="renderLabel(node as unknown as Record<string, unknown>)" />
        <template v-else>
          {{ node.label }}
        </template>
      </XhTreeItemText>
    </XhTreeItem>
  </template>
</template>
