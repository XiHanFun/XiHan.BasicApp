/**
 * 审批人选择的数据源：组织对象（角色 / 岗位 / 部门）与用户。
 *
 * 组织对象清单小且同一会话里很少变，设计器内多个规则编辑器共用一份；打开设计器时调用 reload 刷新一次。
 * 用户按需检索，见过的用户记在缓存里，已选用户回显不必每次请求。
 * 数据取自 workflow-assignees.*：角色、岗位、部门按编码引用，用户按 ID（GUID 字符串）引用，与受理人规则的取值同形。
 */
import type { ResponseOf } from '~/contracts'
import { workflowAssignees } from '~/contracts'

export type AssigneeUser = ResponseOf<'workflow-assignees.users'>['items'][number]
export type AssigneeDepartment = ResponseOf<'workflow-assignees.departments'>['items'][number]

export interface DirectoryOption {
  label: string
  value: string
  description?: string
  /** 停用的部门：已选的照样念得出名字，不能再选 */
  disabled?: boolean
}

export interface OrganizationDirectory {
  roles: DirectoryOption[]
  positions: DirectoryOption[]
  /** 部门拍平成一层，label 为「上级 / 本部门」路径，便于在多选框里检索 */
  departments: DirectoryOption[]
}

/** 用户 ID 是 GUID；规则里别的取值（如从 4.x 搬来的数字 ID）不是 6.0 的用户，不拿去回显 */
const USER_ID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

let organizationPromise: Promise<OrganizationDirectory> | null = null
const userCache = new Map<string, AssigneeUser>()

/**
 * 部门平铺清单按上级 ID 排成树序再拍平：label 带上级路径，description 为编码。
 * 上级不在清单里的部门当作顶层；兄弟之间保持后端给的顺序。
 */
export function flattenDepartments(items: readonly AssigneeDepartment[]): DirectoryOption[] {
  const ids = new Set(items.map(item => item.id))
  const children = new Map<string | null, AssigneeDepartment[]>()
  for (const item of items) {
    const parent = item.parentId && ids.has(item.parentId) ? item.parentId : null
    children.set(parent, [...(children.get(parent) ?? []), item])
  }
  const result: DirectoryOption[] = []
  const visit = (parent: string | null, parentPath: string, seen: Set<string>) => {
    for (const node of children.get(parent) ?? []) {
      if (seen.has(node.id))
        continue
      seen.add(node.id)
      const path = parentPath ? `${parentPath} / ${node.name}` : node.name
      result.push({ label: path, value: node.code, description: node.code, ...(node.isActive ? {} : { disabled: true }) })
      visit(node.id, path, seen)
    }
  }
  visit(null, '', new Set())
  return result
}

/** 加载组织对象清单（同一会话共用；force 时重新取回） */
export function loadOrganizationDirectory(force = false): Promise<OrganizationDirectory> {
  if (!organizationPromise || force) {
    organizationPromise = Promise.all([
      workflowAssignees.roles({}),
      workflowAssignees.positions({}),
      workflowAssignees.departments({}),
    ]).then(([roles, positions, departments]) => ({
      roles: roles.items.map(role => ({ label: role.name, value: role.code, description: role.code })),
      positions: positions.items.map(position => ({ label: position.name, value: position.code, description: position.code })),
      departments: flattenDepartments(departments.items),
    })).catch((error: unknown) => {
      organizationPromise = null
      throw error
    })
  }
  return organizationPromise
}

/** 用户显示名（同名时带上用户名区分） */
export function userLabel(user: AssigneeUser): string {
  return user.displayName && user.displayName !== user.userName
    ? `${user.displayName}（${user.userName}）`
    : user.userName
}

/** 用户转下拉选项（值为用户 ID，与受理人标识同形） */
export function toUserOption(user: AssigneeUser): DirectoryOption {
  return { label: userLabel(user), value: user.id, description: user.userName }
}

/** 按关键字检索在用账号（结果进缓存；条数由后端定） */
export async function searchUsers(keyword: string): Promise<AssigneeUser[]> {
  const { items } = await workflowAssignees.users({ keyword: keyword.trim() || null })
  for (const user of items)
    userCache.set(user.id, user)
  return items
}

/** 按 ID 取用户（缓存命中的不再请求；查不到的与不是用户 ID 的取值不返回；一次最多回显 200 个） */
export async function resolveUsers(ids: string[]): Promise<AssigneeUser[]> {
  const unique = [...new Set(ids.filter(id => USER_ID.test(id)))]
  const missing = unique.filter(id => !userCache.has(id)).slice(0, 200)
  if (missing.length > 0) {
    const { items } = await workflowAssignees.users({ ids: missing })
    for (const user of items)
      userCache.set(user.id, user)
  }
  return unique.map(id => userCache.get(id)).filter((user): user is AssigneeUser => user !== undefined)
}
