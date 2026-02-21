// ==================== 通用类型 ====================

export interface Recordable<T = any> {
  [key: string]: T
}

export interface PageResult<T = any> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ApiResponse<T = any> {
  code: number
  message: string
  data: T
  success: boolean
}

export interface SelectOption {
  label: string
  value: string | number
  disabled?: boolean
}

// ==================== 用户类型 ====================

export interface UserInfo {
  basicId: number
  userName: string
  nickName?: string
  avatar?: string
  email?: string
  phone?: string
  roles: string[]
  permissions: string[]
}

export interface LoginParams {
  username: string
  password: string
  captchaCode?: string
  captchaKey?: string
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  issuedAt: string
  expiresAt: string
  user: UserInfo
  roles: string[]
  permissions: string[]
}

// ==================== 菜单路由类型 ====================

export interface MenuMeta {
  title: string
  icon?: string
  hidden?: boolean
  keepAlive?: boolean
  affixTab?: boolean
  roles?: string[]
  permissions?: string[]
  order?: number
  badge?: string | number
  dot?: boolean
  link?: string
}

export interface MenuRoute {
  id?: string
  path: string
  name: string
  component?: string
  redirect?: string
  meta: MenuMeta
  children?: MenuRoute[]
}

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

export interface TabItem {
  key: string
  title: string
  path: string
  pinned?: boolean
  closable: boolean
}

// ==================== 分页查询参数 ====================

export interface PageQuery {
  page?: number
  pageSize?: number
  keyword?: string
}

export interface UserPageQuery extends PageQuery {
  status?: number | undefined
  roleId?: string
}

export interface RolePageQuery extends PageQuery {
  status?: number | undefined
}
