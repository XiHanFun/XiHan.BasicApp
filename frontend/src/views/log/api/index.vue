<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { ApiLogDetailDto, ApiLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { createPageRequest, logManagementApi, querySortsFromSchema, SignatureType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../_components/trace-nav'

defineOptions({ name: 'LogApiPage' })

const { t } = useI18n()
const message = useMessage()
const router = useRouter()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ApiLogDetailDto | null>(null)

const successOptions = computed(() => [
  { label: t('common.statuses.success'), value: 1 },
  { label: t('common.statuses.failed'), value: 0 },
])

const methodOptions = computed(() => [
  { label: t('log.method.get'), value: 'GET' },
  { label: t('log.method.post'), value: 'POST' },
  { label: t('log.method.put'), value: 'PUT' },
  { label: t('log.method.delete'), value: 'DELETE' },
  { label: t('log.method.patch'), value: 'PATCH' },
])

const signatureTypeOptions = computed(() => [
  { label: t('log.api.signature_type_none'), value: SignatureType.None },
  { label: 'HMAC-SHA256', value: SignatureType.HmacSha256 },
  { label: 'HMAC-SHA512', value: SignatureType.HmacSha512 },
  { label: 'RSA-SHA256', value: SignatureType.RsaSha256 },
  { label: 'RSA-SHA512', value: SignatureType.RsaSha512 },
  { label: 'SM2', value: SignatureType.Sm2 },
  { label: 'SM3', value: SignatureType.Sm3 },
  { label: 'Ed25519', value: SignatureType.Ed25519 },
  { label: 'MD5', value: SignatureType.Md5 },
])

function formatSize(bytes: number | string): string {
  // requestSize/responseSize 为后端 long，经 LongJsonConverter 序列化为字符串，这里显式转数值
  const size = Number(bytes)
  if (size < 1024) {
    return `${size}B`
  }
  if (size < 1048576) {
    return `${(size / 1024).toFixed(1)}KB`
  }
  return `${(size / 1048576).toFixed(1)}MB`
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.api.keyword_placeholder'), order: 0 },
  // 列（顺序对齐实体 SysOpenApiLog 属性声明）
  { key: 'userId', title: t('log.common.user_id'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.user_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.session_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'requestId', title: t('log.common.request_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 14 },
  { key: 'clientId', title: t('log.api.client_id'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  { key: 'appId', title: t('log.api.app_id'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 16 },
  {
    key: 'isSignatureValid',
    title: t('log.api.is_signature_valid'),
    dataType: 'boolean',
    advancedSearch: true,
    sortable: true,
    options: [{ label: t('log.api.signature_valid'), value: 1 }, { label: t('log.api.signature_invalid'), value: 0 }],
    width: 120,
    order: 17,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ApiLogListItemDto).isSignatureValid ? 'success' : 'warning' }, () => (row as unknown as ApiLogListItemDto).isSignatureValid ? t('log.api.signature_valid') : t('log.api.signature_invalid')),
  },
  {
    key: 'signatureType',
    title: t('log.api.signature_type'),
    dataType: 'enum',
    advancedSearch: true,
    searchMultiple: true,
    sortable: true,
    options: signatureTypeOptions.value,
    width: 120,
    order: 18,
    render: row => getOptionLabel(signatureTypeOptions.value, (row as unknown as ApiLogListItemDto).signatureType),
  },
  { key: 'apiPath', title: t('log.api.api_path'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 240, order: 19 },
  { key: 'apiName', title: t('log.api.api_name'), dataType: 'string', sortable: true, minWidth: 120, order: 20 },
  { key: 'method', title: t('log.common.method'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, dictionaryCode: 'HttpMethodType', options: methodOptions.value, searchPlaceholder: t('log.api.method_placeholder'), width: 100, order: 21 },
  { key: 'controllerName', title: t('log.common.controller_name'), dataType: 'string', minWidth: 140, order: 22 },
  { key: 'actionName', title: t('log.common.action_name'), dataType: 'string', minWidth: 140, order: 23 },
  { key: 'statusCode', title: t('log.common.status_code'), dataType: 'number', advancedSearch: true, sortable: true, width: 100, order: 24 },
  { key: 'requestIp', title: t('log.api.request_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('log.api.request_ip_placeholder'), minWidth: 130, order: 25 },
  { key: 'requestLocation', title: t('log.api.request_location'), dataType: 'string', minWidth: 160, order: 26 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 27 },
  { key: 'requestTime', title: t('log.api.request_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, minWidth: 170, order: 28 },
  { key: 'responseTime', title: t('log.api.response_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 29 },
  { key: 'executionTime', title: t('log.common.execution_time'), dataType: 'number', sortable: true, width: 110, order: 30, render: row => `${(row as unknown as ApiLogListItemDto).executionTime}ms` },
  { key: 'requestSize', title: t('log.api.request_size'), dataType: 'number', sortable: true, width: 110, order: 31, render: row => formatSize((row as unknown as ApiLogListItemDto).requestSize) },
  { key: 'responseSize', title: t('log.api.response_size'), dataType: 'number', sortable: true, width: 110, order: 32, render: row => formatSize((row as unknown as ApiLogListItemDto).responseSize) },
  {
    key: 'isSuccess',
    title: t('log.api.is_success'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: successOptions.value,
    searchPlaceholder: t('log.api.success_placeholder'),
    width: 100,
    order: 33,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ApiLogListItemDto).isSuccess ? 'success' : 'error' }, () => (row as unknown as ApiLogListItemDto).isSuccess ? t('common.statuses.success') : t('common.statuses.failed')),
  },
  { key: 'apiVersion', title: t('log.api.api_version'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 90, order: 34 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 35 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: t('log.common.min_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.min_execution_time'), order: 50 },
  { key: 'maxExecutionTime', title: t('log.common.max_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.max_execution_time'), order: 51 },
])

/** 过滤值辅助 */
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toNum(v: unknown): number | undefined {
  return v == null || v === '' ? undefined : Number(v)
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : v === 1 || v === true
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildApiQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      // 排序 + 区间(requestTime)/多选(method、signatureType) 等通用过滤统一走 conditions
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    isSuccess: toBool(f.isSuccess),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    apiPath: toStr(f.apiPath),
    requestIp: toStr(f.requestIp),
    statusCode: toNum(f.statusCode),
    isSignatureValid: toBool(f.isSignatureValid),
    clientId: toStr(f.clientId),
    appId: toStr(f.appId),
    apiVersion: toStr(f.apiVersion),
    traceId: toStr(f.traceId),
    requestId: toStr(f.requestId),
    sessionId: toStr(f.sessionId),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
    // method/signatureType 改多选、requestTime 改区间，均经 conditions.filters 下发
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.api',
  exportPermission: 'saas:api-log:export',
  pageName: t('log.api.page_name'),
  rowKey: 'basicId',
  scrollX: 2400,
  fields: decorateTraceFields(fields.value, router, { timeField: 'requestTime', ipKey: 'requestIp' }),
  resource: {
    page: params => logManagementApi.api.page(buildApiQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.api', buildQuery: buildApiQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('log.trace.action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basic_id') },
  { key: 'sessionId', label: t('log.common.session_id') },
  { key: 'requestId', label: t('log.common.request_id') },
  { key: 'traceId', label: t('log.common.trace_id') },
  { key: 'userName', label: t('log.common.user_name') },
  { key: 'userId', label: t('log.common.user_id') },
  { key: 'clientId', label: t('log.api.client_id') },
  { key: 'appId', label: t('log.api.app_id') },
  { key: 'apiPath', label: t('log.api.api_path'), span: 2 },
  { key: 'apiName', label: t('log.api.api_name') },
  { key: 'apiVersion', label: t('log.api.api_version') },
  { key: 'controllerName', label: t('log.common.controller_name') },
  { key: 'actionName', label: t('log.common.action_name') },
  { key: 'method', label: t('log.common.method') },
  { key: 'statusCode', label: t('log.common.status_code') },
  { key: 'isSuccess', falseText: t('common.statuses.failed'), label: t('log.api.is_success'), trueText: t('common.statuses.success'), type: 'boolean' },
  { key: 'isSignatureValid', falseText: t('log.api.signature_invalid'), label: t('log.api.is_signature_valid'), trueText: t('log.api.signature_valid'), type: 'boolean' },
  { key: 'signatureType', label: t('log.api.signature_type'), options: signatureTypeOptions.value, type: 'enum' },
  { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
  { key: 'requestSize', label: t('log.api.request_size'), type: 'bytes' },
  { key: 'responseSize', label: t('log.api.response_size'), type: 'bytes' },
  { key: 'requestIp', label: t('log.api.request_ip') },
  { key: 'requestLocation', label: t('log.api.request_location') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'referer', label: t('log.common.referer'), span: 2 },
  { key: 'requestTime', label: t('log.api.request_time'), type: 'date' },
  { key: 'responseTime', label: t('log.api.response_time'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.created_id') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'remark', label: t('common.fields.remark'), span: 2 },
  { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
  { key: 'requestParams', label: t('log.common.request_params'), type: 'code' },
  { key: 'requestBody', label: t('log.common.request_body'), type: 'code' },
  { key: 'responseBody', label: t('log.api.response_body'), type: 'code' },
  { key: 'requestHeaders', label: t('log.common.request_headers'), type: 'code' },
  { key: 'responseHeaders', label: t('log.common.response_headers'), type: 'code' },
  { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
  { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
  { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ApiLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
  else if (payload.key === 'trace' && row) {
    if (!gotoTrace(router, row, row.requestTime)) {
      message.warning(t('log.trace.value_required'))
    }
  }
}

async function handleDetail(row: ApiLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.api.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error(t('log.api.detail_load_failed'))
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
      :title="t('log.api.detail_title')"
    />
  </SchemaPage>
</template>
