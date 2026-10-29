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

/**
 * 保存本人资料（后端 `profile.update`，整体覆盖；nickName 即显示名、必填，gender 是壳层的数字 0 未设置 / 1 男 / 2 女，空白当作没填）。
 * 4.x 的 avatar、timeZone、language 不在 6.0 的资料里：头像走上传端点与 `profile.remove-avatar`（uploadAvatarApi / removeAvatarApi），时区与语言是偏好
 */
export interface UpdateProfileParams {
  nickName: string
  realName?: string
  gender: number
  birthday?: string
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

/**
 * 个人中心「登录日志」的结果（4.x 后端 LoginResult 枚举的序列化值，含认证审计事件）：
 * 6.0 记的是认证事件 + 成败 + 错误码，由壳层（src/app/context.ts）按事件换成这里的结果；RolesActivated 对应 6.0 新增的会话角色激活
 */
export type LoginAuditResult
  = | 'AccountDisabled'
    | 'AccountLocked'
    | 'Failed'
    | 'ImpersonationEnded'
    | 'ImpersonationStarted'
    | 'InvalidCredentials'
    | 'Logout'
    | 'MfaBound'
    | 'MfaUnbound'
    | 'PasswordChanged'
    | 'PasswordReset'
    | 'RequiresTwoFactor'
    | 'RolesActivated'
    | 'SessionRevoked'
    | 'Success'
    | 'TenantSwitched'
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

/**
 * 用户通知偏好（渠道 × 类型；与后端 `profile.notification-preference` 同名同义，整体覆盖保存，没保存过即全收）。
 * 强制阅读与紧急通知不受偏好影响。4.x 的推送、机器人两个渠道与审批、营销两种类型 6.0 没有效果（没有推送渠道、机器人是群广播不分人、
 * 审批随后续的流程模块、没有营销类通知），不显示也不存
 */
export interface NotificationPreference {
  channelInApp: boolean
  channelEmail: boolean
  channelSms: boolean
  typeAnnouncement: boolean
  typeTask: boolean
  typeSecurity: boolean
}

/**
 * 绑定的第三方账号（个人中心「账号绑定」；后端 `ExternalLoginResponse` 的投影）：
 * provider 是规范的提供方类型（github / gitee / google / qq / wechat / wecom / lark / dingtalk），登录配置里的名称可能是自定义的；
 * providerDisplayName 是第三方账号自己的显示名（4.x 同义），最近一次用它登录的时间沿用 *Time 命名
 */
export interface ExternalLoginItem {
  provider: string
  providerDisplayName?: string
  email?: string
  avatarUrl?: string
  lastLoginTime?: string
}

/** 一段时间里的次数（个人中心「统计」；后端 `my-activity.summary` 的 logins / accesses / operations，沿用 4.x 的 *Count 命名）；6.0 不记在线时长，没有 4.x 的 onlineTime */
export interface UserActivityPeriod {
  loginCount: number
  accessCount: number
  operationCount: number
}

/** 有活动的一天（日期是 yyyy-MM-dd，按请求的时区分天）；6.0 没有 4.x 的 onlineMinutes */
export interface UserActivityTrendPoint {
  date: string
  accessCount: number
  operationCount: number
}

/** 本人的活跃度：今天、本周（周一起）、本月的次数，最近一次的时间（近一年内，没有即不给），近 371 天里有活动的日子（稀疏、日期正序） */
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

/** 开放接口的调用安全设置（签名算法与 IP 白名单，作用于本人全部凭证；algorithms 为可选的签名算法） */
export interface ApiCallSettings {
  signAlgorithm: string
  ipWhitelist: string[]
  algorithms: string[]
}

/** 我的 OAuth 应用（个人中心开发者设置；与后端 MyOAuthAppItemDto 对应） */
export interface MyOAuthAppItem {
  basicId: number | string
  clientId: string
  appName: string
  appDescription?: string | null
  homepage?: string | null
  logo?: string | null
  redirectUris?: string | null
  clientType: 'Confidential' | 'Public'
  grantTypes: string
  scopes?: string | null
  status: 'Disabled' | 'Enabled'
  createdTime: string
}

/** 我的 OAuth 应用密钥（明文仅创建/重置机密客户端时返回一次；公开客户端为空） */
export interface MyOAuthAppSecret {
  basicId: number | string
  clientId: string
  clientType: 'Confidential' | 'Public'
  clientSecret: string
}

/** 创建我的 OAuth 应用入参（精简字段 + 客户端类型） */
export interface MyOAuthAppCreateInput {
  appName: string
  clientType: 'Confidential' | 'Public'
  homepage?: string | null
  appDescription?: string | null
  redirectUris: string
  logo?: string | null
}

/** 已授权的第三方应用（个人中心「OAuth 应用」页签；与后端 OAuthConsentResponse 对应，授权时间沿用壳层的 *Time 命名） */
export interface OAuthConsentItem {
  clientId: string
  appName?: string | null
  logo?: string | null
  homepage?: string | null
  scopes: string[]
  grantedTime: string
}

/** 更新我的 OAuth 应用入参 */
export interface MyOAuthAppUpdateInput {
  basicId: number | string
  appName: string
  homepage?: string | null
  appDescription?: string | null
  redirectUris: string
  logo?: string | null
}
