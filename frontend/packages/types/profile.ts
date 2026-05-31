// ==================== 个人中心类型 ====================

export interface UserProfile {
  userId: string
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
  tenantId?: null | string
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
  userId: string
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

export interface UserActivityPeriod {
  loginCount: number
  accessCount: number
  operationCount: number
  /** 在线时长（秒） */
  onlineTime: number
}

export interface UserActivityTrendPoint {
  date: string
  accessCount: number
  operationCount: number
  /** 在线时长（分钟） */
  onlineMinutes: number
}

export interface UserActivity {
  today: UserActivityPeriod
  thisWeek: UserActivityPeriod
  thisMonth: UserActivityPeriod
  lastLoginTime?: string
  lastAccessTime?: string
  lastOperationTime?: string
  trend: UserActivityTrendPoint[]
}
