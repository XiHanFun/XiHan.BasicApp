import type { Ref } from 'vue'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import type { SelectOption, TreeSelectOption } from '~/types'
import { computed, reactive, ref } from 'vue'
import { departments as departmentsApi, memberships as membershipsApi, positions as positionsApi, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]
export type DepartmentRecord = DepartmentNode['department']
export type DepartmentStatus = DepartmentRecord['status']
export type DepartmentDetail = ResponseOf<'departments.get'>
export type MembershipRow = ResponseOf<'memberships.list'>['items'][number]

/** SchemaPage 树形模式的一行：部门字段平铺，带直接成员数与子行（`children`） */
export interface DepartmentRow extends DepartmentRecord {
  memberCount: number
  children: DepartmentRow[]
}

/** 候选用户：主文本取显示名，副文本取登录名（XCombobox 两行都参与本地筛选） */
export interface UserOption extends SelectOption {
  description: string
}

/** 部门弹窗表单：新建与编辑共用（编码只在新建时可改；上级在编辑时经 departments.move 落库） */
export interface DepartmentForm {
  code: string
  name: string
  parentId: null | string
  /** 负责人（本租户的用户）；为空即没有 */
  leaderUserId: null | string
  order: null | number
  description: string
  status: DepartmentStatus
}

export type DepartmentEditorMode = 'create' | 'edit'

/** 任职弹窗表单：添加成员与编辑任职共用（用户只在添加时选；主部门只在添加时可勾，已有归属设主走「设为主部门」） */
export interface MembershipForm {
  userId: null | string
  positionId: null | string
  employeeNumber: string
  jobGrade: string
  /** 入职日期，时间戳（毫秒），与 XDatePicker 同一口径 */
  hiredAt: null | number
  order: null | number
  isPrimary: boolean
}

export type MembershipEditorMode = 'create' | 'edit'

/** 候选清单与成员列表取前 100 条（与改版前的页面同一上限） */
const LIST_LIMIT = 100

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '')
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** departments.tree 的节点（部门 + 成员数 + 子节点）→ SchemaPage 树表的行 */
export function toDepartmentRows(nodes: DepartmentNode[]): DepartmentRow[] {
  return nodes.map(node => ({ ...node.department, memberCount: Number(node.memberCount), children: toDepartmentRows(node.children) }))
}

/** 上级部门的候选树：编辑时去掉部门自己连同整棵子树（后端也拒绝循环，这里只是少给错误的选项） */
export function toParentOptions(rows: DepartmentRow[], excludeId?: string): TreeSelectOption[] {
  return rows.filter(row => row.id !== excludeId).map((row) => {
    const children = toParentOptions(row.children, excludeId)
    return children.length > 0 ? { label: row.name, value: row.id, children } : { label: row.name, value: row.id }
  })
}

/**
 * SchemaPage 的资源（树形模式）：整棵树（departments.tree 一次取回、不分页）、删除与启停。
 * 部门树不接受筛选与排序：查询里出现即报错，而不是静默丢掉条件。取回的树经 onTree 交给页面，供上级候选复用。
 */
export function departmentsResource(onTree?: (rows: DepartmentRow[]) => void): SchemaResource<DepartmentRow> {
  return {
    tree: async (params: SchemaQueryParams) => {
      if ((params.sorts ?? []).length > 0 || (params.conditionFilters ?? []).length > 0 || Object.values(params.filters).some(value => !isBlank(value)))
        throw new Error('部门树（departments.tree）不接受筛选与排序：页面字段不要声明 searchable / advancedSearch / sortable')
      const rows = toDepartmentRows((await departmentsApi.tree({})).items)
      onTree?.(rows)
      return rows
    },
    remove: async id => failedCodes(await departmentsApi.delete({ ids: [String(id)] })),
    // 更新是整体替换：先取当前值再只改状态（负责人原样带回）
    updateStatus: async (id, enabled) => {
      const { department } = await departmentsApi.get({ id: String(id) })
      return departmentsApi.update({
        id: department.id,
        name: department.name,
        leaderUserId: department.leaderUserId ?? null,
        order: department.order,
        description: department.description ?? null,
        status: enabled ? 'Active' : 'Disabled',
      })
    },
  }
}

