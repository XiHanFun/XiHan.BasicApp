<script lang="ts" setup generic="V extends string">
import { XhToggleGroupItem, XhToggleGroupRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useControlAttrs } from './control-attrs'

/** 分段选择器：一组互斥选项排成按钮条，用于视图切换、格式切换这类小集合 */
defineOptions({ name: 'XSegmented', inheritAttrs: false })

const props = withDefaults(defineProps<{
  options: ReadonlyArray<{ label: string, value: V, disabled?: boolean }>
  size?: 'sm' | 'md' | 'lg'
  disabled?: boolean
}>(), {
  size: 'sm',
  disabled: false,
})

// 组本身就是控件，字段挂来的 id 与 aria-* 落在根上，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const value = defineModel<V>('value', { required: true })

const collection = computed(() => props.options.map(option => ({
  value: String(option.value),
  label: option.label,
  ...(option.disabled ? { disabled: true } : {}),
})))

/** 分段选择器不允许空选，机器给回 null 时保持原值 */
function onValueChange(details: { value: string | string[] | null }) {
  const next = Array.isArray(details.value) ? details.value[0] : details.value
  if (next !== null && next !== undefined)
    value.value = next as V
}
</script>

<template>
  <XhToggleGroupRoot
    v-bind="controlAttrs"
    class="x-segmented"
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="String(value)"
    :disabled="disabled"
    :size="size"
    disallow-empty
    @value-change="onValueChange"
  >
    <XhToggleGroupItem
      v-for="option in collection"
      :key="option.value"
      :value="option.value"
    >
      {{ option.label }}
    </XhToggleGroupItem>
  </XhToggleGroupRoot>
</template>
