<script setup lang="ts">
import type { PermissionChangeLogRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { permissionChangeLogs } from '~/contracts'
import { permissionChangeLogDetailSchema } from '../../components/log-detail-fields'
import { permissionChangeLabel, permissionChangeOptions } from '../../components/log-options'
import { permissionChangeLogsResource } from '../../components/log-resources'
import { permissionChangeTone } from '../../components/log-tones'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingPermissionChangeLogsView' })

const { t } = useI18n()
const router = useRouter()
const resource = permissionChangeLogsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<PermissionChangeLogRow>(id => permissionChangeLogs.get({ id }), () => t('auditing.common.detail_load_failed'))

const asRow = (row: object) => row as PermissionChangeLogRow

// 操作人是公共列里的用户；目标是角色、用户或套餐，按变更类型取其一
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.permission_change_logs.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  {
    key: 'changeType',
    title: t('auditing.permission_change_logs.change_type'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    options: permissionChangeOptions(t),
    searchPlaceholder: t('auditing.permission_change_logs.change_type'),
    exportable: true,
    width: 140,
    order: 2,
    render: row => renderTag(permissionChangeLabel(t, asRow(row).changeType), permissionChangeTone(asRow(row).changeType)),
  },
  { key: 'roleName', title: t('auditing.permission_change_logs.role_name'), dataType: 'string', sortable: true, exportable: true, minWidth: 140, order: 3 },
  { key: 'targetUserName', title: t('auditing.permission_change_logs.target_user_name'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 130, order: 4 },
  { key: 'editionCode', title: t('auditing.permission_change_logs.edition_code'), dataType: 'string', exportable: true, minWidth: 120, order: 5 },
  { key: 'targetTenantId', title: t('auditing.permission_change_logs.target_tenant'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 120, order: 6 },
  { key: 'permissionCodes', title: t('auditing.permission_change_logs.permission_codes'), dataType: 'string', advancedSearch: true, ellipsis: true, exportable: true, minWidth: 240, order: 7 },
  { key: 'detail', title: t('auditing.permission_change_logs.detail'), dataType: 'string', ellipsis: true, minWidth: 160, order: 8 },
  { key: 'userName', title: t('auditing.permission_change_logs.operator'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 120, order: 9 },
  { key: 'clientIp', title: t('auditing.common.client_ip'), dataType: 'string', advancedSearch: true, minWidth: 130, order: 10 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, exportable: true, minWidth: 260, order: 11 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.permission-change-logs',
  pageName: t('auditing.permission_change_logs.title'),
  exportPermission: 'auditing.permission-change-logs.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => permissionChangeLogDetailSchema(t))

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
      :title="t('auditing.permission_change_logs.detail_title')"
    />
  </SchemaPage>
</template>
