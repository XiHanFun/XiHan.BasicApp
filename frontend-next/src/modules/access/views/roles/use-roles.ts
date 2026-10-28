import type { DataScopeDraft, DataScopeValue, DepartmentNode } from './data-scope'
import type { PermissionGrantItem, SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { departments as departmentsApi, permissionDefinitions, permissions as permissionsApi, roles as rolesApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'
import { buildNameMap, isDataScopeComplete, isDataScopeDirty, toDataScopePayload } from './data-scope'

export type RoleRow = ResponseOf<'roles.list'>['items'][number]
export type RoleDetail = ResponseOf<'roles.get'>
export type RoleStatus = RoleRow['status']
export type PermissionItem = ResponseOf<'permissions.list'>['items'][number]
export type RoleMember = ResponseOf<'roles.members'>['items'][number]
export type UserOption = ResponseOf<'users.list'>['items'][number]
export type { DataScopeValue, DepartmentNode }

export const ROLE_STATUSES: readonly RoleStatus[] = ['Active', 'Disabled']

/** 候选用户一次取的上限（后端 PageInfo 的页大小上限，与 4.x 的成员目录同为 500） */
const CANDIDATE_LIMIT = 500

/** 权限定义表（生成物）：详情与目录外的已授权限按它取显示名 */
const definitions: Readonly<Record<string, { displayName: string, description: string } | undefined>> = permissionDefinitions

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

/** 批量删除 / 指派的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 修改角色是整体覆盖名称 / 说明 / 排序 / 状态；数据范围与自定义部门不传即保留 */
function updateBody(role: RoleDetail['role'], patch: { status?: RoleStatus } = {}) {
  return { id: role.id, name: role.name, description: role.description ?? null, order: role.order, status: patch.status ?? role.status }
}

/**
 * SchemaPage 的资源：分页（作用侧 = 当前租户或平台）、删除、启停。
 * 搜索按后端白名单：关键字匹配角色码与名称，状态精确匹配；排序只放角色码、名称、排序与创建时间。
 */
export function rolesResource(): SchemaResource<RoleRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await rolesApi.list(toPageRequest(params, { filters: { status: 'eq' } })),
      params,
    ),
    // 内建角色由后端逐项拒绝（ACCESS_CONFLICT_002）
    remove: async id => failedCodes(await rolesApi.delete({ ids: [String(id)] })),
    // 先取当前值再只改状态
    updateStatus: async (id, enabled) => {
      const { role } = await rolesApi.get({ id: String(id) })
      return rolesApi.update(updateBody(role, { status: enabled ? 'Active' : 'Disabled' }))
    },
  }
}

// ── 新建 / 编辑 ──

/** 弹窗表单：新建与编辑共用一份（角色码只在新建时可填，状态只在编辑时可改） */
export interface RoleForm {
  code: string
  name: string
  description: string
  order: number
  status: RoleStatus
}

export type RoleEditorMode = 'create' | 'edit'

function emptyForm(): RoleForm {
  return { code: '', name: '', description: '', order: 0, status: 'Active' }
}

/**
 * 角色的编辑弹窗：新建（数据范围按侧取缺省，另在「数据范围」里改）、编辑名称 / 说明 / 排序 / 状态。
 * 字段级错误挂回对应字段，其余错误给整条提示。
 */
export function useRoleEditor() {
  const open = ref(false)
  const mode = ref<RoleEditorMode>('create')
  const editing = ref<RoleRow | null>(null)
  const form = reactive<RoleForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<RoleForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  function startEdit(row: RoleRow) {
    mode.value = 'edit'
    editing.value = row
    reset({ code: row.code, name: row.name, description: row.description ?? '', order: row.order, status: row.status })
    open.value = true
  }

  function fieldError(field: keyof RoleForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await rolesApi.update({ id: current.id, name: form.name.trim(), description: blankToNull(form.description), order: form.order, status: form.status })
      }
      else {
        await rolesApi.create({ code: form.code.trim(), name: form.name.trim(), description: blankToNull(form.description), order: form.order })
      }
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

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, startCreate, startEdit, submit }
}

