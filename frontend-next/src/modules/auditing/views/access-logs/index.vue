<script setup lang="ts">
import type { AccessLogRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { accessLogs } from '~/contracts'
import { renderHttpMethod } from '../../components/http-method'
import { accessLogDetailSchema } from '../../components/log-detail-fields'
import { accessResultLabel, accessResultOptions, HTTP_METHOD_OPTIONS } from '../../components/log-options'
import { accessLogsResource } from '../../components/log-resources'
import { accessResultTone } from '../../components/log-tones'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingAccessLogsView' })

const { t } = useI18n()
const router = useRouter()
const resource = accessLogsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<AccessLogRow>(id => accessLogs.get({ id }), () => t('auditing.common.detail_load_failed'))

// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as AccessLogRow

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索（筛选只登记后端白名单，见 log-resources） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.access_logs.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  { key: 'method', title: t('auditing.common.method'), dataType: 'enum', advancedSearch: true, options: HTTP_METHOD_OPTIONS, width: 100, order: 2, render: row => renderHttpMethod(asRow(row).method) },
  { key: 'operationId', title: t('auditing.common.operation_id'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 180, order: 3 },
  { key: 'path', title: t('auditing.common.path'), dataType: 'string', advancedSearch: true, exportable: true, ellipsis: true, minWidth: 220, order: 4 },
  {
    key: 'result',
    title: t('auditing.access_logs.result'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: accessResultOptions(t),
    searchPlaceholder: t('auditing.access_logs.result'),
    exportable: true,
    width: 110,
    order: 5,
    render: row => renderTag(accessResultLabel(t, asRow(row).result), accessResultTone(asRow(row).result)),
  },
  { key: 'statusCode', title: t('auditing.common.status_code'), dataType: 'number', sortable: true, advancedSearch: true, exportable: true, width: 100, order: 6 },
  { key: 'errorCode', title: t('auditing.common.error_code'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 180, order: 7 },
  { key: 'durationMs', title: t('auditing.common.duration'), dataType: 'number', sortable: true, exportable: true, width: 110, order: 8, render: row => `${asRow(row).durationMs}ms` },
  { key: 'userName', title: t('auditing.common.user_name'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 120, order: 9 },
  { key: 'clientIp', title: t('auditing.common.client_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('auditing.common.client_ip'), exportable: true, minWidth: 130, order: 10 },
  { key: 'browser', title: t('auditing.common.browser'), dataType: 'string', minWidth: 110, order: 11 },
  { key: 'os', title: t('auditing.common.os'), dataType: 'string', minWidth: 110, order: 12 },
  { key: 'device', title: t('auditing.common.device'), dataType: 'string', visible: false, minWidth: 100, order: 13 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 260, order: 14 },
  { key: 'sessionId', title: t('auditing.common.session_id'), dataType: 'string', advancedSearch: true, visible: false, minWidth: 280, order: 15 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.access-logs',
  pageName: t('auditing.access_logs.title'),
  exportPermission: 'auditing.access-logs.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => accessLogDetailSchema(t))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  if (payload.key === 'view' && row)
    void openDetail(row)
  else if (payload.key === 'trace' && row && !gotoTrace(router, row, row.occurredAt))
    toast.warning(t('auditing.traces.value_required'))
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <LogDetailDrawer
      v-model:show="detailVisible"
      :schema="detailSchema"
      :loading="detailLoading"
      :record="detailRecord"
      :title="t('auditing.access_logs.detail_title')"
    />
  </SchemaPage>
</template>
