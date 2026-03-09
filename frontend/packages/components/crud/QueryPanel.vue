<script setup lang="ts">
import type { CrudSearchField } from './types'
import { NButton, NCard, NCollapseTransition, NInput } from 'naive-ui'
import { computed, ref } from 'vue'
import EnumSelect from './EnumSelect.vue'
import MultiSelect from './MultiSelect.vue'

const props = withDefaults(defineProps<{
  modelValue: Record<string, any>
  fields: CrudSearchField[]
  title?: string
  defaultCollapsed?: boolean
}>(), {
  title: '查询条件',
  defaultCollapsed: false,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: Record<string, any>): void
  (e: 'search', value: Record<string, any>): void
  (e: 'reset', value: Record<string, any>): void
}>()

const collapsed = ref(props.defaultCollapsed)

const actionDisabled = computed(() => props.fields.length === 0)

function resolveWidth(width?: number | string) {
  if (typeof width === 'number') {
    return `${width}px`
  }
  if (typeof width === 'string' && width.trim().length > 0) {
    return width
  }
  return '220px'
}

function updateFieldValue(key: string, value: any) {
  emit('update:modelValue', {
    ...props.modelValue,
    [key]: value,
  })
}

function getResetValue(field: CrudSearchField, rawValue: unknown) {
  if (field.type === 'multi-select' || Array.isArray(rawValue)) {
    return []
  }
  if (typeof rawValue === 'string') {
    return ''
  }
  return undefined
}

function handleSearch() {
  emit('search', props.modelValue)
}

function handleReset() {
  const nextModel = { ...props.modelValue }
  for (const field of props.fields) {
    nextModel[field.key] = getResetValue(field, nextModel[field.key])
  }
  emit('update:modelValue', nextModel)
  emit('reset', nextModel)
}
</script>

<template>
  <NCard :bordered="false">
    <div class="mb-3 flex items-center gap-2">
      <span class="text-sm font-medium">{{ props.title }}</span>
      <NButton class="ml-auto" text type="primary" @click="collapsed = !collapsed">
        {{ collapsed ? '展开条件' : '收起条件' }}
      </NButton>
    </div>

    <NCollapseTransition :show="!collapsed">
      <div class="flex flex-wrap items-center gap-3">
        <div
          v-for="field in props.fields"
          :key="field.key"
          class="min-w-[140px]"
          :style="{ width: resolveWidth(field.width) }"
        >
          <NInput
            v-if="!field.type || field.type === 'input'"
            :value="props.modelValue[field.key]"
            :placeholder="field.placeholder || field.label || '请输入'"
            clearable
            v-bind="field.props"
            @keydown.enter="handleSearch"
            @update:value="(value) => updateFieldValue(field.key, value)"
          />
          <EnumSelect
            v-else-if="field.type === 'select'"
            :model-value="props.modelValue[field.key]"
            :placeholder="field.placeholder || field.label || '请选择'"
            :options="field.options ?? []"
            v-bind="field.props"
            @update:model-value="(value) => updateFieldValue(field.key, value)"
          />
          <MultiSelect
            v-else
            :model-value="props.modelValue[field.key] ?? []"
            :placeholder="field.placeholder || field.label || '请选择'"
            :options="field.options ?? []"
            v-bind="field.props"
            @update:model-value="(value) => updateFieldValue(field.key, value)"
          />
        </div>

        <div class="ml-auto flex items-center gap-2">
          <NButton type="primary" :disabled="actionDisabled" @click="handleSearch">
            搜索
          </NButton>
          <NButton :disabled="actionDisabled" @click="handleReset">
            重置
          </NButton>
          <slot name="actions" />
        </div>
      </div>
    </NCollapseTransition>
  </NCard>
</template>
