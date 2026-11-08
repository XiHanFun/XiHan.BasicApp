/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { components, RequestOf } from '~/contracts'
import { reactive, ref } from 'vue'
import { permissions as permissionsApi, permissionRequests as requestsApi, roles as rolesApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'
import { formatDate } from '~/utils'

/** 一条授权申请（列表、详情与审批单据同一形态） */
export type PermissionRequestRow = components['schemas']['PermissionRequestResponse']
export type PermissionRequestStatus = PermissionRequestRow['status']
export type RequestTargetKind = 'role' | 'permission'
/** 手工处理只能置为已驳回或已撤回（审批通过必须走流程） */
export type CloseStatus = Extract<PermissionRequestStatus, 'Rejected' | 'Withdrawn'>

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 组合框候选：value 一律是字符串 */
export interface RequestOption {
  value: string
  label: string
  description?: string
}

/** 发起 / 修改重提的表单：时间是日期选择器的毫秒时间戳 */
export interface PermissionRequestForm {
  targetKind: RequestTargetKind
  roleId: string
  permissionCode: string
  expectedEffectiveAt: null | number
  expectedExpiresAt: null | number
  reason: string
  remark: string
}

function emptyForm(): PermissionRequestForm {
  return {
    targetKind: 'role',
    roleId: '',
    permissionCode: '',
    expectedEffectiveAt: null,
    expectedExpiresAt: null,
    reason: '',
    remark: '',
  }
}

/** 候选一次取的上限（后端页大小上限） */
const CANDIDATE_LIMIT = 500

function toMillis(value?: null | string): null | number {
  return value
    ? new Date(value).getTime()
    : null
}

function toIso(value: null | number): null | string {
  return value === null
    ? null
    : new Date(value).toISOString()
}

/** 状态标签：枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对 */
export function requestStatusLabel(t: Translate, status: PermissionRequestStatus): string {
  switch (status) {
    case 'Approved': return t('access.permission_requests.statuses.approved')
    case 'Rejected': return t('access.permission_requests.statuses.rejected')
    case 'Withdrawn': return t('access.permission_requests.statuses.withdrawn')
    default: return t('access.permission_requests.statuses.pending')
  }
}

/** 状态色调：待审批黄、已批准绿、已驳回红、已撤回中性 */
export const REQUEST_STATUS_TONE: Record<PermissionRequestStatus, Tone> = {
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'danger',
  Withdrawn: 'neutral',
}

/** 「{from} 至 {to}」：两头都可以不填 */
export function expectedValidity(t: Translate, row: Pick<PermissionRequestRow, 'expectedEffectiveAt' | 'expectedExpiresAt'>): string {
  return t('access.permission_requests.validity_range', {
    from: row.expectedEffectiveAt
      ? formatDate(row.expectedEffectiveAt)
      : '-',
    to: row.expectedExpiresAt
      ? formatDate(row.expectedExpiresAt)
      : '-',
  })
}

/** 申请的对象：角色名或权限名（二选一，后端已按界面语言给出） */
export function requestTarget(row: Pick<PermissionRequestRow, 'permissionCode' | 'permissionName' | 'roleCode' | 'roleId' | 'roleName'>): string {
  const parts: string[] = []
  if (row.roleId)
    parts.push(row.roleName ?? row.roleCode ?? row.roleId)
  if (row.permissionCode)
    parts.push(row.permissionName ?? row.permissionCode)
  return parts.join(' / ') || '-'
}

/** 确认框里的申请名：申请人 · 申请的对象 */
export function requestName(row: PermissionRequestRow): string {
  return `${row.requestUserName ?? row.requestUserId} · ${requestTarget(row)}`
}

function isOwnPending(row: Pick<PermissionRequestRow, 'requestUserId' | 'status'>, me: null | string): boolean {
  return row.status === 'Pending' && !!me && row.requestUserId === me
}

/** 撤回：申请人撤回自己待审批的申请 */
export function canWithdraw(row: Pick<PermissionRequestRow, 'requestUserId' | 'status'>, me: null | string): boolean {
  return isOwnPending(row, me)
}

/** 修改重提：申请人改自己待审批的申请并重新发起审批 */
export function canResubmit(row: Pick<PermissionRequestRow, 'requestUserId' | 'status'>, me: null | string): boolean {
  return isOwnPending(row, me)
}

/** 手工驳回 / 撤回：管理员处理别人待审批的申请（不能处理自己的） */
export function canClose(row: Pick<PermissionRequestRow, 'requestUserId' | 'status'>, me: null | string): boolean {
  return row.status === 'Pending' && !!me && row.requestUserId !== me
}

/** 查看审批：审批启用且发起过流程 */
export function canTrace(row: Pick<PermissionRequestRow, 'approvalEnabled' | 'workflowInstanceId'>): boolean {
  return row.approvalEnabled && !!row.workflowInstanceId
}

/** 表单 → 提交请求：只带当前申请类型的对象，时间换成 ISO，空备注不传 */
export function toCreateRequest(form: PermissionRequestForm): RequestOf<'permission-requests.create'> {
  return {
    roleId: form.targetKind === 'role'
      ? form.roleId
      : null,
    permissionCode: form.targetKind === 'permission'
      ? form.permissionCode
      : null,
    reason: form.reason.trim(),
    expectedEffectiveAt: toIso(form.expectedEffectiveAt),
    expectedExpiresAt: toIso(form.expectedExpiresAt),
    remark: form.remark.trim() || null,
  }
}

/** 表单 → 修改重提请求 */
export function toUpdateRequest(id: string, form: PermissionRequestForm): RequestOf<'permission-requests.update'> {
  return {
    id,
    ...toCreateRequest(form),
  }
}

/** 申请 → 表单（修改重提时回填；申请类型按申请里有的对象取，角色优先） */
export function toForm(row: PermissionRequestRow): PermissionRequestForm {
  return {
    targetKind: row.roleId ? 'role' : 'permission',
    roleId: row.roleId ?? '',
    permissionCode: row.permissionCode ?? '',
    expectedEffectiveAt: toMillis(row.expectedEffectiveAt),
    expectedExpiresAt: toMillis(row.expectedExpiresAt),
    reason: row.reason,
    remark: row.remark ?? '',
  }
}

/**
 * SchemaPage 的资源：分页（作用侧 = 当前租户或平台）
 * 搜索按后端白名单：关键字匹配申请原因与备注，状态多选（合成 in）；排序只放提交时间
 */
export function permissionRequestsResource(): SchemaResource<PermissionRequestRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await requestsApi.list(toPageRequest(params, {
      multiples: ['status'],
    })), params),
  }
}

