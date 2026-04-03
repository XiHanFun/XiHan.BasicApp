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
  isSystemAccount: boolean
  twoFactorEnabled: boolean
  /** 0=未启用 1=TOTP 2=邮箱 3=手机 */
  twoFactorMethod: number
  emailVerified: boolean
  phoneVerified: boolean
  lastPasswordChangeTime?: string
  lastUserNameChangeTime?: string
  canChangeUserName: boolean
}

export interface UpdateProfileParams {
  nickName?: string
  realName?: string
  avatar?: string
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

export interface ChangeEmailParams {
  newEmail: string
  password: string
}

export interface ChangePhoneParams {
  newPhone: string
  password: string
}

export interface LoginLogItem {
  loginTime: string
  loginIp?: string
  loginLocation?: string
  browser?: string
  os?: string
  loginResult: number
  message?: string
}

export interface LoginLogPage {
  items: LoginLogItem[]
  total: number
}

export interface ChangeUserNameParams {
  userName: string
  password: string
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

export interface ExternalLoginItem {
  provider: string
  providerDisplayName?: string
  email?: string
  avatarUrl?: string
  lastLoginTime?: string
}
