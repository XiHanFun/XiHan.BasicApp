<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { DiffLogDetailDto, DiffLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { AuditOperationType, AuditRiskLevel, createPageRequest, diffLogApi, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import { diffLogDetailFields } from '../_components/log-detail-fields'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../_components/trace-nav'

defineOptions({ name: 'LogDiffPage' })

const { t } = useI18n()
const message = useMessage()
const router = useRouter()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<DiffLogDetailDto | null>(null)

// SchemaSelectOption 仅支持 string|number，布尔筛选用 1/0 表示，请求时转换
const successOptions = computed(() => [
  { label: t('log.diff.result_success'), value: 1 },
  { label: t('log.diff.result_failed'), value: 0 },
])

const operationTypeOptions = computed(() => [
  { label: t('log.diff.type_create'), value: AuditOperationType.Create },
  { label: t('log.diff.type_update'), value: AuditOperationType.Update },
  { label: t('log.diff.type_delete'), value: AuditOperationType.Delete },
  { label: t('log.diff.type_restore'), value: AuditOperationType.Restore },
  { label: t('log.diff.type_import'), value: AuditOperationType.Import },
  { label: t('log.diff.type_export'), value: AuditOperationType.Export },
  { label: t('log.diff.type_query'), value: AuditOperationType.Query },
  { label: t('log.diff.type_other'), value: AuditOperationType.Other },
])

const riskLevelOptions = computed(() => [
  { label: t('log.diff.risk_low'), value: AuditRiskLevel.Low },
  { label: t('log.diff.risk_medium'), value: AuditRiskLevel.Medium },
  { label: t('log.diff.risk_high'), value: AuditRiskLevel.High },
  { label: t('log.diff.risk_very_high'), value: AuditRiskLevel.VeryHigh },
  { label: t('log.diff.risk_critical'), value: AuditRiskLevel.Critical },
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
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.diff.keyword_placeholder'), order: 0 },
  { key: 'userId', title: t('log.common.user_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.user_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 100, order: 11 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 160, order: 12 },
  { key: 'entityType', title: t('log.diff.entity_type'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 13 },
  { key: 'entityName', title: t('log.diff.entity_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 14 },
  { key: 'tableName', title: t('log.diff.table_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 140, order: 15 },
  { key: 'entityId', title: t('log.diff.entity_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 120, order: 16 },
  {
    key: 'operationType',
    title: t('log.diff.operation_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: operationTypeOptions.value,
    searchPlaceholder: t('log.diff.operation_type_placeholder'),
    width: 90,
    order: 17,
    render: row => getOptionLabel(operationTypeOptions.value, (row as unknown as DiffLogListItemDto).operationType),
  },
  { key: 'changeDescription', title: t('log.diff.change_description'), dataType: 'string', minWidth: 220, order: 18 },
  {
    key: 'riskLevel',
    title: t('log.diff.risk_level'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: riskLevelOptions.value,
    searchPlaceholder: t('log.diff.risk_level_placeholder'),
    width: 100,
    order: 19,
    render: (row) => {
      const level = (row as unknown as DiffLogListItemDto).riskLevel
      return h(NTag, { size: 'small', round: true, bordered: false, type: riskTagType(level) }, () => getOptionLabel(riskLevelOptions.value, level))
    },
  },
  {
    key: 'isSuccess',
    title: t('log.diff.is_success'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: successOptions.value,
    searchPlaceholder: t('log.diff.is_success_placeholder'),
    width: 90,
    order: 20,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as DiffLogListItemDto).isSuccess ? 'success' : 'error' }, () => (row as unknown as DiffLogListItemDto).isSuccess ? t('log.diff.result_success') : t('log.diff.result_failed')),
  },
  { key: 'executionTime', title: t('log.common.execution_time'), dataType: 'number', sortable: true, width: 110, order: 21, render: row => `${(row as unknown as DiffLogListItemDto).executionTime}ms` },
  { key: 'operationIp', title: t('log.diff.operation_ip'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.diff.operation_ip_placeholder'), minWidth: 130, order: 22 },
  { key: 'auditTime', title: t('log.diff.audit_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 24 },
  // 仅高级搜索
  { key: 'minExecutionTime', title: t('log.common.min_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.min_execution_time'), order: 40 },
  { key: 'maxExecutionTime', title: t('log.common.max_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.max_execution_time'), order: 41 },
  { key: 'auditTime', title: t('log.diff.audit_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchRange: true, searchPlaceholder: t('log.diff.audit_time'), order: 42 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}

/** 查询构建（resource.page 与导出快照复用）。区间(auditTime)/多选(operationType/riskLevel)走 conditions.filters，FLS 门控后由框架统一应用 */
function buildDiffQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
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
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.diff',
  exportPermission: 'saas:diff-log:export',
  pageName: t('log.diff.page_name'),
  rowKey: 'basicId',
  scrollX: 2300,
  fields: decorateTraceFields(fields.value, router, { timeField: 'auditTime', ipKey: 'operationIp' }),
  resource: {
    page: params => diffLogApi.page(buildDiffQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.diff', buildQuery: buildDiffQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('log.trace.action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => diffLogDetailFields(t))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as DiffLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
  else if (payload.key === 'trace' && row) {
    if (!gotoTrace(router, row, row.auditTime)) {
      message.warning(t('log.trace.value_required'))
    }
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
    message.error(t('log.diff.detail_load_failed'))
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
      :title="t('log.diff.detail_title')"
    />
  </SchemaPage>
</template>
