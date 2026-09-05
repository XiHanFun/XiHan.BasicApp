<script lang="ts" setup>
import type { TreeSelectOption } from '~/types'
import { XhTreeSelectBranch, XhTreeSelectBranchContent, XhTreeSelectBranchControl, XhTreeSelectBranchText, XhTreeSelectBranchTrigger, XhTreeSelectItem, XhTreeSelectItemIndicator, XhTreeSelectItemText } from '@xihan-ui/vue'

defineOptions({ name: 'XTreeSelectNodes' })

defineProps<{
  nodes: TreeSelectOption[]
}>()
</script>

<template>
  <template v-for="node in nodes" :key="node.value">
    <XhTreeSelectBranch v-if="node.children?.length" :value="String(node.value)">
      <XhTreeSelectBranchControl>
        <!-- 不写内容：字形由组件库出，展开时它自己翻转 -->
        <XhTreeSelectBranchTrigger />
        <XhTreeSelectBranchText>{{ node.label }}</XhTreeSelectBranchText>
      </XhTreeSelectBranchControl>
      <XhTreeSelectBranchContent>
        <XTreeSelectNodes :nodes="node.children" />
      </XhTreeSelectBranchContent>
    </XhTreeSelectBranch>
    <XhTreeSelectItem v-else :value="String(node.value)" :disabled="node.disabled">
      <!-- 不写内容：选中勾由组件库出 -->
      <XhTreeSelectItemIndicator />
      <XhTreeSelectItemText>{{ node.label }}</XhTreeSelectItemText>
    </XhTreeSelectItem>
  </template>
</template>
