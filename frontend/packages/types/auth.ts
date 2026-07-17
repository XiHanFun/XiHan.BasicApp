import type { MenuRoute } from './menu'

// ==================== 认证 & 用户类型 ====================

export interface UserInfo {
  basicId: string
  userName: string
  nickName?: string
  appTitle?: string
  appSubtitle?: string
  appLogo?: string
  avatar?: string
  email?: string
  phone?: string
  tenantId?: null | string
  /** 是否处于平台运维态（无租户上下文） */
  isPlatform?: boolean
  /** 是否可进入平台运维态（超管 / 平台管理员） */
  canAccessPlatform?: boolean
  roles: string[]
  permissions: string[]
}

/** 切换租户 / 进入平台运维态参数（切换复用当前登录会话，无需设备标识） */
export interface SwitchTenantParams {
  /** 目标租户标识；为空表示切换到平台运维态 */
  tenantId?: null | string
}

export interface OAuthProviderItem {
  name: string
  displayName: string
}

export interface LoginConfig {
  loginMethods: string[]
  // 与后端序列化键一致：OAuthProviders 经 camelCase 策略输出为 oAuthProviders
  oAuthProviders: OAuthProviderItem[]
}

/** 登录参数（先登录后选租户：登录不携带租户，落点由后端按成员关系决定） */
export interface LoginParams {
  /** 登录账号（邮箱，全平台唯一；平台账号也可用用户名） */
  username: string
  password: string
  /** 双因素验证码（开启 2FA 时必填） */
  twoFactorCode?: string
  /** 用户选择的双因素方式（totp/email/phone） */
  twoFactorMethod?: string
  /** 设备唯一标识（设备指纹） */
  deviceId?: string
}

export interface RegisterParams {
  username: string
  password: string
  nickName?: string
  /** 邮箱（必填，全平台唯一的登录身份标识） */
  email: string
  phone?: string
}

export interface PhoneLoginParams {
  phone: string
  code: string
}

export interface EmailLoginParams {
  email: string
  code: string
  deviceId?: string
}

export interface VerificationCodeResult {
  expiresInSeconds: number
  debugCode?: string
}

export interface PasswordResetResult {
  accepted: boolean
  /** 一次性重置链接，仅开发环境回显，便于本地无邮件时联调 */
  debugResetUrl?: string
}

export interface PasswordResetConfirmResult {
  success: boolean
}

/** 登录响应（区分正常登录与双因素验证挑战） */
export interface LoginResponse {
  requiresTwoFactor: boolean
  /** 可用的双因素方式列表 */
  availableTwoFactorMethods?: string[]
  /** 当前选中的双因素方式 */
  twoFactorMethod?: string
  /** 验证码是否已发送（邮箱/手机方式） */
  codeSent?: boolean
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
