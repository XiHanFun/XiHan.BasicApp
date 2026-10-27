import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { departments as departmentsApi, memberships as membershipsApi, positions as positionsApi, roles as rolesApi, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type UserDetail = ResponseOf<'users.get'>
export type RoleOption = ResponseOf<'roles.list'>['items'][number]
export type MembershipRow = ResponseOf<'memberships.list'>['items'][number]
export type PositionOption = ResponseOf<'positions.list'>['items'][number]
type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]

export interface DepartmentOption {
  id: string
  name: string
  depth: number
}

/** 选中用户时要装哪些面板（由页面按权限码决定，composable 不碰授权） */
export interface DetailSections {
  roles: boolean
  departments: boolean
}

export interface MemberForm {
  departmentId: string
  positionId: string
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function flatten(nodes: DepartmentNode[], depth: number, out: DepartmentOption[]): DepartmentOption[] {
  for (const node of nodes) {
    out.push({ id: node.department.id, name: node.department.name, depth })
    flatten(node.children, depth + 1, out)
  }
  return out
}

/**
 * 用户详情面板的状态与动作：账号（锁定 / 解锁 / 重置密码 / 删除）、角色（勾选草稿按差异 roles.assign / unassign）、部门（归属增删）。
 * 契约类型来自生成物；批量操作逐项失败以错误码显示。
 */
export function useUserDetail() {
  const user = ref<UserDetail | null>(null)
  const sections = ref<DetailSections>({ roles: false, departments: false })
  const error = ref<string | null>(null)
  const notice = ref<'roles_saved' | 'password_reset' | 'department_added' | 'locked' | null>(null)
  const working = ref(false)

  const roleOptions = ref<RoleOption[]>([])
  const assigned = ref<Set<string>>(new Set())
  const roleDraft = ref<Set<string>>(new Set())
  const rolesDirty = computed(() => assigned.value.size !== roleDraft.value.size || [...roleDraft.value].some(id => !assigned.value.has(id)))

  const memberships = ref<MembershipRow[]>([])
  const departmentOptions = ref<DepartmentOption[]>([])
  const positionOptions = ref<PositionOption[]>([])
  const memberForm = reactive<MemberForm>({ departmentId: '', positionId: '' })

  const resetForm = reactive({ newPassword: '' })
  const resetErrors = ref<Record<string, string[]>>({})

  function resetError(field: 'newPassword'): string | undefined {
    return resetErrors.value[field]?.[0]
  }

  async function loadRoles(userId: string) {
    const [catalog, mine] = await Promise.all([
      rolesApi.list({ page: { index: 1, size: 100 }, filters: [], sorts: [{ field: 'order' }, { field: 'code' }] }),
      rolesApi.byUser({ userId }),
    ])
    roleOptions.value = catalog.items
    assigned.value = new Set(mine.items.map(r => r.id))
    roleDraft.value = new Set(assigned.value)
  }

  async function loadMemberships(userId: string) {
    memberships.value = (await membershipsApi.list({ page: { index: 1, size: 100 }, filters: [{ field: 'userId', operator: 'eq', value: userId }], sorts: [{ field: 'order' }, { field: 'createdAt' }] })).items
  }

  /** 加入部门的候选（部门树展平 + 启用的岗位） */
  async function loadDepartmentOptions() {
    const [tree, positions] = await Promise.all([
      departmentsApi.tree({}),
      positionsApi.list({ page: { index: 1, size: 100 }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [{ field: 'order' }] }),
    ])
    departmentOptions.value = flatten(tree.items, 0, [])
    positionOptions.value = positions.items
  }

  async function select(id: string, wanted: DetailSections) {
    error.value = null
    notice.value = null
    resetErrors.value = {}
    sections.value = wanted
    try {
      user.value = await usersApi.get({ id })
      memberForm.departmentId = ''
      memberForm.positionId = ''
      resetForm.newPassword = ''
      await Promise.all([
        wanted.roles ? loadRoles(id) : Promise.resolve(),
        wanted.departments ? loadMemberships(id) : Promise.resolve(),
      ])
    }
    catch (e) {
      user.value = null
      error.value = describe(e)
    }
  }

  async function reload() {
    const current = user.value
    if (current)
      user.value = await usersApi.get({ id: current.id })
  }

  function toggleRole(roleId: string) {
    const next = new Set(roleDraft.value)
    if (next.has(roleId))
      next.delete(roleId)
    else next.add(roleId)
    roleDraft.value = next
  }

  /** 按差异提交：新勾的 roles.assign、取消的 roles.unassign（每个角色一次） */
  async function saveRoles(): Promise<boolean> {
    const current = user.value
    if (!current)
      return false
    working.value = true
    error.value = null
    notice.value = null
    try {
      const failed: string[] = []
      for (const roleId of [...roleDraft.value].filter(id => !assigned.value.has(id)))
        failed.push(...(await rolesApi.assign({ id: roleId, userIds: [current.id] })).failed.map(f => f.code))
      for (const roleId of [...assigned.value].filter(id => !roleDraft.value.has(id)))
        failed.push(...(await rolesApi.unassign({ id: roleId, userIds: [current.id] })).failed.map(f => f.code))
      await loadRoles(current.id)
      if (failed.length > 0) {
        error.value = failed.join('; ')
        return false
      }
      notice.value = 'roles_saved'
      return true
    }
    catch (e) {
      error.value = describe(e)
      return false
    }
    finally {
      working.value = false
    }
  }

  async function batch(action: 'lock' | 'unlock' | 'delete'): Promise<boolean> {
    const current = user.value
    if (!current)
      return false
    working.value = true
    error.value = null
    notice.value = null
    try {
      const api = action === 'lock' ? usersApi.lock : action === 'unlock' ? usersApi.unlock : usersApi.delete
      const result = await api({ ids: [current.id] })
      if (result.failed.length > 0) {
        error.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      if (action === 'delete') {
        user.value = null
        return true
      }
      await reload()
      if (action === 'lock')
        notice.value = 'locked'
      return true
    }
    catch (e) {
      error.value = describe(e)
      return false
    }
    finally {
      working.value = false
    }
  }

  const lock = () => batch('lock')
  const unlock = () => batch('unlock')
  const remove = () => batch('delete')

  async function resetPassword(): Promise<boolean> {
    const current = user.value
    if (!current)
      return false
    working.value = true
    error.value = null
    notice.value = null
    resetErrors.value = {}
    try {
      await usersApi.resetPassword({ id: current.id, newPassword: resetForm.newPassword })
      resetForm.newPassword = ''
      notice.value = 'password_reset'
      return true
    }
    catch (e) {
      if (e instanceof ApiError) {
        resetErrors.value = e.errors ?? {}
        if (!e.hasFieldErrors)
          error.value = e.detail
      }
      else {
        error.value = String(e)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  async function addDepartment(): Promise<boolean> {
    const current = user.value
    if (!current || !memberForm.departmentId)
      return false
    working.value = true
    error.value = null
    notice.value = null
    try {
      await membershipsApi.create({ userId: current.id, departmentId: memberForm.departmentId, positionId: memberForm.positionId || null })
      memberForm.departmentId = ''
      memberForm.positionId = ''
      await loadMemberships(current.id)
      notice.value = 'department_added'
      return true
    }
    catch (e) {
      error.value = describe(e)
      return false
    }
    finally {
      working.value = false
    }
  }

  async function removeDepartment(membershipId: string): Promise<boolean> {
    const current = user.value
    if (!current)
      return false
    working.value = true
    error.value = null
    notice.value = null
    try {
      const result = await membershipsApi.delete({ ids: [membershipId] })
      if (result.failed.length > 0)
        error.value = result.failed.map(f => f.code).join('; ')
      await loadMemberships(current.id)
      return result.failed.length === 0
    }
    catch (e) {
      error.value = describe(e)
      return false
    }
    finally {
      working.value = false
    }
  }

  return {
    user,
    sections,
    error,
    notice,
    working,
    roleOptions,
    assigned,
    roleDraft,
    rolesDirty,
    memberships,
    departmentOptions,
    positionOptions,
    memberForm,
    resetForm,
    resetError,
    select,
    toggleRole,
    saveRoles,
    lock,
    unlock,
    remove,
    resetPassword,
    loadDepartmentOptions,
    addDepartment,
    removeDepartment,
  }
}
