<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { SysExceptionLog } from '~/types'
import { NButton, NCard, NTag } from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { clearExceptionLogApi, getExceptionLogPageApi } from '@/api'
import { CrudProTable } from '~/components'
import { formatDate } from '~/utils'

const loading = ref(false)
const rows = ref<SysExceptionLog[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumns<SysExceptionLog> = [
  { title: '用户ID', key: 'userId', width: 100, ellipsis: { tooltip: true } },
  { title: '用户名', key: 'userName', width: 100 },
  { title: '请求ID', key: 'requestId', width: 140, ellipsis: { tooltip: true } },
  { title: '会话ID', key: 'sessionId', width: 140, ellipsis: { tooltip: true } },
  { title: '异常类型', key: 'exceptionType', width: 160, ellipsis: { tooltip: true } },
  { title: '异常消息', key: 'exceptionMessage', width: 250, ellipsis: { tooltip: true } },
  { title: '异常堆栈', key: 'exceptionStackTrace', width: 300, ellipsis: { tooltip: true } },
  { title: '内部异常类型', key: 'innerExceptionType', width: 160, ellipsis: { tooltip: true } },
  { title: '内部异常消息', key: 'innerExceptionMessage', width: 250, ellipsis: { tooltip: true } },
  { title: '内部异常堆栈', key: 'innerExceptionStackTrace', width: 300, ellipsis: { tooltip: true } },
  { title: '异常来源', key: 'exceptionSource', width: 140, ellipsis: { tooltip: true } },
  { title: '异常位置', key: 'exceptionLocation', width: 200, ellipsis: { tooltip: true } },
  {
    title: '严重级别',
    key: 'severityLevel',
    width: 90,
    render: (row) => {
      const level = row.severityLevel ?? 0
      const map = [
        { label: '低', type: 'info' },
        { label: '中', type: 'warning' },
        { label: '高', type: 'error' },
        { label: '严重', type: 'error' },
      ] as const
      const item = map[level] ?? map[0]
      return h(NTag, { type: item.type, size: 'small', bordered: false }, { default: () => item.label })
    },
  },
  { title: '请求路径', key: 'requestPath', width: 200, ellipsis: { tooltip: true } },
  { title: '请求方法', key: 'requestMethod', width: 80 },
  { title: '控制器', key: 'controllerName', width: 140, ellipsis: { tooltip: true } },
  { title: '操作名称', key: 'actionName', width: 140, ellipsis: { tooltip: true } },
  { title: '请求参数', key: 'requestParams', width: 200, ellipsis: { tooltip: true } },
  { title: '请求体', key: 'requestBody', width: 200, ellipsis: { tooltip: true } },
  { title: '请求头', key: 'requestHeaders', width: 200, ellipsis: { tooltip: true } },
  {
    title: '状态码',
    key: 'statusCode',
    width: 80,
    render: row => h(NTag, {
      type: (row.statusCode ?? 0) < 400 ? 'info' : 'error',
      size: 'small',
      bordered: false,
    }, { default: () => row.statusCode }),
  },
  { title: '操作IP', key: 'operationIp', width: 130 },
  { title: '操作地区', key: 'operationLocation', width: 120, ellipsis: { tooltip: true } },
  { title: 'UserAgent', key: 'userAgent', width: 200, ellipsis: { tooltip: true } },
  { title: '浏览器', key: 'browser', width: 100 },
  { title: '操作系统', key: 'os', width: 100 },
  { title: '设备类型', key: 'deviceType', width: 100 },
  { title: '设备信息', key: 'deviceInfo', width: 140, ellipsis: { tooltip: true } },
  { title: '应用名称', key: 'applicationName', width: 140, ellipsis: { tooltip: true } },
  { title: '应用版本', key: 'applicationVersion', width: 100 },
  { title: '环境名称', key: 'environmentName', width: 100 },
  { title: '服务器主机名', key: 'serverHostName', width: 140, ellipsis: { tooltip: true } },
  { title: '线程ID', key: 'threadId', width: 80 },
  { title: '进程ID', key: 'processId', width: 80 },
  {
    title: '异常时间',
    key: 'exceptionTime',
    width: 170,
    render: row => h('span', null, formatDate(row.exceptionTime ?? '')),
  },
  {
    title: '已处理',
    key: 'isHandled',
    width: 80,
    render: row => h(NTag, {
      type: row.isHandled ? 'success' : 'warning',
      size: 'small',
      bordered: false,
    }, { default: () => row.isHandled ? '是' : '否' }),
  },
  {
    title: '处理时间',
    key: 'handledTime',
    width: 170,
    render: row => h('span', null, row.handledTime ? formatDate(row.handledTime) : '-'),
  },
  { title: '处理人ID', key: 'handledBy', width: 100, ellipsis: { tooltip: true } },
  { title: '处理人', key: 'handledByName', width: 100 },
  { title: '处理备注', key: 'handledRemark', width: 200, ellipsis: { tooltip: true } },
  { title: '业务模块', key: 'businessModule', width: 120, ellipsis: { tooltip: true } },
  { title: '业务ID', key: 'businessId', width: 120, ellipsis: { tooltip: true } },
  { title: '业务类型', key: 'businessType', width: 100 },
  { title: '错误码', key: 'errorCode', width: 100 },
  { title: '扩展数据', key: 'extendData', width: 200, ellipsis: { tooltip: true } },
  { title: '备注', key: 'remark', width: 150, ellipsis: { tooltip: true } },
  {
    title: '创建时间',
    key: 'createdTime',
    width: 170,
    render: row => h('span', null, formatDate(row.createdTime ?? '')),
  },
]

async function fetchData() {
  loading.value = true
  try {
    const data = await getExceptionLogPageApi(query)
    rows.value = data.items
    total.value = data.total
  }
  finally {
    loading.value = false
  }
}

function handlePageChange(page: number) {
  query.page = page
  fetchData()
}

function handlePageSizeChange(pageSize: number) {
  query.page = 1
  query.pageSize = pageSize
  fetchData()
}

async function handleClear() {
  await clearExceptionLogApi()
  fetchData()
}

onMounted(fetchData)
</script>

<template>
  <NCard title="异常日志" :bordered="false" size="small">
    <template #header-extra>
      <NButton size="small" type="error" ghost @click="handleClear">
        清空
      </NButton>
    </template>
    <CrudProTable
      :columns="columns"
      :data="rows"
      :loading="loading"
      :pagination="{ page: query.page, pageSize: query.pageSize, total }"
      :show-toolbar="false"
      :scroll-x="6400"
      max-height="calc(100vh - 280px)"
      @update:page="handlePageChange"
      @update:page-size="handlePageSizeChange"
    />
  </NCard>
</template>
