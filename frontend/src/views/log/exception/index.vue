<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { ExceptionLogDetailDto, ExceptionLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, DeviceType, logManagementApi, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogExceptionPage' })

const { t } = useI18n()
const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ExceptionLogDetailDto | null>(null)

const handledOptions = computed(() => [
  { label: t('log.exception.handled'), value: 1 },
  { label: t('log.exception.unhandled'), value: 0 },
])

const severityOptions = computed(() => [
  { label: t('log.exception.severity_low'), value: 1 },
  { label: t('log.exception.severity_medium'), value: 2 },
  { label: t('log.exception.severity_high'), value: 3 },
  { label: t('log.exception.severity_critical'), value: 4 },
  { label: t('log.exception.severity_fatal'), value: 5 },
])

const deviceTypeOptions = computed(() => [
  { label: t('log.exception.device_type_unknown'), value: DeviceType.Unknown },
  { label: t('log.exception.device_type_web'), value: DeviceType.Web },
  { label: 'iOS', value: DeviceType.iOS },
  { label: 'Android', value: DeviceType.Android },
  { label: 'Windows', value: DeviceType.Windows },
  { label: 'macOS', value: DeviceType.macOS },
  { label: 'Linux', value: DeviceType.Linux },
  { label: t('log.exception.device_type_tablet'), value: DeviceType.Tablet },
  { label: t('log.exception.device_type_mini_program'), value: DeviceType.MiniProgram },
  { label: 'API', value: DeviceType.Api },
])

function severityType(level: number) {
  switch (level) {
    case 1: return 'info'
    case 2: return 'warning'
    case 3:
    case 4:
    case 5: return 'error'
    default: return 'default'
  }
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.exception.keyword_placeholder'), order: 0 },
  // 列（顺序对齐实体 SysExceptionLog 属性声明）
  { key: 'userId', title: t('log.common.user_id'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.user_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.session_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'requestId', title: t('log.common.request_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 14 },
  { key: 'exceptionType', title: t('log.exception.exception_type'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 160, order: 15 },
  { key: 'exceptionMessage', title: t('log.exception.exception_message'), dataType: 'string', minWidth: 260, order: 16 },
  { key: 'exceptionSource', title: t('log.exception.exception_source'), dataType: 'string', advancedSearch: true, minWidth: 140, order: 17 },
  { key: 'exceptionLocation', title: t('log.exception.exception_location'), dataType: 'string', advancedSearch: true, minWidth: 200, order: 18 },
  {
    key: 'severityLevel',
    title: t('log.exception.severity_level'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: severityOptions.value,
    searchPlaceholder: t('log.exception.severity_level_placeholder'),
    width: 90,
    order: 19,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: severityType((row as unknown as ExceptionLogListItemDto).severityLevel) }, () => getOptionLabel(severityOptions.value, (row as unknown as ExceptionLogListItemDto).severityLevel)),
  },
  { key: 'requestPath', title: t('log.exception.request_path'), dataType: 'string', advancedSearch: true, minWidth: 200, order: 20 },
  { key: 'requestMethod', title: t('log.exception.request_method'), dataType: 'string', advancedSearch: true, sortable: true, width: 90, order: 21 },
  { key: 'controllerName', title: t('log.common.controller_name'), dataType: 'string', minWidth: 140, order: 22 },
  { key: 'actionName', title: t('log.common.action_name'), dataType: 'string', minWidth: 140, order: 23 },
  { key: 'statusCode', title: t('log.common.status_code'), dataType: 'number', advancedSearch: true, sortable: true, width: 100, order: 24 },
  { key: 'operationIp', title: t('log.exception.operation_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('log.exception.operation_ip_placeholder'), minWidth: 130, order: 25 },
  { key: 'operationLocation', title: t('log.exception.operation_location'), dataType: 'string', minWidth: 160, order: 26 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 27 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 28 },
  {
    key: 'deviceType',
    title: t('log.exception.device_type'),
    dataType: 'enum',
    advancedSearch: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'DeviceType',
    options: deviceTypeOptions.value,
    width: 100,
    order: 29,
    render: row => getOptionLabel(deviceTypeOptions.value, (row as unknown as ExceptionLogListItemDto).deviceType),
  },
  { key: 'applicationName', title: t('log.exception.application_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 130, order: 30 },
  { key: 'applicationVersion', title: t('log.exception.application_version'), dataType: 'string', sortable: true, minWidth: 120, order: 31 },
  { key: 'environmentName', title: t('log.exception.environment_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 100, order: 32 },
  { key: 'serverHostName', title: t('log.exception.server_host_name'), dataType: 'string', sortable: true, minWidth: 140, order: 33 },
  { key: 'threadId', title: t('log.exception.thread_id'), dataType: 'number', sortable: true, width: 90, order: 34 },
  { key: 'processId', title: t('log.exception.process_id'), dataType: 'number', sortable: true, width: 90, order: 35 },
  { key: 'exceptionTime', title: t('log.exception.exception_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 36 },
  {
    key: 'isHandled',
    title: t('log.exception.is_handled'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: handledOptions.value,
    searchPlaceholder: t('log.exception.is_handled_placeholder'),
    width: 100,
    order: 37,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ExceptionLogListItemDto).isHandled ? 'success' : 'warning' }, () => (row as unknown as ExceptionLogListItemDto).isHandled ? t('log.exception.handled') : t('log.exception.unhandled')),
  },
  { key: 'handledTime', title: t('log.exception.handled_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 38 },
  { key: 'errorCode', title: t('log.exception.error_code'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 100, order: 39 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 40 },
  // 仅高级搜索（不作为列）：异常时间区间，经 conditions.filters Between 下发
  { key: 'exceptionTime', title: t('log.exception.exception_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchRange: true, order: 50 },
])

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
function buildExceptionQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      // 排序 + 区间(exceptionTime)/多选(severityLevel、deviceType) 等通用过滤统一走 conditions
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    isHandled: toBool(f.isHandled),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    exceptionType: toStr(f.exceptionType),
    exceptionSource: toStr(f.exceptionSource),
    exceptionLocation: toStr(f.exceptionLocation),
    requestPath: toStr(f.requestPath),
    requestMethod: toStr(f.requestMethod),
    statusCode: toNum(f.statusCode),
    operationIp: toStr(f.operationIp),
    errorCode: toStr(f.errorCode),
    applicationName: toStr(f.applicationName),
    environmentName: toStr(f.environmentName),
    traceId: toStr(f.traceId),
    requestId: toStr(f.requestId),
    sessionId: toStr(f.sessionId),
    // severityLevel/deviceType 改为多选、exceptionTime 改为区间，均经 conditions.filters 下发（不再走 DTO 顶层单值）
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.exception',
  exportPermission: 'saas:exception-log:export',
  pageName: t('log.exception.page_name'),
  rowKey: 'basicId',
  scrollX: 2800,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.exception.page(buildExceptionQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.exception', buildQuery: buildExceptionQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basic_id') },
  { key: 'sessionId', label: t('log.common.session_id') },
  { key: 'requestId', label: t('log.common.request_id') },
  { key: 'traceId', label: t('log.common.trace_id') },
  { key: 'userName', label: t('log.common.user_name') },
  { key: 'userId', label: t('log.common.user_id') },
  { key: 'exceptionType', label: t('log.exception.exception_type') },
  { key: 'errorCode', label: t('log.exception.error_code') },
  { key: 'exceptionSource', label: t('log.exception.exception_source') },
  { key: 'exceptionLocation', label: t('log.exception.exception_location'), span: 2 },
  { key: 'severityLevel', label: t('log.exception.severity_level'), options: severityOptions.value, type: 'enum' },
  { key: 'statusCode', label: t('log.common.status_code') },
  { key: 'requestPath', label: t('log.exception.request_path'), span: 2 },
  { key: 'requestMethod', label: t('log.exception.request_method') },
  { key: 'controllerName', label: t('log.common.controller_name') },
  { key: 'actionName', label: t('log.common.action_name') },
  { key: 'operationIp', label: t('log.exception.operation_ip') },
  { key: 'operationLocation', label: t('log.exception.operation_location') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'os', label: t('log.common.os') },
  { key: 'deviceType', label: t('log.exception.device_type'), options: deviceTypeOptions.value, type: 'enum' },
  { key: 'applicationName', label: t('log.exception.application_name') },
  { key: 'applicationVersion', label: t('log.exception.application_version') },
  { key: 'environmentName', label: t('log.exception.environment_name') },
  { key: 'serverHostName', label: t('log.exception.server_host_name') },
  { key: 'threadId', label: t('log.exception.thread_id') },
  { key: 'processId', label: t('log.exception.process_id') },
  { key: 'isHandled', falseText: t('log.exception.unhandled'), label: t('log.exception.is_handled'), trueText: t('log.exception.handled'), type: 'boolean' },
  { key: 'handledBy', label: t('log.exception.handled_by') },
  { key: 'handledTime', label: t('log.exception.handled_time'), type: 'date' },
  { key: 'exceptionTime', label: t('log.exception.exception_time'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.created_id') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'handledRemark', label: t('log.exception.handled_remark'), span: 2 },
  { key: 'remark', label: t('common.fields.remark'), span: 2 },
  { key: 'exceptionMessage', label: t('log.exception.exception_message'), type: 'code' },
  { key: 'exceptionStackTrace', label: t('log.common.exception_stack_trace'), type: 'code' },
  { key: 'requestParams', label: t('log.common.request_params'), type: 'code' },
  { key: 'requestBody', label: t('log.common.request_body'), type: 'code' },
  { key: 'requestHeaders', label: t('log.common.request_headers'), type: 'code' },
  { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
  { key: 'deviceInfo', label: t('log.exception.device_info'), type: 'code' },
  { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ExceptionLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: ExceptionLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.exception.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error(t('log.exception.detail_load_failed'))
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
      :title="t('log.exception.detail_title')"
    />
  </SchemaPage>
</template>
