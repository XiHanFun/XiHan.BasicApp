<script setup lang="ts">
import type { ExceptionLogRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { SchemaPage } from '~/components'
import { toast } from '~/composables'
import { exceptionLogs } from '~/contracts'
import { renderHttpMethod } from '../../components/http-method'
import { exceptionLogDetailSchema } from '../../components/log-detail-fields'
import { exceptionLogsResource } from '../../components/log-resources'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingExceptionLogsView' })

const { t } = useI18n()
const router = useRouter()
const resource = exceptionLogsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<ExceptionLogRow>(id => exceptionLogs.get({ id }), () => t('auditing.common.detail_load_failed'))

const asRow = (row: object) => row as ExceptionLogRow

// 列表不含堆栈，详情才取（详情抽屉把消息与堆栈排在代码块最前）
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.exception_logs.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  { key: 'exceptionType', title: t('auditing.exception_logs.exception_type'), dataType: 'string', sortable: true, searchable: true, searchPlaceholder: t('auditing.exception_logs.exception_type'), exportable: true, minWidth: 220, order: 2 },
  { key: 'message', title: t('auditing.exception_logs.message'), dataType: 'string', ellipsis: true, exportable: true, minWidth: 260, order: 3 },
  { key: 'location', title: t('auditing.exception_logs.location'), dataType: 'string', ellipsis: true, minWidth: 240, order: 4 },
  { key: 'operationId', title: t('auditing.common.operation_id'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 180, order: 5 },
  { key: 'method', title: t('auditing.common.method'), dataType: 'string', width: 100, order: 6, render: row => renderHttpMethod(asRow(row).method) },
  { key: 'path', title: t('auditing.common.path'), dataType: 'string', advancedSearch: true, ellipsis: true, minWidth: 200, order: 7 },
  { key: 'source', title: t('auditing.exception_logs.source'), dataType: 'string', visible: false, minWidth: 160, order: 8 },
  { key: 'userName', title: t('auditing.common.user_name'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 120, order: 9 },
  { key: 'clientIp', title: t('auditing.common.client_ip'), dataType: 'string', advancedSearch: true, minWidth: 130, order: 10 },
  { key: 'machineName', title: t('auditing.exception_logs.machine_name'), dataType: 'string', advancedSearch: true, minWidth: 140, order: 11 },
  { key: 'environmentName', title: t('auditing.exception_logs.environment_name'), dataType: 'string', advancedSearch: true, width: 120, order: 12 },
  { key: 'appVersion', title: t('auditing.exception_logs.app_version'), dataType: 'string', visible: false, width: 140, order: 13 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 260, order: 14 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.exception-logs',
  pageName: t('auditing.exception_logs.title'),
  exportPermission: 'auditing.exception-logs.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => exceptionLogDetailSchema(t))

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
      :title="t('auditing.exception_logs.detail_title')"
    />
  </SchemaPage>
</template>
