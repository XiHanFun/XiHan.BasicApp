<script lang="ts" setup>
import type { TreeSelectOption } from '~/types'
import { XhTreeSelectBranch, XhTreeSelectBranchContent, XhTreeSelectBranchControl, XhTreeSelectBranchText, XhTreeSelectBranchTrigger, XhTreeSelectItem, XhTreeSelectItemIndicator, XhTreeSelectItemText } from '@xihan-ui/vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'XTreeSelectNodes' })

defineProps<{
  nodes: TreeSelectOption[]
}>()
</script>

<template>
  <template v-for="node in nodes" :key="node.value">
    <XhTreeSelectBranch v-if="node.children?.length" :value="String(node.value)">
      <XhTreeSelectBranchControl>
        <XhTreeSelectBranchTrigger>
          <Icon icon="lucide:chevron-right" width="14" height="14" />
        </XhTreeSelectBranchTrigger>
        <XhTreeSelectBranchText>{{ node.label }}</XhTreeSelectBranchText>
      </XhTreeSelectBranchControl>
      <XhTreeSelectBranchContent>
        <XTreeSelectNodes :nodes="node.children" />
      </XhTreeSelectBranchContent>
    </XhTreeSelectBranch>
    <XhTreeSelectItem v-else :value="String(node.value)" :disabled="node.disabled">
      <XhTreeSelectItemIndicator>
        <Icon icon="lucide:check" width="14" height="14" />
      </XhTreeSelectItemIndicator>
      <XhTreeSelectItemText>{{ node.label }}</XhTreeSelectItemText>
    </XhTreeSelectItem>
  </template>
</template>
