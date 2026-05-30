<script setup lang="ts" generic="TQuery extends Partial<import('@/api').PageRequest>">
// CRUD 列表页骨架：搜索区 + 工具栏插槽 + 表格 + 分页 + 默认插槽（承载页面自有弹窗/抽屉）。
// 列表项类型由页面侧 columns/resource 声明保证，脚手架内部仅透传，故用 any 行类型避免泛型逆变冲突。
import type { DataTableColumns } from 'naive-ui'
import type { CrudResource, CrudSearchField } from './types'
import { NCard, NSkeleton } from 'naive-ui'
import { onMounted, ref } from 'vue'
import CrudSearchForm from './CrudSearchForm.vue'
import CrudTable from './CrudTable.vue'
import { useCrud } from './useCrud'

defineOptions({ name: 'CrudPage' })

const props = withDefaults(defineProps<{
  /** 资源 API（列表项类型由页面侧声明，脚手架仅透传） */
  // eslint-disable-next-line ts/no-explicit-any
  resource: CrudResource<any, TQuery>
  /** 表格列定义。脚手架仅透传给表格，列表项类型由页面侧列声明保证，故此处放宽避免逆变冲突 */
  // eslint-disable-next-line ts/no-explicit-any
  columns: DataTableColumns<any>
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

// eslint-disable-next-line ts/no-explicit-any
const crud = useCrud<any, TQuery>(props.resource, {
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
