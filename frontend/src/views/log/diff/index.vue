<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { DiffLogDetailDto, DiffLogListItemDto, PageResult } from '@/api'
import { NTag, useMessage } from 'naive-ui'
import { h, ref } from 'vue'
import { AuditOperationType, AuditRiskLevel, createPageRequest, diffLogApi } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogDiffPage' })

const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<DiffLogDetailDto | null>(null)

// SchemaSelectOption 仅支持 string|number，布尔筛选用 1/0 表示，请求时转换
const successOptions = [
  { label: '成功', value: 1 },
  { label: '失败', value: 0 },
]

const operationTypeOptions = [
  { label: '查询', value: AuditOperationType.Query },
  { label: '新增', value: AuditOperationType.Create },
  { label: '修改', value: AuditOperationType.Update },
  { label: '删除', value: AuditOperationType.Delete },
  { label: '导入', value: AuditOperationType.Import },
  { label: '导出', value: AuditOperationType.Export },
  { label: '其他', value: AuditOperationType.Other },
]

const riskLevelOptions = [
  { label: '低', value: AuditRiskLevel.Low },
  { label: '中', value: AuditRiskLevel.Medium },
  { label: '高', value: AuditRiskLevel.High },
  { label: '很高', value: AuditRiskLevel.VeryHigh },
  { label: '严重', value: AuditRiskLevel.Critical },
]

/** 风险等级 → 标签类型 */
function riskTagType(level: AuditRiskLevel) {
  if (level === AuditRiskLevel.Critical || level === AuditRiskLevel.VeryHigh) {
    return 'error'
  }
  if (level === AuditRiskLevel.High) {
    return 'warning'
  }
  if (level === AuditRiskLevel.Medium) {
    return 'info'
  }
  return 'default'
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索实体/表/用户', order: 0 },
  { key: 'userId', title: '用户主键', dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: '用户名', dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'entityType', title: '实体类型', dataType: 'string', advancedSearch: true, minWidth: 140, order: 12 },
  { key: 'entityName', title: '实体名称', dataType: 'string', advancedSearch: true, minWidth: 140, order: 13 },
  { key: 'tableName', title: '数据表', dataType: 'string', advancedSearch: true, minWidth: 140, order: 14 },
  { key: 'entityId', title: '实体主键', dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  {
    key: 'operationType',
    title: '操作类型',
    dataType: 'enum',
    searchable: true,
    options: operationTypeOptions,
    searchPlaceholder: '操作类型',
    width: 90,
    order: 16,
    render: row => getOptionLabel(operationTypeOptions, (row as unknown as DiffLogListItemDto).operationType),
  },
  { key: 'changeDescription', title: '变更摘要', dataType: 'string', minWidth: 220, order: 17 },
  {
    key: 'riskLevel',
    title: '风险等级',
    dataType: 'enum',
    searchable: true,
    options: riskLevelOptions,
    searchPlaceholder: '风险等级',
    width: 100,
    order: 18,
    render: (row) => {
      const level = (row as unknown as DiffLogListItemDto).riskLevel
      return h(NTag, { size: 'small', round: true, bordered: false, type: riskTagType(level) }, () => getOptionLabel(riskLevelOptions, level))
    },
  },
  {
    key: 'isSuccess',
    title: '执行结果',
    dataType: 'enum',
    searchable: true,
    options: successOptions,
    searchPlaceholder: '执行结果',
    width: 90,
    order: 19,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as DiffLogListItemDto).isSuccess ? 'success' : 'error' }, () => (row as unknown as DiffLogListItemDto).isSuccess ? '成功' : '失败'),
  },
  { key: 'executionTime', title: '执行耗时', dataType: 'number', sortable: true, width: 110, order: 20, render: row => `${(row as unknown as DiffLogListItemDto).executionTime}ms` },
  { key: 'operationIp', title: '操作 IP', dataType: 'string', minWidth: 130, order: 21 },
  { key: 'traceId', title: '链路追踪 ID', dataType: 'string', advancedSearch: true, minWidth: 160, order: 22 },
  { key: 'auditTime', title: '审计时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 24 },
  // 仅高级搜索
  { key: 'minExecutionTime', title: '最小耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最小耗时(ms)', order: 40 },
  { key: 'maxExecutionTime', title: '最大耗时(ms)', dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: '最大耗时(ms)', order: 41 },
  { key: 'auditTimeStart', title: '开始时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '开始时间', order: 42 },
  { key: 'auditTimeEnd', title: '结束时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '结束时间', order: 43 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

const schema: PageSchema = {
  pageCode: 'log.diff',
  pageName: '数据变更',
  rowKey: 'basicId',
  scrollX: 2300,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return diffLogApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        operationType: (f.operationType as AuditOperationType | undefined) ?? undefined,
        riskLevel: (f.riskLevel as AuditRiskLevel | undefined) ?? undefined,
        isSuccess: f.isSuccess == null ? undefined : Number(f.isSuccess) === 1,
        userName: toStr(f.userName),
        userId: toStr(f.userId),
        entityType: toStr(f.entityType),
        entityName: toStr(f.entityName),
        entityId: toStr(f.entityId),
        tableName: toStr(f.tableName),
        traceId: toStr(f.traceId),
        minExecutionTime: toNum(f.minExecutionTime),
        maxExecutionTime: toNum(f.maxExecutionTime),
        auditTimeStart: toIso(f.auditTimeStart),
        auditTimeEnd: toIso(f.auditTimeEnd),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
  ],
}

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'requestId', label: '请求标识' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'auditType', label: '审计类型' },
  { key: 'operationType', label: '操作类型', options: operationTypeOptions, type: 'enum' },
  { key: 'riskLevel', label: '风险等级', options: riskLevelOptions, type: 'enum' },
  { key: 'entityType', label: '实体类型' },
  { key: 'entityName', label: '实体名称' },
  { key: 'tableName', label: '数据表' },
  { key: 'entityId', label: '实体主键' },
  { key: 'primaryKey', label: '主键名' },
  { key: 'primaryKeyValue', label: '主键值' },
  { key: 'changeDescription', label: '变更摘要', span: 2 },
  { key: 'description', label: '描述', span: 2 },
  { key: 'executionTime', label: '执行耗时', type: 'duration' },
  { key: 'operationIp', label: '操作 IP' },
  { key: 'auditTime', label: '审计时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'changedFields', label: '变更字段', type: 'code' },
  { key: 'beforeData', label: '变更前数据', type: 'code' },
  { key: 'afterData', label: '变更后数据', type: 'code' },
  { key: 'extendData', label: '扩展数据', type: 'code' },
  { key: 'exceptionMessage', label: '异常消息', type: 'code' },
  { key: 'exceptionStackTrace', label: '异常堆栈', type: 'code' },
]

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as DiffLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: DiffLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await diffLogApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载数据变更详情失败')
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <LogDetailDrawer
      v-model:show="detailVisible"
      :fields="detailFields"
      :loading="detailLoading"
      :record="detailData"
      title="数据变更详情"
    />
  </SchemaPage>
</template>
