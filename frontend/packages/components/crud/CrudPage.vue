<script setup lang="ts" generic="TList extends Record<string, unknown>, TQuery extends Partial<import('@/api').PageRequest>">
import type { DataTableColumns } from 'naive-ui'
import type { CrudResource, CrudSearchField } from './types'
import { NCard, NSkeleton } from 'naive-ui'
import { onMounted, ref } from 'vue'
import CrudSearchForm from './CrudSearchForm.vue'
import CrudTable from './CrudTable.vue'
import { useCrud } from './useCrud'

defineOptions({ name: 'CrudPage' })

const props = withDefaults(defineProps<{
  /** 资源 API */
  resource: CrudResource<TList, TQuery>
  /** 表格列定义 */
  columns: DataTableColumns<TList>
  /** 搜索字段 schema */
  searchFields?: CrudSearchField[]
  /** 行主键字段 */
  rowKey?: string
  /** 查询默认过滤值 */
  defaultQuery?: Partial<TQuery>
  /** 横向滚动宽度 */
  scrollX?: number
  /** 每页数量 */
  pageSize?: number
  /** 每页可选数量 */
  pageSizes?: number[]
}>(), {
  searchFields: () => [],
  rowKey: 'basicId',
  defaultQuery: undefined,
  scrollX: undefined,
  pageSize: 20,
  pageSizes: () => [10, 20, 50, 100],
})

const firstLoaded = ref(false)

const crud = useCrud<TList, TQuery>(props.resource, {
  defaultQuery: props.defaultQuery,
  pageSize: props.pageSize,
})

const { loading, rows, total, page, pageSize, filters, search, reset, changePage, changePageSize, remove } = crud

async function reload() {
  await crud.load()
}

onMounted(async () => {
  await crud.load()
  firstLoaded.value = true
})

defineExpose({ reload, remove })
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <CrudSearchForm
      v-if="searchFields.length"
      :fields="searchFields"
      :model="filters"
      @search="search"
      @reset="reset"
    />

    <NCard class="flex-1" style="height: 0">
      <NSkeleton v-if="!firstLoaded" :height="48" :repeat="5" text style="padding: 16px" />
      <template v-else>
        <div v-if="$slots.toolbar" class="flex items-center mb-3">
          <slot name="toolbar" :reload="reload" />
        </div>
        <CrudTable
          :columns="columns"
          :data="rows"
          :loading="loading"
          :page="page"
          :page-size="pageSize"
          :page-sizes="pageSizes"
          :row-key="rowKey"
          :scroll-x="scrollX"
          :total="total"
          @update:page="changePage"
          @update:page-size="changePageSize"
        />
      </template>
    </NCard>

    <slot :reload="reload" />
  </div>
</template>
