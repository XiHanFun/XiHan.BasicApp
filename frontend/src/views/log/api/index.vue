<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { LogDetailField } from '../_components/log-detail.types'
import type { ApiLogDetailDto, ApiLogListItemDto } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NIcon,
  NInput,
  NPagination,
  NSelect,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { createPageRequest, logManagementApi, SignatureType } from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogApiPage' })

const message = useMessage()
const tableLoading = ref(false)
const dataList = ref<ApiLogListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)
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

function formatSize(bytes: number): string {
  if (bytes < 1024)
    return `${bytes}B`
  if (bytes < 1048576)
    return `${(bytes / 1024).toFixed(1)}KB`
  return `${(bytes / 1048576).toFixed(1)}MB`
}

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await logManagementApi.api.page({
      ...createPageRequest({
        page: { pageIndex: currentPage.value, pageSize: pageSize.value },
      }),
      isSuccess: queryParams.isSuccess !== undefined ? queryParams.isSuccess === 1 : undefined,
      keyword: queryParams.keyword?.trim() || undefined,
      method: queryParams.method,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询API日志失败')
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<ApiLogListItemDto>>(() => [
  { key: 'sessionId', title: '会话标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'traceId', title: '链路追踪 ID', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'requestId', title: '请求标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'userName', title: '用户名', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'userId', title: '用户主键', minWidth: 80, ellipsis: { tooltip: true } },
  { key: 'apiPath', title: 'API 路径', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'apiName', title: 'API 名称', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'controllerName', title: '控制器', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'actionName', title: '动作', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'method', title: '请求方法', width: 90 },
  { key: 'statusCode', title: '响应状态码', width: 100 },
  { key: 'requestIp', title: '请求 IP', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'requestLocation', title: '请求位置', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'browser', title: '浏览器', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'referer', title: '来源地址', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'userAgent', title: 'User-Agent', minWidth: 260, ellipsis: { tooltip: true } },
  {
    key: 'isSuccess',
    title: '是否成功',
    width: 90,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.isSuccess ? 'success' : 'error', bordered: false }, () => row.isSuccess ? '成功' : '失败')
    },
  },
  {
    key: 'signatureType',
    title: '签名类型',
    width: 110,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(signatureTypeOptions, row.signatureType)) },
  },
  {
    key: 'isSignatureValid',
    title: '签名是否有效',
    width: 110,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.isSignatureValid ? 'success' : 'warning', bordered: false }, () => row.isSignatureValid ? '有效' : '无效')
    },
  },
  {
    key: 'executionTime',
    title: '执行耗时（毫秒）',
    width: 130,
    sorter: true,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, `${row.executionTime}ms`) },
  },
  {
    key: 'requestSize',
    title: '请求大小（字节）',
    width: 130,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatSize(row.requestSize)) },
  },
  {
    key: 'responseSize',
    title: '响应大小（字节）',
    width: 130,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatSize(row.responseSize)) },
  },
  { key: 'errorMessage', title: '错误消息', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'requestParams', title: '请求参数', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'requestBody', title: '请求体', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'responseBody', title: '响应结果', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'requestHeaders', title: '请求头', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'responseHeaders', title: '响应头', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'exceptionStackTrace', title: '异常堆栈', minWidth: 260, ellipsis: { tooltip: true } },
  { key: 'extendData', title: '扩展数据', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'apiVersion', title: 'API 版本', minWidth: 90, ellipsis: { tooltip: true } },
  { key: 'clientId', title: '客户端标识', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'appId', title: '应用标识', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'remark', title: '备注', minWidth: 180, ellipsis: { tooltip: true } },
  {
    key: 'requestTime',
    title: '请求时间',
    minWidth: 170,
    sorter: true,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.requestTime)) },
  },
  {
    key: 'responseTime',
    title: '响应时间',
    minWidth: 170,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, row.responseTime ? formatDate(row.responseTime) : '-') },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime)) },
  },
  {
    key: 'actions',
    title: '操作',
    width: 86,
    fixed: 'right',
    render(row) {
      return h('div', { style: 'display:flex;align-items:center;gap:2px;' }, [
        h(NTooltip, {}, {
          trigger: () => h(NButton, { size: 'small', quaternary: true, circle: true, type: 'primary', onClick: () => handleDetail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '详情',
        }),
      ])
    },
  },
])

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.isSuccess = undefined
  queryParams.method = undefined
  currentPage.value = 1
  fetchData()
}

function handlePageChange(page: number) {
  currentPage.value = page
  fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  fetchData()
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

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

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
          clearable
          placeholder="搜索路径/名称/用户"
          style="width:220px"
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
        :scroll-x="2800"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">
          共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页
        </div>
        <NPagination :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="API 日志详情"
    />
  </div>
</template>
