/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { PermissionNameOf } from '../permissions/permission-names'
import type { DataScopeDraft, DataScopeValue, DepartmentNode } from './data-scope'
import type { GrantWindowDraft, PermissionWindowDraft } from './grant-window'
import type { InheritedPermission, InheritedPermissionSources, RoleHierarchy } from './role-parents'
import type { PermissionGrantItem, SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, PermissionAction, ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { sideUserFilters } from '~/constants'
import { departments as departmentsApi, exportCenter, permissions as permissionsApi, roles as rolesApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'
import { useAppStore } from '~/stores'
import { partitionApprovals } from '../constraint-rules/constraint-rule'
import { applyPermissionTransfer, diffPermissionGrants } from '../permissions/grant-actions'
import { usePermissionNames } from '../permissions/permission-names'
import { buildNameMap, isDataScopeComplete, isDataScopeDirty, toDataScopePayload } from './data-scope'
import { emptyGrantWindow, isGrantWindowValid, toGrantWindowPayload, toPermissionWindowPayload } from './grant-window'
import { summarizeInheritedPermissions } from './role-parents'

export type RoleRow = ResponseOf<'roles.list'>['items'][number]
export type RoleDetail = ResponseOf<'roles.get'>
export type RoleStatus = RoleRow['status']
export type PermissionItem = ResponseOf<'permissions.list'>['items'][number]
export type RoleMember = ResponseOf<'roles.members'>['items'][number]
export type UserOption = ResponseOf<'users.list'>['items'][number]
export type { DataScopeValue, DepartmentNode }

export const ROLE_STATUSES: readonly RoleStatus[] = ['Active', 'Disabled']

/** 候选用户一次取的上限（后端 PageInfo 的页大小上限 500） */
const CANDIDATE_LIMIT = 500

function blankToNull(value: string): null | string {
  return value.trim() === ''
    ? null
    : value.trim()
}

/** 修改角色是整体覆盖名称 / 说明 / 排序 / 状态；数据范围与自定义部门不传即保留 */
function updateBody(role: RoleDetail['role'], patch: { status?: RoleStatus } = {}) {
  return {
    id: role.id,
    name: role.name,
    description: role.description ?? null,
    order: role.order,
    status: patch.status ?? role.status,
  }
}

/** 角色列表的筛选登记 */
const ROLE_QUERY: PageQueryMapping = {
  filters: { status: 'eq' },
}

/**
 * 角色的 SchemaPage 资源
 * 作用侧为当前租户或平台；关键字匹配角色码与名称，排序只放角色码、名称、排序与创建时间
 */
export function rolesResource(): SchemaResource<RoleRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await rolesApi.list(toPageRequest(params, ROLE_QUERY)),
      params,
    ),
    export: exportCenter('roles.list', ROLE_QUERY),
    // 内建角色由后端逐项拒绝（ACCESS_CONFLICT_002）
    remove: async id => throwOnFailed(await rolesApi.delete({
      ids: [
        String(id),
      ],
    })),
    // 先取当前值再只改状态
    updateStatus: async (id, enabled) => {
      const { role } = await rolesApi.get({
        id: String(id),
      })
      return rolesApi.update(updateBody(role, {
        status: enabled ? 'Active' : 'Disabled',
      }))
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
  return {
    code: '',
    name: '',
    description: '',
    order: 0,
    status: 'Active',
  }
}

/**
 * 角色的编辑弹窗：新建、编辑名称 / 说明 / 排序 / 状态
 * 新建时数据范围按侧取缺省，另在「数据范围」里改
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
    reset({
      code: row.code,
      name: row.name,
      description: row.description ?? '',
      order: row.order,
      status: row.status,
    })
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
        await rolesApi.update({
          id: current.id,
          name: form.name.trim(),
          description: blankToNull(form.description),
          order: form.order,
          status: form.status,
        })
      }
      else {
        await rolesApi.create({
          code: form.code.trim(),
          name: form.name.trim(),
          description: blankToNull(form.description),
          order: form.order,
        })
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

  return {
    open,
    mode,
    isEdit,
    editing,
    form,
    fieldError,
    submitError,
    submitting,
    startCreate,
    startEdit,
    submit,
  }
}

// ── 权限分配（授予 / 拒绝穿梭框与逐条有效期） ──

/** 权限穿梭框条目：主键即权限码 */
export interface RolePermissionItem extends PermissionGrantItem {
  basicId: string
}

/** 权限码的「包.资源」段即穿梭框的分段（identity.users.read → identity.users） */
function groupOf(code: string): string {
  return code.split('.').slice(0, 2).join('.')
}

/**
 * 权限目录 → 穿梭框条目，按「包.资源」分段、段名取资源显示名
 * 已授予或已拒绝却不在目录里的（如套餐收窄后不再列出）补进来，原样显示权限码
 * 穿梭框只认条目里的键，不补进来，一动穿梭框它们就被当成收回
 */
export function toPermissionItems(catalog: readonly PermissionItem[], current: readonly string[]): RolePermissionItem[] {
  const items: RolePermissionItem[] = catalog.map(item => ({
    basicId: item.code,
    permissionCode: item.code,
    permissionName: item.displayName,
    groupCode: `${item.package}.${item.resource}`,
    groupName: item.resourceName,
  }))
  const known = new Set(items.map(item => item.basicId))
  for (const code of current) {
    if (!known.has(code)) {
      items.push({
        basicId: code,
        permissionCode: code,
        permissionName: code,
        groupCode: groupOf(code),
        groupName: groupOf(code),
      })
    }
  }
  return items
}

/** 角色现有的授予与拒绝（按权限码）：授予取 `permissions`，拒绝只在 `rules` 里，两者按权限码互斥 */
export function roleActions(detail: Pick<RoleDetail, 'permissions' | 'rules'>): Map<string, PermissionAction> {
  const actions = new Map<string, PermissionAction>(detail.permissions.map(code => [code, 'Grant'] as const))
  for (const rule of detail.rules) {
    if (rule.action === 'Deny')
      actions.set(rule.permission, 'Deny')
  }
  return actions
}

function codesOf(actions: ReadonlyMap<string, PermissionAction>, action: PermissionAction): string[] {
  return [...actions].filter(([, value]) => value === action).map(([code]) => code)
}

/** 一次保存的结果：成功授予 / 拒绝 / 撤销的条数，逐项失败的原因，转为审批的说明（触犯需审批的约束规则，已替用户发起授权申请） */
export interface ChangeOutcome {
  added: number
  denied: number
  removed: number
  failures: string[]
  submitted: string[]
}

function emptyOutcome(): ChangeOutcome {
  return {
    added: 0,
    denied: 0,
    removed: 0,
    failures: [],
    submitted: [],
  }
}

export type PermissionEditorTab = 'deny' | 'grant' | 'validity'

/**
 * 权限分配抽屉：授予与拒绝两个页签共用一份草稿，保存时按差量提交；「有效期」页签逐条改现有的
 * 目录一次取全并按界面语言缓存；这次新授予、新拒绝的一并带上抽屉里填的有效期与原因
 */
export function useRolePermissionEditor() {
  const open = ref(false)
  const role = ref<RoleRow | null>(null)
  const tab = ref<PermissionEditorTab>('grant')
  const catalog = ref<PermissionItem[]>([])
  /** 目录是按哪种界面语言取的：显示名随语言变，换了语言再取 */
  let catalogLocale: null | string = null
  const detail = ref<RoleDetail | null>(null)
  const current = ref(new Map<string, PermissionAction>())
  const actions = ref(new Map<string, PermissionAction>())
  /** 这次新授予、新拒绝的有效期与原因 */
  const windowDraft = ref<PermissionWindowDraft>(emptyGrantWindow())
  /** 从上级继承来的授予与拒绝（按权限码）：穿梭框里标「继承」与「上级拒绝」 */
  const inherited = ref<Map<string, InheritedPermissionSources>>(new Map())
  const loading = ref(false)
  const saving = ref(false)

  const items = computed(() => toPermissionItems(catalog.value, [
    ...current.value.keys(),
  ]))
  const grantIds = computed(() => codesOf(actions.value, 'Grant'))
  const denyIds = computed(() => codesOf(actions.value, 'Deny'))
  const dirty = computed(() => {
    const { grant, deny, revoke } = diffPermissionGrants(actions.value, current.value)
    return grant.length + deny.length + revoke.length > 0
  })
  const windowValid = computed(() => isGrantWindowValid(windowDraft.value))
  const catalogNames = computed(() => new Map(catalog.value.map(item => [item.code, item.displayName] as const)))
  /** 「有效期」页签：现有的授予与拒绝 */
  const entries = computed<RolePermissionEntry[]>(() => detail.value
    ? rolePermissionEntries(detail.value.permissions, detail.value.rules, code => catalogNames.value.get(code) ?? code)
    : [])

  function applyDetail(next: RoleDetail | null) {
    detail.value = next
    current.value = next
      ? roleActions(next)
      : new Map()
    actions.value = new Map(current.value)
  }

  async function start(row: RoleRow): Promise<void> {
    role.value = row
    tab.value = 'grant'
    applyDetail(null)
    windowDraft.value = emptyGrantWindow()
    inherited.value = new Map()
    open.value = true
    loading.value = true
    try {
      const locale = useAppStore().locale
      const [list, roleDetail, hierarchy] = await Promise.all([
        catalogLocale === locale
          ? Promise.resolve(null)
          : permissionsApi.list({}),
        rolesApi.get({ id: row.id }),
        rolesApi.hierarchy({ id: row.id }),
      ])
      if (list) {
        catalog.value = list.items
        catalogLocale = locale
      }
      applyDetail(roleDetail)
      inherited.value = summarizeInheritedPermissions(hierarchy.inherited)
    }
    finally {
      loading.value = false
    }
  }

  /** 某个页签的右栏变成 next */
  function transfer(action: PermissionAction, next: readonly string[]) {
    if (loading.value || saving.value)
      return
    actions.value = applyPermissionTransfer(actions.value, action, next)
  }

  /** 没有改动或有效期不成立返回 null；请求失败照常抛出 */
  async function save(): Promise<ChangeOutcome | null> {
    const target = role.value
    if (!target || !windowValid.value)
      return null
    const { grant, deny, revoke } = diffPermissionGrants(actions.value, current.value)
    if (grant.length + deny.length + revoke.length === 0)
      return null
    saving.value = true
    try {
      const outcome = emptyOutcome()
      const validity = toPermissionWindowPayload(windowDraft.value)
      if (revoke.length > 0) {
        const result = await rolesApi.revoke({ id: target.id, permissions: revoke })
        outcome.removed = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.permission}: ${f.message}`))
      }
      if (grant.length > 0) {
        const result = await rolesApi.grant({ id: target.id, permissions: grant, ...validity })
        outcome.added = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.permission}: ${f.message}`))
      }
      if (deny.length > 0) {
        const result = await rolesApi.deny({ id: target.id, permissions: deny, ...validity })
        outcome.denied = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => `${f.permission}: ${f.message}`))
      }
      applyDetail(await rolesApi.get({ id: target.id }))
      windowDraft.value = emptyGrantWindow()
      return outcome
    }
    finally {
      saving.value = false
    }
  }

  /**
   * 逐条改现有授予或拒绝的有效期与原因：按原动作带新的有效期再提交一次，都清空即普通授予
   * 改完重读现状、草稿随之重置，所以页面在有未保存的穿梭框改动时不放出这个入口
   */
  async function updateEntry(code: string, action: PermissionAction, next: PermissionWindowDraft): Promise<void> {
    const target = role.value
    if (!target)
      return
    saving.value = true
    try {
      const request = {
        id: target.id,
        permissions: [
          code,
        ],
        ...toPermissionWindowPayload(next),
      }
      const result = action === 'Deny'
        ? await rolesApi.deny(request)
        : await rolesApi.grant(request)
      const failed = result.failed[0]
      if (failed)
        throw new Error(failed.message)
      applyDetail(await rolesApi.get({ id: target.id }))
    }
    finally {
      saving.value = false
    }
  }

  return {
    open,
    role,
    tab,
    catalog,
    current,
    actions,
    grantIds,
    denyIds,
    windowDraft,
    windowValid,
    entries,
    inherited,
    items,
    dirty,
    loading,
    saving,
    start,
    transfer,
    save,
    updateEntry,
  }
}

