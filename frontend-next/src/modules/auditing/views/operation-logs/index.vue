<script setup lang="ts">
import type { OperationLogRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { operationLogs } from '~/contracts'
import { operationLogDetailSchema } from '../../components/log-detail-fields'
import { yesNoOptions } from '../../components/log-options'
import { operationLogsResource } from '../../components/log-resources'
import { booleanTone } from '../../components/log-tones'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingOperationLogsView' })

const { t } = useI18n()
const router = useRouter()
const resource = operationLogsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<OperationLogRow>(id => operationLogs.get({ id }), () => t('auditing.common.detail_load_failed'))

const asRow = (row: object) => row as OperationLogRow

// 操作日志是框架的操作审计：只记标了审计的业务操作（读操作在访问日志里），没有客户端地址列
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.operation_logs.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  { key: 'operationId', title: t('auditing.common.operation_id'), dataType: 'string', sortable: true, searchable: true, searchPlaceholder: t('auditing.common.operation_id'), exportable: true, minWidth: 200, order: 2 },
  {
    key: 'succeeded',
    title: t('auditing.common.succeeded'),
    dataType: 'enum',
    searchable: true,
    options: yesNoOptions(t('auditing.common.success'), t('auditing.common.failed')),
    searchPlaceholder: t('auditing.common.succeeded'),
    exportable: true,
    width: 100,
    order: 3,
    render: row => renderTag(asRow(row).succeeded ? t('auditing.common.success') : t('auditing.common.failed'), booleanTone(asRow(row).succeeded)),
  },
  { key: 'errorCode', title: t('auditing.common.error_code'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 180, order: 4 },
  { key: 'errorMessage', title: t('auditing.common.error_message'), dataType: 'string', ellipsis: true, minWidth: 220, order: 5 },
  { key: 'durationMs', title: t('auditing.common.duration'), dataType: 'number', exportable: true, width: 110, order: 6, render: row => asRow(row).durationMs == null ? '-' : `${asRow(row).durationMs}ms` },
  { key: 'userName', title: t('auditing.common.user_name'), dataType: 'string', exportable: true, minWidth: 120, order: 7 },
  { key: 'userId', title: t('auditing.common.user_id'), dataType: 'string', advancedSearch: true, visible: false, minWidth: 280, order: 8 },
  { key: 'resource', title: t('auditing.operation_logs.resource'), dataType: 'string', ellipsis: true, visible: false, minWidth: 220, order: 9 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 260, order: 10 },
  { key: 'sessionId', title: t('auditing.common.session_id'), dataType: 'string', visible: false, minWidth: 280, order: 11 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.operation-logs',
  pageName: t('auditing.operation_logs.title'),
  exportPermission: 'auditing.operation-logs.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => operationLogDetailSchema(t))

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
      :title="t('auditing.operation_logs.detail_title')"
    />
  </SchemaPage>
</template>
