import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { departments as departmentsApi, memberships as membershipsApi, positions as positionsApi, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]
export type DepartmentRow = DepartmentNode['department']
export type DepartmentStatus = DepartmentRow['status']
export type DepartmentDetail = ResponseOf<'departments.get'>
export type MembershipRow = ResponseOf<'memberships.list'>['items'][number]
export type UserOption = ResponseOf<'users.list'>['items'][number]
export type PositionOption = ResponseOf<'positions.list'>['items'][number]

/** 树展平后的一行：深度用于缩进与上级选择 */
export interface FlatDepartment {
  id: string
  code: string
  name: string
  status: DepartmentStatus
  order: number
  depth: number
  memberCount: number
  createdAt: string
}

export interface CreateDepartmentForm {
  code: string
  name: string
  parentId: string
  order: string
}

export interface EditDepartmentForm {
  name: string
  description: string
  order: string
  status: DepartmentStatus
}

export interface MemberForm {
  userId: string
  positionId: string
  employeeNumber: string
  isPrimary: boolean
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

function flatten(nodes: DepartmentNode[], depth: number, out: FlatDepartment[]): FlatDepartment[] {
  for (const node of nodes) {
    const d = node.department
    out.push({ id: d.id, code: d.code, name: d.name, status: d.status, order: d.order, depth, memberCount: Number(node.memberCount), createdAt: d.createdAt })
    flatten(node.children, depth + 1, out)
  }
  return out
}

/**
 * 部门页的状态与动作（租户侧）：整棵树（展平带深度）、创建、选中部门的详情 / 编辑 / 移动 / 删除、成员列表与增删、主部门切换。
 * 契约类型来自生成物；错误只看 `code`、`detail` 与字段级 `errors`；批量操作逐项失败以错误码显示。
 */
export function useDepartments() {
  const tree = ref<DepartmentNode[]>([])
  const total = ref(0)
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const flat = computed(() => flatten(tree.value, 0, []))

  const form = reactive<CreateDepartmentForm>({ code: '', name: '', parentId: '', order: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<DepartmentDetail | null>(null)
  const edit = reactive<EditDepartmentForm>({ name: '', description: '', order: '', status: 'Active' })
  const editErrors = ref<Record<string, string[]>>({})
  const moveTarget = ref('')
  const members = ref<MembershipRow[]>([])
  const memberForm = reactive<MemberForm>({ userId: '', positionId: '', employeeNumber: '', isPrimary: false })
  const memberErrors = ref<Record<string, string[]>>({})
  const userOptions = ref<UserOption[]>([])
  const positionOptions = ref<PositionOption[]>([])
  const detailError = ref<string | null>(null)
  const notice = ref<'saved' | 'moved' | 'member_added' | null>(null)
  const working = ref(false)

  /** 可作为上级的候选：选中部门自己及其子孙除外（后端也会拒绝循环，这里只是少给一个错误的选项） */
  const moveOptions = computed(() => {
    const current = selected.value?.department.id
    if (!current)
      return flat.value
    const rows = flat.value
    const start = rows.findIndex(r => r.id === current)
    if (start < 0)
      return rows
    const depth = rows[start]!.depth
    let end = start + 1
    while (end < rows.length && rows[end]!.depth > depth)
      end++
    return [...rows.slice(0, start), ...rows.slice(end)]
  })

  async function load() {
    loading.value = true
    listError.value = null
    try {
      const result = await departmentsApi.tree({})
      tree.value = result.items
      total.value = Number(result.total)
    }
    catch (error) {
      tree.value = []
      total.value = 0
      listError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof CreateDepartmentForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditDepartmentForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  function memberError(field: keyof MemberForm): string | undefined {
    return memberErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await departmentsApi.create({ code: form.code.trim(), name: form.name.trim(), parentId: form.parentId || null, order: toOrder(form.order) })
      form.code = ''
      form.name = ''
      form.order = ''
      await load()
      await select(created.id)
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

  async function loadMembers(id: string) {
    members.value = (await membershipsApi.list({ page: { index: 1, size: 100 }, filters: [{ field: 'departmentId', operator: 'eq', value: id }], sorts: [{ field: 'order' }, { field: 'createdAt' }] })).items
  }

  /** 候选用户与岗位（本租户前 100 个）：只有能读用户的主体才会调用 */
  async function loadOptions() {
    const [usersResult, positionsResult] = await Promise.all([
      usersApi.list({ page: { index: 1, size: 100 }, filters: [], sorts: [{ field: 'userName' }] }),
      positionsApi.list({ page: { index: 1, size: 100 }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [{ field: 'order' }] }),
    ])
    userOptions.value = usersResult.items
    positionOptions.value = positionsResult.items
  }

  async function select(id: string) {
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    memberErrors.value = {}
    try {
      const detail = await departmentsApi.get({ id })
      selected.value = detail
      edit.name = detail.department.name
      edit.description = detail.department.description ?? ''
      edit.order = String(detail.department.order)
      edit.status = detail.department.status
      moveTarget.value = detail.department.parentId ?? ''
      memberForm.userId = ''
      memberForm.positionId = ''
      memberForm.employeeNumber = ''
      memberForm.isPrimary = false
      await loadMembers(id)
    }
    catch (error) {
      selected.value = null
      members.value = []
      detailError.value = describe(error)
    }
  }

  async function save(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      await departmentsApi.update({ id: current.department.id, name: edit.name.trim(), leaderUserId: current.department.leaderUserId ?? null, order: toOrder(edit.order), description: edit.description.trim() || null, status: edit.status })
      await Promise.all([load(), select(current.department.id)])
      notice.value = 'saved'
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        editErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          detailError.value = error.detail
      }
      else {
        detailError.value = String(error)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 移动：空目标即移到顶层；移到自己或子孙之下由后端拒绝（ORGANIZATION_VALIDATION_002） */
  async function move(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      await departmentsApi.move({ id: current.department.id, parentId: moveTarget.value || null })
      await Promise.all([load(), select(current.department.id)])
      notice.value = 'moved'
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 删除：还有子部门或成员的由后端逐项拒绝（ORGANIZATION_CONFLICT_002） */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await departmentsApi.delete({ ids: [current.department.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selected.value = null
      members.value = []
      await load()
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  async function addMember(): Promise<boolean> {
    const current = selected.value
    if (!current || !memberForm.userId)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    memberErrors.value = {}
    try {
      await membershipsApi.create({
        userId: memberForm.userId,
        departmentId: current.department.id,
        positionId: memberForm.positionId || null,
        isPrimary: memberForm.isPrimary,
        employeeNumber: memberForm.employeeNumber.trim() || null,
      })
      memberForm.userId = ''
      memberForm.positionId = ''
      memberForm.employeeNumber = ''
      memberForm.isPrimary = false
      await Promise.all([load(), loadMembers(current.department.id)])
      notice.value = 'member_added'
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        memberErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          detailError.value = error.detail
      }
      else {
        detailError.value = String(error)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  async function removeMember(membershipId: string): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await membershipsApi.delete({ ids: [membershipId] })
      if (result.failed.length > 0)
        detailError.value = result.failed.map(f => f.code).join('; ')
      await Promise.all([load(), loadMembers(current.department.id)])
      return result.failed.length === 0
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 设主：其余字段原样回传（update 是整体替换） */
  async function setPrimary(membership: MembershipRow): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      await membershipsApi.update({
        id: membership.id,
        positionId: membership.positionId ?? null,
        isPrimary: true,
        employeeNumber: membership.employeeNumber ?? null,
        jobGrade: membership.jobGrade ?? null,
        hiredAt: membership.hiredAt ?? null,
        order: membership.order,
      })
      await loadMembers(current.department.id)
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  return {
    tree,
    flat,
    total,
    loading,
    listError,
    form,
    fieldError,
    submitError,
    submitting,
    selected,
    edit,
    editError,
    moveTarget,
    moveOptions,
    members,
    memberForm,
    memberError,
    userOptions,
    positionOptions,
    detailError,
    notice,
    working,
    load,
    submit,
    select,
    save,
    move,
    remove,
    loadOptions,
    addMember,
    removeMember,
    setPrimary,
  }
}
