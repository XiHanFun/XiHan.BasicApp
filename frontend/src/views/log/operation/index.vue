<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { OperationLogDetailDto, OperationLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { createPageRequest, logManagementApi, OperationExecuteResult, OperationType, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../_components/trace-nav'

defineOptions({ name: 'LogOperationPage' })

const { t } = useI18n()
const message = useMessage()
const router = useRouter()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<OperationLogDetailDto | null>(null)

const resultOptions = computed(() => [
  { label: t('log.operation.result_success'), value: OperationExecuteResult.Success },
  { label: t('log.operation.result_failed'), value: OperationExecuteResult.Failed },
  { label: t('log.operation.result_partial_success'), value: OperationExecuteResult.PartialSuccess },
])

function resultTagType(result: OperationExecuteResult) {
  switch (result) {
    case OperationExecuteResult.Success: return 'success'
    case OperationExecuteResult.PartialSuccess: return 'warning'
    default: return 'error'
  }
}

const operationTypeOptions = computed(() => [
  { label: t('log.operation.type_create'), value: OperationType.Create },
  { label: t('log.operation.type_update'), value: OperationType.Update },
  { label: t('log.operation.type_delete'), value: OperationType.Delete },
  { label: t('log.operation.type_review'), value: OperationType.Review },
  { label: t('log.operation.type_import'), value: OperationType.Import },
  { label: t('log.operation.type_export'), value: OperationType.Export },
  { label: t('log.operation.type_approve'), value: OperationType.Approve },
  { label: t('log.operation.type_start_task'), value: OperationType.StartTask },
  { label: t('log.operation.type_execute'), value: OperationType.Execute },
  { label: t('log.operation.type_restore'), value: OperationType.Restore },
  { label: t('log.operation.type_other'), value: OperationType.Other },
])

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.operation.keyword_placeholder'), order: 0 },
  // 列（顺序对齐实体 SysOperationLog 属性声明）
  { key: 'userId', title: t('log.common.user_id'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.user_name'), dataType: 'string', sortable: true, advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.session_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  {
    key: 'operationType',
    title: t('log.operation.operation_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: operationTypeOptions.value,
    searchPlaceholder: t('log.operation.operation_type_placeholder'),
    width: 90,
    order: 14,
    render: row => getOptionLabel(operationTypeOptions.value, (row as unknown as OperationLogListItemDto).operationType),
  },
  { key: 'module', title: t('log.operation.module'), dataType: 'string', sortable: true, advancedSearch: true, minWidth: 120, order: 15 },
  { key: 'function', title: t('log.operation.function'), dataType: 'string', sortable: true, advancedSearch: true, minWidth: 120, order: 16 },
  { key: 'title', title: t('log.operation.title'), dataType: 'string', sortable: true, advancedSearch: true, minWidth: 160, order: 17 },
  { key: 'description', title: t('log.operation.description'), dataType: 'string', minWidth: 220, order: 18 },
  { key: 'method', title: t('log.common.method'), dataType: 'string', sortable: true, advancedSearch: true, width: 90, order: 19 },
  { key: 'requestUrl', title: t('log.operation.request_url'), dataType: 'string', minWidth: 240, order: 20 },
  { key: 'executionTime', title: t('log.common.execution_time'), dataType: 'number', sortable: true, width: 110, order: 21, render: row => `${(row as unknown as OperationLogListItemDto).executionTime}ms` },
  { key: 'operationIp', title: t('log.operation.operation_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('log.operation.operation_ip_placeholder'), minWidth: 130, order: 22 },
  { key: 'operationLocation', title: t('log.operation.operation_location'), dataType: 'string', minWidth: 160, order: 23 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 24 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 25 },
  {
    key: 'result',
    title: t('log.operation.result'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: resultOptions.value,
    searchPlaceholder: t('log.operation.result_placeholder'),
    width: 90,
    order: 26,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: resultTagType((row as unknown as OperationLogListItemDto).result) },
      () => getOptionLabel(resultOptions.value, (row as unknown as OperationLogListItemDto).result),
    ),
  },
  { key: 'operationTime', title: t('log.operation.operation_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 27 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 28 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: t('log.common.min_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.min_execution_time'), order: 40 },
  { key: 'maxExecutionTime', title: t('log.common.max_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.max_execution_time'), order: 41 },
  // 操作时间区间（合并原 Start/End；走 conditions.filters Between）
  { key: 'operationTime', title: t('log.operation.operation_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchRange: true, order: 42 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化）。排序：前端选择下发 conditions.sorts，后端 FLS 门控 + 默认兜底 */
function buildOperationQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      // 排序 + 区间(operationTime)/多选(operationType、result) 等通用过滤统一走 conditions
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    // operationType、result 改为多选，operationTime 改为区间，均经 conditions.filters 下发（不再走 DTO 顶层单值）
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    module: toStr(f.module),
    function: toStr(f.function),
    title: toStr(f.title),
    method: toStr(f.method),
    operationIp: toStr(f.operationIp),
    traceId: toStr(f.traceId),
    sessionId: toStr(f.sessionId),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.operation',
  exportPermission: 'saas:operation-log:export',
  pageName: t('log.operation.page_name'),
  rowKey: 'basicId',
  scrollX: 2200,
  fields: decorateTraceFields(fields.value, router, { timeField: 'operationTime', ipKey: 'operationIp' }),
  resource: {
    page: params => logManagementApi.operation.page(buildOperationQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.operation', buildQuery: buildOperationQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('log.trace.action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basic_id') },
  { key: 'sessionId', label: t('log.common.session_id') },
  { key: 'traceId', label: t('log.common.trace_id') },
  { key: 'userName', label: t('log.common.user_name') },
  { key: 'userId', label: t('log.common.user_id') },
  { key: 'operationType', label: t('log.operation.operation_type'), options: operationTypeOptions.value, type: 'enum' },
  { key: 'result', label: t('log.operation.result'), options: resultOptions.value, type: 'enum' },
  { key: 'module', label: t('log.operation.module') },
  { key: 'function', label: t('log.operation.function') },
  { key: 'title', label: t('log.operation.title'), span: 2 },
  { key: 'description', label: t('log.operation.description'), span: 2 },
  { key: 'method', label: t('log.common.method') },
  { key: 'requestUrl', label: t('log.operation.request_url'), span: 2 },
  { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
  { key: 'operationIp', label: t('log.operation.operation_ip') },
  { key: 'operationLocation', label: t('log.operation.operation_location') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'os', label: t('log.common.os') },
  { key: 'operationTime', label: t('log.operation.operation_time'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.created_id') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
  { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as OperationLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
  else if (payload.key === 'trace' && row) {
    if (!gotoTrace(router, row, row.operationTime)) {
      message.warning(t('log.trace.value_required'))
    }
  }
}

async function handleDetail(row: OperationLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.operation.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error(t('log.operation.detail_load_failed'))
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
      :title="t('log.operation.detail_title')"
    />
  </SchemaPage>
</template>
