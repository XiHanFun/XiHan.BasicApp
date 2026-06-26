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
  /** 位标志（可组合）：1=TOTP 2=邮箱 4=手机，0=未启用 */
  twoFactorMethod: number
  emailVerified: boolean
  phoneVerified: boolean
  lastPasswordChangeTime?: string
  lastUserNameChangeTime?: string
  canChangeUserName: boolean
  /** 是否已锁定 */
  isLocked: boolean
  /** 锁定结束时间 */
  lockoutEndTime?: string
  /** 连续失败登录次数 */
  failedLoginAttempts: number
  /** 最后失败登录时间 */
  lastFailedLoginTime?: string
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

/** 与后端 LoginResult 枚举（JsonStringEnumConverter 序列化值）一致，含认证审计事件 */
export type LoginAuditResult
  = | 'AccountDisabled'
    | 'AccountLocked'
    | 'Failed'
    | 'InvalidCredentials'
    | 'Logout'
    | 'MfaBound'
    | 'MfaUnbound'
    | 'PasswordChanged'
    | 'PasswordReset'
    | 'RequiresTwoFactor'
    | 'Success'
    | 'TokenRefreshed'
    | 'TwoFactorFailed'

export interface LoginLogItem {
  loginTime: string
  loginIp?: string
  loginLocation?: string
  browser?: string
  os?: string
  loginResult: LoginAuditResult
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

/** 用户通知偏好（渠道 × 类型） */
export interface NotificationPreference {
  channelInApp: boolean
  channelEmail: boolean
  channelSms: boolean
  channelPush: boolean
  typeAnnouncement: boolean
  typeTask: boolean
  typeApproval: boolean
  typeSecurity: boolean
  typeMarketing: boolean
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

/** 个人 API 凭证（与后端 ProfileApiCredentialDto 对应；Status 经 JsonStringEnumConverter 序列化为字符串） */
export interface ApiCredentialItem {
  basicId: number | string
  credentialName: string
  appKey: string
  status: 'Disabled' | 'Enabled'
  lastUsedTime?: string | null
  expirationTime?: string | null
  createdTime: string
}

/** 个人 API 凭证密钥（明文 Secret 仅创建/滚动时返回一次） */
export interface ApiCredentialSecret {
  basicId: number | string
  appKey: string
  appSecret: string
}
