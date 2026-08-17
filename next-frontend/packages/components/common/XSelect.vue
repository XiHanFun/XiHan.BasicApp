<script setup lang="ts">
import type { Size } from '@xihan-ui/kernel'
import {
  XhSelectClearTrigger,
  XhSelectContent,
  XhSelectControl,
  XhSelectIndicator,
  XhSelectItem,
  XhSelectItemIndicator,
  XhSelectItemText,
  XhSelectList,
  XhSelectPositioner,
  XhSelectRoot,
  XhSelectTrigger,
  XhSelectValueText,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useControlAttrs } from './control-attrs'

/**
 * 下拉选择。
 *
 * 存在的理由是两件组件库不管、而全站每个下拉都要做的事：
 * 1. 值类型。组件库的选中值一律是 `string[]`（单选也是长度 1 的数组），而业务里的选项值
 *    大量是数字（枚举）。这里按 String(value) 建映射，收上来再还原成原类型。
 * 2. 清除钮。它是一个要自己摆的部件，不是 prop。
 *
 * 其余能力（collection 铺开条目、placeholder、多选标签折叠）都由组件库给。
 */
defineOptions({ name: 'XSelect', inheritAttrs: false })

const props = withDefaults(defineProps<{
  /** 选项 */
  options?: ReadonlyArray<{ label: string, value: string | number, disabled?: boolean }>
  /** 选中值：单选传标量，多选传数组 */
  value?: string | number | Array<string | number> | null
  multiple?: boolean
  clearable?: boolean
  disabled?: boolean
  placeholder?: string
  size?: Size
  /** 多选标签超出几个后折叠 */
  maxTagCount?: number
}>(), {
  options: () => [],
  value: null,
  multiple: false,
  clearable: false,
  disabled: false,
  placeholder: undefined,
  size: 'sm',
  maxTagCount: undefined,
})

const emit = defineEmits<{
  'update:value': [value: string | number | Array<string | number> | null]
}>()

// 字段挂来的 id 与 aria-* 转交给触发器，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const collection = computed(() => props.options.map(option => ({
  value: String(option.value),
  label: option.label,
  ...(option.disabled ? { disabled: true } : {}),
})))

/** String(value) → 原始值，收上来时按此还原，数字选项不会变成字符串 */
const originalByKey = computed(() => new Map(props.options.map(option => [String(option.value), option.value])))

const selected = computed<string[]>(() => {
  if (props.value == null) {
    return []
  }
  return Array.isArray(props.value) ? props.value.map(String) : [String(props.value)]
})

function onValueChange(next: string[]): void {
  const restored = next.map(key => originalByKey.value.get(key) ?? key)
  if (props.multiple) {
    emit('update:value', restored)
    return
  }
  emit('update:value', restored[0] ?? null)
}
</script>

<template>
  <XhSelectRoot
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="selected"
    :multiple="multiple"
    :disabled="disabled"
    :placeholder="placeholder"
    :size="size"
    :max-tag-count="maxTagCount"
    @update:value="onValueChange"
  >
    <XhSelectControl>
      <XhSelectTrigger v-bind="controlAttrs">
        <XhSelectValueText />
        <XhSelectIndicator>▾</XhSelectIndicator>
      </XhSelectTrigger>
      <XhSelectClearTrigger v-if="clearable">
        ✕
      </XhSelectClearTrigger>
    </XhSelectControl>
    <XhSelectPositioner>
      <XhSelectContent>
        <XhSelectList>
          <XhSelectItem v-for="node in collection" :key="node.value" :value="node.value">
            <XhSelectItemText>{{ node.label }}</XhSelectItemText>
            <XhSelectItemIndicator>✓</XhSelectItemIndicator>
          </XhSelectItem>
        </XhSelectList>
      </XhSelectContent>
    </XhSelectPositioner>
  </XhSelectRoot>
</template>
