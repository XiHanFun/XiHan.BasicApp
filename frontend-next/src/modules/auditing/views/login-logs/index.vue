<script setup lang="ts">
import type { LoginLogRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { loginLogs } from '~/contracts'
import { loginLogDetailSchema } from '../../components/log-detail-fields'
import { loginEventLabel, loginEventOptions, yesNoOptions } from '../../components/log-options'
import { loginLogsResource } from '../../components/log-resources'
import { booleanTone, loginEventTone } from '../../components/log-tones'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingLoginLogsView' })

const { t } = useI18n()
const router = useRouter()
const resource = loginLogsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<LoginLogRow>(id => loginLogs.get({ id }), () => t('auditing.common.detail_load_failed'))

const asRow = (row: object) => row as LoginLogRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.login_logs.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  { key: 'userName', title: t('auditing.common.user_name'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 120, order: 2 },
  {
    key: 'event',
    title: t('auditing.login_logs.event'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    options: loginEventOptions(t),
    searchPlaceholder: t('auditing.login_logs.event'),
    exportable: true,
    width: 130,
    order: 3,
    render: row => renderTag(loginEventLabel(t, asRow(row).event), loginEventTone(asRow(row).event)),
  },
  {
    key: 'succeeded',
    title: t('auditing.common.succeeded'),
    dataType: 'enum',
    searchable: true,
    options: yesNoOptions(t('auditing.common.success'), t('auditing.common.failed')),
    searchPlaceholder: t('auditing.common.succeeded'),
    exportable: true,
    width: 100,
    order: 4,
    render: row => renderTag(asRow(row).succeeded ? t('auditing.common.success') : t('auditing.common.failed'), booleanTone(asRow(row).succeeded)),
  },
  { key: 'errorCode', title: t('auditing.common.error_code'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 180, order: 5 },
  { key: 'errorMessage', title: t('auditing.common.error_message'), dataType: 'string', ellipsis: true, minWidth: 200, order: 6 },
  { key: 'accountTenantId', title: t('auditing.login_logs.account_tenant'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 120, order: 7 },
  { key: 'detail', title: t('auditing.login_logs.detail'), dataType: 'string', ellipsis: true, minWidth: 140, order: 8 },
  { key: 'clientIp', title: t('auditing.common.client_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('auditing.common.client_ip'), exportable: true, minWidth: 130, order: 9 },
  { key: 'browser', title: t('auditing.common.browser'), dataType: 'string', minWidth: 110, order: 10 },
  { key: 'os', title: t('auditing.common.os'), dataType: 'string', minWidth: 110, order: 11 },
  { key: 'device', title: t('auditing.common.device'), dataType: 'string', visible: false, minWidth: 100, order: 12 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, visible: false, minWidth: 260, order: 13 },
  { key: 'sessionId', title: t('auditing.common.session_id'), dataType: 'string', advancedSearch: true, minWidth: 280, order: 14 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.login-logs',
  pageName: t('auditing.login_logs.title'),
  exportPermission: 'auditing.login-logs.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => loginLogDetailSchema(t))

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
      :title="t('auditing.login_logs.detail_title')"
    />
  </SchemaPage>
</template>
