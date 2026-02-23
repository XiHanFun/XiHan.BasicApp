<script setup lang="ts" generic="T extends { id: string | number }">
import { NCard, NDataTable, NPagination } from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'

defineOptions({ name: 'PageQueryTable' })

const props = defineProps<{
  title?: string
  loading?: boolean
  columns: DataTableColumns<T>
  data: T[]
  total: number
  page: number
  pageSize: number
}>()

const emit = defineEmits<{
  pageChange: [page: number]
  pageSizeChange: [size: number]
}>()
</script>

<template>
  <NCard :title="props.title" :bordered="false">
    <NDataTable
      :columns="props.columns"
      :data="props.data"
      :loading="props.loading"
      :row-key="(row: T) => row.id"
      :pagination="false"
      striped
    />
    <div class="mt-4 flex justify-end">
      <NPagination
        :page="props.page"
        :page-size="props.pageSize"
        :item-count="props.total"
        :page-sizes="[10, 20, 50, 100]"
        show-size-picker
        @update:page="emit('pageChange', $event)"
        @update:page-size="emit('pageSizeChange', $event)"
      />
    </div>
  </NCard>
</template>
