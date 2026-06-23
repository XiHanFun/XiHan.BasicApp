<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types.ts'
import type { LoginLogDetailDto, LoginLogListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { NTag, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, LoginResult, logManagementApi } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogLoginPage' })

const { t } = useI18n()
const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<LoginLogDetailDto | null>(null)

const loginResultOptions = computed(() => [
  { label: t('log.login.resultSuccess'), value: LoginResult.Success },
  { label: t('log.login.resultInvalidCredentials'), value: LoginResult.InvalidCredentials },
  { label: t('log.login.resultAccountLocked'), value: LoginResult.AccountLocked },
  { label: t('log.login.resultAccountDisabled'), value: LoginResult.AccountDisabled },
  { label: t('log.login.resultRequiresTwoFactor'), value: LoginResult.RequiresTwoFactor },
  { label: t('log.login.resultTwoFactorFailed'), value: LoginResult.TwoFactorFailed },
  { label: t('log.login.resultLogout'), value: LoginResult.Logout },
  { label: t('log.login.resultTokenRefreshed'), value: LoginResult.TokenRefreshed },
  { label: t('log.login.resultPasswordChanged'), value: LoginResult.PasswordChanged },
  { label: t('log.login.resultPasswordReset'), value: LoginResult.PasswordReset },
  { label: t('log.login.resultMfaBound'), value: LoginResult.MfaBound },
  { label: t('log.login.resultMfaUnbound'), value: LoginResult.MfaUnbound },
  { label: t('log.login.resultFailed'), value: LoginResult.Failed },
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
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('log.login.keywordPlaceholder'), order: 0 },
  // 列（顺序对齐实体 SysLoginLog 属性声明）
  { key: 'userId', title: t('log.common.userId'), dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: t('log.common.userName'), dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: t('log.common.sessionId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: t('log.common.traceId'), dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'loginIp', title: t('log.login.loginIp'), dataType: 'string', searchable: true, searchPlaceholder: t('log.login.loginIpPlaceholder'), minWidth: 130, order: 14 },
  { key: 'loginLocation', title: t('log.login.loginLocation'), dataType: 'string', minWidth: 160, order: 15 },
  { key: 'browser', title: t('log.common.browser'), dataType: 'string', minWidth: 120, order: 16 },
  { key: 'os', title: t('log.common.os'), dataType: 'string', minWidth: 120, order: 17 },
  { key: 'device', title: t('log.common.device'), dataType: 'string', minWidth: 120, order: 18 },
  { key: 'deviceId', title: t('log.common.deviceId'), dataType: 'string', minWidth: 160, order: 19 },
  {
    key: 'isRiskLogin',
    title: t('log.login.isRiskLogin'),
    dataType: 'boolean',
    advancedSearch: true,
    options: riskOptions.value,
    width: 120,
    order: 20,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as LoginLogListItemDto).isRiskLogin ? 'error' : 'info' }, () => (row as unknown as LoginLogListItemDto).isRiskLogin ? t('common.statuses.yes') : t('common.statuses.no')),
  },
  {
    key: 'loginResult',
    title: t('log.login.loginResult'),
    dataType: 'enum',
    searchable: true,
    options: loginResultOptions.value,
    searchPlaceholder: t('log.login.loginResultPlaceholder'),
    width: 120,
    order: 21,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: loginResultType((row as unknown as LoginLogListItemDto).loginResult) }, () => getOptionLabel(loginResultOptions.value, (row as unknown as LoginLogListItemDto).loginResult)),
  },
  { key: 'message', title: t('log.login.message'), dataType: 'string', minWidth: 220, order: 22 },
  { key: 'loginTime', title: t('log.login.loginTime'), dataType: 'datetime', sortable: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, order: 24 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'loginTimeStart', title: t('log.common.startTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.startTime'), order: 40 },
  { key: 'loginTimeEnd', title: t('log.common.endTime'), dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: t('log.common.endTime'), order: 41 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : v === 1 || v === true
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

/** 查询构建（resource.page 与导出快照复用；枚举保持数值以兼容服务端 JSON 反序列化） */
function buildLoginQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    loginResult: (f.loginResult as LoginResult | undefined) ?? undefined,
    userName: toStr(f.userName),
    userId: toStr(f.userId),
    isRiskLogin: toBool(f.isRiskLogin),
    sessionId: toStr(f.sessionId),
    traceId: toStr(f.traceId),
    loginIp: toStr(f.loginIp),
    loginTimeStart: toIso(f.loginTimeStart),
    loginTimeEnd: toIso(f.loginTimeEnd),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'log.login',
  exportPermission: 'saas:login-log:export',
  pageName: t('log.login.pageName'),
  rowKey: 'basicId',
  scrollX: 2000,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.login.page(buildLoginQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    export: { businessType: 'log.login', buildQuery: buildLoginQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

const detailFields = computed<LogDetailField[]>(() => [
  { key: 'basicId', label: t('log.common.basicId') },
  { key: 'sessionId', label: t('log.common.sessionId') },
  { key: 'traceId', label: t('log.common.traceId') },
  { key: 'userName', label: t('log.common.userName') },
  { key: 'userId', label: t('log.common.userId') },
  { key: 'loginIp', label: t('log.login.loginIp') },
  { key: 'loginLocation', label: t('log.login.loginLocation') },
  { key: 'browser', label: t('log.common.browser') },
  { key: 'os', label: t('log.common.os') },
  { key: 'device', label: t('log.common.device') },
  { key: 'deviceId', label: t('log.common.deviceId') },
  { key: 'loginResult', label: t('log.login.loginResult'), options: loginResultOptions.value, type: 'enum' },
  { key: 'isRiskLogin', falseText: t('common.statuses.no'), label: t('log.login.isRiskLogin'), trueText: t('common.statuses.yes'), type: 'boolean' },
  { key: 'loginTime', label: t('log.login.loginTime'), type: 'date' },
  { key: 'createdTime', label: t('common.fields.created_time'), type: 'date' },
  { key: 'createdId', label: t('log.common.createdId') },
  { key: 'createdBy', label: t('common.fields.created_by') },
  { key: 'message', label: t('log.login.message'), type: 'code' },
  { key: 'userAgent', label: t('log.common.userAgent'), type: 'code' },
])

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as LoginLogListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
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
    message.error(t('log.login.detailLoadFailed'))
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
      :title="t('log.login.detailTitle')"
    />
  </SchemaPage>
</template>
