<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { SysOperationLog } from '~/types'
import { NButton, NCard, NTag } from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { clearOperationLogApi, getOperationLogPageApi } from '@/api'
import { CrudProTable } from '~/components'
import { formatDate } from '~/utils'

const loading = ref(false)
const rows = ref<SysOperationLog[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumns<SysOperationLog> = [
  { title: '用户ID', key: 'userId', width: 100, ellipsis: { tooltip: true } },
  { title: '用户名', key: 'userName', width: 100 },
  { title: '操作类型', key: 'operationType', width: 100 },
  { title: '所属模块', key: 'module', width: 120 },
  { title: '功能名称', key: 'function', width: 120, ellipsis: { tooltip: true } },
  { title: '操作标题', key: 'title', width: 140, ellipsis: { tooltip: true } },
  { title: '操作描述', key: 'description', width: 200, ellipsis: { tooltip: true } },
  { title: '请求方法', key: 'method', width: 80 },
  { title: '请求地址', key: 'requestUrl', width: 200, ellipsis: { tooltip: true } },
  { title: '请求参数', key: 'requestParams', width: 200, ellipsis: { tooltip: true } },
  { title: '响应结果', key: 'responseResult', width: 200, ellipsis: { tooltip: true } },
  { title: '执行耗时(ms)', key: 'executionTime', width: 110 },
  { title: '操作IP', key: 'operationIp', width: 130 },
  { title: '操作地区', key: 'operationLocation', width: 120, ellipsis: { tooltip: true } },
  { title: '浏览器', key: 'browser', width: 100 },
  { title: '操作系统', key: 'os', width: 100 },
  {
    title: '操作状态',
    key: 'status',
    width: 90,
    render: row => h(NTag, {
      type: row.status === 'Yes' ? 'success' : 'error',
      size: 'small',
      bordered: false,
    }, { default: () => row.status === 'Yes' ? '成功' : '失败' }),
  },
  { title: '错误消息', key: 'errorMessage', width: 200, ellipsis: { tooltip: true } },
  {
    title: '操作时间',
    key: 'operationTime',
    width: 170,
    render: row => h('span', null, formatDate(row.operationTime ?? '')),
  },
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
    const data = await getOperationLogPageApi(query)
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
  await clearOperationLogApi()
  fetchData()
}

onMounted(fetchData)
</script>

<template>
  <NCard title="操作日志" :bordered="false" size="small">
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
      :scroll-x="2600"
      @update:page="handlePageChange"
      @update:page-size="handlePageSizeChange"
    />
  </NCard>
</template>
