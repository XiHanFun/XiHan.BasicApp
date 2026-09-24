<script lang="ts" setup>
import type { TreeNode } from '@xihan-ui/headless'
import type { VNodeChild } from 'vue'
import {
  XhTreeBranch,
  XhTreeBranchContent,
  XhTreeBranchControl,
  XhTreeBranchText,
  XhTreeBranchTrigger,
  XhTreeItem,
  XhTreeItemIndicator,
  XhTreeItemText,
} from '@xihan-ui/vue'
import { VNodeRender } from './VNodeRender'

/**
 * 树节点的递归渲染：分支自带展开把手；选中统一在行尾画对号。
 * 单选、多选与级联同一写法，半选的横杠也由指示位按机器状态画出。
 */
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
      <!-- 触发器只包箭头：把文字也包进去，点目录名就只剩展开、选不中这一枝 -->
      <XhTreeBranchControl>
        <!-- 箭头对读屏是隐藏的，别让指针把焦点落上去：库要到 click 才把焦点交还给分支 -->
        <XhTreeBranchTrigger @mousedown.prevent />
        <XhTreeBranchText>
          <VNodeRender v-if="renderLabel" :content="renderLabel(node as unknown as Record<string, unknown>)" />
          <template v-else>
            {{ node.label }}
          </template>
        </XhTreeBranchText>
        <XhTreeItemIndicator />
      </XhTreeBranchControl>
      <XhTreeBranchContent>
        <XTreeNodes :nodes="node.children" :render-label="renderLabel" />
      </XhTreeBranchContent>
    </XhTreeBranch>

    <XhTreeItem v-else :value="String(node.value)">
      <XhTreeItemText>
        <VNodeRender v-if="renderLabel" :content="renderLabel(node as unknown as Record<string, unknown>)" />
        <template v-else>
          {{ node.label }}
        </template>
      </XhTreeItemText>
      <XhTreeItemIndicator />
    </XhTreeItem>
  </template>
</template>
