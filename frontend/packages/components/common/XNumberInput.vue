<script setup lang="ts">
import type { Size } from '@xihan-ui/kernel'
import {
  XhNumberFieldControl,
  XhNumberFieldDecrementTrigger,
  XhNumberFieldIncrementTrigger,
  XhNumberFieldInput,
  XhNumberFieldRoot,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useControlAttrs } from './control-attrs'

/**
 * 数字输入。
 *
 * 组件库那侧收发的是**原始输入串**（空串即未填），而业务上下游一律是 `number | null`，
 * 换算收在这里；加减钮也是部件，按 showButton 决定摆不摆。
 */
defineOptions({ name: 'XNumberInput', inheritAttrs: false })

const props = withDefaults(defineProps<{
  value?: number | null
  min?: number
  max?: number
  step?: number
  placeholder?: string
  disabled?: boolean
  /** 是否显示加减钮，默认显示 */
  showButton?: boolean
  size?: Size
}>(), {
  value: null,
  min: undefined,
  max: undefined,
  step: undefined,
  placeholder: undefined,
  disabled: false,
  showButton: true,
  size: 'sm',
})

const emit = defineEmits<{
  'update:value': [value: number | null]
}>()

// 字段挂来的 id 与 aria-* 转交给输入框，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const text = computed(() => (props.value == null ? '' : String(props.value)))

function onValueChange(next: string): void {
  if (next.trim() === '') {
    emit('update:value', null)
    return
  }
  const parsed = Number(next)
  // 输入过程中会出现 '-'、'1.' 这类还不成数的中间态，此时不上抛
  if (Number.isFinite(parsed)) {
    emit('update:value', parsed)
  }
}
</script>

<template>
  <XhNumberFieldRoot
    :class="attrs.class"
    :style="attrs.style"
    :value="text"
    :min="min"
    :max="max"
    :step="step"
    :disabled="disabled"
    :size="size"
    @update:value="onValueChange"
  >
    <XhNumberFieldControl>
      <XhNumberFieldDecrementTrigger v-if="showButton">
        −
      </XhNumberFieldDecrementTrigger>
      <XhNumberFieldInput :placeholder="placeholder" v-bind="controlAttrs" />
      <XhNumberFieldIncrementTrigger v-if="showButton">
        +
      </XhNumberFieldIncrementTrigger>
    </XhNumberFieldControl>
  </XhNumberFieldRoot>
</template>
