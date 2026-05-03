<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { AuditLogListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { auditLogApi, AuditOperationType, AuditRiskLevel, createPageRequest } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemAuditLogPage' })

interface LogGridResult {
  items: AuditLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<AuditLogListItemDto>>()

const queryParams = reactive({
  isSuccess: undefined as number | undefined,
  keyword: '',
  operationType: undefined as AuditOperationType | undefined,
})

const successOptions = [
  { label: '成功', value: 1 },
  { label: '失败', value: 0 },
]

const operationTypeOptions = [
  { label: '登录', value: AuditOperationType.Login },
  { label: '登出', value: AuditOperationType.Logout },
  { label: '查询', value: AuditOperationType.Query },
  { label: '新增', value: AuditOperationType.Create },
  { label: '修改', value: AuditOperationType.Update },
  { label: '删除', value: AuditOperationType.Delete },
  { label: '导入', value: AuditOperationType.Import },
  { label: '导出', value: AuditOperationType.Export },
  { label: '其他', value: AuditOperationType.Other },
]

const riskLevelOptions = [
  { label: '低风险', value: AuditRiskLevel.Low },
  { label: '中风险', value: AuditRiskLevel.Medium },
  { label: '高风险', value: AuditRiskLevel.High },
  { label: '极高风险', value: AuditRiskLevel.VeryHigh },
  { label: '严重风险', value: AuditRiskLevel.Critical },
]

function riskLevelType(level: AuditRiskLevel) {
  switch (level) {
    case AuditRiskLevel.Low: return 'info'
    case AuditRiskLevel.Medium: return 'warning'
    case AuditRiskLevel.High:
    case AuditRiskLevel.VeryHigh:
    case AuditRiskLevel.Critical: return 'error'
    default: return 'default'
  }
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return auditLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isSuccess: queryParams.isSuccess !== undefined ? queryParams.isSuccess === 1 : undefined,
      keyword: queryParams.keyword?.trim() || undefined,
      operationType: queryParams.operationType,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询审计日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<AuditLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'requestId', minWidth: 160, showOverflow: 'tooltip', title: '请求标识' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'auditType', minWidth: 100, showOverflow: 'tooltip', title: '审计类型' },
      {
        field: 'operationType',
        formatter: ({ cellValue }) => getOptionLabel(operationTypeOptions, cellValue),
        title: '操作类型',
        width: 90,
      },
      { field: 'entityType', minWidth: 120, showOverflow: 'tooltip', title: '实体类型' },
      { field: 'entityId', minWidth: 100, showOverflow: 'tooltip', title: '实体 ID' },
      { field: 'entityName', minWidth: 120, showOverflow: 'tooltip', title: '实体名称' },
      { field: 'tableName', minWidth: 120, showOverflow: 'tooltip', title: '表名称' },
      { field: 'primaryKey', minWidth: 100, showOverflow: 'tooltip', title: '主键字段' },
      { field: 'primaryKeyValue', minWidth: 100, showOverflow: 'tooltip', title: '主键值' },
      {
        field: 'isSuccess',
        slots: { default: 'col_success' },
        title: '是否成功',
        width: 90,
      },
      {
        field: 'riskLevel',
        slots: { default: 'col_risk' },
        title: '风险等级',
        width: 100,
      },
      {
        field: 'executionTime',
        formatter: ({ cellValue }) => `${cellValue}ms`,
        sortable: true,
        title: '执行耗时（毫秒）',
        width: 130,
      },
      {
        field: 'auditTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '审计时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
    ],
    id: 'sys_audit_log',
    name: '审计日志',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.isSuccess = undefined
  queryParams.operationType = undefined
  xGrid.value?.commitProxy('reload')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索实体/表名/用户"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.operationType"
          :options="operationTypeOptions"
          clearable
          placeholder="操作类型"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.isSuccess"
          :options="successOptions"
          clearable
          placeholder="结果"
          style="width: 100px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #col_success="{ row }">
          <NTag :type="row.isSuccess ? 'success' : 'error'" round size="small">
            {{ row.isSuccess ? '成功' : '失败' }}
          </NTag>
        </template>
        <template #col_risk="{ row }">
          <NTag :type="riskLevelType(row.riskLevel)" round size="small">
            {{ getOptionLabel(riskLevelOptions, row.riskLevel) }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
