<script setup lang="ts">
import type { DiffLogRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { diffLogs } from '~/contracts'
import { diffLogDetailSchema } from '../../components/log-detail-fields'
import { diffKindLabel, diffKindOptions } from '../../components/log-options'
import { diffLogsResource } from '../../components/log-resources'
import { diffKindTone } from '../../components/log-tones'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingDiffLogsView' })

const { t } = useI18n()
const router = useRouter()
const resource = diffLogsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<DiffLogRow>(id => diffLogs.get({ id }), () => t('auditing.common.detail_load_failed'))

const asRow = (row: object) => row as DiffLogRow

// 列表不含前后值，详情才取
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.diff_logs.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  {
    key: 'kind',
    title: t('auditing.diff_logs.kind'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    options: diffKindOptions(t),
    searchPlaceholder: t('auditing.diff_logs.kind'),
    exportable: true,
    width: 100,
    order: 2,
    render: row => renderTag(diffKindLabel(t, asRow(row).kind), diffKindTone(asRow(row).kind)),
  },
  { key: 'entityType', title: t('auditing.diff_logs.entity_type'), dataType: 'string', sortable: true, searchable: true, searchPlaceholder: t('auditing.diff_logs.entity_type'), exportable: true, minWidth: 180, order: 3 },
  { key: 'tableName', title: t('auditing.diff_logs.table_name'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 160, order: 4 },
  { key: 'entityId', title: t('auditing.diff_logs.entity_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 280, order: 5 },
  { key: 'changedFields', title: t('auditing.diff_logs.changed_fields'), dataType: 'string', ellipsis: true, exportable: true, minWidth: 200, order: 6 },
  { key: 'operationId', title: t('auditing.common.operation_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 180, order: 7 },
  { key: 'userName', title: t('auditing.common.user_name'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 120, order: 8 },
  { key: 'clientIp', title: t('auditing.common.client_ip'), dataType: 'string', advancedSearch: true, minWidth: 130, order: 9 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 260, order: 10 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.diff-logs',
  pageName: t('auditing.diff_logs.title'),
  exportPermission: 'auditing.diff-logs.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => diffLogDetailSchema(t))

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
      :title="t('auditing.diff_logs.detail_title')"
    />
  </SchemaPage>
</template>
