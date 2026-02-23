import type { PageQuery } from './common'

// ==================== 系统管理类型 ====================

export interface SysUser {
  id: string
  username: string
  nickname: string
  avatar?: string
  email?: string
  phone?: string
  gender: number
  status: number
  roles: string[]
  deptId?: string
  createTime: string
  updateTime?: string
  remark?: string
}

export interface SysRole {
  id: string
  name: string
  code: string
  description?: string
  status: number
  sort: number
  permissions: string[]
  createTime: string
  updateTime?: string
}

export interface SysMenu {
  id: string
  parentId?: string
  name: string
  path: string
  component?: string
  icon?: string
  type: number
  permission?: string
  sort: number
  status: number
  hidden: boolean
  children?: SysMenu[]
  createTime: string
}

export interface SysPermission {
  id: string
  permissionName: string
  permissionCode: string
  description?: string
  groupName?: string
  status?: number
}

export interface SysDepartment {
  id: string
  parentId?: string | null
  departmentName: string
  departmentCode?: string
  leader?: string
  phone?: string
  email?: string
  sort?: number
  status?: number
  children?: SysDepartment[]
}

export interface SysTenant {
  id: string
  tenantName: string
  tenantCode?: string
  contactName?: string
  contactPhone?: string
  status?: number
  expiredTime?: string
  createTime?: string
}

export interface SysLogItem {
  id?: string
  createdTime?: string
  userName?: string
  operationName?: string
  message?: string
  ip?: string
  [key: string]: any
}

// ==================== 业务分页查询参数 ====================

export interface UserPageQuery extends PageQuery {
  status?: number | undefined
  roleId?: string
}

export interface RolePageQuery extends PageQuery {
  status?: number | undefined
}