// ── 角色成员（穿梭框与逐个有效期） ──

/** 成员穿梭框的一项：主键即用户主键 */
export interface MemberCandidate {
  basicId: string
  displayName: string
  userName: string
  /** 外部成员：注册在别处、以成员身份加入本租户 */
  isExternalMember: boolean
  /** 运维支持成员：平台账号以运维支持身份进入本租户 */
  isSupportMember: boolean
}

/**
 * 候选人（本侧用户，含本租户的外部成员）并上现有成员
 * 候选有上限、也可能读不到用户目录，不并上现有成员，他们会在穿梭框里「消失」、保存时被当成移出
 */
export function mergeMemberCandidates(users: readonly UserOption[], members: readonly RoleMember[]): MemberCandidate[] {
  const items: MemberCandidate[] = users.map(user => ({
    basicId: user.id,
    displayName: user.displayName,
    userName: user.userName,
    isExternalMember: user.isExternalMember,
    isSupportMember: user.isSupportMember,
  }))
  const known = new Set(items.map(item => item.basicId))
  for (const member of members) {
    if (!known.has(member.userId)) {
      items.push({
        basicId: member.userId,
        displayName: member.displayName,
        userName: member.userName,
        isExternalMember: member.isExternalMember,
        isSupportMember: member.isSupportMember,
      })
    }
  }
  return items
}

