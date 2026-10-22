<script setup lang="ts">
import type { AccessPolicyHitListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { XhButton, XhButtonLabel, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { accessPolicyHitApi, createPageRequest, querySortsFromSchema } from '@/api'
import { ACCESS_POLICY_ACTION_OPTIONS, ACCESS_POLICY_HIT_SOURCE_OPTIONS, ACCESS_POLICY_PHASE_OPTIONS } from '@/constants'
import { SchemaPage } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import { actionTone } from '../../setting/access-policy/access-policy'
import { decorateTraceFields, gotoTrace } from '../_components/trace-nav'

defineOptions({ name: 'LogAccessPolicyPage' })

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const actionOptions = useEnumOptions('AccessPolicyAction', ACCESS_POLICY_ACTION_OPTIONS)
const phaseOptions = useEnumOptions('AccessPolicyPhase', ACCESS_POLICY_PHASE_OPTIONS)
const sourceOptions = useEnumOptions('AccessPolicyHitSource', ACCESS_POLICY_HIT_SOURCE_OPTIONS)
const blockedOptions = computed(() => [
  { label: t('log.access_policy.blocked_yes'), value: 'true' },
  { label: t('log.access_policy.blocked_no'), value: 'false' },
])

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
/** 从访问策略页跳过来时只看那一条策略 */
const routePolicyCode = ref(typeof route.query.policyCode === 'string' ? route.query.policyCode : null)

function clearRoutePolicy() {
  routePolicyCode.value = null
  void router.replace({ query: {} })
  void schemaPageRef.value?.reload()
}

function toStr(value: unknown): string | undefined {
  return (value as string | undefined)?.trim() || undefined
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'policyCode', title: t('log.access_policy.policy_code'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.access_policy.policy_code_placeholder'), minWidth: 180, order: 1 },
  { key: 'policyName', title: t('log.access_policy.policy_name'), dataType: 'string', minWidth: 160, order: 2 },
  {
    key: 'source',
    title: t('log.access_policy.source'),
    dataType: 'enum',
    advancedSearch: true,
    searchMultiple: true,
    options: sourceOptions.value,
    width: 100,
    order: 3,
    render: row => getOptionLabel(sourceOptions.value, (row as unknown as AccessPolicyHitListItemDto).source),
  },
  {
    key: 'action',
    title: t('log.access_policy.action'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: actionOptions.value,
    searchPlaceholder: t('log.access_policy.action_placeholder'),
    width: 96,
    order: 4,
    render: (row) => {
      const action = (row as unknown as AccessPolicyHitListItemDto).action
      return h(XhTagRoot, { variant: 'subtle', tone: actionTone(action) }, () => h(XhTagLabel, () => getOptionLabel(actionOptions.value, action)))
    },
  },
  {
    key: 'isBlocked',
    title: t('log.access_policy.is_blocked'),
    dataType: 'enum',
    searchable: true,
    options: blockedOptions.value,
    width: 88,
    order: 5,
    render: (row) => {
      const blocked = (row as unknown as AccessPolicyHitListItemDto).isBlocked
      return h(XhTagRoot, { variant: 'subtle', tone: blocked ? 'danger' : 'neutral' }, () => h(XhTagLabel, () => blocked ? t('log.access_policy.blocked_yes') : t('log.access_policy.blocked_no')))
    },
  },
  {
    key: 'phase',
    title: t('log.access_policy.phase'),
    dataType: 'enum',
    advancedSearch: true,
    searchMultiple: true,
    options: phaseOptions.value,
    width: 100,
    order: 6,
    render: row => getOptionLabel(phaseOptions.value, (row as unknown as AccessPolicyHitListItemDto).phase),
  },
  { key: 'hitCount', title: t('log.access_policy.hit_count'), dataType: 'number', sortable: true, width: 88, order: 7 },
  { key: 'ip', title: t('log.access_policy.ip'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.access_policy.ip_placeholder'), minWidth: 130, order: 8 },
  { key: 'location', title: t('log.access_policy.location'), dataType: 'string', minWidth: 160, order: 9 },
  { key: 'userName', title: t('log.access_policy.user_name'), dataType: 'string', advancedSearch: true, minWidth: 120, order: 10 },
  { key: 'method', title: t('log.access_policy.method'), dataType: 'string', width: 80, order: 11 },
  { key: 'path', title: t('log.access_policy.path'), dataType: 'string', advancedSearch: true, minWidth: 220, order: 12 },
  { key: 'permissionCodes', title: t('log.access_policy.permission_codes'), dataType: 'string', minWidth: 200, order: 13 },
  { key: 'clientKind', title: t('log.access_policy.client_kind'), dataType: 'string', width: 96, order: 14 },
  { key: 'userAgent', title: t('log.access_policy.user_agent'), dataType: 'string', minWidth: 220, order: 15 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 16 },
  { key: 'hitTime', title: t('log.access_policy.hit_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 17 },
  { key: 'hitTime', title: t('log.access_policy.hit_time'), dataType: 'datetime', visible: false, advancedSearch: true, searchRange: true, searchPlaceholder: t('log.access_policy.hit_time'), order: 40 },
])

/** 查询构建：动作、来源、时机多选与命中时间区间走 conditions.filters */
function buildQuery(params: SchemaQueryParams) {
  const f = params.filters
  const blocked = toStr(f.isBlocked)
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    policyCode: toStr(f.policyCode) ?? routePolicyCode.value,
    ip: toStr(f.ip),
    userName: toStr(f.userName),
    path: toStr(f.path),
    traceId: toStr(f.traceId),
    isBlocked: blocked === undefined ? null : blocked === 'true',
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.access-policy',
  pageName: t('log.access_policy.page_name'),
  rowKey: 'basicId',
  fields: decorateTraceFields(fields.value, router, {
    timeField: 'hitTime',
    ipKey: 'ip',
    extraDimensions: { userName: 'UserName' },
  }),
  resource: {
    page: params => accessPolicyHitApi.page(buildQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
  actions: [
    { key: 'trace', title: t('log.trace.action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AccessPolicyHitListItemDto | undefined
  if (payload.key === 'trace' && row && !gotoTrace(router, row, row.hitTime)) {
    toast.warning(t('log.trace.value_required'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <template v-if="routePolicyCode" #toolbar-leading>
      <XhTagRoot variant="subtle" tone="info">
        <XhTagLabel>{{ t('log.access_policy.filtered_by_policy', { code: routePolicyCode }) }}</XhTagLabel>
      </XhTagRoot>
      <XhButton variant="ghost" size="sm" @click="clearRoutePolicy">
        <XhButtonLabel>{{ t('log.access_policy.clear_filter') }}</XhButtonLabel>
      </XhButton>
    </template>
  </SchemaPage>
</template>