// ── 权限分配（穿梭框） ──

/** 权限穿梭框条目：主键即权限码 */
export interface RolePermissionItem extends PermissionGrantItem {
  basicId: string
}

/** 权限码的「包.资源」段即穿梭框的分段（identity.users.read → identity.users） */
function groupOf(code: string): string {
  return code.split('.').slice(0, 2).join('.')
}

/**
 * 权限目录 → 穿梭框条目（按「包.资源」分段）。已授予却不在目录里的（例如套餐收窄后目录不再列出）补进来：
 * 穿梭框只认条目里有的键，不补进来，一动穿梭框这些授权就从草稿里掉出去，保存时被当成收回
 */
export function toPermissionItems(catalog: readonly PermissionItem[], granted: readonly string[]): RolePermissionItem[] {
  const items: RolePermissionItem[] = catalog.map(item => ({
    basicId: item.code,
    permissionCode: item.code,
    permissionName: item.displayName,
    groupCode: `${item.package}.${item.resource}`,
    groupName: `${item.package}.${item.resource}`,
  }))
  const known = new Set(items.map(item => item.basicId))
  for (const code of granted) {
    if (!known.has(code))
      items.push({ basicId: code, permissionCode: code, permissionName: definitions[code]?.displayName ?? code, groupCode: groupOf(code), groupName: groupOf(code) })
  }
  return items
}

/** 草稿与已授予比出差量：新勾的授予、取消的撤销 */
export function diffGrants(draft: readonly string[], granted: readonly string[]): { grant: string[], revoke: string[] } {
  const current = new Set(granted)
  const next = new Set(draft)
  return {
    grant: draft.filter(code => !current.has(code)),
    revoke: granted.filter(code => !next.has(code)),
  }
}

/** 一次保存的结果：成功授予 / 撤销的条数，逐项失败（权限码: 错误码） */
export interface ChangeOutcome {
  added: number
  removed: number
  failures: string[]
}

/** 权限分配抽屉：目录一次取全并缓存，草稿只改本地，保存时按差量授予 / 撤销 */
export function useRolePermissionEditor() {
  const open = ref(false)
  const role = ref<RoleRow | null>(null)
  const catalog = ref<PermissionItem[]>([])
  const granted = ref<string[]>([])
  const draft = ref<string[]>([])
  const loading = ref(false)
  const saving = ref(false)

  const items = computed(() => toPermissionItems(catalog.value, granted.value))
  const dirty = computed(() => {
    const { grant, revoke } = diffGrants(draft.value, granted.value)
    return grant.length > 0 || revoke.length > 0
  })

  function applyGranted(codes: string[]) {
    granted.value = codes
    draft.value = [...codes]
  }

  async function start(row: RoleRow): Promise<void> {
    role.value = row
    applyGranted([])
    open.value = true
    loading.value = true
    try {
      const [list, detail] = await Promise.all([
        catalog.value.length > 0 ? Promise.resolve(null) : permissionsApi.list({}),
        rolesApi.get({ id: row.id }),
      ])
      if (list)
        catalog.value = list.items
      applyGranted(detail.permissions)
    }
    finally {
      loading.value = false
    }
  }

  /** 没有改动返回 null；请求失败照常抛出 */
  async function save(): Promise<ChangeOutcome | null> {
    const current = role.value
    if (!current)
      return null
    const { grant, revoke } = diffGrants(draft.value, granted.value)
    if (grant.length === 0 && revoke.length === 0)
      return null
    saving.value = true
    try {
      const outcome: ChangeOutcome = { added: 0, removed: 0, failures: [] }
      if (grant.length > 0) {
        const result = await rolesApi.grant({ id: current.id, permissions: grant })
        outcome.added = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.permission}: ${f.code}`))
      }
      if (revoke.length > 0) {
        const result = await rolesApi.revoke({ id: current.id, permissions: revoke })
        outcome.removed = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.permission}: ${f.code}`))
      }
      applyGranted((await rolesApi.get({ id: current.id })).permissions)
      return outcome
    }
    finally {
      saving.value = false
    }
  }

  return { open, role, catalog, granted, draft, items, dirty, loading, saving, start, save }
}

