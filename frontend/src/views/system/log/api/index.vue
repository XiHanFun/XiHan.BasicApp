<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NModal, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { apiLogApi } from '@/api'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'
import { XSystemQueryPanel } from '~/components'

defineOptions({ name: 'SystemLogApiPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const detailVisible = ref(false)
const detailData = ref<Record<string, any>>({})

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return apiLogApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
  })
}

const signatureTypeMap: Record<number | string, string> = {
  0: '无',
  1: 'HMAC-SHA256',
  2: 'HMAC-SHA512',
  3: 'RSA-SHA256',
  4: 'RSA-SHA512',
  5: 'SM2',
  6: 'SM3',
  7: 'Ed25519',
  99: 'MD5',
}

const options = useVxeTable(
  {
    id: 'sys_api_log',
    name: '接口日志',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'clientId', title: '客户端', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'traceId', title: '链路ID', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'apiPath', title: '接口路径', minWidth: 220, showOverflow: 'tooltip' },
      { field: 'method', title: '请求方式', width: 90 },
      {
        field: 'statusCode',
        title: '状态码',
        width: 90,
        slots: { default: 'col_statusCode' },
      },
      {
        field: 'isSignatureValid',
        title: '签名',
        width: 80,
        slots: { default: 'col_signature' },
      },
      {
        field: 'signatureType',
        title: '签名类型',
        width: 110,
        formatter: ({ cellValue }) => signatureTypeMap[cellValue] ?? '未知',
      },
      {
        field: 'isSuccess',
        title: '结果',
        width: 80,
        slots: { default: 'col_result' },
      },
      { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'requestIp', title: 'IP 地址', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'requestLocation', title: '地点', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'os', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'executionTime', title: '耗时(ms)', width: 100, sortable: true },
      { field: 'requestSize', title: '请求大小(B)', width: 110 },
      { field: 'responseSize', title: '响应大小(B)', width: 110 },
      { field: 'errorMessage', title: '错误消息', minWidth: 200, showOverflow: 'tooltip' },
      {
        field: 'requestTime',
        title: '请求时间',
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
    await apiLogApi.clear()
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
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索客户端/接口路径/用户名"
          clearable
          style="width: 300px"
          @keyup.enter="handleSearch"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
      </div>
    </XSystemQueryPanel>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NPopconfirm @positive-click="handleClear">
            <template #trigger>
              <NButton type="error" size="small">
                清空日志
              </NButton>
            </template>
            确认清空所有接口日志？
          </NPopconfirm>
        </template>
        <template #col_statusCode="{ row }">
          <NTag :type="row.statusCode < 400 ? 'success' : 'error'" size="small">
            {{ row.statusCode }}
          </NTag>
        </template>
        <template #col_signature="{ row }">
          <NTag :type="row.isSignatureValid ? 'success' : 'error'" size="small">
            {{ row.isSignatureValid ? '有效' : '无效' }}
          </NTag>
        </template>
        <template #col_result="{ row }">
          <NTag :type="row.isSuccess ? 'success' : 'error'" size="small">
            {{ row.isSuccess ? '成功' : '失败' }}
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
      title="接口详情"
      preset="card"
      style="width: 800px; max-height: 80vh"
      :auto-focus="false"
    >
      <div class="space-y-3 text-sm">
        <div>
          <span class="font-medium text-gray-500">客户端：</span>
          {{ detailData.clientId }}
          <span v-if="detailData.appId" class="ml-4 font-medium text-gray-500">应用：</span>
          {{ detailData.appId }}
        </div>
        <div>
          <span class="font-medium text-gray-500">签名：</span>
          {{ detailData.isSignatureValid ? '有效' : '无效' }}
          <span class="ml-4 font-medium text-gray-500">算法：</span>
          {{ signatureTypeMap[detailData.signatureType] ?? '未知' }}
        </div>
        <div>
          <span class="font-medium text-gray-500">请求：</span>
          {{ detailData.method }} {{ detailData.apiPath }}
        </div>
        <div>
          <span class="font-medium text-gray-500">状态码：</span>
          {{ detailData.statusCode }}
          <span class="ml-4 font-medium text-gray-500">耗时：</span>
          {{ detailData.executionTime }}ms
        </div>
        <div>
          <span class="font-medium text-gray-500">IP/地点：</span>
          {{ detailData.requestIp }} {{ detailData.requestLocation }}
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
        <div v-if="detailData.requestBody">
          <span class="font-medium text-gray-500">请求体：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-60 text-xs bg-gray-100 rounded dark:bg-gray-800"
          >{{ detailData.requestBody }}</pre>
        </div>
        <div v-if="detailData.responseBody">
          <span class="font-medium text-gray-500">响应体：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-60 text-xs bg-gray-100 rounded dark:bg-gray-800"
          >{{ detailData.responseBody }}</pre>
        </div>
        <div v-if="detailData.errorMessage">
          <span class="font-medium text-gray-500">错误信息：</span>
          {{ detailData.errorMessage }}
        </div>
        <div v-if="detailData.exceptionStackTrace">
          <span class="font-medium text-gray-500">异常堆栈：</span>
          <pre
            class="overflow-auto p-3 mt-1 max-h-60 text-xs bg-gray-100 rounded dark:bg-gray-800"
          >{{ detailData.exceptionStackTrace }}</pre>
        </div>
      </div>
    </NModal>
  </div>
</template>
