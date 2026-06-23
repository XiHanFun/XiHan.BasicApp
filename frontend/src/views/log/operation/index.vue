<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { OperationLogDetailDto, OperationLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, logManagementApi, OperationExecuteResult, OperationType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogOperationPage' })

const { t } = useI18n()
const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<OperationLogDetailDto | null>(null)

const resultOptions = computed(() => [
  { label: t('log.operation.resultSuccess'), value: OperationExecuteResult.Success },
  { label: t('log.operation.resultFailed'), value: OperationExecuteResult.Failed },
  { label: t('log.operation.resultPartialSuccess'), value: OperationExecuteResult.PartialSuccess },
])

function resultTagType(result: OperationExecuteResult) {
  switch (result) {
    case OperationExecuteResult.Success: return 'success'
    case OperationExecuteResult.PartialSuccess: return 'warning'
    default: return 'error'
  }
}

const operationTypeOptions = computed(() => [
  { label: t('log.operation.typeCreate'), value: OperationType.Create },
  { label: t('log.operation.typeUpdate'), value: OperationType.Update },
  { label: t('log.operation.typeDelete'), value: OperationType.Delete },
  { label: t('log.operation.typeReview'), value: OperationType.Review },
  { label: t('log.operation.typeImport'), value: OperationType.Import },
  { label: t('log.operation.typeExport'), value: OperationType.Export },
  { label: t('log.operation.typeApprove'), value: OperationType.Approve },
  { label: t('log.operation.typeStartTask'), value: OperationType.StartTask },
  { label: t('log.operation.typeExecute'), value: OperationType.Execute },
  { label: t('log.operation.typeRestore'), value: OperationType.Restore },
  { label: t('log.operation.typeOther'), value: OperationType.Other },
])

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.operation.keywordPlaceholder'), order: 0 },
  // 列（顺序对齐实体 SysOperationLog 属性声明）
  { key: 'userId', title: t('log.common.userId'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.userName'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.sessionId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: t('log.common.traceId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  {
    key: 'operationType',
    title: t('log.operation.operationType'),
    dataType: 'enum',
    searchable: true,
    options: operationTypeOptions.value,
    searchPlaceholder: t('log.operation.operationTypePlaceholder'),
    width: 90,
    order: 14,
    render: row => getOptionLabel(operationTypeOptions.value, (row as unknown as OperationLogListItemDto).operationType),
  },
  { key: 'module', title: t('log.operation.module'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  { key: 'function', title: t('log.operation.function'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 16 },
  { key: 'title', title: t('log.operation.title'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 17 },
  { key: 'description', title: t('log.operation.description'), dataType: 'string', minWidth: 220, order: 18 },
  { key: 'method', title: t('log.common.method'), dataType: 'string', advancedSearch: true, width: 90, order: 19 },
  { key: 'requestUrl', title: t('log.operation.requestUrl'), dataType: 'string', minWidth: 240, order: 20 },
  { key: 'executionTime', title: t('log.common.executionTime'), dataType: 'number', sortable: true, width: 110, order: 21, render: row => `${(row as unknown as OperationLogListItemDto).executionTime}ms` },
  { key: 'operationIp', title: t('log.operation.operationIp'), dataType: 'string', searchable: true, searchPlaceholder: t('log.operation.operationIpPlaceholder'), minWidth: 130, order: 22 },
  { key: 'operationLocation', title: t('log.operation.operationLocation'), dataType: 'string', minWidth: 160, order: 23 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 24 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 25 },
  {
    key: 'result',
    title: t('log.operation.result'),
    dataType: 'enum',
    searchable: true,
    options: resultOptions.value,
    searchPlaceholder: t('log.operation.resultPlaceholder'),
    width: 90,
    order: 26,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: resultTagType((row as unknown as OperationLogListItemDto).result) },
      () => getOptionLabel(resultOptions.value, (row as unknown as OperationLogListItemDto).result),
    ),
  },
  { key: 'operationTime', title: t('log.operation.operationTime'), dataType: 'datetime', sortable: true, minWidth: 170, order: 27 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, order: 28 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: t('log.common.minExecutionTime'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.minExecutionTime'), order: 40 },
  { key: 'maxExecutionTime', title: t('log.common.maxExecutionTime'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.maxExecutionTime'), order: 41 },
  { key: 'operationTimeStart', title: t('log.common.startTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.startTime'), order: 42 },
  { key: 'operationTimeEnd', title: t('log.common.endTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.endTime'), order: 43 },
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
function buildOperationQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    operationType: (f.operationType as OperationType | undefined) ?? undefined,
    result: (f.result as OperationExecuteResult | undefined) ?? undefined,
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
    operationTimeStart: toIso(f.operationTimeStart),
    operationTimeEnd: toIso(f.operationTimeEnd),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.operation',
  exportPermission: 'saas:operation-log:export',
  pageName: t('log.operation.pageName'),
  rowKey: 'basicId',
  scrollX: 2200,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.operation.page(buildOperationQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.operation', buildQuery: buildOperationQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basicId') },
  { key: 'sessionId', label: t('log.common.sessionId') },
  { key: 'traceId', label: t('log.common.traceId') },
  { key: 'userName', label: t('log.common.userName') },
  { key: 'userId', label: t('log.common.userId') },
  { key: 'operationType', label: t('log.operation.operationType'), options: operationTypeOptions.value, type: 'enum' },
  { key: 'result', label: t('log.operation.result'), options: resultOptions.value, type: 'enum' },
  { key: 'module', label: t('log.operation.module') },
  { key: 'function', label: t('log.operation.function') },
  { key: 'title', label: t('log.operation.title'), span: 2 },
  { key: 'description', label: t('log.operation.description'), span: 2 },
  { key: 'method', label: t('log.common.method') },
  { key: 'requestUrl', label: t('log.operation.requestUrl'), span: 2 },
  { key: 'executionTime', label: t('log.common.executionTime'), type: 'duration' },
  { key: 'operationIp', label: t('log.operation.operationIp') },
  { key: 'operationLocation', label: t('log.operation.operationLocation') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'os', label: t('log.common.os') },
  { key: 'operationTime', label: t('log.operation.operationTime'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.createdId') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'userAgent', label: t('log.common.userAgent'), type: 'code' },
  { key: 'errorMessage', label: t('log.common.errorMessage'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as OperationLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
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
    message.error(t('log.operation.detailLoadFailed'))
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
      :title="t('log.operation.detailTitle')"
    />
  </SchemaPage>
</template>
