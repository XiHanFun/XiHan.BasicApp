import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { RequestOf, ResponseOf } from '~/contracts'
import { reactive, ref } from 'vue'
import { delegations as delegationsApi, permissionDefinitions, permissions as permissionsApi, roles as rolesApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type DelegationRow = ResponseOf<'delegations.list'>['items'][number]
export type DelegationStatus = DelegationRow['status']
export type DelegationTargetKind = 'role' | 'permission'

export const DELEGATION_STATUSES: readonly DelegationStatus[] = ['Pending', 'Active', 'Expired', 'Revoked']

/** 组合框候选：value 一律是字符串 */
export interface DelegationOption {
  value: string
  label: string
  description?: string
}

/** 新建委托的表单（4.x 的委托弹窗）：时间是日期选择器的毫秒时间戳 */
export interface DelegationForm {
  delegatorId: string
  delegateeId: string
  targetKind: DelegationTargetKind
  roleId: string
  permission: string
  effectiveAt: null | number
  expiresAt: null | number
  reason: string
}

function emptyForm(): DelegationForm {
  return { delegatorId: '', delegateeId: '', targetKind: 'role', roleId: '', permission: '', effectiveAt: null, expiresAt: null, reason: '' }
}

/** 候选一次取的上限（后端页大小上限） */
const CANDIDATE_LIMIT = 500

/** 权限定义表（生成物）：委托对象的显示名按它取，不在表里的原样显示权限码 */
const definitions: Readonly<Record<string, { displayName: string } | undefined>> = permissionDefinitions

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 委托对象的显示名：角色名，或权限的显示名 */
export function delegationTarget(row: Pick<DelegationRow, 'permission' | 'roleId' | 'roleName'>): string {
  if (row.permission)
    return definitions[row.permission]?.displayName ?? row.permission
  return row.roleName ?? row.roleId ?? '-'
}

/** 确认框里的委托名：委托人 → 接收人 · 委托对象 */
export function delegationName(row: DelegationRow): string {
  const parties = [row.delegatorName ?? row.delegatorId, row.delegateeName ?? row.delegateeId].join(' → ')
  return `${parties} · ${delegationTarget(row)}`
}

/** 只有待生效与生效中的能撤销 */
export function canRevoke(row: Pick<DelegationRow, 'status'>): boolean {
  return row.status === 'Pending' || row.status === 'Active'
}

/** 表单 → 新建请求：只带当前委托类型的对象，时间换成 ISO（到期时间由提交前校验保证有值） */
export function toCreateRequest(form: DelegationForm): RequestOf<'delegations.create'> {
  return {
    delegatorId: form.delegatorId,
    delegateeId: form.delegateeId,
    roleId: form.targetKind === 'role' ? form.roleId : null,
    permission: form.targetKind === 'permission' ? form.permission : null,
    effectiveAt: form.effectiveAt === null ? null : new Date(form.effectiveAt).toISOString(),
    expiresAt: new Date(form.expiresAt ?? 0).toISOString(),
    reason: form.reason.trim() || null,
  }
}

/**
 * SchemaPage 的资源：分页（作用侧 = 当前租户或平台）与删除。
 * 搜索按后端白名单：关键字匹配权限码与原因，状态多选（合成 in）；排序只放生效、到期与创建时间。
 */
export function delegationsResource(): SchemaResource<DelegationRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await delegationsApi.list(toPageRequest(params, { multiples: ['status'] })),
      params,
    ),
    remove: async id => failedCodes(await delegationsApi.delete({ ids: [String(id)] })),
  }
}

/** 撤销一条委托 */
export function revokeDelegation(id: string): Promise<DelegationRow> {
  return delegationsApi.revoke({ id })
}

/**
 * 新建委托的弹窗（4.x 的委托表单）：委托人与接收人远程检索本侧用户，委托对象二选一（启用的非内置角色，或本侧权限目录）。
 * 字段级错误挂回对应字段。
 */
export function useDelegationEditor() {
  const open = ref(false)
  const form = reactive<DelegationForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)
  const userOptions = ref<DelegationOption[]>([])
  const userLoading = ref(false)
  const roleOptions = ref<DelegationOption[]>([])
  const permissionOptions = ref<DelegationOption[]>([])
  const targetLoading = ref(false)
  let userSeq = 0

  /** 只认最后一次检索的结果，免得慢请求把新结果盖掉（已选中的名字由组合框记住，不必留在候选里） */
  async function searchUsers(keyword: string) {
    const seq = ++userSeq
    userLoading.value = true
    try {
      const result = await usersApi.list({ page: { index: 1, size: 50 }, filters: [], sorts: [], keyword: keyword.trim() || null })
      if (seq === userSeq)
        userOptions.value = result.items.map(user => ({ value: user.id, label: user.displayName, description: `@${user.userName}` }))
    }
    finally {
      if (seq === userSeq)
        userLoading.value = false
    }
  }

  /** 角色与权限候选打开弹窗时各取一次 */
  async function loadTargets() {
    targetLoading.value = true
    try {
      const [roles, catalog] = await Promise.all([
        rolesApi.list({ page: { index: 1, size: CANDIDATE_LIMIT }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [{ field: 'order', descending: false }], keyword: null }),
        permissionsApi.list({}),
      ])
      roleOptions.value = roles.items.filter(role => !role.isBuiltIn).map(role => ({ value: role.id, label: role.name, description: role.code }))
      permissionOptions.value = catalog.items.map(item => ({ value: item.code, label: item.displayName, description: item.code }))
    }
    finally {
      targetLoading.value = false
    }
  }

  function start() {
    Object.assign(form, emptyForm())
    fieldErrors.value = {}
    submitError.value = null
    userOptions.value = []
    open.value = true
    return Promise.all([searchUsers(''), loadTargets()])
  }

  function fieldError(field: string): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      await delegationsApi.create(toCreateRequest(form))
      open.value = false
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = error.detail
      }
      else {
        submitError.value = String(error)
      }
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, form, fieldErrors, submitError, submitting, userOptions, userLoading, roleOptions, permissionOptions, targetLoading, searchUsers, start, fieldError, submit }
}
