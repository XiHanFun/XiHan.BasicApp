<script setup lang="ts">
import type { SelectOption } from '~/types'
import { NSelect } from 'naive-ui'

const props = withDefaults(defineProps<{
  modelValue?: Array<number | string>
  options: SelectOption[] | Array<Record<string, any>>
  placeholder?: string
  clearable?: boolean
  disabled?: boolean
  maxTagCount?: 'responsive' | number
}>(), {
  modelValue: () => [],
  placeholder: '请选择',
  clearable: true,
  disabled: false,
  maxTagCount: 'responsive',
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: Array<number | string>): void
}>()
</script>

<template>
  <NSelect
    multiple
    filterable
    :value="props.modelValue"
    :options="props.options as any"
    :placeholder="props.placeholder"
    :clearable="props.clearable"
    :disabled="props.disabled"
    :max-tag-count="props.maxTagCount"
    @update:value="(value) => emit('update:modelValue', (value ?? []) as Array<number | string>)"
  />
</template>
