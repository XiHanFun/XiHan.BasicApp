<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { AccessLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { accessLogApi, AccessResult, createPageRequest } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemAccessLogPage' })

interface LogGridResult {
  items: AccessLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<AccessLogListItemDto>>()

const queryParams = reactive({
  accessResult: undefined as AccessResult | undefined,
  keyword: '',
  method: undefined as string | undefined,
})

const accessResultOptions = [
  { label: '成功', value: AccessResult.Success },
  { label: '失败', value: AccessResult.Failed },
  { label: '权限不足', value: AccessResult.Forbidden },
  { label: '未授权', value: AccessResult.Unauthorized },
  { label: '资源不存在', value: AccessResult.NotFound },
  { label: '服务器错误', value: AccessResult.ServerError },
]

const methodOptions = [
  { label: 'GET', value: 'GET' },
  { label: 'POST', value: 'POST' },
  { label: 'PUT', value: 'PUT' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'PATCH', value: 'PATCH' },
]

function accessResultType(result: AccessResult) {
  switch (result) {
    case AccessResult.Success: return 'success'
    case AccessResult.Failed: return 'error'
    case AccessResult.Forbidden:
    case AccessResult.Unauthorized: return 'warning'
    default: return 'default'
  }
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return accessLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      accessResult: queryParams.accessResult,
      keyword: queryParams.keyword?.trim() || undefined,
      method: queryParams.method,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询访问日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<AccessLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户' },
      { field: 'resourcePath', minWidth: 240, showOverflow: 'tooltip', title: '资源路径' },
      { field: 'resourceName', minWidth: 120, showOverflow: 'tooltip', title: '资源名称' },
      { field: 'method', title: '方法', width: 80 },
      { field: 'statusCode', title: '状态码', width: 80 },
      {
        field: 'accessResult',
        slots: { default: 'col_result' },
        title: '结果',
        width: 100,
      },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '耗时',
        width: 90,
      },
      {
        field: 'accessTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '访问时间',
      },
    ],
    id: 'sys_access_log',
    name: '访问日志',
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
  queryParams.accessResult = undefined
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
          v-model:value="queryParams.accessResult"
          :options="accessResultOptions"
          clearable
          placeholder="访问结果"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.method"
          :options="methodOptions"
          clearable
          placeholder="方法"
          style="width: 100px"
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
        <template #col_result="{ row }">
          <NTag :type="accessResultType(row.accessResult)" round size="small">
            {{ getOptionLabel(accessResultOptions, row.accessResult) }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
