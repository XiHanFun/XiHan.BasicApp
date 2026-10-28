<script setup lang="ts">
import type { ConditionHitRow } from '../../components/log-resources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { conditionHits } from '~/contracts'
import { renderHttpMethod } from '../../components/http-method'
import { conditionHitDetailSchema } from '../../components/log-detail-fields'
import { conditionActionLabel, conditionActionOptions, conditionPhaseLabel, conditionPhaseOptions, conditionSourceLabel, conditionSourceOptions, yesNoOptions } from '../../components/log-options'
import { conditionHitsResource } from '../../components/log-resources'
import { conditionActionTone } from '../../components/log-tones'
import LogDetailDrawer from '../../components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../../components/trace-nav'
import { useLogDetail } from '../../components/use-log-detail'

defineOptions({ name: 'AuditingConditionHitsView' })

const { t } = useI18n()
const router = useRouter()
const resource = conditionHitsResource()
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<ConditionHitRow>(id => conditionHits.get({ id }), () => t('auditing.common.detail_load_failed'))

const asRow = (row: object) => row as ConditionHitRow

// 写入方是 Access 的访问策略与权限条件（排在本批之后），在那之前这里一直是空的
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('auditing.common.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('auditing.condition_hits.keyword_placeholder'), width: 220, order: 0 },
  { key: 'occurredAt', title: t('auditing.common.occurred_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, exportable: true, minWidth: 170, order: 1 },
  { key: 'policyName', title: t('auditing.condition_hits.policy_name'), dataType: 'string', exportable: true, minWidth: 160, order: 2 },
  { key: 'policyCode', title: t('auditing.condition_hits.policy_code'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 180, order: 3 },
  {
    key: 'action',
    title: t('auditing.condition_hits.action'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: conditionActionOptions(t),
    searchPlaceholder: t('auditing.condition_hits.action'),
    exportable: true,
    width: 110,
    order: 4,
    render: row => renderTag(conditionActionLabel(t, asRow(row).action), conditionActionTone(asRow(row).action)),
  },
  {
    key: 'isBlocked',
    title: t('auditing.condition_hits.is_blocked'),
    dataType: 'enum',
    searchable: true,
    options: yesNoOptions(t('auditing.condition_hits.blocked_yes'), t('auditing.condition_hits.blocked_no')),
    searchPlaceholder: t('auditing.condition_hits.is_blocked'),
    exportable: true,
    width: 100,
    order: 5,
    render: row => renderTag(asRow(row).isBlocked ? t('auditing.condition_hits.blocked_yes') : t('auditing.condition_hits.blocked_no'), asRow(row).isBlocked ? 'danger' : 'neutral'),
  },
  { key: 'source', title: t('auditing.condition_hits.source'), dataType: 'enum', advancedSearch: true, options: conditionSourceOptions(t), exportable: true, width: 120, order: 6, render: row => conditionSourceLabel(t, asRow(row).source) },
  { key: 'phase', title: t('auditing.condition_hits.phase'), dataType: 'enum', advancedSearch: true, options: conditionPhaseOptions(t), exportable: true, width: 120, order: 7, render: row => conditionPhaseLabel(t, asRow(row).phase) },
  { key: 'hitCount', title: t('auditing.condition_hits.hit_count'), dataType: 'number', sortable: true, exportable: true, width: 90, order: 8 },
  { key: 'method', title: t('auditing.common.method'), dataType: 'string', width: 100, order: 9, render: row => renderHttpMethod(asRow(row).method) },
  { key: 'path', title: t('auditing.common.path'), dataType: 'string', advancedSearch: true, ellipsis: true, exportable: true, minWidth: 200, order: 10 },
  { key: 'userName', title: t('auditing.common.user_name'), dataType: 'string', sortable: true, advancedSearch: true, exportable: true, minWidth: 120, order: 11 },
  { key: 'clientIp', title: t('auditing.common.client_ip'), dataType: 'string', searchable: true, searchPlaceholder: t('auditing.common.client_ip'), exportable: true, minWidth: 130, order: 12 },
  { key: 'traceId', title: t('auditing.common.trace_id'), dataType: 'string', advancedSearch: true, visible: false, minWidth: 260, order: 13 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'auditing.condition-hits',
  pageName: t('auditing.condition_hits.title'),
  exportPermission: 'auditing.condition-hits.read',
  rowKey: 'id',
  fields: decorateTraceFields(fields.value, router),
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('common.actions.detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('auditing.common.trace_action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailSchema = computed(() => conditionHitDetailSchema(t))

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
      :title="t('auditing.condition_hits.detail_title')"
    />
  </SchemaPage>
</template>
