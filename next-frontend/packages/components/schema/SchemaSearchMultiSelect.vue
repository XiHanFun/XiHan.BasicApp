<script setup lang="ts">
import XSelect from '../common/XSelect.vue'

/**
 * 搜索多选下拉组件（封装：枚举/字典多选）。
 * - 值为选项值数组（string|number）或 null，受控 v-model:value。
 * - 标签过多时折叠，避免撑高搜索行。
 */
defineOptions({ name: 'SchemaSearchMultiSelect' })

defineProps<{
  /** 已选值数组 */
  value?: Array<string | number> | null
  /** 选项 */
  options?: ReadonlyArray<{ label: string, value: string | number }>
  /** 占位 */
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:value': [Array<string | number> | null]
}>()
</script>

<template>
  <XSelect
    multiple
    clearable
    :value="value ?? []"
    :options="options ?? []"
    size="sm"
    :max-tag-count="2"
    :placeholder="placeholder"
    @update:value="(v) => emit('update:value', (v as Array<string | number>))"
  />
</template>
