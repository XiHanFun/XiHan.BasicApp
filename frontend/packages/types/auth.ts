import type { MenuRoute } from './menu'

// ==================== 认证 & 用户类型 ====================

export interface UserInfo {
  basicId: number
  userName: string
  nickName?: string
  appTitle?: string
  appLogo?: string
  avatar?: string
  email?: string
  phone?: string
  tenantId?: null | number
  roles: string[]
  permissions: string[]
}

export interface LoginConfig {
  loginMethods: string[]
  tenantEnabled: boolean
  oauthProviders: string[]
}

export interface LoginParams {
  username: string
  password: string
  tenantId?: null | number
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  issuedAt: string
  expiresAt: string
}

export interface PermissionInfo {
  roles: string[]
  permissions: string[]
  menus: MenuRoute[]
}
