<script lang="ts" setup>
import {
  XhTagsInputControl,
  XhTagsInputInput,
  XhTagsInputItem,
  XhTagsInputItemDeleteTrigger,
  XhTagsInputItemPreview,
  XhTagsInputItemText,
  XhTagsInputRoot,
} from '@xihan-ui/vue'
import { useControlAttrs } from './control-attrs'

/** 可增删的标签串：回车成条，退格/叉号删条 */
defineOptions({ name: 'XTagsInput', inheritAttrs: false })

withDefaults(defineProps<{
  placeholder?: string
  disabled?: boolean
  /** 最多几条 */
  max?: number
}>(), {
  placeholder: undefined,
  disabled: false,
  max: undefined,
})

// 字段挂来的 id 与 aria-* 转交给输入框，见 control-attrs.ts
const { attrs, controlAttrs } = useControlAttrs()

const value = defineModel<string[]>('value', { default: () => [] })
</script>

<template>
  <XhTagsInputRoot
    v-slot="{ value: items }"
    class="x-tags-input"
    :class="attrs.class"
    :style="attrs.style"
    :value="value"
    :disabled="disabled"
    :max="max"
    @update:value="(next: string[]) => (value = next)"
  >
    <XhTagsInputControl>
      <XhTagsInputItem v-for="item in items" :key="item" :value="item">
        <XhTagsInputItemPreview>
          <XhTagsInputItemText>{{ item }}</XhTagsInputItemText>
          <XhTagsInputItemDeleteTrigger />
        </XhTagsInputItemPreview>
      </XhTagsInputItem>
      <XhTagsInputInput :placeholder="placeholder" v-bind="controlAttrs" />
    </XhTagsInputControl>
  </XhTagsInputRoot>
</template>
