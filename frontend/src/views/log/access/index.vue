<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { LogDetailField } from '../_components/log-detail.types'
import type { AccessLogDetailDto, AccessLogListItemDto } from '@/api'
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
import { AccessResult, createPageRequest, logManagementApi } from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogAccessPage' })

const message = useMessage()
const tableLoading = ref(false)
const dataList = ref<AccessLogListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)
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

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await logManagementApi.access.page({
      ...createPageRequest({
        page: { pageIndex: currentPage.value, pageSize: pageSize.value },
      }),
      accessResult: queryParams.accessResult,
      keyword: queryParams.keyword?.trim() || undefined,
      method: queryParams.method,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询访问日志失败')
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<AccessLogListItemDto>>(() => [
  { key: 'sessionId', title: '会话标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'traceId', title: '链路追踪 ID', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'userName', title: '用户名', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'userId', title: '用户主键', minWidth: 80, ellipsis: { tooltip: true } },
  { key: 'resourcePath', title: '资源路径', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'resourceName', title: '资源名称', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'resourceType', title: '资源类型', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'method', title: '请求方法', width: 90 },
  { key: 'statusCode', title: '响应状态码', width: 100 },
  { key: 'accessIp', title: '访问 IP', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'accessLocation', title: '访问位置', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'browser', title: '浏览器', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'os', title: '操作系统', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'device', title: '设备', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'referer', title: '来源地址', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'userAgent', title: 'User-Agent', minWidth: 260, ellipsis: { tooltip: true } },
  {
    key: 'accessResult',
    title: '访问结果',
    width: 100,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: accessResultType(row.accessResult), bordered: false }, () => getOptionLabel(accessResultOptions, row.accessResult))
    },
  },
  { key: 'errorMessage', title: '错误消息', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'extendData', title: '扩展数据', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'remark', title: '备注', minWidth: 180, ellipsis: { tooltip: true } },
  {
    key: 'executionTime',
    title: '执行耗时（毫秒）',
    width: 130,
    sorter: true,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, `${row.executionTime}ms`) },
  },
  {
    key: 'accessTime',
    title: '访问时间',
    minWidth: 170,
    sorter: true,
    render(row) { return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.accessTime)) },
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
  queryParams.accessResult = undefined
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

async function handleDetail(row: AccessLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.access.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载访问日志详情失败')
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
          placeholder="搜索路径/用户"
          style="width:220px"
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
        :scroll-x="2000"
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
      title="访问日志详情"
    />
  </div>
</template>
