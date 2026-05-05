<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { DiffLogDetailDto, DiffLogListItemDto } from '@/api'
import type { LogDetailField } from '../_components/log-detail.types'
import { NButton, NIcon, NSelect, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import { diffLogApi, AuditOperationType, AuditRiskLevel, createPageRequest } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'SystemDiffLogPage' })

interface LogGridResult {
  items: DiffLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<DiffLogListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<DiffLogDetailDto | null>(null)

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

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'requestId', label: '请求标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'auditType', label: '审计类型' },
  { key: 'operationType', label: '操作类型', options: operationTypeOptions, type: 'enum' },
  { key: 'entityType', label: '实体类型' },
  { key: 'entityId', label: '实体 ID' },
  { key: 'entityName', label: '实体名称' },
  { key: 'tableName', label: '表名称' },
  { key: 'primaryKey', label: '主键字段' },
  { key: 'primaryKeyValue', label: '主键值' },
  { key: 'operationIp', label: '操作 IP' },
  { key: 'isSuccess', falseText: '失败', label: '是否成功', trueText: '成功', type: 'boolean' },
  { key: 'riskLevel', label: '风险等级', options: riskLevelOptions, type: 'enum' },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'auditTime', label: '审计时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'description', label: '操作描述', span: 2 },
  { key: 'changeDescription', label: '变更摘要', span: 2 },
  { key: 'remark', label: '备注', span: 2 },
  { key: 'beforeData', label: '变更前数据', type: 'code' },
  { key: 'afterData', label: '变更后数据', type: 'code' },
  { key: 'changedFields', label: '变更字段', type: 'code' },
  { key: 'exceptionMessage', label: '异常消息', type: 'code' },
  { key: 'exceptionStackTrace', label: '异常堆栈', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
]

function riskLevelType(level: AuditRiskLevel) {
  switch (level) {
    case AuditRiskLevel.Low:
      return 'info'
    case AuditRiskLevel.Medium:
      return 'warning'
    case AuditRiskLevel.High:
    case AuditRiskLevel.VeryHigh:
    case AuditRiskLevel.Critical:
      return 'error'
    default:
      return 'default'
  }
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<LogGridResult> {
  return diffLogApi
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
    .then((result) => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询审计日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<DiffLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'sessionId', minWidth: 160, showOverflow: 'tooltip', title: '会话标识' },
      { field: 'traceId', minWidth: 160, showOverflow: 'tooltip', title: '链路追踪 ID' },
      { field: 'requestId', minWidth: 160, showOverflow: 'tooltip', title: '请求标识' },
      { field: 'userName', minWidth: 100, showOverflow: 'tooltip', title: '用户名' },
      { field: 'userId', minWidth: 80, showOverflow: 'tooltip', title: '用户主键' },
      { field: 'auditType', minWidth: 100, showOverflow: 'tooltip', title: '审计类型' },
      { field: 'description', minWidth: 220, showOverflow: 'tooltip', title: '操作描述' },
      { field: 'changeDescription', minWidth: 220, showOverflow: 'tooltip', title: '变更摘要' },
      {
        field: 'operationType',
        formatter: ({ cellValue }) => getOptionLabel(operationTypeOptions, cellValue),
        title: '操作类型',
        width: 90,
      },
      { field: 'operationIp', minWidth: 130, showOverflow: 'tooltip', title: '操作 IP' },
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
      { field: 'exceptionMessage', minWidth: 220, showOverflow: 'tooltip', title: '异常消息' },
      { field: 'beforeData', minWidth: 260, showOverflow: 'tooltip', title: '变更前数据' },
      { field: 'afterData', minWidth: 260, showOverflow: 'tooltip', title: '变更后数据' },
      { field: 'changedFields', minWidth: 220, showOverflow: 'tooltip', title: '变更字段' },
      { field: 'exceptionStackTrace', minWidth: 260, showOverflow: 'tooltip', title: '异常堆栈' },
      { field: 'extendData', minWidth: 240, showOverflow: 'tooltip', title: '扩展数据' },
      { field: 'remark', minWidth: 180, showOverflow: 'tooltip', title: '备注' },
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
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 86,
      },
    ],
    id: 'sys_diff_log',
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

async function handleDetail(row: DiffLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await diffLogApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载审计日志详情失败')
  }
  finally {
    detailLoading.value = false
  }
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
        <template #col_actions="{ row }">
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="审计日志详情"
    />
  </div>
</template>
