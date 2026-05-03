<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { apiLogApi, createPageRequest } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemApiLogPage' })

interface LogGridResult {
  items: ApiLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ApiLogListItemDto>>()

const queryParams = reactive({
  isSuccess: undefined as boolean | undefined,
  keyword: '',
  method: undefined as string | undefined,
})

const successOptions = [
  { label: '成功', value: true },
  { label: '失败', value: false },
]

const methodOptions = [
  { label: 'GET', value: 'GET' },
  { label: 'POST', value: 'POST' },
  { label: 'PUT', value: 'PUT' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'PATCH', value: 'PATCH' },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return apiLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isSuccess: queryParams.isSuccess,
      keyword: queryParams.keyword?.trim() || undefined,
      method: queryParams.method,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询API日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<ApiLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户' },
      { field: 'apiPath', minWidth: 240, showOverflow: 'tooltip', title: '接口路径' },
      { field: 'controllerName', minWidth: 120, showOverflow: 'tooltip', title: '控制器' },
      { field: 'actionName', minWidth: 120, showOverflow: 'tooltip', title: '方法名' },
      { field: 'method', title: '请求方式', width: 90 },
      { field: 'statusCode', title: '状态码', width: 80 },
      {
        field: 'isSuccess',
        slots: { default: 'col_success' },
        title: '结果',
        width: 80,
      },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '耗时',
        width: 90,
      },
      {
        field: 'requestTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '请求时间',
      },
    ],
    id: 'sys_api_log',
    name: 'API日志',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.isSuccess = undefined
  queryParams.method = undefined
  xGrid.value?.commitProxy('reload')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索路径/用户"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.isSuccess"
          :options="successOptions"
          clearable
          placeholder="结果"
          style="width: 100px"
        />
        <NSelect
          v-model:value="queryParams.method"
          :options="methodOptions"
          clearable
          placeholder="请求方式"
          style="width: 110px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #col_success="{ row }">
          <NTag :type="row.isSuccess ? 'success' : 'error'" round size="small">
            {{ row.isSuccess ? '成功' : '失败' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
