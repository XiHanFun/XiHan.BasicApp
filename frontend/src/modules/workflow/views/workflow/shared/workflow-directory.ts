/**
 * 审批人选择的数据源：组织对象（角色 / 岗位 / 部门）与用户。
 *
 * 组织对象清单小且同一会话里很少变，设计器内多个规则编辑器共用一份；打开设计器时调用 reload 刷新一次。
 * 用户按需检索，见过的用户记在缓存里，已选用户回显不必每次请求。
 */
import type { WorkflowDepartmentOptionDto, WorkflowUserOptionDto } from '../../../api'
import { workflowDirectoryApi } from '../../../api'

export interface DirectoryOption {
  label: string
  value: string
  description?: string
}

export interface OrganizationDirectory {
  roles: DirectoryOption[]
  positions: DirectoryOption[]
  /** 部门拍平成一层，label 为「上级 / 本部门」路径，便于在多选框里检索 */
  departments: DirectoryOption[]
}

let organizationPromise: Promise<OrganizationDirectory> | null = null
const userCache = new Map<string, WorkflowUserOptionDto>()

/** 部门树拍平：label 带上级路径，description 为编码 */
export function flattenDepartments(tree: WorkflowDepartmentOptionDto[], parentPath = ''): DirectoryOption[] {
  const result: DirectoryOption[] = []
  for (const node of tree) {
    const path = parentPath ? `${parentPath} / ${node.name}` : node.name
    result.push({ label: path, value: node.code, description: node.code })
    result.push(...flattenDepartments(node.children ?? [], path))
  }
  return result
}

/** 加载组织对象清单（同一会话共用；force 时重新取回） */
export function loadOrganizationDirectory(force = false): Promise<OrganizationDirectory> {
  if (!organizationPromise || force) {
    organizationPromise = Promise.all([
      workflowDirectoryApi.roleOptions(),
      workflowDirectoryApi.positionOptions(),
      workflowDirectoryApi.departmentTree(),
    ]).then(([roles, positions, departments]) => ({
      roles: roles.map(role => ({ label: role.name, value: role.code, description: role.code })),
      positions: positions.map(position => ({ label: position.name, value: position.code, description: position.code })),
      departments: flattenDepartments(departments),
    })).catch((error: unknown) => {
      organizationPromise = null
      throw error
    })
  }
  return organizationPromise
}

/** 用户显示名（同名时带上用户名区分） */
export function userLabel(user: WorkflowUserOptionDto): string {
  return user.displayName && user.displayName !== user.userName
    ? `${user.displayName}（${user.userName}）`
    : user.userName
}

/** 用户转下拉选项（值为主键字符串，与受理人标识同形） */
export function toUserOption(user: WorkflowUserOptionDto): DirectoryOption {
  return { label: userLabel(user), value: String(user.userId), description: user.userName }
}

/** 按关键字检索启用用户（结果进缓存） */
export async function searchUsers(keyword: string): Promise<WorkflowUserOptionDto[]> {
  const users = await workflowDirectoryApi.userOptions({ keyword: keyword.trim() || null, limit: 50 })
  for (const user of users)
    userCache.set(String(user.userId), user)
  return users
}

/** 按主键取用户（缓存命中的不再请求；查不到的主键不返回） */
export async function resolveUsers(ids: string[]): Promise<WorkflowUserOptionDto[]> {
  const unique = [...new Set(ids.filter(id => id.trim() !== ''))]
  const missing = unique.filter(id => !userCache.has(id))
  if (missing.length > 0) {
    const users = await workflowDirectoryApi.userOptions({ ids: missing, limit: missing.length })
    for (const user of users)
      userCache.set(String(user.userId), user)
  }
  return unique.map(id => userCache.get(id)).filter((user): user is WorkflowUserOptionDto => user !== undefined)
}
