<script setup lang="ts">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { CrudSearchField } from './types'
import { NButton, NIcon, NInput, NSelect } from 'naive-ui'
import { Icon } from '~/iconify'

defineOptions({ name: 'CrudSearchForm' })

defineProps<{
  /** 搜索字段 schema */
  fields: CrudSearchField[]
  /** 查询过滤对象（由 useCrud 提供的 reactive filters） */
  model: Record<string, unknown>
}>()

const emit = defineEmits<{
  search: []
  reset: []
}>()
</script>

<template>
  <div class="xh-query-panel">
    <div class="xh-query-panel__content">
      <template v-for="field in fields" :key="field.field">
        <NInput
          v-if="(field.type ?? 'input') === 'input'"
          v-model:value="(model[field.field] as string)"
          clearable
          :placeholder="field.placeholder"
          :style="{ width: `${field.width ?? 200}px` }"
          @keyup.enter="emit('search')"
        />
        <NSelect
          v-else-if="field.type === 'select'"
          v-model:value="(model[field.field] as string)"
          clearable
          :options="(field.options as SelectMixedOption[] | undefined)"
          :placeholder="field.placeholder"
          :style="{ width: `${field.width ?? 160}px` }"
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
    </div>
  </div>
</template>