/** 草稿与现有成员比出差量：新加入的指派、移出的撤销（两边都按用户主键） */
export function diffMembers(draft: readonly string[], members: readonly RoleMember[]): { assign: string[], unassign: string[] } {
  const current = new Set(members.map(member => member.userId))
  const next = new Set(draft)
  return {
    assign: draft.filter(userId => !current.has(userId)),
    unassign: members.filter(member => !next.has(member.userId)).map(member => member.userId),
  }
}

export type MemberEditorTab = 'members' | 'validity'

/**
 * 角色成员抽屉：挪好后一次提交加入与移出，新加入的带上抽屉里填的有效期；「有效期」页签逐个改现有成员的
 * 读不到用户目录时只能移出现有成员；候选只列调用方这一侧的账号（平台角色只认平台账号）
 */
export function useRoleMemberEditor(isPlatformCaller: () => boolean) {
  const open = ref(false)
  const role = ref<RoleRow | null>(null)
  const tab = ref<MemberEditorTab>('members')
  const members = ref<RoleMember[]>([])
  const users = ref<UserOption[]>([])
  const draft = ref<string[]>([])
  /** 这次新加入成员的有效期 */
  const windowDraft = ref<PermissionWindowDraft>(emptyGrantWindow())
  const loading = ref(false)
  const saving = ref(false)

  const items = computed(() => mergeMemberCandidates(users.value, members.value))
  const dirty = computed(() => {
    const { assign, unassign } = diffMembers(draft.value, members.value)
    return assign.length > 0 || unassign.length > 0
  })
  const windowValid = computed(() => isGrantWindowValid(windowDraft.value))

  function applyMembers(current: RoleMember[]) {
    members.value = current
    draft.value = current.map(member => member.userId)
  }

  async function start(row: RoleRow, canListUsers: boolean): Promise<void> {
    role.value = row
    tab.value = 'members'
    users.value = []
    applyMembers([])
    windowDraft.value = emptyGrantWindow()
    open.value = true
    loading.value = true
    try {
      const [current, candidates] = await Promise.all([
        rolesApi.members({ id: row.id }),
        canListUsers
          ? usersApi.list({
              page: { index: 1, size: CANDIDATE_LIMIT },
              filters: sideUserFilters(isPlatformCaller()),
              sorts: [
                { field: 'userName' },
              ],
            })
          : Promise.resolve(null),
      ])
      users.value = candidates?.items ?? []
      applyMembers(current.items)
    }
    finally {
      loading.value = false
    }
  }

  /** 没有改动或有效期不成立返回 null；请求失败照常抛出 */
  async function save(): Promise<ChangeOutcome | null> {
    const current = role.value
    if (!current || !windowValid.value)
      return null
    const { assign, unassign } = diffMembers(draft.value, members.value)
    if (assign.length === 0 && unassign.length === 0)
      return null
    saving.value = true
    try {
      const outcome = emptyOutcome()
      if (assign.length > 0) {
        const result = await rolesApi.assign({
          id: current.id,
          userIds: assign,
          ...toGrantWindowPayload(windowDraft.value),
        })
        const { submitted, failed } = partitionApprovals(result.failed)
        outcome.added = result.succeeded.length
        outcome.failures.push(...failed.map(f => f.message))
        outcome.submitted.push(...submitted.map(f => f.message))
      }
      if (unassign.length > 0) {
        const result = await rolesApi.unassign({ id: current.id, userIds: unassign })
        outcome.removed = result.succeeded.length
        outcome.failures.push(...result.failed.map(f => f.message))
      }
      applyMembers((await rolesApi.members({ id: current.id })).items)
      windowDraft.value = emptyGrantWindow()
      return outcome
    }
    finally {
      saving.value = false
    }
  }

  /**
   * 改一个现有成员的有效期，都清空即立即生效、长期有效
   * 改完重读成员、草稿随之重置，所以页面在有未保存的成员改动时不放出这个入口
   */
  async function updateMember(userId: string, next: GrantWindowDraft): Promise<void> {
    const current = role.value
    if (!current)
      return
    saving.value = true
    try {
      await rolesApi.updateMember({
        id: current.id,
        userId,
        ...toGrantWindowPayload(next),
      })
      applyMembers((await rolesApi.members({ id: current.id })).items)
    }
    finally {
      saving.value = false
    }
  }

  return {
    open,
    role,
    tab,
    members,
    users,
    draft,
    windowDraft,
    windowValid,
    items,
    dirty,
    loading,
    saving,
    start,
    save,
    updateMember,
  }
}

