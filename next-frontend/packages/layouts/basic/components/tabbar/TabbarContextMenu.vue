<script lang="ts" setup>
import type { AppDropdownOption } from '~/types'
import { XhContextMenuRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { indexDropdownOptions, toDropdownCollection, VNodeRender } from '~/components'

/**
 * 标签页右键菜单。
 *
 * 开合与落点由父组件给（它在标签上监听右键并记下坐标），所以这里不接管右键事件：
 * 触发区是一枚定位到该坐标的零尺寸锚点，菜单贴着它弹出，open 受控于 show。
 */
interface Props {
  show: boolean
  x: number
  y: number
  options: AppDropdownOption[]
}

const props = defineProps<Props>()

const emit = defineEmits<{
  select: [key: string]
  close: []
}>()

const collection = computed(() => toDropdownCollection(props.options))

const byKey = computed(() => indexDropdownOptions(props.options))
</script>

<template>
  <XhContextMenuRoot
    :collection="collection"
    :open="show"
    placement="bottom-start"
    @update:open="(open: boolean) => !open && emit('close')"
    @select="(details: { value: string }) => emit('select', details.value)"
  >
    <template #trigger>
      <span
        class="tabbar-context-anchor"
        aria-hidden="true"
        :style="{ insetInlineStart: `${x}px`, insetBlockStart: `${y}px` }"
      />
    </template>
    <template #item="node">
      <span class="tabbar-context-item">
        <span v-if="byKey.get(node.value)?.icon" class="tabbar-context-item__icon" aria-hidden="true">
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
  </XhContextMenuRoot>
</template>

<style scoped>
/* 锚点只用来定位，不可见也不吃指针 */
.tabbar-context-anchor {
  position: fixed;
  inline-size: 1px;
  block-size: 1px;
  pointer-events: none;
}

.tabbar-context-item {
  display: inline-flex;
  gap: 8px;
  align-items: center;
  min-inline-size: 0;
}

.tabbar-context-item__icon {
  display: inline-flex;
  flex: none;
  align-items: center;
  opacity: 0.8;
}
</style>
