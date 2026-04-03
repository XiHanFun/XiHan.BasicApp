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

export interface OAuthProviderItem {
  name: string
  displayName: string
}

export interface LoginConfig {
  loginMethods: string[]
  tenantEnabled: boolean
  oauthProviders: OAuthProviderItem[]
}

export interface LoginParams {
  username: string
  password: string
  tenantId?: null | number
  /** 双因素验证码（开启 2FA 时必填） */
  twoFactorCode?: string
}

export interface RegisterParams {
  username: string
  password: string
  nickName?: string
  email?: string
  phone?: string
  tenantId?: null | number
}

export interface PhoneLoginParams {
  phone: string
  code: string
  tenantId?: null | number
}

export interface VerificationCodeResult {
  expiresInSeconds: number
  debugCode?: string
}

export interface PasswordResetResult {
  accepted: boolean
  temporaryPassword?: string
}

/** 登录响应（区分正常登录与双因素验证挑战） */
export interface LoginResponse {
  requiresTwoFactor: boolean
  token: LoginToken | null
}

/** 鉴权令牌 */
export interface LoginToken {
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

