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
