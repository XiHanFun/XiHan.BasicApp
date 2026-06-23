<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { DiffLogDetailDto, DiffLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { AuditOperationType, AuditRiskLevel, createPageRequest, diffLogApi } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogDiffPage' })

const { t } = useI18n()
const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<DiffLogDetailDto | null>(null)

// SchemaSelectOption 仅支持 string|number，布尔筛选用 1/0 表示，请求时转换
const successOptions = computed(() => [
  { label: t('log.diff.resultSuccess'), value: 1 },
  { label: t('log.diff.resultFailed'), value: 0 },
])

const operationTypeOptions = computed(() => [
  { label: t('log.diff.typeCreate'), value: AuditOperationType.Create },
  { label: t('log.diff.typeUpdate'), value: AuditOperationType.Update },
  { label: t('log.diff.typeDelete'), value: AuditOperationType.Delete },
  { label: t('log.diff.typeRestore'), value: AuditOperationType.Restore },
  { label: t('log.diff.typeImport'), value: AuditOperationType.Import },
  { label: t('log.diff.typeExport'), value: AuditOperationType.Export },
  { label: t('log.diff.typeQuery'), value: AuditOperationType.Query },
  { label: t('log.diff.typeOther'), value: AuditOperationType.Other },
])

const riskLevelOptions = computed(() => [
  { label: t('log.diff.riskLow'), value: AuditRiskLevel.Low },
  { label: t('log.diff.riskMedium'), value: AuditRiskLevel.Medium },
  { label: t('log.diff.riskHigh'), value: AuditRiskLevel.High },
  { label: t('log.diff.riskVeryHigh'), value: AuditRiskLevel.VeryHigh },
  { label: t('log.diff.riskCritical'), value: AuditRiskLevel.Critical },
])

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
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('log.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.diff.keywordPlaceholder'), order: 0 },
  { key: 'userId', title: t('log.common.userId'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.userName'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'entityType', title: t('log.diff.entityType'), dataType: 'string', advancedSearch: true, minWidth: 140, order: 12 },
  { key: 'entityName', title: t('log.diff.entityName'), dataType: 'string', advancedSearch: true, minWidth: 140, order: 13 },
  { key: 'tableName', title: t('log.diff.tableName'), dataType: 'string', advancedSearch: true, minWidth: 140, order: 14 },
  { key: 'entityId', title: t('log.diff.entityId'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  {
    key: 'operationType',
    title: t('log.diff.operationType'),
    dataType: 'enum',
    searchable: true,
    options: operationTypeOptions.value,
    searchPlaceholder: t('log.diff.operationTypePlaceholder'),
    width: 90,
    order: 16,
    render: row => getOptionLabel(operationTypeOptions.value, (row as unknown as DiffLogListItemDto).operationType),
  },
  { key: 'changeDescription', title: t('log.diff.changeDescription'), dataType: 'string', minWidth: 220, order: 17 },
  {
    key: 'riskLevel',
    title: t('log.diff.riskLevel'),
    dataType: 'enum',
    searchable: true,
    options: riskLevelOptions.value,
    searchPlaceholder: t('log.diff.riskLevelPlaceholder'),
    width: 100,
    order: 18,
    render: (row) => {
      const level = (row as unknown as DiffLogListItemDto).riskLevel
      return h(NTag, { size: 'small', round: true, bordered: false, type: riskTagType(level) }, () => getOptionLabel(riskLevelOptions.value, level))
    },
  },
  {
    key: 'isSuccess',
    title: t('log.diff.isSuccess'),
    dataType: 'enum',
    searchable: true,
    options: successOptions.value,
    searchPlaceholder: t('log.diff.isSuccessPlaceholder'),
    width: 90,
    order: 19,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as DiffLogListItemDto).isSuccess ? 'success' : 'error' }, () => (row as unknown as DiffLogListItemDto).isSuccess ? t('log.diff.resultSuccess') : t('log.diff.resultFailed')),
  },
  { key: 'executionTime', title: t('log.common.executionTime'), dataType: 'number', sortable: true, width: 110, order: 20, render: row => `${(row as unknown as DiffLogListItemDto).executionTime}ms` },
  { key: 'operationIp', title: t('log.diff.operationIp'), dataType: 'string', searchable: true, searchPlaceholder: t('log.diff.operationIpPlaceholder'), minWidth: 130, order: 21 },
  { key: 'traceId', title: t('log.common.traceId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 22 },
  { key: 'auditTime', title: t('log.diff.auditTime'), dataType: 'datetime', sortable: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: t('log.common.createdTime'), dataType: 'datetime', minWidth: 170, order: 24 },
  // 仅高级搜索
  { key: 'minExecutionTime', title: t('log.common.minExecutionTime'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.minExecutionTime'), order: 40 },
  { key: 'maxExecutionTime', title: t('log.common.maxExecutionTime'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.maxExecutionTime'), order: 41 },
  { key: 'auditTimeStart', title: t('log.common.startTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.startTime'), order: 42 },
  { key: 'auditTimeEnd', title: t('log.common.endTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.endTime'), order: 43 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildDiffQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
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
    operationIp: toStr(f.operationIp),
    traceId: toStr(f.traceId),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
    auditTimeStart: toIso(f.auditTimeStart),
    auditTimeEnd: toIso(f.auditTimeEnd),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.diff',
  exportPermission: 'saas:diff-log:export',
  pageName: t('log.diff.pageName'),
  rowKey: 'basicId',
  scrollX: 2300,
  fields: fields.value,
  resource: {
    page: params => diffLogApi.page(buildDiffQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.diff', buildQuery: buildDiffQuery },
  },
  actions: [
    { key: 'view', title: t('log.common.viewDetail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basicId') },
  { key: 'requestId', label: t('log.common.requestId') },
  { key: 'sessionId', label: t('log.common.sessionId') },
  { key: 'traceId', label: t('log.common.traceId') },
  { key: 'userName', label: t('log.common.userName') },
  { key: 'userId', label: t('log.common.userId') },
  { key: 'auditType', label: t('log.diff.auditType') },
  { key: 'operationType', label: t('log.diff.operationType'), options: operationTypeOptions.value, type: 'enum' },
  { key: 'riskLevel', label: t('log.diff.riskLevel'), options: riskLevelOptions.value, type: 'enum' },
  { key: 'entityType', label: t('log.diff.entityType') },
  { key: 'entityName', label: t('log.diff.entityName') },
  { key: 'tableName', label: t('log.diff.tableName') },
  { key: 'entityId', label: t('log.diff.entityId') },
  { key: 'primaryKey', label: t('log.diff.primaryKey') },
  { key: 'primaryKeyValue', label: t('log.diff.primaryKeyValue') },
  { key: 'changeDescription', label: t('log.diff.changeDescription'), span: 2 },
  { key: 'description', label: t('log.diff.description'), span: 2 },
  { key: 'executionTime', label: t('log.common.executionTime'), type: 'duration' },
  { key: 'operationIp', label: t('log.diff.operationIp') },
  { key: 'auditTime', label: t('log.diff.auditTime'), type: 'date' },
  { key: 'createdTime', label: t('log.common.createdTime'), type: 'date' },
  { key: 'changedFields', label: t('log.diff.changedFields'), type: 'code' },
  { key: 'beforeData', label: t('log.diff.beforeData'), type: 'code' },
  { key: 'afterData', label: t('log.diff.afterData'), type: 'code' },
  { key: 'extendData', label: t('log.common.extendData'), type: 'code' },
  { key: 'exceptionMessage', label: t('log.common.exceptionMessage'), type: 'code' },
  { key: 'exceptionStackTrace', label: t('log.common.exceptionStackTrace'), type: 'code' },
])

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
    message.error(t('log.diff.detailLoadFailed'))
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
      :title="t('log.diff.detailTitle')"
    />
  </SchemaPage>
</template>
