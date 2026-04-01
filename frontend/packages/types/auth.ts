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

// ==================== 个人中心类型 ====================

export interface UserProfile {
  userId: number
  userName: string
  realName?: string
  nickName?: string
  avatar?: string
  email?: string
  phone?: string
  gender: number
  birthday?: string
  timeZone?: string
  language?: string
  country?: string
  remark?: string
  tenantId?: null | number
  lastLoginTime?: string
  lastLoginIp?: string
  twoFactorEnabled: boolean
  emailVerified: boolean
  phoneVerified: boolean
  lastPasswordChangeTime?: string
}

export interface UpdateProfileParams {
  nickName?: string
  realName?: string
  avatar?: string
  email?: string
  phone?: string
  gender?: number
  birthday?: string
  timeZone?: string
  language?: string
  country?: string
  remark?: string
}

export interface ChangePasswordParams {
  userId: number
  oldPassword: string
  newPassword: string
}

export interface UserSessionItem {
  sessionId: string
  deviceName?: string
  deviceType: number
  browser?: string
  operatingSystem?: string
  ipAddress?: string
  location?: string
  loginTime: string
  lastActivityTime: string
  isCurrent: boolean
}

export interface TwoFactorSetupResult {
  sharedKey: string
  authenticatorUri: string
}
