<script lang="ts" setup generic="V extends string">
import { XhSegmentedRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useControlAttrs } from './control-attrs'

/** 分段选择器：一组互斥选项排成按钮条，用于视图切换、格式切换这类小集合 */
defineOptions({ name: 'XSegmented', inheritAttrs: false })

const props = withDefaults(defineProps<{
  options: ReadonlyArray<{ label: string, value: V, disabled?: boolean }>
  size?: 'sm' | 'md' | 'lg'
  disabled?: boolean
  /** 铺满容器宽度，各段等分 */
  block?: boolean
}>(), {
  size: 'sm',
  disabled: false,
  block: false,
})

// 组本身就是控件，字段挂来的 id 与 aria-* 落在根上，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const value = defineModel<V>('value', { required: true })

const collection = computed(() => props.options.map(option => ({
  value: String(option.value),
  label: option.label,
  ...(option.disabled ? { disabled: true } : {}),
})))

/** 选不空，机器给回 null 时保持原值 */
function onValueChange(details: { value: string | null }) {
  if (details.value != null) {
    value.value = details.value as V
  }
}
</script>

<template>
  <!-- 条目由 collection 代铺，滑动指示器与连体皮肤归组件库 -->
  <XhSegmentedRoot
    v-bind="controlAttrs"
    :class="attrs.class"
    :style="attrs.style"
    :collection="collection"
    :value="String(value)"
    :disabled="disabled"
    :size="size"
    :block="block"
    @value-change="onValueChange"
  />
</template>
