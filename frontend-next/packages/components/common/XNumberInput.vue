<script setup lang="ts">
import type { Size } from '@xihan-ui/core'
import type { NumberDraft } from './number-input-value'
import {
  XhNumberFieldControl,
  XhNumberFieldDecrementTrigger,
  XhNumberFieldIncrementTrigger,
  XhNumberFieldInput,
  XhNumberFieldPrefix,
  XhNumberFieldRoot,
  XhNumberFieldSuffix,
} from '@xihan-ui/vue'
import { computed, shallowRef, useSlots } from 'vue'
import { useControlAttrs } from './control-attrs'
import {
  assertPrecision,
  defaultStep,
  formatNumber,
  parseNumber,
  resolveDisplayText,
  toModelValue,
} from './number-input-value'

/**
 * 数字输入。
 *
 * 组件库那侧收发的是**原始输入串**（空串即未填），而业务上下游一律是 `number | null`，
 * 换算收在 number-input-value.ts；加减钮也是部件，按 showButton 决定摆不摆。
 * 钮里不放字：皮肤在空钮上画加减字形，放了字就盖掉了。
 */
defineOptions({ name: 'XNumberInput', inheritAttrs: false })

const props = withDefaults(defineProps<{
  value?: number | null
  min?: number
  max?: number
  /** 不写时随 precision 走最小一位（2 位小数即 0.01），都不写为 1 */
  step?: number
  /** 固定小数位：上抛的值按位数回舍，显示补齐到这么多位；整数字段写 0 */
  precision?: number
  placeholder?: string
  /** 三态不写时随外层 Field / Form 走；写了以本处为准 */
  disabled?: boolean
  readOnly?: boolean
  invalid?: boolean
  /** 是否显示加减钮，默认显示 */
  showButton?: boolean
  size?: Size
}>(), {
  value: null,
  min: undefined,
  max: undefined,
  step: undefined,
  precision: undefined,
  placeholder: undefined,
  disabled: undefined,
  readOnly: undefined,
  invalid: undefined,
  showButton: true,
  size: 'sm',
})

const emit = defineEmits<{
  'update:value': [value: number | null]
}>()

const slots = useSlots()
// 字段挂来的 id 与 aria-* 转交给输入框，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

/** 给了位数时显示与读回成对换算，两个方向互逆，按一下加号值不会漂 */
const codec = computed(() => {
  const precision = props.precision
  assertPrecision(precision)
  if (precision === undefined)
    return undefined
  return {
    parse: parseNumber,
    format: (value: number) => formatNumber(value, precision),
  }
})

const resolvedStep = computed(() => props.step ?? defaultStep(props.precision))

/** 用户正在打的那一串：'-0'、'1.' 这类中途写法由它保住，不被值的格式化改掉 */
const draft = shallowRef<NumberDraft | null>(null)

const text = computed(() => resolveDisplayText(props.value, draft.value, props.precision))

function onValueChange(next: string): void {
  const value = toModelValue(next, props.precision)
  // 还不成数的中间态不上抛，模型值保持原样，只留住框里的字
  if (value === undefined) {
    draft.value = { text: next, value: props.value }
    return
  }
  draft.value = { text: next, value }
  emit('update:value', value)
}
</script>

<template>
  <XhNumberFieldRoot
    :class="attrs.class"
    :style="attrs.style"
    :value="text"
    :min="min"
    :max="max"
    :step="resolvedStep"
    :parse="codec?.parse"
    :format="codec?.format"
    :disabled="disabled"
    :read-only="readOnly"
    :invalid="invalid"
    :size="size"
    @update:value="onValueChange"
  >
    <XhNumberFieldControl>
      <XhNumberFieldDecrementTrigger v-if="showButton" />
      <XhNumberFieldPrefix v-if="slots.prefix">
        <slot name="prefix" />
      </XhNumberFieldPrefix>
      <XhNumberFieldInput :placeholder="placeholder" v-bind="controlAttrs" />
      <XhNumberFieldSuffix v-if="slots.suffix">
        <slot name="suffix" />
      </XhNumberFieldSuffix>
      <XhNumberFieldIncrementTrigger v-if="showButton" />
    </XhNumberFieldControl>
  </XhNumberFieldRoot>
</template>
