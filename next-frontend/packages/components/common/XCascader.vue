<script lang="ts" setup>
import type { CascaderNode } from '@xihan-ui/headless'
import type { TreeSelectOption } from '~/types'
import {
  XhCascaderClearTrigger,
  XhCascaderColumn,
  XhCascaderContent,
  XhCascaderItem,
  XhCascaderItemText,
  XhCascaderPositioner,
  XhCascaderRoot,
  XhCascaderTrigger,
  XhCascaderValueText,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useControlAttrs } from './control-attrs'

/**
 * 逐列下钻的层级选择器。
 * 机器的值是整条路径，这里对外只暴露末级的键——调用点绑的是单个 id。
 */
defineOptions({ name: 'XCascader', inheritAttrs: false })

const props = withDefaults(defineProps<{
  options?: TreeSelectOption[]
  placeholder?: string
  disabled?: boolean
  clearable?: boolean
}>(), {
  options: () => [],
  placeholder: undefined,
  disabled: false,
  clearable: false,
})

// 字段挂来的 id 与 aria-* 转交给触发器，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const value = defineModel<string | number | null>('value', { default: null })

function toNodes(options: ReadonlyArray<TreeSelectOption>): CascaderNode[] {
  return options.map(option => ({
    value: String(option.value),
    label: option.label,
    ...(option.disabled ? { disabled: true } : {}),
    ...(option.children?.length ? { children: toNodes(option.children) } : {}),
  }))
}

const collection = computed(() => toNodes(props.options))

/** 末级键 → 整条路径：机器要路径才能把各列定位到位 */
const pathValue = computed<string[]>(() => {
  if (value.value === null || value.value === undefined)
    return []
  const target = String(value.value)
  const walk = (nodes: CascaderNode[], trail: string[]): string[] | null => {
    for (const node of nodes) {
      const next = [...trail, node.value]
      if (node.value === target)
        return next
      const hit = node.children?.length ? walk(node.children, next) : null
      if (hit)
        return hit
    }
    return null
  }
  return walk(collection.value, []) ?? []
})

function onValueChange(details: { value: readonly string[] | readonly (readonly string[])[] }) {
  const path = (Array.isArray(details.value[0]) ? details.value[0] : details.value) as readonly string[]
  value.value = path.length ? path[path.length - 1]! : null
}
</script>

<template>
  <XhCascaderRoot
    v-slot="{ columns }"
    class="x-cascader"
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="pathValue"
    :disabled="disabled"
    @value-change="onValueChange"
  >
    <XhCascaderTrigger v-bind="controlAttrs">
      <XhCascaderValueText :placeholder="placeholder" />
      <XhCascaderClearTrigger v-if="clearable" />
    </XhCascaderTrigger>
    <XhCascaderPositioner>
      <XhCascaderContent>
        <XhCascaderColumn
          v-for="column in columns"
          :key="column.level"
          :level="column.level"
        >
          <XhCascaderItem
            v-for="node in column.items"
            :key="node.value"
            :value="node.value"
            :level="column.level"
          >
            <XhCascaderItemText>{{ node.label }}</XhCascaderItemText>
          </XhCascaderItem>
        </XhCascaderColumn>
      </XhCascaderContent>
    </XhCascaderPositioner>
  </XhCascaderRoot>
</template>
