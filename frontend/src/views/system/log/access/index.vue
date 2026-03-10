<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { SysAccessLog } from '~/types'
import { NButton, NCard, NTag } from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { clearAccessLogApi, getAccessLogPageApi } from '@/api'
import { CrudProTable } from '~/components'
import { formatDate } from '~/utils'

const loading = ref(false)
const rows = ref<SysAccessLog[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumns<SysAccessLog> = [
  { title: '用户ID', key: 'userId', width: 100, ellipsis: { tooltip: true } },
  { title: '用户名', key: 'userName', width: 100 },
  { title: '会话ID', key: 'sessionId', width: 140, ellipsis: { tooltip: true } },
  { title: '资源路径', key: 'resourcePath', width: 200, ellipsis: { tooltip: true } },
  { title: '资源名称', key: 'resourceName', width: 120, ellipsis: { tooltip: true } },
  { title: '资源类型', key: 'resourceType', width: 90 },
  { title: '请求方式', key: 'method', width: 80 },
  { title: '访问结果', key: 'accessResult', width: 90, ellipsis: { tooltip: true } },
  {
    title: '状态码',
    key: 'statusCode',
    width: 80,
    render: row => h(NTag, {
      type: (row.statusCode ?? 0) < 400 ? 'success' : 'error',
      size: 'small',
      bordered: false,
    }, { default: () => row.statusCode }),
  },
  { title: '访问IP', key: 'accessIp', width: 130 },
  { title: '访问地区', key: 'accessLocation', width: 120, ellipsis: { tooltip: true } },
  { title: 'UserAgent', key: 'userAgent', width: 200, ellipsis: { tooltip: true } },
  { title: '浏览器', key: 'browser', width: 100 },
  { title: '操作系统', key: 'os', width: 100 },
  { title: '设备', key: 'device', width: 100, ellipsis: { tooltip: true } },
  { title: '来源页面', key: 'referer', width: 180, ellipsis: { tooltip: true } },
  { title: '响应耗时(ms)', key: 'responseTime', width: 110 },
  { title: '响应大小(B)', key: 'responseSize', width: 110 },
  {
    title: '访问时间',
    key: 'accessTime',
    width: 170,
    render: row => h('span', null, formatDate(row.accessTime ?? '')),
  },
  {
    title: '离开时间',
    key: 'leaveTime',
    width: 170,
    render: row => h('span', null, row.leaveTime ? formatDate(row.leaveTime) : '-'),
  },
  { title: '停留时长(s)', key: 'stayTime', width: 110 },
  { title: '错误消息', key: 'errorMessage', width: 200, ellipsis: { tooltip: true } },
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
    const data = await getAccessLogPageApi(query)
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
  await clearAccessLogApi()
  fetchData()
}

onMounted(fetchData)
</script>

<template>
  <NCard title="访问日志" :bordered="false" size="small">
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
      :scroll-x="2800"
      max-height="calc(100vh - 280px)"
      @update:page="handlePageChange"
      @update:page-size="handlePageSizeChange"
    />
  </NCard>
</template>
