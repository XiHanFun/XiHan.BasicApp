<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { LogDetailField } from '../_components/log-detail.types'
import type { ExceptionLogDetailDto, ExceptionLogListItemDto } from '@/api'
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
import { createPageRequest, DeviceType, logManagementApi } from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogExceptionPage' })

const message = useMessage()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ExceptionLogDetailDto | null>(null)
const tableLoading = ref(false)
const dataList = ref<ExceptionLogListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

const queryParams = reactive({
  isHandled: undefined as number | undefined,
  keyword: '',
  severityLevel: undefined as number | undefined,
})

const handledOptions = [
  { label: '已处理', value: 1 },
  { label: '未处理', value: 0 },
]

const severityOptions = [
  { label: '低', value: 1 },
  { label: '中', value: 2 },
  { label: '高', value: 3 },
  { label: '严重', value: 4 },
  { label: '致命', value: 5 },
]

const deviceTypeOptions = [
  { label: '未知', value: DeviceType.Unknown },
  { label: 'Web浏览器', value: DeviceType.Web },
  { label: 'iOS', value: DeviceType.IOS },
  { label: 'Android', value: DeviceType.Android },
  { label: 'Windows', value: DeviceType.Windows },
  { label: 'macOS', value: DeviceType.MacOS },
  { label: 'Linux', value: DeviceType.Linux },
  { label: '平板', value: DeviceType.Tablet },
  { label: '小程序', value: DeviceType.MiniProgram },
  { label: 'API', value: DeviceType.Api },
]

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'requestId', label: '请求标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'exceptionType', label: '异常类型' },
  { key: 'errorCode', label: '错误代码' },
  { key: 'exceptionSource', label: '异常源' },
  { key: 'exceptionLocation', label: '异常发生位置', span: 2 },
  { key: 'severityLevel', label: '严重级别', options: severityOptions, type: 'enum' },
  { key: 'statusCode', label: '响应状态码' },
  { key: 'requestPath', label: '请求路径', span: 2 },
  { key: 'requestMethod', label: '请求方法' },
  { key: 'controllerName', label: '控制器' },
  { key: 'actionName', label: '动作' },
  { key: 'operationIp', label: '操作 IP' },
  { key: 'operationLocation', label: '操作位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'deviceType', label: '设备类型', options: deviceTypeOptions, type: 'enum' },
  { key: 'applicationName', label: '应用程序名称' },
  { key: 'applicationVersion', label: '应用程序版本' },
  { key: 'environmentName', label: '环境名称' },
  { key: 'serverHostName', label: '服务器主机' },
  { key: 'threadId', label: '线程 ID' },
  { key: 'processId', label: '进程 ID' },
  { key: 'isHandled', falseText: '未处理', label: '处理状态', trueText: '已处理', type: 'boolean' },
  { key: 'handledBy', label: '处理人主键' },
  { key: 'handledTime', label: '处理时间', type: 'date' },
  { key: 'exceptionTime', label: '异常时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'handledRemark', label: '处理备注', span: 2 },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'exceptionMessage', label: '异常消息', type: 'code' },
  { key: 'exceptionStackTrace', label: '异常堆栈', type: 'code' },
  { key: 'requestParams', label: '请求参数', type: 'code' },
  { key: 'requestBody', label: '请求体', type: 'code' },
  { key: 'requestHeaders', label: '请求头', type: 'code' },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
  { key: 'deviceInfo', label: '设备信息', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
]

function severityType(level: number) {
  switch (level) {
    case 1: return 'info'
    case 2: return 'warning'
    case 3:
    case 4:
    case 5: return 'error'
    default: return 'default'
  }
}

function severityLabel(level: number) {
  return severityOptions.find(o => o.value === level)?.label ?? '未知'
}

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await logManagementApi.exception.page({
      ...createPageRequest({
        page: { pageIndex: currentPage.value, pageSize: pageSize.value },
      }),
      isHandled: queryParams.isHandled !== undefined ? queryParams.isHandled === 1 : undefined,
      keyword: queryParams.keyword?.trim() || undefined,
      severityLevel: queryParams.severityLevel,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询异常日志失败')
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<ExceptionLogListItemDto>>(() => [
  { key: 'sessionId', title: '会话标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'traceId', title: '链路追踪 ID', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'requestId', title: '请求标识', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'userName', title: '用户名', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'userId', title: '用户主键', minWidth: 80, ellipsis: { tooltip: true } },
  { key: 'exceptionType', title: '异常类型', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'exceptionMessage', title: '异常消息', minWidth: 260, ellipsis: { tooltip: true } },
  { key: 'exceptionSource', title: '异常源', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'exceptionLocation', title: '异常发生位置', minWidth: 200, ellipsis: { tooltip: true } },
  { key: 'controllerName', title: '控制器', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'actionName', title: '动作', minWidth: 140, ellipsis: { tooltip: true } },
  {
    key: 'severityLevel',
    title: '严重级别',
    width: 90,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: severityType(row.severityLevel), bordered: false }, () => severityLabel(row.severityLevel))
    },
  },
  { key: 'requestPath', title: '请求路径', minWidth: 200, ellipsis: { tooltip: true } },
  { key: 'requestMethod', title: '请求方法', width: 90 },
  { key: 'statusCode', title: '响应状态码', width: 100 },
  { key: 'operationIp', title: '操作 IP', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'operationLocation', title: '操作位置', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'browser', title: '浏览器', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'os', title: '操作系统', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'deviceInfo', title: '设备信息', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'userAgent', title: 'User-Agent', minWidth: 260, ellipsis: { tooltip: true } },
  {
    key: 'deviceType',
    title: '设备类型',
    width: 100,
    render(row) {
      return h('span', {}, getOptionLabel(deviceTypeOptions, row.deviceType))
    },
  },
  { key: 'applicationName', title: '应用程序名称', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'applicationVersion', title: '应用程序版本', minWidth: 120, ellipsis: { tooltip: true } },
  { key: 'environmentName', title: '环境名称', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'errorCode', title: '错误代码', minWidth: 100, ellipsis: { tooltip: true } },
  { key: 'serverHostName', title: '服务器主机', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'threadId', title: '线程 ID', minWidth: 90 },
  { key: 'processId', title: '进程 ID', minWidth: 90 },
  {
    key: 'isHandled',
    title: '是否已处理',
    width: 100,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.isHandled ? 'success' : 'warning', bordered: false }, () => row.isHandled ? '已处理' : '未处理')
    },
  },
  { key: 'handledRemark', title: '处理备注', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'exceptionStackTrace', title: '异常堆栈', minWidth: 260, ellipsis: { tooltip: true } },
  { key: 'requestParams', title: '请求参数', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'requestBody', title: '请求体', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'requestHeaders', title: '请求头', minWidth: 240, ellipsis: { tooltip: true } },
  { key: 'extendData', title: '扩展数据', minWidth: 240, ellipsis: { tooltip: true } },
  {
    key: 'handledTime',
    title: '处理时间',
    minWidth: 170,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, row.handledTime ? formatDate(row.handledTime) : '-')
    },
  },
  { key: 'handledBy', title: '处理人主键', minWidth: 90, ellipsis: { tooltip: true } },
  { key: 'remark', title: '备注', minWidth: 180, ellipsis: { tooltip: true } },
  {
    key: 'exceptionTime',
    title: '异常时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.exceptionTime))
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
  queryParams.isHandled = undefined
  queryParams.severityLevel = undefined
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

async function handleDetail(row: ExceptionLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.exception.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载异常日志详情失败')
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
          placeholder="搜索异常类型/位置/用户"
          style="width:220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.severityLevel"
          :options="severityOptions"
          clearable
          placeholder="严重等级"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.isHandled"
          :options="handledOptions"
          clearable
          placeholder="处理状态"
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
      title="异常日志详情"
    />
  </div>
</template>
