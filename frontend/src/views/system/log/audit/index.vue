<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import { NButton, NModal, NPopconfirm, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { clearAuditLogApi } from '@/api'
import requestClient from '@/api/request'
import { buildPageRequest, flattenPageResponse } from '@/api/helpers'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemLogAuditPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const detailVisible = ref(false)
const detailData = ref<Record<string, any>>({})

const queryParams = reactive({
  keyword: '',
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return requestClient.post('/api/AuditLog/Page', buildPageRequest({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
  })).then(flattenPageResponse)
}

const options = useVxeTable({
  id: 'sys_audit_log',
  name: '审计日志',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'userName', title: '用户名', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'realName', title: '真实姓名', minWidth: 100, showOverflow: 'tooltip' },
    { field: 'departmentName', title: '部门', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'auditType', title: '审计类型', width: 100 },
    { field: 'operationType', title: '操作类型', width: 100 },
    { field: 'module', title: '模块', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'function', title: '功能', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'entityType', title: '实体类型', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'entityName', title: '实体名称', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'description', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
    { field: 'changedFields', title: '变更字段', minWidth: 160, showOverflow: 'tooltip' },
    { field: 'changeDescription', title: '变更描述', minWidth: 180, showOverflow: 'tooltip' },
    {
      field: 'isSuccess',
      title: '结果',
      width: 80,
      slots: { default: 'col_result' },
    },
    {
      field: 'riskLevel',
      title: '风险等级',
      width: 100,
      slots: { default: 'col_risk' },
    },
    { field: 'operationIp', title: 'IP 地址', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'operationLocation', title: '操作地点', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'browser', title: '浏览器', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'os', title: '操作系统', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'executionTime', title: '耗时(ms)', width: 100, sortable: true },
    { field: 'auditTime', title: '审计时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    { field: 'createdTime', title: '创建时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue) },
    {
      title: '操作',
      width: 80,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
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
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleDetail(row: Record<string, any>) {
  detailData.value = row
  detailVisible.value = true
}

async function handleClear() {
  try {
    await clearAuditLogApi()
    message.success('清空成功')
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('清空失败')
  }
}
</script>

<template>
  <div class="h-full flex flex-col gap-2 overflow-hidden p-3">
    <vxe-card style="padding: 10px 16px">
      <div class="flex items-center gap-3">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索用户名/模块/实体"
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
            确认清空所有审计日志？
          </NPopconfirm>
        </template>
        <template #col_result="{ row }">
          <NTag :type="row.isSuccess ? 'success' : 'error'" size="small">
            {{ row.isSuccess ? '成功' : '失败' }}
          </NTag>
        </template>
        <template #col_risk="{ row }">
          <NTag
            :type="row.riskLevel >= 4 ? 'error' : row.riskLevel >= 3 ? 'warning' : row.riskLevel >= 2 ? 'info' : 'success'"
            size="small"
          >
            {{ ['低', '低', '中', '高', '极高', '极高'][row.riskLevel] ?? '未知' }}
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
      title="审计详情"
      preset="card"
      style="width: 800px; max-height: 80vh"
      :auto-focus="false"
    >
      <div class="space-y-3 text-sm">
        <div><span class="font-medium text-gray-500">用户：</span>{{ detailData.userName }} ({{ detailData.realName }})</div>
        <div><span class="font-medium text-gray-500">部门：</span>{{ detailData.departmentName }}</div>
        <div><span class="font-medium text-gray-500">审计类型：</span>{{ detailData.auditType }}</div>
        <div><span class="font-medium text-gray-500">操作类型：</span>{{ detailData.operationType }}</div>
        <div><span class="font-medium text-gray-500">模块/功能：</span>{{ detailData.module }} / {{ detailData.function }}</div>
        <div><span class="font-medium text-gray-500">实体：</span>{{ detailData.entityType }} - {{ detailData.entityName }} (ID: {{ detailData.entityId }})</div>
        <div><span class="font-medium text-gray-500">表名/主键：</span>{{ detailData.tableName }} / {{ detailData.primaryKey }} = {{ detailData.primaryKeyValue }}</div>
        <div><span class="font-medium text-gray-500">描述：</span>{{ detailData.description }}</div>
        <div><span class="font-medium text-gray-500">变更描述：</span>{{ detailData.changeDescription }}</div>
        <div><span class="font-medium text-gray-500">请求：</span>{{ detailData.requestMethod }} {{ detailData.requestPath }}</div>
        <div><span class="font-medium text-gray-500">IP/地点：</span>{{ detailData.operationIp }} {{ detailData.operationLocation }}</div>
        <div v-if="detailData.changedFields">
          <span class="font-medium text-gray-500">变更字段：</span>
          <pre class="mt-1 max-h-72 overflow-auto rounded bg-gray-100 p-3 text-xs dark:bg-gray-800">{{ detailData.changedFields }}</pre>
        </div>
        <div v-if="detailData.beforeData">
          <span class="font-medium text-gray-500">变更前：</span>
          <pre class="mt-1 max-h-72 overflow-auto rounded bg-gray-100 p-3 text-xs dark:bg-gray-800">{{ detailData.beforeData }}</pre>
        </div>
        <div v-if="detailData.afterData">
          <span class="font-medium text-gray-500">变更后：</span>
          <pre class="mt-1 max-h-72 overflow-auto rounded bg-gray-100 p-3 text-xs dark:bg-gray-800">{{ detailData.afterData }}</pre>
        </div>
        <div v-if="detailData.exceptionMessage">
          <span class="font-medium text-gray-500">异常信息：</span>{{ detailData.exceptionMessage }}
        </div>
      </div>
    </NModal>
  </div>
</template>
