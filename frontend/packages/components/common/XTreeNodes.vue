<script lang="ts" setup>
import type { TreeNode } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import {
  XhTreeBranch,
  XhTreeBranchCheckbox,
  XhTreeBranchContent,
  XhTreeBranchControl,

  XhTreeBranchText,
  XhTreeItem,
  XhTreeItemCheckbox,
  XhTreeItemIndicator,
  XhTreeItemText,
} from '@xihan-ui/vue'
import { VNodeRender } from './VNodeRender'

/** 树节点的递归渲染：分支自带展开把手；多选档每个节点前出复选框，单选档只报选中指示 */
defineOptions({ name: 'XTreeNodes' })

defineProps<{
  nodes: ReadonlyArray<TreeNode>
  /** 多选档：节点前渲染复选框（含级联半选态） */
  checkable?: boolean
  /** 逐节点自定义标签；不给就用 label */
  renderLabel?: (node: Record<string, unknown>) => VNodeChild
}>()
</script>

<template>
  <template v-for="node in nodes" :key="node.value">
    <XhTreeBranch v-if="node.children?.length" :value="String(node.value)">
      <!-- 触发器只包箭头：把文字也包进去，点目录名就只剩展开、选不中这一枝 -->
      <XhTreeBranchControl>
        <!-- 箭头对读屏是隐藏的，别让指针把焦点落上去：库要到 click 才把焦点交还给分支 -->
        <XhTreeBranchTrigger @mousedown.prevent />
        <XhTreeBranchCheckbox v-if="checkable" @mousedown.prevent />
        <XhTreeBranchText>
          <VNodeRender v-if="renderLabel" :content="renderLabel(node as unknown as Record<string, unknown>)" />
          <template v-else>
            {{ node.label }}
          </template>
        </XhTreeBranchText>
      </XhTreeBranchControl>
      <XhTreeBranchContent>
        <XTreeNodes :nodes="node.children" :checkable="checkable" :render-label="renderLabel" />
      </XhTreeBranchContent>
    </XhTreeBranch>

    <XhTreeItem v-else :value="String(node.value)" :disabled="node.disabled">
      <XhTreeItemCheckbox v-if="checkable" @mousedown.prevent />
      <XhTreeItemIndicator v-else />
      <XhTreeItemText>
        <VNodeRender v-if="renderLabel" :content="renderLabel(node as unknown as Record<string, unknown>)" />
        <template v-else>
          {{ node.label }}
        </template>
      </XhTreeItemText>
    </XhTreeItem>
  </template>
</template>
