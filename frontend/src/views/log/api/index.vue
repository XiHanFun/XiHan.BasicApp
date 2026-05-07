<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { LogDetailField } from '../_components/log-detail.types'
import type { ApiLogDetailDto, ApiLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, logManagementApi, SignatureType } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogApiPage' })

interface LogGridResult {
  items: ApiLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ApiLogListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ApiLogDetailDto | null>(null)

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

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'requestId', label: '请求标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'clientId', label: '客户端标识' },
  { key: 'appId', label: '应用标识' },
  { key: 'apiPath', label: 'API 路径', span: 2 },
  { key: 'apiName', label: 'API 名称' },
  { key: 'apiVersion', label: 'API 版本' },
  { key: 'controllerName', label: '控制器' },
  { key: 'actionName', label: '动作' },
  { key: 'method', label: '请求方法' },
  { key: 'statusCode', label: '响应状态码' },
  { key: 'isSuccess', falseText: '失败', label: '是否成功', trueText: '成功', type: 'boolean' },
  { key: 'isSignatureValid', falseText: '无效', label: '签名是否有效', trueText: '有效', type: 'boolean' },
  { key: 'signatureType', label: '签名类型', options: signatureTypeOptions, type: 'enum' },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'requestSize', label: '请求大小', type: 'bytes' },
  { key: 'responseSize', label: '响应大小', type: 'bytes' },
  { key: 'requestIp', label: '请求 IP' },
  { key: 'requestLocation', label: '请求位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'referer', label: '来源地址', span: 2 },
  { key: 'requestTime', label: '请求时间', type: 'date' },
  { key: 'responseTime', label: '响应时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'requestParams', label: '请求参数', type: 'code' },
  { key: 'requestBody', label: '请求体', type: 'code' },
  { key: 'responseBody', label: '响应结果', type: 'code' },
  { key: 'requestHeaders', label: '请求头', type: 'code' },
  { key: 'responseHeaders', label: '响应头', type: 'code' },
  { key: 'errorMessage', label: '错误消息', type: 'code' },
  { key: 'exceptionStackTrace', label: '异常堆栈', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return logManagementApi.api
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
  if (bytes < 1024)
    return `${bytes}B`
  if (bytes < 1048576)
    return `${(bytes / 1024).toFixed(1)}KB`
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
      { field: 'controllerName', minWidth: 140, showOverflow: 'tooltip', title: '控制器' },
      { field: 'actionName', minWidth: 140, showOverflow: 'tooltip', title: '动作' },
      { field: 'method', title: '请求方法', width: 90 },
      { field: 'statusCode', title: '响应状态码', width: 100 },
      { field: 'requestIp', minWidth: 130, showOverflow: 'tooltip', title: '请求 IP' },
      { field: 'requestLocation', minWidth: 160, showOverflow: 'tooltip', title: '请求位置' },
      { field: 'browser', minWidth: 120, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'referer', minWidth: 220, showOverflow: 'tooltip', title: '来源地址' },
      { field: 'userAgent', minWidth: 260, showOverflow: 'tooltip', title: 'User-Agent' },
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
      { field: 'errorMessage', minWidth: 220, showOverflow: 'tooltip', title: '错误消息' },
      { field: 'requestParams', minWidth: 240, showOverflow: 'tooltip', title: '请求参数' },
      { field: 'requestBody', minWidth: 240, showOverflow: 'tooltip', title: '请求体' },
      { field: 'responseBody', minWidth: 240, showOverflow: 'tooltip', title: '响应结果' },
      { field: 'requestHeaders', minWidth: 240, showOverflow: 'tooltip', title: '请求头' },
      { field: 'responseHeaders', minWidth: 240, showOverflow: 'tooltip', title: '响应头' },
      { field: 'exceptionStackTrace', minWidth: 260, showOverflow: 'tooltip', title: '异常堆栈' },
      { field: 'extendData', minWidth: 240, showOverflow: 'tooltip', title: '扩展数据' },
      { field: 'apiVersion', minWidth: 90, showOverflow: 'tooltip', title: 'API 版本' },
      { field: 'clientId', minWidth: 120, showOverflow: 'tooltip', title: '客户端标识' },
      { field: 'appId', minWidth: 120, showOverflow: 'tooltip', title: '应用标识' },
      { field: 'remark', minWidth: 180, showOverflow: 'tooltip', title: '备注' },
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
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 86,
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

async function handleDetail(row: ApiLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.api.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载 API 日志详情失败')
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
      title="API 日志详情"
    />
  </div>
</template>
