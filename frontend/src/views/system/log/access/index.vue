<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { clearAccessLogApi } from '@/api'
import { buildPageRequest, flattenPageResponse } from '@/api/helpers'
import requestClient from '@/api/request'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLogAccessPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
  _sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams,
) {
  return requestClient
    .post(
      '/api/AccessLog/Page',
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
    id: 'sys_access_log',
    name: '访问日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'sessionId', title: '会话ID', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'resourcePath', title: '资源路径', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'resourceName', title: '资源名称', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'resourceType', title: '资源类型', width: 100 },
      { field: 'method', title: '请求方式', width: 100 },
      {
        field: 'accessResult',
        title: '访问结果',
        width: 100,
        slots: { default: 'col_accessResult' },
      },
      {
        field: 'statusCode',
        title: '状态码',
        width: 90,
        slots: { default: 'col_statusCode' },
      },
      { field: 'accessIp', title: 'IP 地址', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'accessLocation', title: '访问地点', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'os', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'device', title: '设备', minWidth: 100, showOverflow: 'tooltip' },
      { field: 'referer', title: '来源页面', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'responseTime', title: '耗时(ms)', width: 100, sortable: true },
      { field: 'responseSize', title: '响应大小(B)', width: 110 },
      {
        field: 'accessTime',
        title: '访问时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'leaveTime',
        title: '离开时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      { field: 'stayTime', title: '停留(s)', width: 90 },
      { field: 'errorMessage', title: '错误消息', minWidth: 200, showOverflow: 'tooltip' },
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
        query: ({ page, sort }) => handleQueryApi(page, sort),
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
    await clearAccessLogApi()
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
          placeholder="搜索用户名/资源路径"
          clearable
          style="width: 260px"
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
            确认清空所有访问日志？
          </NPopconfirm>
        </template>
        <template #col_accessResult="{ row }">
          <NTag :type="row.accessResult === 'Success' ? 'success' : 'error'" size="small">
            {{ row.accessResult === 'Success' ? '成功' : '失败' }}
          </NTag>
        </template>
        <template #col_statusCode="{ row }">
          <NTag :type="row.statusCode < 400 ? 'success' : 'error'" size="small">
            {{ row.statusCode }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
