<script setup lang="ts">
import type { Placement } from '@xihan-ui/kernel'
import type { AppDropdownOption } from '~/types'
import { XhMenuRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { indexDropdownOptions, toDropdownCollection } from './dropdown-collection'
import { VNodeRender } from './VNodeRender'

/**
 * 下拉菜单：触发器经默认插槽传入，条目由 AppDropdownOption 描述。
 *
 * 存在的理由是条目里的图标与富标签——collection 只收纯串，图标和自定义标签得走 item 插槽，
 * 全站几处下拉（面包屑同级跳转、用户菜单、标签页右键菜单）没必要各摆一遍。
 */
defineOptions({ name: 'XDropdown' })

const props = withDefaults(defineProps<{
  options: AppDropdownOption[]
  placement?: Placement
}>(), {
  placement: 'bottom-end',
})

const emit = defineEmits<{ select: [key: string] }>()

const collection = computed(() => toDropdownCollection(props.options))

const byKey = computed(() => indexDropdownOptions(props.options))
</script>

<template>
  <!-- 触发器借用调用方给的那个元素（按钮/头像块），不再套一层 button -->
  <XhMenuRoot
    trigger-as-child
    :collection="collection"
    :placement="placement"
    @select="(details: { value: string }) => emit('select', details.value)"
  >
    <template #trigger>
      <slot />
    </template>
    <template #item="node">
      <span class="x-dropdown__item">
        <span v-if="byKey.get(node.value)?.icon" class="x-dropdown__icon" aria-hidden="true">
          <VNodeRender :content="byKey.get(node.value)!.icon!()" />
        </span>
        <VNodeRender
          v-if="typeof byKey.get(node.value)?.label === 'function'"
          :content="(byKey.get(node.value)!.label as () => never)()"
        />
        <template v-else>
          {{ node.label }}
        </template>
      </span>
    </template>
  </XhMenuRoot>
</template>

<style scoped>
.x-dropdown__item {
  display: inline-flex;
  gap: 8px;
  align-items: center;
  min-inline-size: 0;
}

.x-dropdown__icon {
  display: inline-flex;
  flex: none;
  align-items: center;
  opacity: 0.8;
}
</style>
