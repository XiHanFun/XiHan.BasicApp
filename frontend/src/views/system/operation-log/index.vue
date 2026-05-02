<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { OperationLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  operationLogApi,
  OperationType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOperationLogPage' })

interface LogGridResult {
  items: OperationLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<OperationLogListItemDto>>()

const queryParams = reactive({
  keyword: '',
  operationType: undefined as OperationType | undefined,
  status: undefined as EnableStatus | undefined,
})

const statusOptions = [
  { label: '成功', value: EnableStatus.Enabled },
  { label: '失败', value: EnableStatus.Disabled },
]

const operationTypeOptions = [
  { label: '登录', value: OperationType.Login },
  { label: '登出', value: OperationType.Logout },
  { label: '查询', value: OperationType.Query },
  { label: '新增', value: OperationType.Create },
  { label: '修改', value: OperationType.Update },
  { label: '删除', value: OperationType.Delete },
  { label: '导入', value: OperationType.Import },
  { label: '导出', value: OperationType.Export },
  { label: '其他', value: OperationType.Other },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return operationLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      keyword: queryParams.keyword?.trim() || undefined,
      operationType: queryParams.operationType,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询操作日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<OperationLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户' },
      { field: 'title', minWidth: 160, showOverflow: 'tooltip', title: '操作标题' },
      { field: 'module', minWidth: 120, showOverflow: 'tooltip', title: '模块' },
      {
        field: 'operationType',
        formatter: ({ cellValue }) => getOptionLabel(operationTypeOptions, cellValue),
        title: '操作类型',
        width: 90,
      },
      { field: 'method', title: '请求方法', width: 90 },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '耗时',
        width: 90,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      {
        field: 'operationTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '操作时间',
      },
    ],
    id: 'sys_operation_log',
    name: '操作日志',
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
  queryParams.operationType = undefined
  queryParams.status = undefined
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
          placeholder="搜索标题/模块/用户"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.operationType"
          :options="operationTypeOptions"
          clearable
          placeholder="操作类型"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="状态"
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
        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ row.status === EnableStatus.Enabled ? '成功' : '失败' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
