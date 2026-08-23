<script setup lang="ts" generic="TRow extends object">
import type { ListFieldSchema } from './types'
import { computed } from 'vue'
import XDatePicker from '../common/XDatePicker.vue'
import XInput from '../common/XInput.vue'
import XSelect from '../common/XSelect.vue'
import SchemaSearchDateRange from './SchemaSearchDateRange.vue'
import SchemaSearchMultiSelect from './SchemaSearchMultiSelect.vue'

/**
 * 搜索控件分发器：按字段 schema 选择渲染控件，统一绑定到 model[field.key]。
 * 区间(searchRange) → SchemaSearchDateRange；多选(searchMultiple) → SchemaSearchMultiSelect；
 * 枚举/标签/布尔(有 options) → 单选下拉；date/datetime → 日期选择；其余 → 文本输入。
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
const options = computed(() => props.field.options ?? [])

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
  <XSelect
    v-else-if="isSelect"
    v-model:value="(model[field.key] as string | number | null)"
    clearable
    size="sm"
    :options="options"
    :placeholder="placeholder"
  />
  <XDatePicker
    v-else-if="isDate"
    v-model:value="(model[field.key] as number | null)"
    clearable
    size="sm"
    class="w-full"
    :placeholder="placeholder"
  />
  <XInput
    v-else
    v-model:value="(model[field.key] as string)"
    clearable
    size="sm"
    :placeholder="placeholder"
    @enter="emit('search')"
  />
</template>
