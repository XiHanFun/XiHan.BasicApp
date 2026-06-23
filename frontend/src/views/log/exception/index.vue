<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { ExceptionLogDetailDto, ExceptionLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, DeviceType, logManagementApi } from '@/api'
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
  { label: t('log.exception.severityLow'), value: 1 },
  { label: t('log.exception.severityMedium'), value: 2 },
  { label: t('log.exception.severityHigh'), value: 3 },
  { label: t('log.exception.severityCritical'), value: 4 },
  { label: t('log.exception.severityFatal'), value: 5 },
])

const deviceTypeOptions = computed(() => [
  { label: t('log.exception.deviceTypeUnknown'), value: DeviceType.Unknown },
  { label: t('log.exception.deviceTypeWeb'), value: DeviceType.Web },
  { label: 'iOS', value: DeviceType.iOS },
  { label: 'Android', value: DeviceType.Android },
  { label: 'Windows', value: DeviceType.Windows },
  { label: 'macOS', value: DeviceType.macOS },
  { label: 'Linux', value: DeviceType.Linux },
  { label: t('log.exception.deviceTypeTablet'), value: DeviceType.Tablet },
  { label: t('log.exception.deviceTypeMiniProgram'), value: DeviceType.MiniProgram },
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
  { key: 'keyword', title: t('log.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.exception.keywordPlaceholder'), order: 0 },
  // 列（顺序对齐实体 SysExceptionLog 属性声明）
  { key: 'userId', title: t('log.common.userId'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.userName'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.sessionId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'requestId', title: t('log.common.requestId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'traceId', title: t('log.common.traceId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 14 },
  { key: 'exceptionType', title: t('log.exception.exceptionType'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 15 },
  { key: 'exceptionMessage', title: t('log.exception.exceptionMessage'), dataType: 'string', minWidth: 260, order: 16 },
  { key: 'exceptionSource', title: t('log.exception.exceptionSource'), dataType: 'string', advancedSearch: true, minWidth: 140, order: 17 },
  { key: 'exceptionLocation', title: t('log.exception.exceptionLocation'), dataType: 'string', advancedSearch: true, minWidth: 200, order: 18 },
  {
    key: 'severityLevel',
    title: t('log.exception.severityLevel'),
    dataType: 'enum',
    searchable: true,
    options: severityOptions.value,
    searchPlaceholder: t('log.exception.severityLevelPlaceholder'),
    width: 90,
    order: 19,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: severityType((row as unknown as ExceptionLogListItemDto).severityLevel) }, () => getOptionLabel(severityOptions.value, (row as unknown as ExceptionLogListItemDto).severityLevel)),
  },
  { key: 'requestPath', title: t('log.exception.requestPath'), dataType: 'string', advancedSearch: true, minWidth: 200, order: 20 },
  { key: 'requestMethod', title: t('log.exception.requestMethod'), dataType: 'string', advancedSearch: true, width: 90, order: 21 },
  { key: 'controllerName', title: t('log.common.controllerName'), dataType: 'string', minWidth: 140, order: 22 },
  { key: 'actionName', title: t('log.common.actionName'), dataType: 'string', minWidth: 140, order: 23 },
  { key: 'statusCode', title: t('log.common.statusCode'), dataType: 'number', advancedSearch: true, width: 100, order: 24 },
  { key: 'operationIp', title: t('log.exception.operationIp'), dataType: 'string', searchable: true, searchPlaceholder: t('log.exception.operationIpPlaceholder'), minWidth: 130, order: 25 },
  { key: 'operationLocation', title: t('log.exception.operationLocation'), dataType: 'string', minWidth: 160, order: 26 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 27 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 28 },
  {
    key: 'deviceType',
    title: t('log.exception.deviceType'),
    dataType: 'enum',
    advancedSearch: true,
    options: deviceTypeOptions.value,
    width: 100,
    order: 29,
    render: row => getOptionLabel(deviceTypeOptions.value, (row as unknown as ExceptionLogListItemDto).deviceType),
  },
  { key: 'applicationName', title: t('log.exception.applicationName'), dataType: 'string', advancedSearch: true, minWidth: 130, order: 30 },
  { key: 'applicationVersion', title: t('log.exception.applicationVersion'), dataType: 'string', minWidth: 120, order: 31 },
  { key: 'environmentName', title: t('log.exception.environmentName'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 32 },
  { key: 'serverHostName', title: t('log.exception.serverHostName'), dataType: 'string', minWidth: 140, order: 33 },
  { key: 'threadId', title: t('log.exception.threadId'), dataType: 'number', width: 90, order: 34 },
  { key: 'processId', title: t('log.exception.processId'), dataType: 'number', width: 90, order: 35 },
  { key: 'exceptionTime', title: t('log.exception.exceptionTime'), dataType: 'datetime', sortable: true, minWidth: 170, order: 36 },
  {
    key: 'isHandled',
    title: t('log.exception.isHandled'),
    dataType: 'boolean',
    searchable: true,
    options: handledOptions.value,
    searchPlaceholder: t('log.exception.isHandledPlaceholder'),
    width: 100,
    order: 37,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as ExceptionLogListItemDto).isHandled ? 'success' : 'warning' }, () => (row as unknown as ExceptionLogListItemDto).isHandled ? t('log.exception.handled') : t('log.exception.unhandled')),
  },
  { key: 'handledTime', title: t('log.exception.handledTime'), dataType: 'datetime', minWidth: 170, order: 38 },
  { key: 'errorCode', title: t('log.exception.errorCode'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 39 },
  { key: 'createdTime', title: t('log.common.createdTime'), dataType: 'datetime', minWidth: 170, order: 40 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'exceptionTimeStart', title: t('log.common.startTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.startTime'), order: 50 },
  { key: 'exceptionTimeEnd', title: t('log.common.endTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.endTime'), order: 51 },
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
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildExceptionQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    severityLevel: toNum(f.severityLevel),
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
    deviceType: (f.deviceType as DeviceType | undefined) ?? undefined,
    traceId: toStr(f.traceId),
    requestId: toStr(f.requestId),
    sessionId: toStr(f.sessionId),
    exceptionTimeStart: toIso(f.exceptionTimeStart),
    exceptionTimeEnd: toIso(f.exceptionTimeEnd),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.exception',
  exportPermission: 'saas:exception-log:export',
  pageName: t('log.exception.pageName'),
  rowKey: 'basicId',
  scrollX: 2800,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.exception.page(buildExceptionQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.exception', buildQuery: buildExceptionQuery },
  },
  actions: [
    { key: 'view', title: t('log.common.viewDetail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basicId') },
  { key: 'sessionId', label: t('log.common.sessionId') },
  { key: 'requestId', label: t('log.common.requestId') },
  { key: 'traceId', label: t('log.common.traceId') },
  { key: 'userName', label: t('log.common.userName') },
  { key: 'userId', label: t('log.common.userId') },
  { key: 'exceptionType', label: t('log.exception.exceptionType') },
  { key: 'errorCode', label: t('log.exception.errorCode') },
  { key: 'exceptionSource', label: t('log.exception.exceptionSource') },
  { key: 'exceptionLocation', label: t('log.exception.exceptionLocation'), span: 2 },
  { key: 'severityLevel', label: t('log.exception.severityLevel'), options: severityOptions.value, type: 'enum' },
  { key: 'statusCode', label: t('log.common.statusCode') },
  { key: 'requestPath', label: t('log.exception.requestPath'), span: 2 },
  { key: 'requestMethod', label: t('log.exception.requestMethod') },
  { key: 'controllerName', label: t('log.common.controllerName') },
  { key: 'actionName', label: t('log.common.actionName') },
  { key: 'operationIp', label: t('log.exception.operationIp') },
  { key: 'operationLocation', label: t('log.exception.operationLocation') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'os', label: t('log.common.os') },
  { key: 'deviceType', label: t('log.exception.deviceType'), options: deviceTypeOptions.value, type: 'enum' },
  { key: 'applicationName', label: t('log.exception.applicationName') },
  { key: 'applicationVersion', label: t('log.exception.applicationVersion') },
  { key: 'environmentName', label: t('log.exception.environmentName') },
  { key: 'serverHostName', label: t('log.exception.serverHostName') },
  { key: 'threadId', label: t('log.exception.threadId') },
  { key: 'processId', label: t('log.exception.processId') },
  { key: 'isHandled', falseText: t('log.exception.unhandled'), label: t('log.exception.isHandled'), trueText: t('log.exception.handled'), type: 'boolean' },
  { key: 'handledBy', label: t('log.exception.handledBy') },
  { key: 'handledTime', label: t('log.exception.handledTime'), type: 'date' },
  { key: 'exceptionTime', label: t('log.exception.exceptionTime'), type: 'date' },
  { key: 'createdTime', label: t('log.common.createdTime'), type: 'date' },
  { key: 'createdId', label: t('log.common.createdId') },
  { key: 'createdBy', label: t('log.common.createdBy') },
  { key: 'handledRemark', label: t('log.exception.handledRemark'), span: 2 },
  { key: 'remark', label: t('log.common.remark'), span: 2 },
  { key: 'exceptionMessage', label: t('log.exception.exceptionMessage'), type: 'code' },
  { key: 'exceptionStackTrace', label: t('log.common.exceptionStackTrace'), type: 'code' },
  { key: 'requestParams', label: t('log.common.requestParams'), type: 'code' },
  { key: 'requestBody', label: t('log.common.requestBody'), type: 'code' },
  { key: 'requestHeaders', label: t('log.common.requestHeaders'), type: 'code' },
  { key: 'userAgent', label: t('log.common.userAgent'), type: 'code' },
  { key: 'deviceInfo', label: t('log.exception.deviceInfo'), type: 'code' },
  { key: 'extendData', label: t('log.common.extendData'), type: 'code' },
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
    message.error(t('log.exception.detailLoadFailed'))
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
      :title="t('log.exception.detailTitle')"
    />
  </SchemaPage>
</template>
