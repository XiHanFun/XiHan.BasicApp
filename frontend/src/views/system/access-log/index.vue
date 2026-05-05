<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { AccessLogDetailDto, AccessLogListItemDto } from '@/api'
import type { LogDetailField } from '../_components/log-detail.types'
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
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'SystemAccessLogPage' })

interface LogGridResult {
  items: AccessLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<AccessLogListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<AccessLogDetailDto | null>(null)

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

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'resourcePath', label: '资源路径', span: 2 },
  { key: 'resourceName', label: '资源名称' },
  { key: 'resourceType', label: '资源类型' },
  { key: 'method', label: '请求方法' },
  { key: 'statusCode', label: '响应状态码' },
  { key: 'accessResult', label: '访问结果', options: accessResultOptions, type: 'enum' },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'accessIp', label: '访问 IP' },
  { key: 'accessLocation', label: '访问位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'device', label: '设备' },
  { key: 'referer', label: '来源地址', span: 2 },
  { key: 'accessTime', label: '访问时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'errorMessage', label: '错误消息', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
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
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'resourcePath', minWidth: 240, showOverflow: 'tooltip', title: '资源路径' },
      { field: 'resourceName', minWidth: 120, showOverflow: 'tooltip', title: '资源名称' },
      { field: 'resourceType', minWidth: 100, showOverflow: 'tooltip', title: '资源类型' },
      { field: 'method', title: '请求方法', width: 90 },
      { field: 'statusCode', title: '响应状态码', width: 100 },
      { field: 'accessIp', minWidth: 130, showOverflow: 'tooltip', title: '访问 IP' },
      { field: 'accessLocation', minWidth: 160, showOverflow: 'tooltip', title: '访问位置' },
      { field: 'browser', minWidth: 120, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'os', minWidth: 120, showOverflow: 'tooltip', title: '操作系统' },
      { field: 'device', minWidth: 120, showOverflow: 'tooltip', title: '设备' },
      { field: 'referer', minWidth: 220, showOverflow: 'tooltip', title: '来源地址' },
      { field: 'userAgent', minWidth: 260, showOverflow: 'tooltip', title: 'User-Agent' },
      {
        field: 'accessResult',
        slots: { default: 'col_result' },
        title: '访问结果',
        width: 100,
      },
      { field: 'errorMessage', minWidth: 220, showOverflow: 'tooltip', title: '错误消息' },
      { field: 'extendData', minWidth: 240, showOverflow: 'tooltip', title: '扩展数据' },
      { field: 'remark', minWidth: 180, showOverflow: 'tooltip', title: '备注' },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '执行耗时（毫秒）',
        width: 130,
      },
      {
        field: 'accessTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '访问时间',
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

async function handleDetail(row: AccessLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await accessLogApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载访问日志详情失败')
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
      title="访问日志详情"
    />
  </div>
</template>
