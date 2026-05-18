<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { LogDetailField } from '../_components/log-detail.types'
import type { OperationLogDetailDto, OperationLogListItemDto } from '@/api'
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
import {
  createPageRequest,
  EnableStatus,
  logManagementApi,
  OperationType,
} from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogOperationPage' })

const message = useMessage()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<OperationLogDetailDto | null>(null)
const tableLoading = ref(false)
const dataList = ref<OperationLogListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await logManagementApi.operation.page({
      ...createPageRequest({
        page: { pageIndex: currentPage.value, pageSize: pageSize.value },
      }),
      keyword: queryParams.keyword?.trim() || undefined,
      operationType: queryParams.operationType,
      status: queryParams.status,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询操作日志失败')
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<OperationLogListItemDto>>(() => [
  { key: 'sessionId', title: '会话标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'traceId', title: '链路追踪 ID', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'userName', title: '用户名', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'userId', title: '用户主键', minWidth: 80, ellipsis: { tooltip: true } },
  { key: 'title', title: '操作标题', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'description', title: '操作描述', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'module', title: '操作模块', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'function', title: '操作功能', minWidth: 120, ellipsis: { tooltip: true } },
  {
    key: 'operationType',
    title: '操作类型',
    width: 90,
    render(row) {
      return h('span', {}, getOptionLabel(operationTypeOptions, row.operationType))
    },
  },
  { key: 'method', title: '请求方法', width: 90 },
  { key: 'requestUrl', title: '请求地址', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'operationIp', title: '操作 IP', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'operationLocation', title: '操作位置', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'browser', title: '浏览器', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'os', title: '操作系统', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'userAgent', title: 'User-Agent', minWidth: 260, ellipsis: { tooltip: true } },
  {
    key: 'executionTime',
    title: '执行耗时（毫秒）',
    width: 130,
    sorter: true,
    render(row) {
      return h('span', {}, `${row.executionTime}ms`)
    },
  },
  {
    key: 'status',
    title: '操作状态',
    width: 90,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => row.status === EnableStatus.Enabled ? '成功' : '失败')
    },
  },
  { key: 'errorMessage', title: '错误消息', minWidth: 220, ellipsis: { tooltip: true } },
  {
    key: 'operationTime',
    title: '操作时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.operationTime))
    },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
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

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.operationType = undefined
  queryParams.status = undefined
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

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
          clearable
          placeholder="搜索标题/模块/用户"
          style="width:220px"
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
        :scroll-x="2400"
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
      title="操作日志详情"
    />
  </div>
</template>
