<script setup lang="ts">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { ListFieldSchema } from './types'
import { NButton, NDatePicker, NDrawer, NDrawerContent, NForm, NFormItem, NInput, NSelect, NSpace } from 'naive-ui'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaAdvancedSearch' })

defineProps<{
  /** 是否显示 */
  show: boolean
  /** 高级搜索字段（由 selectors.toAdvancedFields 派生） */
  fields: ListFieldSchema[]
  /** 过滤条件模型（与表格共享的 filters） */
  model: Record<string, unknown>
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'search': []
  'reset': []
}>()

function asOptions(field: ListFieldSchema): SelectMixedOption[] {
  return (field.options as unknown as SelectMixedOption[] | undefined) ?? []
}

function isSelect(field: ListFieldSchema): boolean {
  return (field.dataType === 'enum' || field.dataType === 'tag' || field.dataType === 'boolean') && !!field.options
}

function isDate(field: ListFieldSchema): boolean {
  return field.dataType === 'date' || field.dataType === 'datetime'
}

function onSearch() {
  emit('search')
  emit('update:show', false)
}
</script>

<template>
  <NDrawer
    :show="show"
    :height="320"
    placement="top"
    @update:show="(value) => emit('update:show', value)"
  >
    <NDrawerContent title="高级搜索" closable>
      <NForm label-placement="top" :show-feedback="false">
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <NFormItem v-for="field in fields" :key="field.key" :label="field.title">
            <NSelect
              v-if="isSelect(field)"
              v-model:value="(model[field.key] as string)"
              clearable
              :options="asOptions(field)"
              :placeholder="field.searchPlaceholder ?? field.title"
            />
            <NDatePicker
              v-else-if="isDate(field)"
              v-model:value="(model[field.key] as number)"
              clearable
              :type="field.dataType === 'datetime' ? 'datetime' : 'date'"
              :placeholder="field.searchPlaceholder ?? field.title"
              class="w-full"
            />
            <NInput
              v-else
              v-model:value="(model[field.key] as string)"
              clearable
              :placeholder="field.searchPlaceholder ?? field.title"
              @keyup.enter="onSearch"
            />
          </NFormItem>
        </div>
      </NForm>

      <template #footer>
        <NSpace>
          <NButton @click="emit('reset')">
            重置
          </NButton>
          <NButton type="primary" @click="onSearch">
            <template #icon>
              <Icon icon="lucide:search" />
            </template>
            查询
          </NButton>
        </NSpace>
      </template>
    </NDrawerContent>
  </NDrawer>
</template>
