<script setup lang="ts" generic="TRow extends object">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { ListFieldSchema } from './types'
import { NButton, NDatePicker, NIcon, NInput, NSelect } from 'naive-ui'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaSearchPanel' })

defineProps<{
  /** 常用搜索字段（由 selectors.toSearchFields 派生） */
  fields: ListFieldSchema<TRow>[]
  /** 过滤条件模型（来自 useSchemaTable.filters） */
  model: Record<string, unknown>
  /** 是否存在高级搜索字段（控制按钮显隐） */
  hasAdvanced?: boolean
}>()

const emit = defineEmits<{
  search: []
  reset: []
  openAdvanced: []
}>()

/** 选项断言：业务 SelectOption 与 Naive 选项结构兼容（空则回退空数组以满足非空类型） */
function asOptions(field: ListFieldSchema<TRow>): SelectMixedOption[] {
  return (field.options as unknown as SelectMixedOption[] | undefined) ?? []
}

/** 控件宽度：枚举/标签更窄，文本更宽 */
function controlWidth(field: ListFieldSchema<TRow>): number {
  if (field.dataType === 'enum' || field.dataType === 'tag' || field.dataType === 'boolean') {
    return 140
  }
  if (field.dataType === 'date' || field.dataType === 'datetime') {
    return 260
  }
  return 200
}
</script>

<template>
  <div class="xh-query-panel">
    <div class="xh-query-panel__content">
      <template v-for="field in fields" :key="field.key">
        <!-- 枚举/标签/布尔 → 下拉 -->
        <NSelect
          v-if="(field.dataType === 'enum' || field.dataType === 'tag' || field.dataType === 'boolean') && field.options"
          v-model:value="(model[field.key] as string)"
          clearable
          :options="asOptions(field)"
          :placeholder="field.searchPlaceholder ?? field.title"
          :style="{ width: `${controlWidth(field)}px` }"
        />
        <!-- 日期/时间 → 日期选择 -->
        <NDatePicker
          v-else-if="field.dataType === 'date' || field.dataType === 'datetime'"
          v-model:value="(model[field.key] as number)"
          clearable
          :type="field.dataType === 'datetime' ? 'datetime' : 'date'"
          :placeholder="field.searchPlaceholder ?? field.title"
          :style="{ width: `${controlWidth(field)}px` }"
        />
        <!-- 默认 → 文本 -->
        <NInput
          v-else
          v-model:value="(model[field.key] as string)"
          clearable
          :placeholder="field.searchPlaceholder ?? field.title"
          :style="{ width: `${controlWidth(field)}px` }"
          @keyup.enter="emit('search')"
        />
      </template>

      <NButton size="small" type="primary" @click="emit('search')">
        <template #icon>
          <NIcon><Icon icon="lucide:search" /></NIcon>
        </template>
        查询
      </NButton>
      <NButton size="small" @click="emit('reset')">
        <template #icon>
          <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
        </template>
        重置
      </NButton>
      <NButton v-if="hasAdvanced" size="small" quaternary @click="emit('openAdvanced')">
        <template #icon>
          <NIcon><Icon icon="lucide:sliders-horizontal" /></NIcon>
        </template>
        高级搜索
      </NButton>
    </div>
  </div>
</template>