// ── 角色成员（穿梭框） ──

/** 成员穿梭框的一项：主键即用户主键 */
export interface MemberCandidate {
  basicId: string
  displayName: string
  userName: string
}

/**
 * 候选人（本侧用户）并上现有成员：候选有上限，也可能读不到用户目录，已在角色里的人不在候选里就会在穿梭框里「消失」，
 * 保存时被当成移出
 */
export function mergeMemberCandidates(users: readonly UserOption[], members: readonly RoleMember[]): MemberCandidate[] {
  const items: MemberCandidate[] = users.map(user => ({ basicId: user.id, displayName: user.displayName, userName: user.userName }))
  const known = new Set(items.map(item => item.basicId))
  for (const member of members) {
    if (!known.has(member.userId))
      items.push({ basicId: member.userId, displayName: member.displayName, userName: member.userName })
  }
  return items
}

/** 草稿与现有成员比出差量：新加入的指派、移出的撤销（6.0 两边都按用户主键） */
export function diffMembers(draft: readonly string[], members: readonly RoleMember[]): { assign: string[], unassign: string[] } {
  const current = new Set(members.map(member => member.userId))
  const next = new Set(draft)
  return {
    assign: draft.filter(userId => !current.has(userId)),
    unassign: members.filter(member => !next.has(member.userId)).map(member => member.userId),
  }
}

/** 角色成员抽屉：挪好后一次提交加入与移出；读不到用户目录时只能移出现有成员 */
export function useRoleMemberEditor() {
  const open = ref(false)
  const role = ref<RoleRow | null>(null)
  const members = ref<RoleMember[]>([])
  const users = ref<UserOption[]>([])
  const draft = ref<string[]>([])
  const loading = ref(false)
  const saving = ref(false)

  const items = computed(() => mergeMemberCandidates(users.value, members.value))
  const dirty = computed(() => {
    const { assign, unassign } = diffMembers(draft.value, members.value)
    return assign.length > 0 || unassign.length > 0
  })

  function applyMembers(current: RoleMember[]) {
    members.value = current
    draft.value = current.map(member => member.userId)
  }

  async function start(row: RoleRow, canListUsers: boolean): Promise<void> {
    role.value = row
    users.value = []
    applyMembers([])
    open.value = true
    loading.value = true
    try {
      const [current, candidates] = await Promise.all([
        rolesApi.members({ id: row.id }),
        canListUsers ? usersApi.list({ page: { index: 1, size: CANDIDATE_LIMIT }, filters: [], sorts: [{ field: 'userName' }] }) : Promise.resolve(null),
      ])
      users.value = candidates?.items ?? []
      applyMembers(current.items)
    }
    finally {
      loading.value = false
    }
  }

  async function save(): Promise<ChangeOutcome | null> {
    const current = role.value
    if (!current)
      return null
    const { assign, unassign } = diffMembers(draft.value, members.value)
    if (assign.length === 0 && unassign.length === 0)
      return null
    saving.value = true
    try {
      const outcome: ChangeOutcome = { added: 0, removed: 0, failures: [] }
      if (assign.length > 0) {
        const result = await rolesApi.assign({ id: current.id, userIds: assign })
        outcome.added = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.id}: ${f.code}`))
      }
      if (unassign.length > 0) {
        const result = await rolesApi.unassign({ id: current.id, userIds: unassign })
        outcome.removed = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.id}: ${f.code}`))
      }
      applyMembers((await rolesApi.members({ id: current.id })).items)
      return outcome
    }
    finally {
      saving.value = false
    }
  }

  return { open, role, members, users, draft, items, dirty, loading, saving, start, save }
}

