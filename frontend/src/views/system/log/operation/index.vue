<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { clearOperationLogApi } from '@/api'
import requestClient from '@/api/request'
import { buildPageRequest, flattenPageResponse } from '@/api/helpers'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLogOperationPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return requestClient
    .post(
      '/api/OperationLog/Page',
      buildPageRequest({
        page: page.currentPage,
        pageSize: page.pageSize,
        keyword: queryParams.keyword,
      }),
    )
    .then(flattenPageResponse)
}

const options = useVxeTable(
  {
    id: 'sys_operation_log',
    name: '操作日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
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

async function handleClear() {
  try {
    await clearOperationLogApi()
    message.success('清空成功')
    xGrid.value?.commitProxy('reload')
  } catch {
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
            确认清空所有操作日志？
          </NPopconfirm>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 'Yes' ? 'success' : 'error'" size="small">
            {{ row.status === 'Yes' ? '成功' : '失败' }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
