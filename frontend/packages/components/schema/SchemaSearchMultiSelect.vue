<script setup lang="ts">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import { NSelect } from 'naive-ui'

/**
 * 搜索多选下拉组件（封装：枚举/字典多选）。
 * - 值为选项值数组（string|number）或 null，受控 v-model:value。
 * - 标签过多时按容器宽度折叠（max-tag-count=responsive），避免撑高搜索行。
 */
defineOptions({ name: 'SchemaSearchMultiSelect' })

defineProps<{
  /** 已选值数组 */
  value?: Array<string | number> | null
  /** 选项 */
  options?: SelectMixedOption[]
  /** 占位 */
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:value': [Array<string | number> | null]
}>()
</script>

<template>
  <NSelect
    multiple
    :value="value ?? null"
    :options="options ?? []"
    clearable
    size="small"
    max-tag-count="responsive"
    :placeholder="placeholder"
    @update:value="(v) => emit('update:value', (v as Array<string | number> | null))"
  />
</template>
