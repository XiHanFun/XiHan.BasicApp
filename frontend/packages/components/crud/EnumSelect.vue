<script setup lang="ts">
import type { SelectOption } from '~/types'
import { NSelect } from 'naive-ui'

const props = withDefaults(defineProps<{
  modelValue?: number | string
  options: SelectOption[] | Array<Record<string, any>>
  placeholder?: string
  clearable?: boolean
  disabled?: boolean
}>(), {
  modelValue: undefined,
  placeholder: '请选择',
  clearable: true,
  disabled: false,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: number | string | undefined): void
}>()
</script>

<template>
  <NSelect
    :value="props.modelValue"
    :options="props.options as any"
    :placeholder="props.placeholder"
    :clearable="props.clearable"
    :disabled="props.disabled"
    @update:value="(value) => emit('update:modelValue', value as number | string | undefined)"
  />
</template>
