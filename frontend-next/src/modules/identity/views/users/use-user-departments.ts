import type { MembershipRow } from './use-user-detail'
import type { UserRow } from './use-users'
import type { ResponseOf } from '~/contracts'
import type { SelectOption } from '~/types'
import { reactive, ref } from 'vue'
import { departments as departmentsApi, memberships as membershipsApi, positions as positionsApi } from '~/contracts'
import { ApiError } from '~/request'
import { loadMemberships } from './use-user-detail'

type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]

/** 加入部门时要装哪些候选（由页面按权限码决定） */
export interface DepartmentPickers {
  /** 部门候选（加入部门要用） */
  departments: boolean
  /** 岗位候选（可选的岗位） */
  positions: boolean
}

export interface MemberForm {
  departmentId: null | string
  positionId: null | string
}

/** 部门树展平成下拉选项，层级用全角空格缩进（与 4.x 用户页的部门候选同一写法） */
export function flattenDepartments(nodes: DepartmentNode[], depth = 0, out: SelectOption[] = []): SelectOption[] {
  for (const node of nodes) {
    out.push({ label: `${'　'.repeat(depth)}${node.department.name}`, value: node.department.id })
    flattenDepartments(node.children, depth + 1, out)
  }
  return out
}

/**
 * 所属部门抽屉（租户侧）：列出该用户的部门归属，加入部门（可带岗位）与移出。
 * 部门与岗位是租户数据，平台侧不出这个入口；字段级错误挂回字段，其余失败抛给页面提示。
 */
export function useUserDepartments() {
  const open = ref(false)
  const user = ref<null | UserRow>(null)
  const loading = ref(false)
  const working = ref(false)
  const memberships = ref<MembershipRow[]>([])
  const departmentOptions = ref<SelectOption[]>([])
  const positionOptions = ref<SelectOption[]>([])
  const form = reactive<MemberForm>({ departmentId: null, positionId: null })
  const fieldErrors = ref<Record<string, string[]>>({})

  function fieldError(field: keyof MemberForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function loadPickers(pickers: DepartmentPickers) {
    const [tree, positions] = await Promise.all([
      pickers.departments ? departmentsApi.tree({}) : Promise.resolve(null),
      pickers.positions ? positionsApi.list({ page: { index: 1, size: 100 }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [{ field: 'order' }] }) : Promise.resolve(null),
    ])
    departmentOptions.value = tree ? flattenDepartments(tree.items) : []
    positionOptions.value = positions ? positions.items.map(position => ({ label: position.name, value: position.id })) : []
  }

  /** 打开抽屉并装载归属与候选；取数失败时抛出，由页面提示 */
  async function start(row: UserRow, pickers: DepartmentPickers): Promise<void> {
    user.value = row
    memberships.value = []
    form.departmentId = null
    form.positionId = null
    fieldErrors.value = {}
    open.value = true
    loading.value = true
    try {
      const [rows] = await Promise.all([loadMemberships(row.id), loadPickers(pickers)])
      memberships.value = rows
    }
    finally {
      loading.value = false
    }
  }

  /** 加入部门：成功清空表单并重取归属；字段级错误挂回字段并返回 false，其余失败抛出 */
  async function add(): Promise<boolean> {
    const current = user.value
    if (!current || !form.departmentId)
      return false
    working.value = true
    fieldErrors.value = {}
    try {
      await membershipsApi.create({ userId: current.id, departmentId: form.departmentId, positionId: form.positionId })
      form.departmentId = null
      form.positionId = null
      memberships.value = await loadMemberships(current.id)
      return true
    }
    catch (error) {
      if (error instanceof ApiError && error.hasFieldErrors) {
        fieldErrors.value = error.errors ?? {}
        return false
      }
      throw error
    }
    finally {
      working.value = false
    }
  }

  /** 移出部门：被后端拒绝时带着错误码抛出；无论成败都按服务端重取归属 */
  async function remove(membershipId: string): Promise<void> {
    const current = user.value
    if (!current)
      return
    working.value = true
    try {
      const result = await membershipsApi.delete({ ids: [membershipId] })
      memberships.value = await loadMemberships(current.id)
      if (result.failed.length > 0)
        throw new Error(result.failed.map(f => f.code).join('; '))
    }
    finally {
      working.value = false
    }
  }

  return { open, user, loading, working, memberships, departmentOptions, positionOptions, form, fieldError, start, add, remove }
}
