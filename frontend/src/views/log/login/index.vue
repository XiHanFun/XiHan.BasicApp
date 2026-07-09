<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { LoginLogDetailDto, LoginLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { createPageRequest, LoginResult, logManagementApi, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import { loginLogDetailFields } from '../_components/log-detail-fields'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'
import { decorateTraceFields, gotoTrace } from '../_components/trace-nav'

defineOptions({ name: 'LogLoginPage' })

const { t } = useI18n()
const message = useMessage()
const router = useRouter()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<LoginLogDetailDto | null>(null)

const loginResultOptions = computed(() => [
  { label: t('log.login.result_success'), value: LoginResult.Success },
  { label: t('log.login.result_invalid_credentials'), value: LoginResult.InvalidCredentials },
  { label: t('log.login.result_account_locked'), value: LoginResult.AccountLocked },
  { label: t('log.login.result_account_disabled'), value: LoginResult.AccountDisabled },
  { label: t('log.login.result_requires_two_factor'), value: LoginResult.RequiresTwoFactor },
  { label: t('log.login.result_two_factor_failed'), value: LoginResult.TwoFactorFailed },
  { label: t('log.login.result_logout'), value: LoginResult.Logout },
  { label: t('log.login.result_token_refreshed'), value: LoginResult.TokenRefreshed },
  { label: t('log.login.result_password_changed'), value: LoginResult.PasswordChanged },
  { label: t('log.login.result_password_reset'), value: LoginResult.PasswordReset },
  { label: t('log.login.result_mfa_bound'), value: LoginResult.MfaBound },
  { label: t('log.login.result_mfa_unbound'), value: LoginResult.MfaUnbound },
  { label: t('log.login.result_failed'), value: LoginResult.Failed },
])

const riskOptions = computed(() => [
  { label: t('common.statuses.yes'), value: 1 },
  { label: t('common.statuses.no'), value: 0 },
])

function loginResultType(result: LoginResult) {
  switch (result) {
    case LoginResult.Success: return 'success'
    case LoginResult.Logout:
    case LoginResult.TokenRefreshed: return 'info'
    case LoginResult.InvalidCredentials:
    case LoginResult.TwoFactorFailed:
    case LoginResult.Failed: return 'error'
    case LoginResult.AccountLocked:
    case LoginResult.AccountDisabled:
    case LoginResult.RequiresTwoFactor:
    case LoginResult.PasswordChanged:
    case LoginResult.PasswordReset:
    case LoginResult.MfaBound:
    case LoginResult.MfaUnbound: return 'warning'
    default: return 'default'
  }
}

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.login.keyword_placeholder'), order: 0 },
  // 列（顺序对齐实体 SysLoginLog 属性声明）
  { key: 'userId', title: t('log.common.user_id'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.user_name'), dataType: 'string', advancedSearch: true, sortable: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.session_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: t('log.common.trace_id'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'loginIp', title: t('log.login.login_ip'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('log.login.login_ip_placeholder'), minWidth: 130, order: 14 },
  { key: 'loginLocation', title: t('log.login.login_location'), dataType: 'string', minWidth: 160, order: 15 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 16 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 17 },
  { key: 'device', title: t('log.common.device'), dataType: 'string', minWidth: 120, order: 18 },
  { key: 'deviceId', title: t('log.common.device_id'), dataType: 'string', minWidth: 160, order: 19 },
  {
    key: 'isRiskLogin',
    title: t('log.login.is_risk_login'),
    dataType: 'boolean',
    advancedSearch: true,
    sortable: true,
    options: riskOptions.value,
    width: 120,
    order: 20,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as LoginLogListItemDto).isRiskLogin ? 'error' : 'info' }, () => (row as unknown as LoginLogListItemDto).isRiskLogin ? t('common.statuses.yes') : t('common.statuses.no')),
  },
  {
    key: 'loginResult',
    title: t('log.login.login_result'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: loginResultOptions.value,
    searchPlaceholder: t('log.login.login_result_placeholder'),
    width: 120,
    order: 21,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: loginResultType((row as unknown as LoginLogListItemDto).loginResult) }, () => getOptionLabel(loginResultOptions.value, (row as unknown as LoginLogListItemDto).loginResult)),
  },
  { key: 'message', title: t('log.login.message'), dataType: 'string', minWidth: 220, order: 22 },
  { key: 'loginTime', title: t('log.login.login_time'), dataType: 'datetime', sortable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 24 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : v === 1 || v === true
}

/** 查询构建（resource.page 与导出快照复用；时间区间/枚举多选经 conditions.filters 下发） */
function buildLoginQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    isRiskLogin: toBool(f.isRiskLogin),
    sessionId: toStr(f.sessionId),
    traceId: toStr(f.traceId),
    loginIp: toStr(f.loginIp),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.login',
  exportPermission: 'saas:login-log:export',
  pageName: t('log.login.page_name'),
  rowKey: 'basicId',
  scrollX: 2000,
  fields: decorateTraceFields(fields.value, router, { timeField: 'loginTime', ipKey: 'loginIp' }),
  resource: {
    page: params => logManagementApi.login.page(buildLoginQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.login', buildQuery: buildLoginQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
    { key: 'trace', title: t('log.trace.action'), scope: 'row', icon: 'lucide:route' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => loginLogDetailFields(t))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as LoginLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
  else if (payload.key === 'trace' && row) {
    if (!gotoTrace(router, row, row.loginTime)) {
      message.warning(t('log.trace.value_required'))
    }
  }
}

async function handleDetail(row: LoginLogListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await logManagementApi.login.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error(t('log.login.detail_load_failed'))
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
      :title="t('log.login.detail_title')"
    />
  </SchemaPage>
</template>