// ── 数据范围 ──

/**
 * 数据范围抽屉：档位与自定义部门一次保存，只在租户侧（平台角色固定为全部）
 * 保存走 roles.update，其余字段按打开时的详情原样回传，自定义部门整体替换
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
        canReadDepartments
          ? departmentsApi.tree({})
          : Promise.resolve(null),
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
      await rolesApi.update({
        ...updateBody(current.role),
        ...toDataScopePayload(draft.value),
      })
      open.value = false
      return true
    }
    finally {
      saving.value = false
    }
  }

  return {
    open,
    role,
    detail,
    tree,
    draft,
    dirty,
    complete,
    loading,
    saving,
    start,
    save,
  }
}

// ── 详情 ──

export type PermissionRule = RoleDetail['rules'][number]

/** 详情里的一条权限分配（「动作 / 有效期」两列）：显示名取当前界面语言的权限目录，不在目录里的原样显示权限码 */
export interface RolePermissionEntry {
  code: string
  displayName: string
  action: PermissionRule['action']
  effectiveAt: null | string
  expiresAt: null | string
  reason: null | string
}

/**
 * 角色的授予与拒绝条目，先列授予再列拒绝
 * 授予取 `permissions`，有效期与原因由同码的 Grant 规则补上；拒绝只在 `rules` 里
 * 两者按权限码互斥：后端写拒绝即撤掉同码授予
 */
