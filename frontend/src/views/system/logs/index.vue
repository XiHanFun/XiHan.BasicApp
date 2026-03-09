<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { FrontendRequestLog, SysLogItem } from '~/types'
import {
  NButton,
  NCard,
  NRadioButton,
  NRadioGroup,
  NTabPane,
  NTabs,
  NTag,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { getAccessLogsApi, getAuditLogsApi, getExceptionLogsApi, getOperationLogsApi } from '@/api'
import { CrudProTable } from '~/components'
import { clearRequestLogs, formatDate, useRequestLogs } from '~/utils'

const loading = ref(false)
const rows = ref<SysLogItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })
const activeType = ref<'access' | 'operation' | 'exception' | 'audit'>('audit')
const activePanel = ref<'backend' | 'frontend'>('backend')
const frontendRequestLogs = useRequestLogs()
const frontendRows = computed(() => [...frontendRequestLogs.value])

const columns: DataTableColumns<SysLogItem> = [
  { title: '用户', key: 'userName', width: 140 },
  { title: '操作', key: 'operationName', width: 180 },
  { title: 'IP', key: 'ip', width: 140 },
  { title: '消息', key: 'message' },
  {
    title: '时间',
    key: 'createdTime',
    width: 180,
    render: row => h('span', null, formatDate(row.createdTime ?? '')),
  },
]

const loaders = computed(() => ({
  access: getAccessLogsApi,
  operation: getOperationLogsApi,
  exception: getExceptionLogsApi,
  audit: getAuditLogsApi,
}))

const frontendColumns: DataTableColumns<FrontendRequestLog> = [
  {
    title: '状态',
    key: 'status',
    width: 90,
    render: (row) => {
      const map = {
        success: { label: '成功', type: 'success' },
        error: { label: '失败', type: 'error' },
        pending: { label: '请求中', type: 'warning' },
      } as const
      const current = map[row.status] ?? map.pending
      return h(NTag, { type: current.type, size: 'small', bordered: false }, { default: () => current.label })
    },
  },
  { title: '方法', key: 'method', width: 80 },
  { title: '地址', key: 'url', minWidth: 240, ellipsis: { tooltip: true } },
  { title: '状态码', key: 'statusCode', width: 90 },
  { title: '业务码', key: 'responseCode', width: 100 },
  { title: '耗时(ms)', key: 'duration', width: 100 },
  {
    title: '消息',
    key: 'message',
    minWidth: 180,
    ellipsis: { tooltip: true },
  },
  {
    title: '时间',
    key: 'startedAt',
    width: 170,
    render: row => formatDate(new Date(row.startedAt).toISOString()),
  },
  {
    title: '请求ID',
    key: 'requestId',
    minWidth: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '追踪ID',
    key: 'traceId',
    minWidth: 180,
    ellipsis: { tooltip: true },
  },
]

async function fetchData() {
  loading.value = true
  try {
    const data = await loaders.value[activeType.value](query)
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

function handleClearFrontendLogs() {
  clearRequestLogs()
}

onMounted(fetchData)
</script>

<template>
  <NCard title="日志中心" :bordered="false" size="small">
    <NTabs v-model:value="activePanel" type="line" animated>
      <NTabPane name="backend" tab="后端日志">
        <NRadioGroup v-model:value="activeType" class="mb-3" @update:value="fetchData">
          <NRadioButton value="audit">审计日志</NRadioButton>
          <NRadioButton value="operation">操作日志</NRadioButton>
          <NRadioButton value="access">访问日志</NRadioButton>
          <NRadioButton value="exception">异常日志</NRadioButton>
        </NRadioGroup>
        <CrudProTable
          :columns="columns"
          :data="rows"
          :loading="loading"
          :pagination="{ page: query.page, pageSize: query.pageSize, total }"
          :show-toolbar="false"
          @update:page="handlePageChange"
          @update:page-size="handlePageSizeChange"
        />
      </NTabPane>

      <NTabPane name="frontend" tab="前端请求日志">
        <div class="mb-3 flex items-center justify-between">
          <span class="text-xs text-gray-500">累计 {{ frontendRequestLogs.length }} 条</span>
          <NButton size="small" type="error" ghost @click="handleClearFrontendLogs">
            清空前端日志
          </NButton>
        </div>
        <CrudProTable
          :columns="frontendColumns"
          :data="frontendRows"
          :show-toolbar="false"
          :show-pagination="false"
        />
      </NTabPane>
    </NTabs>
  </NCard>
</template>
