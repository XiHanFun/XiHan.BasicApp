<script setup lang="ts" generic="TRow extends object">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { ListFieldSchema } from './types'
import { NDatePicker, NInput, NSelect } from 'naive-ui'
import { computed } from 'vue'
import SchemaSearchDateRange from './SchemaSearchDateRange.vue'
import SchemaSearchMultiSelect from './SchemaSearchMultiSelect.vue'

/**
 * 搜索控件分发器：按字段 schema 选择渲染控件，统一绑定到 model[field.key]。
 * 区间(searchRange) → SchemaSearchDateRange；多选(searchMultiple) → SchemaSearchMultiSelect；
 * 枚举/标签/布尔(有 options) → 单选 NSelect；date/datetime → NDatePicker；其余 → NInput。
 */
defineOptions({ name: 'SchemaSearchField' })

const props = defineProps<{
  field: ListFieldSchema<TRow>
  model: Record<string, unknown>
}>()

const emit = defineEmits<{
  search: []
}>()

const placeholder = computed(() => props.field.searchPlaceholder ?? props.field.title)
const options = computed<SelectMixedOption[]>(() => (props.field.options as unknown as SelectMixedOption[] | undefined) ?? [])

const isRange = computed(() => !!props.field.searchRange && (props.field.dataType === 'date' || props.field.dataType === 'datetime'))
const isMulti = computed(() => !!props.field.searchMultiple && options.value.length > 0)
const isSelect = computed(() => (props.field.dataType === 'enum' || props.field.dataType === 'tag' || props.field.dataType === 'boolean') && options.value.length > 0)
const isDate = computed(() => props.field.dataType === 'date' || props.field.dataType === 'datetime')
</script>

<template>
  <SchemaSearchDateRange
    v-if="isRange"
    v-model:value="(model[field.key] as [number, number] | null)"
    :type="field.dataType === 'datetime' ? 'datetime' : 'date'"
    :placeholder="placeholder"
  />
  <SchemaSearchMultiSelect
    v-else-if="isMulti"
    v-model:value="(model[field.key] as Array<string | number> | null)"
    :options="options"
    :placeholder="placeholder"
  />
  <NSelect
    v-else-if="isSelect"
    v-model:value="(model[field.key] as string)"
    clearable
    size="small"
    :options="options"
    :placeholder="placeholder"
  />
  <NDatePicker
    v-else-if="isDate"
    v-model:value="(model[field.key] as number)"
    clearable
    size="small"
    class="w-full"
    :type="field.dataType === 'datetime' ? 'datetime' : 'date'"
    :placeholder="placeholder"
  />
  <NInput
    v-else
    v-model:value="(model[field.key] as string)"
    clearable
    size="small"
    :placeholder="placeholder"
    @keyup.enter="emit('search')"
  />
</template>