export function rolePermissionEntries(permissions: readonly string[], rules: readonly PermissionRule[], nameOf: PermissionNameOf): RolePermissionEntry[] {
  const grantRules = new Map(rules.filter(rule => rule.action === 'Grant').map(rule => [rule.permission, rule] as const))
  const entry = (code: string, action: PermissionRule['action'], rule?: PermissionRule): RolePermissionEntry => ({
    code,
    displayName: nameOf(code),
    action,
    effectiveAt: rule?.effectiveAt ?? null,
    expiresAt: rule?.expiresAt ?? null,
    reason: rule?.reason ?? null,
  })
  return [
    ...permissions.map(code => entry(code, 'Grant', grantRules.get(code))),
    ...rules.filter(rule => rule.action === 'Deny').map(rule => entry(rule.permission, 'Deny', rule)),
  ]
}

/** 详情里一条继承来的授予或拒绝：显示名取当前界面语言的权限目录 */
export interface InheritedPermissionEntry extends InheritedPermission {
  displayName: string
}

/** 详情抽屉：角色概览、权限分配、继承来的权限、自定义部门（租户侧）、成员与上下级 */
export function useRoleDetail() {
  const open = ref(false)
  const loading = ref(false)
  const detail = ref<RoleDetail | null>(null)
  const members = ref<RoleMember[]>([])
  const hierarchy = ref<RoleHierarchy | null>(null)
  const departmentNames = ref<Map<string, string>>(new Map())
  const permissionNames = usePermissionNames()

  const permissions = computed<RolePermissionEntry[]>(() => detail.value
    ? rolePermissionEntries(detail.value.permissions, detail.value.rules, permissionNames.nameOf)
    : [])
  const inherited = computed<InheritedPermissionEntry[]>(() => (hierarchy.value?.inherited ?? []).map(item => ({
    ...item,
    displayName: permissionNames.nameOf(item.permission),
  })))
  const ancestors = computed(() => hierarchy.value?.ancestors ?? [])
  const descendants = computed(() => hierarchy.value?.descendants ?? [])

  async function start(row: RoleRow, canReadDepartments: boolean): Promise<void> {
    open.value = true
    loading.value = true
    detail.value = null
    members.value = []
    hierarchy.value = null
    try {
      const [current, list, relatives, departments] = await Promise.all([
        rolesApi.get({ id: row.id }),
        rolesApi.members({ id: row.id }),
        rolesApi.hierarchy({ id: row.id }),
        canReadDepartments && row.dataScope === 'Custom'
          ? departmentsApi.tree({})
          : Promise.resolve(null),
        permissionNames.load(),
      ])
      detail.value = current
      members.value = list.items
      hierarchy.value = relatives
      departmentNames.value = departments
        ? buildNameMap(departments.items)
        : new Map()
    }
    finally {
      loading.value = false
    }
  }

  function departmentName(id: string): string {
    return departmentNames.value.get(id) ?? id
  }

  return {
    open,
    loading,
    detail,
    members,
    permissions,
    inherited,
    ancestors,
    descendants,
    start,
    departmentName,
  }
}