/** 撤回自己待审批的一条申请（批量接口传单个 ID） */
export async function withdrawPermissionRequest(id: string): Promise<void> {
  throwOnFailed(await requestsApi.withdraw({
    ids: [id],
  }))
}

/** 提交出错：字段级错误挂回字段，其余留在弹窗里 */
function captureError(error: unknown, fieldErrors: { value: Record<string, string[]> }, submitError: { value: null | string }): void {
  if (error instanceof ApiError) {
    fieldErrors.value = error.errors ?? {}
    if (!error.hasFieldErrors)
      submitError.value = error.detail
  }
  else {
    submitError.value = error instanceof Error
      ? error.message
      : String(error)
  }
}

/**
 * 发起申请与修改重提共用的弹窗：申请对象二选一（启用的角色，或本侧权限目录），打开时各取一次候选
 * 字段级错误挂回对应字段；Workflow 关掉时提交返回 503 `ACCESS_UNAVAILABLE_002`，原因留在弹窗里
 */
export function usePermissionRequestEditor() {
  const open = ref(false)
  const editingId = ref<null | string>(null)
  const form = reactive<PermissionRequestForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)
  const roleOptions = ref<RequestOption[]>([])
  const permissionOptions = ref<RequestOption[]>([])
  const targetLoading = ref(false)

  /** 角色与权限候选打开弹窗时各取一次（启用的角色与本侧可用权限） */
  async function loadTargets() {
    targetLoading.value = true
    try {
      const [roles, catalog] = await Promise.all([
        rolesApi.list({
          page: { index: 1, size: CANDIDATE_LIMIT },
          filters: [
            { field: 'status', operator: 'eq', value: 'Active' },
          ],
          sorts: [
            { field: 'order', descending: false },
          ],
          keyword: null,
        }),
        permissionsApi.list({}),
      ])
      roleOptions.value = roles.items.map(role => ({ value: role.id, label: role.name, description: role.code }))
      permissionOptions.value = catalog.items.map(item => ({ value: item.code, label: item.displayName, description: item.code }))
    }
    finally {
      targetLoading.value = false
    }
  }

  /** 打开弹窗：不带申请即发起，带申请即修改重提（回填原内容） */
  function start(row?: PermissionRequestRow) {
    Object.assign(form, row
      ? toForm(row)
      : emptyForm())
    editingId.value = row?.id ?? null
    fieldErrors.value = {}
    submitError.value = null
    open.value = true
    return loadTargets()
  }

  function fieldError(field: string): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      if (editingId.value)
        await requestsApi.update(toUpdateRequest(editingId.value, form))
      else
        await requestsApi.create(toCreateRequest(form))
      open.value = false
      return true
    }
    catch (error) {
      captureError(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return {
    open,
    editingId,
    form,
    fieldErrors,
    submitError,
    submitting,
    roleOptions,
    permissionOptions,
    targetLoading,
    start,
    fieldError,
    submit,
  }
}

/**
 * 手工驳回 / 撤回的弹窗（W2）：选处理方式、填备注（也是流程的取消原因），批量接口传单个 ID
 */
export function usePermissionRequestCloser() {
  const open = ref(false)
  const target = ref<null | PermissionRequestRow>(null)
  const status = ref<CloseStatus>('Rejected')
  const remark = ref('')
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  function start(row: PermissionRequestRow) {
    target.value = row
    status.value = 'Rejected'
    remark.value = ''
    submitError.value = null
    open.value = true
  }

  async function submit(): Promise<boolean> {
    if (!target.value)
      return false
    submitting.value = true
    submitError.value = null
    try {
      throwOnFailed(await requestsApi.setStatus({
        ids: [target.value.id],
        status: status.value,
        remark: remark.value.trim() || null,
      }))
      open.value = false
      return true
    }
    catch (error) {
      submitError.value = error instanceof ApiError
        ? error.detail
        : error instanceof Error
          ? error.message
          : String(error)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return {
    open,
    target,
    status,
    remark,
    submitError,
    submitting,
    start,
    submit,
  }
}
