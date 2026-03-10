<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { SysAuditLog } from '~/types'
import { NButton, NCard, NTag } from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { clearAuditLogApi, getAuditLogPageApi } from '@/api'
import { CrudProTable } from '~/components'
import { formatDate } from '~/utils'

const loading = ref(false)
const rows = ref<SysAuditLog[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumns<SysAuditLog> = [
  { title: '用户ID', key: 'userId', width: 100, ellipsis: { tooltip: true } },
  { title: '用户名', key: 'userName', width: 100 },
  { title: '真实姓名', key: 'realName', width: 100 },
  { title: '部门ID', key: 'departmentId', width: 100, ellipsis: { tooltip: true } },
  { title: '部门名称', key: 'departmentName', width: 120, ellipsis: { tooltip: true } },
  { title: '审计类型', key: 'auditType', width: 100 },
  { title: '操作类型', key: 'operationType', width: 100 },
  { title: '实体类型', key: 'entityType', width: 160, ellipsis: { tooltip: true } },
  { title: '实体ID', key: 'entityId', width: 120, ellipsis: { tooltip: true } },
  { title: '实体名称', key: 'entityName', width: 120, ellipsis: { tooltip: true } },
  { title: '表名', key: 'tableName', width: 140, ellipsis: { tooltip: true } },
  { title: '主键名', key: 'primaryKey', width: 100, ellipsis: { tooltip: true } },
  { title: '主键值', key: 'primaryKeyValue', width: 120, ellipsis: { tooltip: true } },
  { title: '所属模块', key: 'module', width: 120, ellipsis: { tooltip: true } },
  { title: '功能名称', key: 'function', width: 120, ellipsis: { tooltip: true } },
  { title: '操作描述', key: 'description', width: 200, ellipsis: { tooltip: true } },
  { title: '变更前数据', key: 'beforeData', width: 200, ellipsis: { tooltip: true } },
  { title: '变更后数据', key: 'afterData', width: 200, ellipsis: { tooltip: true } },
  { title: '变更字段', key: 'changedFields', width: 200, ellipsis: { tooltip: true } },
  { title: '变更描述', key: 'changeDescription', width: 200, ellipsis: { tooltip: true } },
  { title: '请求路径', key: 'requestPath', width: 200, ellipsis: { tooltip: true } },
  { title: '请求方法', key: 'requestMethod', width: 80 },
  { title: '请求参数', key: 'requestParams', width: 200, ellipsis: { tooltip: true } },
  { title: '响应结果', key: 'responseResult', width: 200, ellipsis: { tooltip: true } },
  { title: '执行耗时(ms)', key: 'executionTime', width: 110 },
  { title: '操作IP', key: 'operationIp', width: 130 },
  { title: '操作地区', key: 'operationLocation', width: 120, ellipsis: { tooltip: true } },
  { title: '浏览器', key: 'browser', width: 100 },
  { title: '操作系统', key: 'os', width: 100 },
  { title: '设备类型', key: 'deviceType', width: 100 },
  { title: '设备信息', key: 'deviceInfo', width: 140, ellipsis: { tooltip: true } },
  { title: 'UserAgent', key: 'userAgent', width: 200, ellipsis: { tooltip: true } },
  { title: '会话ID', key: 'sessionId', width: 140, ellipsis: { tooltip: true } },
  { title: '请求ID', key: 'requestId', width: 140, ellipsis: { tooltip: true } },
  { title: '业务ID', key: 'businessId', width: 120, ellipsis: { tooltip: true } },
  { title: '业务类型', key: 'businessType', width: 100 },
  {
    title: '结果',
    key: 'isSuccess',
    width: 80,
    render: row => h(NTag, {
      type: row.isSuccess ? 'success' : 'error',
      size: 'small',
      bordered: false,
    }, { default: () => row.isSuccess ? '成功' : '失败' }),
  },
  { title: '异常消息', key: 'exceptionMessage', width: 200, ellipsis: { tooltip: true } },
  { title: '异常堆栈', key: 'exceptionStackTrace', width: 300, ellipsis: { tooltip: true } },
  {
    title: '风险等级',
    key: 'riskLevel',
    width: 90,
    render: (row) => {
      const level = row.riskLevel ?? 0
      const map = [
        { label: '低', type: 'info' },
        { label: '中', type: 'warning' },
        { label: '高', type: 'error' },
      ] as const
      const item = map[level] ?? map[0]
      return h(NTag, { type: item.type, size: 'small', bordered: false }, { default: () => item.label })
    },
  },
  {
    title: '审计时间',
    key: 'auditTime',
    width: 170,
    render: row => h('span', null, formatDate(row.auditTime ?? '')),
  },
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
    const data = await getAuditLogPageApi(query)
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
  await clearAuditLogApi()
  fetchData()
}

onMounted(fetchData)
</script>

<template>
  <NCard title="审计日志" :bordered="false" size="small">
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
      :scroll-x="6200"
      max-height="calc(100vh - 280px)"
      @update:page="handlePageChange"
      @update:page-size="handlePageSizeChange"
    />
  </NCard>
</template>
