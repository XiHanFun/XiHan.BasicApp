<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { AccessLogDetailDto, AccessLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { AccessResult, createPageRequest, logManagementApi, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogAccessPage' })

const { t } = useI18n()
const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<AccessLogDetailDto | null>(null)

const accessResultOptions = computed(() => [
  { label: t('log.access.result_success'), value: AccessResult.Success },
  { label: t('log.access.result_failed'), value: AccessResult.Failed },
  { label: t('log.access.result_forbidden'), value: AccessResult.Forbidden },
  { label: t('log.access.result_unauthorized'), value: AccessResult.Unauthorized },
  { label: t('log.access.result_not_found'), value: AccessResult.NotFound },
  { label: t('log.access.result_server_error'), value: AccessResult.ServerError },
])

const methodOptions = computed(() => [
  { label: t('log.method.get'), value: 'GET' },
  { label: t('log.method.post'), value: 'POST' },
  { label: t('log.method.put'), value: 'PUT' },
  { label: t('log.method.delete'), value: 'DELETE' },
  { label: t('log.method.patch'), value: 'PATCH' },
])

function accessResultType(result: AccessResult) {
  switch (result) {
    case AccessResult.Success: return 'success'
    case AccessResult.Failed: return 'error'
    case AccessResult.Forbidden:
    case AccessResult.Unauthorized: return 'warning'
    default: return 'default'
  }
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.access.keyword_placeholder'), width: 220, order: 0 },
  // 列（顺序对齐实体 SysAccessLog 属性声明）
  { key: 'userId', title: t('log.common.user_id'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.user_name'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.session_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'resourcePath', title: t('log.access.resource_path'), dataType: 'string', advancedSearch: true, minWidth: 240, order: 14 },
  { key: 'resourceName', title: t('log.access.resource_name'), dataType: 'string', minWidth: 120, order: 15 },
  { key: 'resourceType', title: t('log.access.resource_type'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 16 },
  {
    key: 'method',
    title: t('log.common.method'),
    dataType: 'enum',
    searchable: true,
    dictionaryCode: 'HttpMethodType',
    options: methodOptions.value,
    searchPlaceholder: t('log.access.method_placeholder'),
    width: 100,
    order: 17,
    // 直接展示原始方法字符串：OPTIONS/HEAD 等不在搜索选项内，避免按枚举映射后显示为空
    render: row => (row as unknown as AccessLogListItemDto).method || '-',
  },
  {
    key: 'accessResult',
    title: t('log.access.access_result'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: accessResultOptions.value,
    searchPlaceholder: t('log.access.access_result_placeholder'),
    width: 110,
    order: 18,
    render: (row) => {
      const r = row as unknown as AccessLogListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: accessResultType(r.accessResult) }, () => getOptionLabel(accessResultOptions.value, r.accessResult))
    },
  },
  { key: 'statusCode', title: t('log.common.status_code'), dataType: 'number', advancedSearch: true, sortable: true, width: 100, order: 19 },
  { key: 'accessIp', title: t('log.access.access_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('log.access.access_ip_placeholder'), minWidth: 130, order: 20 },
  { key: 'accessLocation', title: t('log.access.access_location'), dataType: 'string', minWidth: 160, order: 21 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 22 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 23 },
  { key: 'device', title: t('log.common.device'), dataType: 'string', minWidth: 120, order: 24 },
  { key: 'executionTime', title: t('log.common.execution_time'), dataType: 'number', sortable: true, width: 110, order: 25, render: row => `${(row as unknown as AccessLogListItemDto).executionTime}ms` },
  { key: 'accessTime', title: t('log.access.access_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 26 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 27 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'minExecutionTime', title: t('log.common.min_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.min_execution_time'), order: 40 },
  { key: 'maxExecutionTime', title: t('log.common.max_execution_time'), dataType: 'number', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.max_execution_time'), order: 41 },
  { key: 'accessTimeStart', title: t('log.common.start_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.start_time'), order: 42 },
  { key: 'accessTimeEnd', title: t('log.common.end_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.end_time'), order: 43 },
])

/** 过滤值辅助：trim 字符串 / 转数字 / 时间戳转 ISO */
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
function buildAccessQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sortField, params.sortOrder) },
    }),
    keyword: toStr(f.keyword),
    accessResult: (f.accessResult as AccessResult | undefined) ?? undefined,
    method: toStr(f.method),
    accessIp: toStr(f.accessIp),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    resourcePath: toStr(f.resourcePath),
    resourceType: toStr(f.resourceType),
    sessionId: toStr(f.sessionId),
    traceId: toStr(f.traceId),
    statusCode: toNum(f.statusCode),
    minExecutionTime: toNum(f.minExecutionTime),
    maxExecutionTime: toNum(f.maxExecutionTime),
    accessTimeStart: toIso(f.accessTimeStart),
    accessTimeEnd: toIso(f.accessTimeEnd),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.access',
  exportPermission: 'saas:access-log:export',
  pageName: t('log.access.page_name'),
  rowKey: 'basicId',
  scrollX: 2200,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.access.page(buildAccessQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.access', buildQuery: buildAccessQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basic_id') },
  { key: 'sessionId', label: t('log.common.session_id') },
  { key: 'traceId', label: t('log.common.trace_id') },
  { key: 'userName', label: t('log.common.user_name') },
  { key: 'userId', label: t('log.common.user_id') },
  { key: 'resourcePath', label: t('log.access.resource_path'), span: 2 },
  { key: 'resourceName', label: t('log.access.resource_name') },
  { key: 'resourceType', label: t('log.access.resource_type') },
  { key: 'method', label: t('log.common.method') },
  { key: 'statusCode', label: t('log.common.status_code') },
  { key: 'accessResult', label: t('log.access.access_result'), options: accessResultOptions.value, type: 'enum' },
  { key: 'executionTime', label: t('log.common.execution_time'), type: 'duration' },
  { key: 'accessIp', label: t('log.access.access_ip') },
  { key: 'accessLocation', label: t('log.access.access_location') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'os', label: t('log.common.os') },
  { key: 'device', label: t('log.common.device') },
  { key: 'referer', label: t('log.common.referer'), span: 2 },
  { key: 'accessTime', label: t('log.access.access_time'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.created_id') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'remark', label: t('common.fields.remark'), span: 2 },
  { key: 'userAgent', label: t('log.common.user_agent'), type: 'code' },
  { key: 'errorMessage', label: t('log.common.error_message'), type: 'code' },
  { key: 'extendData', label: t('log.common.extend_data'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AccessLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

async function handleDetail(row: AccessLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.access.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error(t('log.access.detail_load_failed'))
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
      :title="t('log.access.detail_title')"
    />
  </SchemaPage>
</template>