// ── 数据范围 ──

/**
 * 数据范围抽屉（只在租户侧：平台角色固定为全部）：档位与自定义部门一次保存。
 * 保存走 roles.update，名称 / 说明 / 排序 / 状态按打开时取到的详情原样回传，自定义部门整体替换。
 */
export function useRoleDataScopeEditor() {
  const open = ref(false)
  const role = ref<RoleRow | null>(null)
  const detail = ref<RoleDetail | null>(null)
  const tree = ref<DepartmentNode[]>([])
  const draft = ref<DataScopeDraft>({ dataScope: 'SelfOnly', departments: [] })
  /** 打开时的现状，保存钮只在有改动时可用 */
  const original = ref<DataScopeDraft>({ dataScope: 'SelfOnly', departments: [] })
  const loading = ref(false)
  const saving = ref(false)

  const dirty = computed(() => isDataScopeDirty(draft.value, original.value))
  const complete = computed(() => isDataScopeComplete(draft.value))

  function applyDraft(next: DataScopeDraft) {
    draft.value = next
    original.value = next
  }

  async function start(row: RoleRow, canReadDepartments: boolean): Promise<void> {
    role.value = row
    detail.value = null
    tree.value = []
    applyDraft({ dataScope: row.dataScope, departments: [] })
    open.value = true
    loading.value = true
    try {
      const [current, departments] = await Promise.all([
        rolesApi.get({ id: row.id }),
        canReadDepartments ? departmentsApi.tree({}) : Promise.resolve(null),
      ])
      detail.value = current
      tree.value = departments?.items ?? []
      applyDraft({
        dataScope: current.role.dataScope,
        departments: current.role.dataScope === 'Custom'
          ? current.customDepartments.map(item => ({ departmentId: item.departmentId, includeChildren: item.includeChildren ?? false }))
          : [],
      })
    }
    finally {
      loading.value = false
    }
  }

  /** 不完整（自定义却没选部门）不提交；请求失败照常抛出 */
  async function save(): Promise<boolean> {
    const current = detail.value
    if (!current || !complete.value)
      return false
    saving.value = true
    try {
      await rolesApi.update({ ...updateBody(current.role), ...toDataScopePayload(draft.value) })
      open.value = false
      return true
    }
    finally {
      saving.value = false
    }
  }

  return { open, role, detail, tree, draft, dirty, complete, loading, saving, start, save }
}

// ── 详情 ──

/** 详情里的一条已授权限：显示名取生成的权限定义表，不在表里的原样显示权限码 */
export interface GrantedPermission {
  code: string
  displayName: string
  description: null | string
}

/** 详情抽屉：角色概览、已授权限、自定义部门（租户侧）与成员 */
export function useRoleDetail() {
  const open = ref(false)
  const loading = ref(false)
  const detail = ref<RoleDetail | null>(null)
  const members = ref<RoleMember[]>([])
  const departmentNames = ref<Map<string, string>>(new Map())

  const permissions = computed<GrantedPermission[]>(() => (detail.value?.permissions ?? []).map(code => ({
    code,
    displayName: definitions[code]?.displayName ?? code,
    description: definitions[code]?.description ?? null,
  })))

  async function start(row: RoleRow, canReadDepartments: boolean): Promise<void> {
    open.value = true
    loading.value = true
    detail.value = null
    members.value = []
    try {
      const [current, list, departments] = await Promise.all([
        rolesApi.get({ id: row.id }),
        rolesApi.members({ id: row.id }),
        canReadDepartments && row.dataScope === 'Custom' ? departmentsApi.tree({}) : Promise.resolve(null),
      ])
      detail.value = current
      members.value = list.items
      departmentNames.value = departments ? buildNameMap(departments.items) : new Map()
    }
    finally {
      loading.value = false
    }
  }

  function departmentName(id: string): string {
    return departmentNames.value.get(id) ?? id
  }

  return { open, loading, detail, members, permissions, start, departmentName }
}
