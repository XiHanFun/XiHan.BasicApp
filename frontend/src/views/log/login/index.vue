<script setup lang="ts">
import type { LogDetailField } from '../_components/log-detail.types'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { LoginLogDetailDto, LoginLogListItemDto, PageResult } from '@/api'
import { NTag, useMessage } from 'naive-ui'
import { h, ref } from 'vue'
import { createPageRequest, LoginResult, logManagementApi } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'
import LogDetailDrawer from '../_components/LogDetailDrawer.vue'

defineOptions({ name: 'LogLoginPage' })

const message = useMessage()

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<LoginLogDetailDto | null>(null)

const loginResultOptions = [
  { label: '成功', value: LoginResult.Success },
  { label: '密码错误', value: LoginResult.InvalidCredentials },
  { label: '账号锁定', value: LoginResult.AccountLocked },
  { label: '账号禁用', value: LoginResult.AccountDisabled },
  { label: '需二次验证', value: LoginResult.RequiresTwoFactor },
  { label: '二次验证失败', value: LoginResult.TwoFactorFailed },
  { label: '登出', value: LoginResult.Logout },
  { label: '令牌刷新', value: LoginResult.TokenRefreshed },
  { label: '密码修改', value: LoginResult.PasswordChanged },
  { label: '密码重置', value: LoginResult.PasswordReset },
  { label: '绑定MFA', value: LoginResult.MfaBound },
  { label: '解绑MFA', value: LoginResult.MfaUnbound },
  { label: '其他失败', value: LoginResult.Failed },
]

const riskOptions = [
  { label: '是', value: 1 },
  { label: '否', value: 0 },
]

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
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索用户/会话/链路', order: 0 },
  // 列（顺序对齐实体 SysLoginLog 属性声明）
  { key: 'userId', title: '用户主键', dataType: 'string', advancedSearch: true, minWidth: 90, order: 10 },
  { key: 'userName', title: '用户名', dataType: 'string', advancedSearch: true, minWidth: 100, order: 11 },
  { key: 'sessionId', title: '会话标识', dataType: 'string', advancedSearch: true, minWidth: 160, order: 12 },
  { key: 'traceId', title: '链路追踪 ID', dataType: 'string', advancedSearch: true, minWidth: 160, order: 13 },
  { key: 'loginIp', title: '登录 IP', dataType: 'string', minWidth: 130, order: 14 },
  { key: 'loginLocation', title: '登录位置', dataType: 'string', minWidth: 160, order: 15 },
  { key: 'browser', title: '浏览器', dataType: 'string', minWidth: 120, order: 16 },
  { key: 'os', title: '操作系统', dataType: 'string', minWidth: 120, order: 17 },
  { key: 'device', title: '设备', dataType: 'string', minWidth: 120, order: 18 },
  { key: 'deviceId', title: '设备标识', dataType: 'string', minWidth: 160, order: 19 },
  {
    key: 'isRiskLogin',
    title: '是否风险登录',
    dataType: 'boolean',
    advancedSearch: true,
    options: riskOptions,
    width: 120,
    order: 20,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as LoginLogListItemDto).isRiskLogin ? 'error' : 'info' }, () => (row as unknown as LoginLogListItemDto).isRiskLogin ? '是' : '否'),
  },
  {
    key: 'loginResult',
    title: '登录/登出结果',
    dataType: 'enum',
    searchable: true,
    options: loginResultOptions,
    searchPlaceholder: '登录/登出结果',
    width: 120,
    order: 21,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: loginResultType((row as unknown as LoginLogListItemDto).loginResult) }, () => getOptionLabel(loginResultOptions, (row as unknown as LoginLogListItemDto).loginResult)),
  },
  { key: 'message', title: '消息', dataType: 'string', minWidth: 220, order: 22 },
  { key: 'loginTime', title: '登录时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 23 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 24 },
  // 仅高级搜索（不作为列，范围条件置于高级区末尾）
  { key: 'loginTimeStart', title: '开始时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '开始时间', order: 40 },
  { key: 'loginTimeEnd', title: '结束时间', dataType: 'datetime', visible: false, advancedSearch: true, searchPlaceholder: '结束时间', order: 41 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : v === 1 || v === true
}
function toIso(v: unknown): string | undefined {
  return v == null || v === '' ? undefined : new Date(v as number).toISOString()
}

const schema: PageSchema = {
  pageCode: 'log.login',
  pageName: '登录日志',
  rowKey: 'basicId',
  scrollX: 2000,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return logManagementApi.login.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        loginResult: (f.loginResult as LoginResult | undefined) ?? undefined,
        userName: toStr(f.userName),
        userId: toStr(f.userId),
        isRiskLogin: toBool(f.isRiskLogin),
        sessionId: toStr(f.sessionId),
        traceId: toStr(f.traceId),
        loginTimeStart: toIso(f.loginTimeStart),
        loginTimeEnd: toIso(f.loginTimeEnd),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
  ],
}

const detailFields: LogDetailField[] = [
  { key: 'basicId', label: '日志主键' },
  { key: 'sessionId', label: '会话标识' },
  { key: 'traceId', label: '链路追踪 ID' },
  { key: 'userName', label: '用户名' },
  { key: 'userId', label: '用户主键' },
  { key: 'loginIp', label: '登录 IP' },
  { key: 'loginLocation', label: '登录位置' },
  { key: 'browser', label: '浏览器' },
  { key: 'os', label: '操作系统' },
  { key: 'device', label: '设备' },
  { key: 'deviceId', label: '设备标识' },
  { key: 'loginResult', label: '登录/登出结果', options: loginResultOptions, type: 'enum' },
  { key: 'isRiskLogin', falseText: '否', label: '是否风险登录', trueText: '是', type: 'boolean' },
  { key: 'loginTime', label: '登录时间', type: 'date' },
  { key: 'createdTime', label: '创建时间', type: 'date' },
  { key: 'createdId', label: '创建人主键' },
  { key: 'createdBy', label: '创建人' },
  { key: 'message', label: '消息', type: 'code' },
  { key: 'userAgent', label: 'User-Agent', type: 'code' },
]

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
    message.error('加载登录日志详情失败')
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
      title="登录日志详情"
    />
  </SchemaPage>
</template>
