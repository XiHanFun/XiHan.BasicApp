<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NModal, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { operationLogApi } from '@/api'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLogOperationPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const detailVisible = ref(false)
const detailData = ref<Record<string, any>>({})

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return operationLogApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
  })
}

const options = useVxeTable(
  {
    id: 'sys_operation_log',
    name: '操作日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'traceId', title: '链路ID', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'sessionId', title: '会话ID', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'module', title: '模块', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'function', title: '功能', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'title', title: '操作标题', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'description', title: '描述', minWidth: 260, showOverflow: 'tooltip' },
      { field: 'operationType', title: '操作类型', width: 100 },
      { field: 'method', title: '请求方式', width: 90 },
      { field: 'requestUrl', title: '请求地址', minWidth: 220, showOverflow: 'tooltip' },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      { field: 'errorMessage', title: '错误消息', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'operationIp', title: 'IP 地址', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'operationLocation', title: '操作地点', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'os', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'userAgent', title: 'User-Agent', minWidth: 200, showOverflow: 'tooltip', visible: false },
      { field: 'executionTime', title: '耗时(ms)', width: 100, sortable: true },
      {
        field: 'operationTime',
        title: '操作时间',
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
    await operationLogApi.clear()
    message.success('清空成功')
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('清空失败')
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <vxe-card style="padding: 10px 16px">
      <div class="flex gap-3 items-center">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索用户名/模块/操作标题"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NPopconfirm @positive-click="handleClear">
            <template #trigger>
              <NButton type="error" size="small">
                清空日志
              </NButton>
            </template>
            确认清空所有操作日志？
          </NPopconfirm>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 'Yes' ? 'success' : 'error'" size="small">
            {{ row.status === 'Yes' ? '成功' : '失败' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NButton size="small" type="primary" text @click="handleDetail(row)">
            详情
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="detailVisible"
      title="操作详情"
      preset="card"
      style="width: 800px; max-height: 80vh"
      :auto-focus="false"
    >
      <div class="space-y-3 text-sm">
        <div>
          <span class="font-medium text-gray-500">用户：</span>
          {{ detailData.userName }}
        </div>
        <div>
          <span class="font-medium text-gray-500">操作类型：</span>
          {{ detailData.operationType }}
        </div>
        <div>
          <span class="font-medium text-gray-500">模块/功能：</span>
          {{ detailData.module }} / {{ detailData.function }}
        </div>
        <div>
          <span class="font-medium text-gray-500">标题：</span>
          {{ detailData.title }}
        </div>
        <div>
          <span class="font-medium text-gray-500">描述：</span>
          {{ detailData.description }}
        </div>
        <div>
          <span class="font-medium text-gray-500">请求：</span>
          {{ detailData.method }} {{ detailData.requestUrl }}
        </div>
        <div>
          <span class="font-medium text-gray-500">状态：</span>
          {{ detailData.status === 'Yes' ? '成功' : '失败' }}
          <span v-if="detailData.errorMessage" class="ml-4 text-red-500">{{ detailData.errorMessage }}</span>
        </div>
        <div>
          <span class="font-medium text-gray-500">IP/地点：</span>
          {{ detailData.operationIp }} {{ detailData.operationLocation }}
        </div>
        <div>
          <span class="font-medium text-gray-500">耗时：</span>
          {{ detailData.executionTime }}ms
        </div>
        <div>
          <span class="font-medium text-gray-500">链路ID：</span>
          {{ detailData.traceId }}
        </div>
        <div v-if="detailData.requestParams">
          <span class="font-medium text-gray-500">请求参数：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-60 text-xs bg-gray-100 rounded dark:bg-gray-800"
          >{{ detailData.requestParams }}</pre>
        </div>
        <div v-if="detailData.responseResult">
          <span class="font-medium text-gray-500">响应结果：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-60 text-xs bg-gray-100 rounded dark:bg-gray-800"
          >{{ detailData.responseResult }}</pre>
        </div>
      </div>
    </NModal>
  </div>
</template>
