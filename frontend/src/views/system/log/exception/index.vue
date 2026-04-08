<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NModal, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { exceptionLogApi } from '@/api'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLogExceptionPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const detailVisible = ref(false)
const detailData = ref<Record<string, any>>({})

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return exceptionLogApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
  })
}

const options = useVxeTable(
  {
    id: 'sys_exception_log',
    name: '异常日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'traceId', title: '链路ID', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'exceptionType', title: '异常类型', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'exceptionMessage', title: '异常消息', minWidth: 260, showOverflow: 'tooltip' },
      { field: 'exceptionSource', title: '异常来源', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'exceptionLocation', title: '异常位置', minWidth: 180, showOverflow: 'tooltip' },
      {
        field: 'severityLevel',
        title: '严重等级',
        width: 100,
        slots: { default: 'col_severity' },
      },
      { field: 'requestPath', title: '请求路径', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'requestMethod', title: '请求方式', width: 90 },
      { field: 'controllerName', title: '控制器', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'actionName', title: '操作名称', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'statusCode', title: '状态码', width: 80 },
      { field: 'errorCode', title: '错误码', width: 100, showOverflow: 'tooltip' },
      { field: 'operationIp', title: 'IP 地址', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'operationLocation', title: '操作地点', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'os', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'deviceType', title: '设备类型', width: 100 },
      { field: 'businessModule', title: '业务模块', minWidth: 120, showOverflow: 'tooltip' },
      {
        field: 'isHandled',
        title: '已处理',
        width: 80,
        slots: { default: 'col_handled' },
      },
      { field: 'handledByName', title: '处理人', minWidth: 100, showOverflow: 'tooltip' },
      {
        field: 'handledTime',
        title: '处理时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'exceptionTime',
        title: '异常时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'createdTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'actions',
        title: '操作',
        width: 80,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
    toolbarConfig: {
      slots: { buttons: 'toolbar_buttons' },
      refresh: true,
      export: true,
      zoom: true,
      custom: true,
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleDetail(row: Record<string, any>) {
  detailData.value = row
  detailVisible.value = true
}

async function handleClear() {
  try {
    await exceptionLogApi.clear()
    message.success('清空成功')
    xGrid.value?.commitProxy('reload')
  } catch {
    message.error('清空失败')
  }
}

function getSeverityTag(level: number | undefined) {
  const map: Record<number, { label: string; type: 'success' | 'warning' | 'error' | 'info' }> = {
    0: { label: '调试', type: 'info' },
    1: { label: '信息', type: 'success' },
    2: { label: '警告', type: 'warning' },
    3: { label: '错误', type: 'error' },
    4: { label: '致命', type: 'error' },
  }
  return map[level ?? 3] ?? { label: '未知', type: 'info' }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <vxe-card style="padding: 10px 16px">
      <div class="flex gap-3 items-center">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索异常类型/异常消息/请求路径"
          clearable
          style="width: 320px"
          @keyup.enter="handleSearch"
        />
        <NButton type="primary" size="small" @click="handleSearch">查询</NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NPopconfirm @positive-click="handleClear">
            <template #trigger>
              <NButton type="error" size="small">清空日志</NButton>
            </template>
            确认清空所有异常日志？
          </NPopconfirm>
        </template>
        <template #col_severity="{ row }">
          <NTag :type="getSeverityTag(row.severityLevel).type" size="small">
            {{ getSeverityTag(row.severityLevel).label }}
          </NTag>
        </template>
        <template #col_handled="{ row }">
          <NTag :type="row.isHandled ? 'success' : 'warning'" size="small">
            {{ row.isHandled ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NButton size="small" type="primary" text @click="handleDetail(row)">详情</NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="detailVisible"
      title="异常详情"
      preset="card"
      style="width: 800px; max-height: 80vh"
      :auto-focus="false"
    >
      <div class="space-y-3 text-sm">
        <div>
          <span class="font-medium text-gray-500">异常类型：</span>
          {{ detailData.exceptionType }}
        </div>
        <div>
          <span class="font-medium text-gray-500">异常消息：</span>
          {{ detailData.exceptionMessage }}
        </div>
        <div>
          <span class="font-medium text-gray-500">异常来源：</span>
          {{ detailData.exceptionSource }}
        </div>
        <div>
          <span class="font-medium text-gray-500">异常位置：</span>
          {{ detailData.exceptionLocation }}
        </div>
        <div>
          <span class="font-medium text-gray-500">请求路径：</span>
          {{ detailData.requestMethod }} {{ detailData.requestPath }}
        </div>
        <div>
          <span class="font-medium text-gray-500">控制器/操作：</span>
          {{ detailData.controllerName }}.{{ detailData.actionName }}
        </div>
        <div>
          <span class="font-medium text-gray-500">错误码：</span>
          {{ detailData.errorCode }}
        </div>
        <div>
          <span class="font-medium text-gray-500">业务模块：</span>
          {{ detailData.businessModule }}
        </div>
        <div>
          <span class="font-medium text-gray-500">异常时间：</span>
          {{ formatDate(detailData.exceptionTime) }}
        </div>
        <div v-if="detailData.handledByName">
          <span class="font-medium text-gray-500">处理人：</span>
          {{ detailData.handledByName }}
          <span class="ml-4 font-medium text-gray-500">处理时间：</span>
          {{ formatDate(detailData.handledTime) }}
        </div>
        <div v-if="detailData.handledRemark">
          <span class="font-medium text-gray-500">处理备注：</span>
          {{ detailData.handledRemark }}
        </div>
        <div v-if="detailData.exceptionStackTrace">
          <span class="font-medium text-gray-500">堆栈信息：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-60 text-xs bg-gray-100 rounded dark:bg-gray-800"
            >{{ detailData.exceptionStackTrace }}</pre
          >
        </div>
        <div v-if="detailData.innerExceptionType">
          <span class="font-medium text-gray-500">内部异常：</span>
          {{ detailData.innerExceptionType }} - {{ detailData.innerExceptionMessage }}
        </div>
        <div v-if="detailData.innerExceptionStackTrace">
          <span class="font-medium text-gray-500">内部堆栈：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-40 text-xs bg-gray-100 rounded dark:bg-gray-800"
            >{{ detailData.innerExceptionStackTrace }}</pre
          >
        </div>
      </div>
    </NModal>
  </div>
</template>
