<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { ApiLogDetailDto, ApiLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, logManagementApi, SignatureType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogApiPage' })

const { t } = useI18n()
const message = useMessage()

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
  { label: t('log.api.signatureTypeNone'), value: SignatureType.None },
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
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.api.keywordPlaceholder'), order: 0 },
  // 列（顺序对齐实体 SysOpenApiLog 属性声明）
  { key: 'userId', title: t('log.common.userId'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.userName'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.sessionId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'requestId', title: t('log.common.requestId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'traceId', title: t('log.common.traceId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 14 },
  { key: 'clientId', title: t('log.api.clientId'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 15 },
  { key: 'appId', title: t('log.api.appId'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 16 },
  {
    key: 'isSignatureValid',
    title: t('log.api.isSignatureValid'),
    dataType: 'boolean',
    advancedSearch: true,
    options: [{ label: t('log.api.signatureValid'), value: 1 }, { label: t('log.api.signatureInvalid'), value: 0 }],
    width: 120,
    order: 17,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ApiLogListItemDto).isSignatureValid ? 'success' : 'warning' }, () => (row as unknown as ApiLogListItemDto).isSignatureValid ? t('log.api.signatureValid') : t('log.api.signatureInvalid')),
  },
  {
    key: 'signatureType',
    title: t('log.api.signatureType'),
    dataType: 'enum',
    advancedSearch: true,
    options: signatureTypeOptions.value,
    width: 120,
    order: 18,
    render: row => getOptionLabel(signatureTypeOptions.value, (row as unknown as ApiLogListItemDto).signatureType),
  },
  { key: 'apiPath', title: t('log.api.apiPath'), dataType: 'string', advancedSearch: true, minWidth: 240, order: 19 },
  { key: 'apiName', title: t('log.api.apiName'), dataType: 'string', minWidth: 120, order: 20 },
  { key: 'method', title: t('log.common.method'), dataType: 'enum', searchable: true, options: methodOptions.value, searchPlaceholder: t('log.api.methodPlaceholder'), width: 100, order: 21 },
  { key: 'controllerName', title: t('log.common.controllerName'), dataType: 'string', minWidth: 140, order: 22 },
  { key: 'actionName', title: t('log.common.actionName'), dataType: 'string', minWidth: 140, order: 23 },
  { key: 'statusCode', title: t('log.common.statusCode'), dataType: 'number', advancedSearch: true, width: 100, order: 24 },
  { key: 'requestIp', title: t('log.api.requestIp'), dataType: 'string', searchable: true, searchPlaceholder: t('log.api.requestIpPlaceholder'), minWidth: 130, order: 25 },
  { key: 'requestLocation', title: t('log.api.requestLocation'), dataType: 'string', minWidth: 160, order: 26 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 27 },
  { key: 'requestTime', title: t('log.api.requestTime'), dataType: 'datetime', sortable: true, minWidth: 170, order: 28 },
  { key: 'responseTime', title: t('log.api.responseTime'), dataType: 'datetime', minWidth: 170, order: 29 },
  { key: 'executionTime', title: t('log.common.executionTime'), dataType: 'number', sortable: true, width: 110, order: 30, render: row => `${(row as unknown as ApiLogListItemDto).executionTime}ms` },
  { key: 'requestSize', title: t('log.api.requestSize'), dataType: 'number', width: 110, order: 31, render: row => formatSize((row as unknown as ApiLogListItemDto).requestSize) },
  { key: 'responseSize', title: t('log.api.responseSize'), dataType: 'number', width: 110, order: 32, render: row => formatSize((row as unknown as ApiLogListItemDto).responseSize) },
  {
    key: 'isSuccess',
    title: t('log.api.isSuccess'),
    dataType: 'boolean',
    searchable: true,
    options: successOptions.value,
    searchPlaceholder: t('log.api.successPlaceholder'),
    width: 100,
    order: 33,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ApiLogListItemDto).isSuccess ? 'success' : 'error' }, () => (row as unknown as ApiLogListItemDto).isSuccess ? t('common.statuses.success') : t('common.statuses.failed')),
  },
  { key: 'apiVersion', title: t('log.api.apiVersion'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 34 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, order: 35 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: t('log.common.minExecutionTime'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.minExecutionTime'), order: 50 },
  { key: 'maxExecutionTime', title: t('log.common.maxExecutionTime'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.maxExecutionTime'), order: 51 },
  { key: 'requestTimeStart', title: t('log.common.startTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.startTime'), order: 52 },
  { key: 'requestTimeEnd', title: t('log.common.endTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.endTime'), order: 53 },
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
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildApiQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    isSuccess: toBool(f.isSuccess),
    method: toStr(f.method),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    apiPath: toStr(f.apiPath),
    requestIp: toStr(f.requestIp),
    statusCode: toNum(f.statusCode),
    signatureType: (f.signatureType as SignatureType | undefined) ?? undefined,
    isSignatureValid: toBool(f.isSignatureValid),
    clientId: toStr(f.clientId),
    appId: toStr(f.appId),
    apiVersion: toStr(f.apiVersion),
    traceId: toStr(f.traceId),
    requestId: toStr(f.requestId),
    sessionId: toStr(f.sessionId),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
    requestTimeStart: toIso(f.requestTimeStart),
    requestTimeEnd: toIso(f.requestTimeEnd),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.api',
  exportPermission: 'saas:api-log:export',
  pageName: t('log.api.pageName'),
  rowKey: 'basicId',
  scrollX: 2400,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.api.page(buildApiQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.api', buildQuery: buildApiQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basicId') },
  { key: 'sessionId', label: t('log.common.sessionId') },
  { key: 'requestId', label: t('log.common.requestId') },
  { key: 'traceId', label: t('log.common.traceId') },
  { key: 'userName', label: t('log.common.userName') },
  { key: 'userId', label: t('log.common.userId') },
  { key: 'clientId', label: t('log.api.clientId') },
  { key: 'appId', label: t('log.api.appId') },
  { key: 'apiPath', label: t('log.api.apiPath'), span: 2 },
  { key: 'apiName', label: t('log.api.apiName') },
  { key: 'apiVersion', label: t('log.api.apiVersion') },
  { key: 'controllerName', label: t('log.common.controllerName') },
  { key: 'actionName', label: t('log.common.actionName') },
  { key: 'method', label: t('log.common.method') },
  { key: 'statusCode', label: t('log.common.statusCode') },
  { key: 'isSuccess', falseText: t('common.statuses.failed'), label: t('log.api.isSuccess'), trueText: t('common.statuses.success'), type: 'boolean' },
  { key: 'isSignatureValid', falseText: t('log.api.signatureInvalid'), label: t('log.api.isSignatureValid'), trueText: t('log.api.signatureValid'), type: 'boolean' },
  { key: 'signatureType', label: t('log.api.signatureType'), options: signatureTypeOptions.value, type: 'enum' },
  { key: 'executionTime', label: t('log.common.executionTime'), type: 'duration' },
  { key: 'requestSize', label: t('log.api.requestSize'), type: 'bytes' },
  { key: 'responseSize', label: t('log.api.responseSize'), type: 'bytes' },
  { key: 'requestIp', label: t('log.api.requestIp') },
  { key: 'requestLocation', label: t('log.api.requestLocation') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'referer', label: t('log.common.referer'), span: 2 },
  { key: 'requestTime', label: t('log.api.requestTime'), type: 'date' },
  { key: 'responseTime', label: t('log.api.responseTime'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.createdId') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'remark', label: t('common.fields.remark'), span: 2 },
  { key: 'userAgent', label: t('log.common.userAgent'), type: 'code' },
  { key: 'requestParams', label: t('log.common.requestParams'), type: 'code' },
  { key: 'requestBody', label: t('log.common.requestBody'), type: 'code' },
  { key: 'responseBody', label: t('log.api.responseBody'), type: 'code' },
  { key: 'requestHeaders', label: t('log.common.requestHeaders'), type: 'code' },
  { key: 'responseHeaders', label: t('log.common.responseHeaders'), type: 'code' },
  { key: 'errorMessage', label: t('log.common.errorMessage'), type: 'code' },
  { key: 'exceptionStackTrace', label: t('log.common.exceptionStackTrace'), type: 'code' },
  { key: 'extendData', label: t('log.common.extendData'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ApiLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
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
    message.error(t('log.api.detailLoadFailed'))
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
      :title="t('log.api.detailTitle')"
    />
  </SchemaPage>
</template>
