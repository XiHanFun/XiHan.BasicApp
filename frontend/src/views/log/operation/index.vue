<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { LogDetailField } from '../_components/log-detail.types'
import type { OperationLogDetailDto, OperationLogListItemDto } from '@/api'
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
  logManagementApi,
  OperationType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogOperationPage' })

interface LogGridResult {
  items: OperationLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<OperationLogListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<OperationLogDetailDto | null>(null)

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

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'operationType', label: '操作类型', options: operationTypeOptions, type: 'enum' },
  { key: 'status', label: '操作状态', options: statusOptions, type: 'enum' },
  { key: 'module', label: '操作模块' },
  { key: 'function', label: '操作功能' },
  { key: 'title', label: '操作标题', span: 2 },
  { key: 'description', label: '操作描述', span: 2 },
  { key: 'method', label: '请求方法' },
  { key: 'requestUrl', label: '请求地址', span: 2 },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'operationIp', label: '操作 IP' },
  { key: 'operationLocation', label: '操作位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'operationTime', label: '操作时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'errorMessage', label: '错误消息', type: 'code' },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return logManagementApi.operation
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
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'title', minWidth: 160, showOverflow: 'tooltip', title: '操作标题' },
      { field: 'description', minWidth: 220, showOverflow: 'tooltip', title: '操作描述' },
      { field: 'module', minWidth: 120, showOverflow: 'tooltip', title: '操作模块' },
      { field: 'function', minWidth: 120, showOverflow: 'tooltip', title: '操作功能' },
      {
        field: 'operationType',
        formatter: ({ cellValue }) => getOptionLabel(operationTypeOptions, cellValue),
        title: '操作类型',
        width: 90,
      },
      { field: 'method', title: '请求方法', width: 90 },
      { field: 'requestUrl', minWidth: 240, showOverflow: 'tooltip', title: '请求地址' },
      { field: 'operationIp', minWidth: 130, showOverflow: 'tooltip', title: '操作 IP' },
      { field: 'operationLocation', minWidth: 160, showOverflow: 'tooltip', title: '操作位置' },
      { field: 'browser', minWidth: 120, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'os', minWidth: 120, showOverflow: 'tooltip', title: '操作系统' },
      { field: 'userAgent', minWidth: 260, showOverflow: 'tooltip', title: 'User-Agent' },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '执行耗时（毫秒）',
        width: 130,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '操作状态',
        width: 90,
      },
      { field: 'errorMessage', minWidth: 220, showOverflow: 'tooltip', title: '错误消息' },
      {
        field: 'operationTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '操作时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 86,
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

async function handleDetail(row: OperationLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.operation.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载操作日志详情失败')
  }
  finally {
    detailLoading.value = false
  }
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
        <template #col_actions="{ row }">
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="操作日志详情"
    />
  </div>
</template>