function emptyDepartmentForm(): DepartmentForm {
  return { code: '', name: '', parentId: null, leaderUserId: null, order: 0, description: '', status: 'Active' }
}

/**
 * 部门的编辑弹窗：新建（顶层或某部门之下）、编辑。
 * 6.0 的上级不在 departments.update 里改：编辑时上级变了，保存后再调 departments.move。
 * 负责人按关键字远程检索用户（users.list，要能查看用户）；编辑时先按用户详情把现任负责人放进候选，组合框才显示得出名字。
 * 字段级错误挂回对应字段，其余错误给整条提示。
 */
export function useDepartmentEditor(tree: Ref<DepartmentRow[]>) {
  const open = ref(false)
  const mode = ref<DepartmentEditorMode>('create')
  const editing = ref<DepartmentRow | null>(null)
  const form = reactive<DepartmentForm>(emptyDepartmentForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)
  const leaderOptions = ref<UserOption[]>([])
  const leaderLoading = ref(false)
  let leaderSeq = 0

  const isEdit = computed(() => mode.value === 'edit')
  const parentOptions = computed(() => toParentOptions(tree.value, isEdit.value ? editing.value?.id : undefined))

  function reset(next: Partial<DepartmentForm> = {}) {
    Object.assign(form, emptyDepartmentForm(), next)
    fieldErrors.value = {}
    submitError.value = null
    leaderOptions.value = []
  }

  /** 只认最后一次检索的结果，免得慢请求把新结果盖掉；当前选中的负责人始终留在候选里 */
  async function searchLeaders(keyword: string) {
    const seq = ++leaderSeq
    leaderLoading.value = true
    try {
      const result = await usersApi.list({ page: { index: 1, size: LIST_LIMIT }, filters: [], sorts: [{ field: 'userName' }], keyword: keyword.trim() || null })
      if (seq !== leaderSeq)
        return
      const options: UserOption[] = result.items.map(user => ({ label: user.displayName, value: user.id, description: user.userName }))
      const selected = leaderOptions.value.find(option => option.value === form.leaderUserId)
      if (selected && !options.some(option => option.value === selected.value))
        options.unshift(selected)
      leaderOptions.value = options
    }
    finally {
      if (seq === leaderSeq)
        leaderLoading.value = false
    }
  }

  /** 新建：给了上级即「新增子部门」 */
  function startCreate(parentId: null | string = null) {
    mode.value = 'create'
    editing.value = null
    reset({ parentId })
    open.value = true
  }

  /** leaders：能检索用户时才取现任负责人（看不了用户时负责人一栏不显示，原样带回） */
  function startEdit(row: DepartmentRow, leaders = false) {
    mode.value = 'edit'
    editing.value = row
    reset({ code: row.code, name: row.name, parentId: row.parentId ?? null, leaderUserId: row.leaderUserId ?? null, order: row.order, description: row.description ?? '', status: row.status })
    open.value = true
    if (leaders && row.leaderUserId)
      void seedLeader(row.id, row.leaderUserId)
  }

  /** 现任负责人可能不在第一页候选里：按用户详情放进候选，与检索结果同样显示显示名与登录名 */
  async function seedLeader(departmentId: string, userId: string) {
    try {
      const user = await usersApi.get({ id: userId })
      if (editing.value?.id === departmentId && form.leaderUserId === userId && !leaderOptions.value.some(option => option.value === userId))
        leaderOptions.value = [{ label: user.displayName, value: user.id, description: user.userName }, ...leaderOptions.value]
    }
    catch (error) {
      submitError.value = describe(error)
    }
  }

  function fieldError(field: keyof DepartmentForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await departmentsApi.update({
          id: current.id,
          name: form.name.trim(),
          leaderUserId: form.leaderUserId,
          order: form.order ?? 0,
          description: blankToNull(form.description),
          status: form.status,
        })
        // 移动：空目标即移到顶层；移到自己或子孙之下由后端拒绝（ORGANIZATION_VALIDATION_002）
        if ((form.parentId ?? null) !== (current.parentId ?? null))
          await departmentsApi.move({ id: current.id, parentId: form.parentId })
      }
      else {
        await departmentsApi.create({
          code: form.code.trim(),
          name: form.name.trim(),
          parentId: form.parentId,
          leaderUserId: form.leaderUserId,
          order: form.order ?? 0,
          description: blankToNull(form.description),
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

  return { open, mode, isEdit, editing, form, parentOptions, leaderOptions, leaderLoading, searchLeaders, fieldError, submitError, submitting, startCreate, startEdit, submit }
}

function listMembers(departmentId: string) {
  return membershipsApi.list({
    page: { index: 1, size: LIST_LIMIT },
    filters: [{ field: 'departmentId', operator: 'eq', value: departmentId }],
    sorts: [{ field: 'order' }, { field: 'createdAt' }],
  })
}

/**
 * 部门详情对话框：详情（祖先链、负责人、子部门数、成员数）与成员列表（能读归属的主体才取）；子部门取自树里这一行的子行。
 */
export function useDepartmentDetail(canReadMembers: () => boolean) {
  const open = ref(false)
  const loading = ref(false)
  const row = ref<DepartmentRow | null>(null)
  const detail = ref<DepartmentDetail | null>(null)
  const members = ref<MembershipRow[]>([])
  const error = ref<string | null>(null)

  /** 路径：由远到近的祖先，再到部门自己 */
  const path = computed(() => {
    const current = detail.value
    if (!current)
      return ''
    return [...current.ancestors].reverse().map(a => a.name).concat(current.department.name).join(' / ')
  })

  async function load(id: string) {
    const [loaded, memberPage] = await Promise.all([
      departmentsApi.get({ id }),
      canReadMembers() ? listMembers(id) : Promise.resolve(null),
    ])
    detail.value = loaded
    members.value = memberPage?.items ?? []
  }

  async function show(target: DepartmentRow) {
    row.value = target
    detail.value = null
    members.value = []
    error.value = null
    open.value = true
    loading.value = true
    try {
      await load(target.id)
    }
    catch (failure) {
      error.value = describe(failure)
    }
    finally {
      loading.value = false
    }
  }

  /** 成员增删改之后重取详情与成员（失败照常抛出，由页面提示） */
  async function refresh() {
    if (row.value)
      await load(row.value.id)
  }

  return { open, loading, row, detail, members, error, path, show, refresh }
}

function emptyMembershipForm(): MembershipForm {
  return { userId: null, positionId: null, employeeNumber: '', jobGrade: '', hiredAt: null, order: 0, isPrimary: false }
}

function toTimestamp(value: null | string | undefined): null | number {
  return value ? new Date(value).getTime() : null
}

function toIso(value: null | number): null | string {
  return value === null ? null : new Date(value).toISOString()
}

/** 候选从哪里取：没有读用户 / 读岗位权限的主体不发对应请求，页面也不出对应字段 */
export interface MembershipOptionScope {
  users: boolean
  positions: boolean
}

/**
 * 任职弹窗：添加成员（选用户、岗位、工号、职级、入职日期、排序、是否主部门）与编辑任职（用户与主部门不在这里改）。
 * 候选用户与启用中的岗位在打开时取（各前 100 条）；编辑时原岗位已停用也补进候选，免得选中值显示为空。
 */
export function useMembershipEditor() {
  const open = ref(false)
  const mode = ref<MembershipEditorMode>('create')
  const departmentId = ref<string | null>(null)
  const editing = ref<MembershipRow | null>(null)
  const form = reactive<MembershipForm>(emptyMembershipForm())
  const userOptions = ref<UserOption[]>([])
  const positionOptions = ref<SelectOption[]>([])
  const optionsLoading = ref(false)
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')

  async function loadOptions(scope: MembershipOptionScope) {
    optionsLoading.value = true
    try {
      const [usersPage, positionsPage] = await Promise.all([
        scope.users ? usersApi.list({ page: { index: 1, size: LIST_LIMIT }, filters: [], sorts: [{ field: 'userName' }] }) : Promise.resolve(null),
        scope.positions ? positionsApi.list({ page: { index: 1, size: LIST_LIMIT }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [{ field: 'order' }] }) : Promise.resolve(null),
      ])
      if (usersPage)
        userOptions.value = usersPage.items.map(user => ({ label: user.displayName, value: user.id, description: user.userName }))
      if (positionsPage) {
        const options: SelectOption[] = positionsPage.items.map(position => ({ label: position.name, value: position.id }))
        const current = editing.value
        if (current?.positionId && !options.some(option => option.value === current.positionId))
          options.push({ label: current.positionName ?? current.positionId, value: current.positionId })
        positionOptions.value = options
      }
    }
    catch (error) {
      submitError.value = describe(error)
    }
    finally {
      optionsLoading.value = false
    }
  }

  function reset(next: Partial<MembershipForm> = {}) {
    Object.assign(form, emptyMembershipForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate(targetDepartmentId: string, scope: MembershipOptionScope) {
    mode.value = 'create'
    departmentId.value = targetDepartmentId
    editing.value = null
    reset()
    open.value = true
    return loadOptions(scope)
  }

  function startEdit(member: MembershipRow, scope: MembershipOptionScope) {
    mode.value = 'edit'
    departmentId.value = member.departmentId
    editing.value = member
    reset({
      userId: member.userId,
      positionId: member.positionId ?? null,
      employeeNumber: member.employeeNumber ?? '',
      jobGrade: member.jobGrade ?? '',
      hiredAt: toTimestamp(member.hiredAt),
      order: member.order,
      isPrimary: member.isPrimary,
    })
    open.value = true
    return loadOptions({ users: false, positions: scope.positions })
  }

  function fieldError(field: keyof MembershipForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await membershipsApi.update({
          id: current.id,
          positionId: form.positionId,
          isPrimary: current.isPrimary,
          employeeNumber: blankToNull(form.employeeNumber),
          jobGrade: blankToNull(form.jobGrade),
          hiredAt: toIso(form.hiredAt),
          order: form.order ?? 0,
        })
      }
      else {
        await membershipsApi.create({
          userId: form.userId ?? '',
          departmentId: departmentId.value ?? '',
          positionId: form.positionId,
          isPrimary: form.isPrimary,
          employeeNumber: blankToNull(form.employeeNumber),
          jobGrade: blankToNull(form.jobGrade),
          hiredAt: toIso(form.hiredAt),
          order: form.order ?? 0,
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

  return { open, mode, isEdit, editing, form, userOptions, positionOptions, optionsLoading, fieldError, submitError, submitting, startCreate, startEdit, submit }
}

/** 设为主部门：update 是整体替换，其余字段原样带回 */
export async function setPrimaryMembership(member: MembershipRow): Promise<void> {
  await membershipsApi.update({
    id: member.id,
    positionId: member.positionId ?? null,
    isPrimary: true,
    employeeNumber: member.employeeNumber ?? null,
    jobGrade: member.jobGrade ?? null,
    hiredAt: member.hiredAt ?? null,
    order: member.order,
  })
}

/** 移出部门：被后端逐项拒绝时带着错误码抛出 */
export async function removeMembership(id: string): Promise<void> {
  failedCodes(await membershipsApi.delete({ ids: [id] }))
}
