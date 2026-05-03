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
import { apiLogApi, createPageRequest, SignatureType } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemApiLogPage' })

interface LogGridResult {
  items: ApiLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ApiLogListItemDto>>()

const queryParams = reactive({
  isSuccess: undefined as number | undefined,
  keyword: '',
  method: undefined as string | undefined,
})

const successOptions = [
  { label: '成功', value: 1 },
  { label: '失败', value: 0 },
]

const methodOptions = [
  { label: 'GET', value: 'GET' },
  { label: 'POST', value: 'POST' },
  { label: 'PUT', value: 'PUT' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'PATCH', value: 'PATCH' },
]

const signatureTypeOptions = [
  { label: '无签名', value: SignatureType.None },
  { label: 'HMAC-SHA256', value: SignatureType.HmacSha256 },
  { label: 'HMAC-SHA512', value: SignatureType.HmacSha512 },
  { label: 'RSA-SHA256', value: SignatureType.RsaSha256 },
  { label: 'RSA-SHA512', value: SignatureType.RsaSha512 },
  { label: 'SM2', value: SignatureType.Sm2 },
  { label: 'SM3', value: SignatureType.Sm3 },
  { label: 'Ed25519', value: SignatureType.Ed25519 },
  { label: 'MD5', value: SignatureType.Md5 },
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
      isSuccess: queryParams.isSuccess !== undefined ? queryParams.isSuccess === 1 : undefined,
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

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes}B`
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)}KB`
  return `${(bytes / 1048576).toFixed(1)}MB`
}

const tableOptions = useVxeTable<ApiLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'requestId', minWidth: 160, showOverflow: 'tooltip', title: '请求标识' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'apiPath', minWidth: 240, showOverflow: 'tooltip', title: 'API 路径' },
      { field: 'apiName', minWidth: 120, showOverflow: 'tooltip', title: 'API 名称' },
      { field: 'method', title: '请求方法', width: 90 },
      { field: 'statusCode', title: '响应状态码', width: 100 },
      {
        field: 'isSuccess',
        slots: { default: 'col_success' },
        title: '是否成功',
        width: 90,
      },
      {
        field: 'signatureType',
        formatter: ({ cellValue }) => getOptionLabel(signatureTypeOptions, cellValue),
        title: '签名类型',
        width: 110,
      },
      {
        field: 'isSignatureValid',
        slots: { default: 'col_signature' },
        title: '签名是否有效',
        width: 110,
      },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '执行耗时（毫秒）',
        width: 130,
      },
      {
        field: 'requestSize',
        formatter: ({ cellValue }) => formatSize(cellValue),
        title: '请求大小（字节）',
        width: 130,
      },
      {
        field: 'responseSize',
        formatter: ({ cellValue }) => formatSize(cellValue),
        title: '响应大小（字节）',
        width: 130,
      },
      { field: 'apiVersion', minWidth: 90, showOverflow: 'tooltip', title: 'API 版本' },
      { field: 'clientId', minWidth: 120, showOverflow: 'tooltip', title: '客户端标识' },
      { field: 'appId', minWidth: 120, showOverflow: 'tooltip', title: '应用标识' },
      {
        field: 'requestTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '请求时间',
      },
      {
        field: 'responseTime',
        formatter: ({ cellValue }) => cellValue ? formatDate(cellValue) : '-',
        minWidth: 170,
        title: '响应时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
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
          placeholder="搜索路径/名称/用户"
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
        <template #col_signature="{ row }">
          <NTag :type="row.isSignatureValid ? 'success' : 'warning'" round size="small">
            {{ row.isSignatureValid ? '有效' : '无效' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
