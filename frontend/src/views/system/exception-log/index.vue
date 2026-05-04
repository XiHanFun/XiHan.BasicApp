<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ExceptionLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, DeviceType, exceptionLogApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemExceptionLogPage' })

interface LogGridResult {
  items: ExceptionLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ExceptionLogListItemDto>>()

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

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return exceptionLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isHandled: queryParams.isHandled !== undefined ? queryParams.isHandled === 1 : undefined,
      keyword: queryParams.keyword?.trim() || undefined,
      severityLevel: queryParams.severityLevel,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询异常日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<ExceptionLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'requestId', minWidth: 160, showOverflow: 'tooltip', title: '请求标识' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'exceptionType', minWidth: 160, showOverflow: 'tooltip', title: '异常类型' },
      { field: 'exceptionMessage', minWidth: 260, showOverflow: 'tooltip', title: '异常消息' },
      { field: 'exceptionSource', minWidth: 140, showOverflow: 'tooltip', title: '异常源' },
      { field: 'exceptionLocation', minWidth: 200, showOverflow: 'tooltip', title: '异常发生位置' },
      { field: 'controllerName', minWidth: 140, showOverflow: 'tooltip', title: '控制器' },
      { field: 'actionName', minWidth: 140, showOverflow: 'tooltip', title: '动作' },
      {
        field: 'severityLevel',
        slots: { default: 'col_severity' },
        title: '严重级别',
        width: 90,
      },
      { field: 'requestPath', minWidth: 200, showOverflow: 'tooltip', title: '请求路径' },
      { field: 'requestMethod', title: '请求方法', width: 90 },
      { field: 'statusCode', title: '响应状态码', width: 100 },
      { field: 'operationIp', minWidth: 130, showOverflow: 'tooltip', title: '操作 IP' },
      { field: 'operationLocation', minWidth: 160, showOverflow: 'tooltip', title: '操作位置' },
      { field: 'browser', minWidth: 120, showOverflow: 'tooltip', title: '浏览器' },
      { field: 'os', minWidth: 120, showOverflow: 'tooltip', title: '操作系统' },
      { field: 'deviceInfo', minWidth: 140, showOverflow: 'tooltip', title: '设备信息' },
      { field: 'userAgent', minWidth: 260, showOverflow: 'tooltip', title: 'User-Agent' },
      {
        field: 'deviceType',
        formatter: ({ cellValue }) => getOptionLabel(deviceTypeOptions, cellValue),
        title: '设备类型',
        width: 100,
      },
      { field: 'applicationName', minWidth: 130, showOverflow: 'tooltip', title: '应用程序名称' },
      { field: 'applicationVersion', minWidth: 120, showOverflow: 'tooltip', title: '应用程序版本' },
      { field: 'environmentName', minWidth: 100, showOverflow: 'tooltip', title: '环境名称' },
      { field: 'errorCode', minWidth: 100, showOverflow: 'tooltip', title: '错误代码' },
      { field: 'serverHostName', minWidth: 140, showOverflow: 'tooltip', title: '服务器主机' },
      { field: 'threadId', minWidth: 90, title: '线程 ID' },
      { field: 'processId', minWidth: 90, title: '进程 ID' },
      {
        field: 'isHandled',
        slots: { default: 'col_handled' },
        title: '是否已处理',
        width: 100,
      },
      { field: 'handledRemark', minWidth: 220, showOverflow: 'tooltip', title: '处理备注' },
      { field: 'exceptionStackTrace', minWidth: 260, showOverflow: 'tooltip', title: '异常堆栈' },
      { field: 'requestParams', minWidth: 240, showOverflow: 'tooltip', title: '请求参数' },
      { field: 'requestBody', minWidth: 240, showOverflow: 'tooltip', title: '请求体' },
      { field: 'requestHeaders', minWidth: 240, showOverflow: 'tooltip', title: '请求头' },
      { field: 'extendData', minWidth: 240, showOverflow: 'tooltip', title: '扩展数据' },
      {
        field: 'handledTime',
        formatter: ({ cellValue }) => cellValue ? formatDate(cellValue) : '-',
        minWidth: 170,
        title: '处理时间',
      },
      { field: 'handledBy', minWidth: 90, showOverflow: 'tooltip', title: '处理人主键' },
      { field: 'remark', minWidth: 180, showOverflow: 'tooltip', title: '备注' },
      {
        field: 'exceptionTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '异常时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
    ],
    id: 'sys_exception_log',
    name: '异常日志',
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
  queryParams.isHandled = undefined
  queryParams.severityLevel = undefined
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
          placeholder="搜索异常类型/位置/用户"
          style="width: 220px"
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
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #col_severity="{ row }">
          <NTag :type="severityType(row.severityLevel)" round size="small">
            {{ severityLabel(row.severityLevel) }}
          </NTag>
        </template>
        <template #col_handled="{ row }">
          <NTag :type="row.isHandled ? 'success' : 'warning'" round size="small">
            {{ row.isHandled ? '已处理' : '未处理' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
