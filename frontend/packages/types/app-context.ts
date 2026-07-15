import type {
  EmailLoginParams,
  LoginConfig,
  LoginParams,
  LoginResponse,
  LoginToken,
  PasswordResetConfirmResult,
  PasswordResetResult,
  PermissionInfo,
  PhoneLoginParams,
  SwitchTenantParams,
  UserInfo,
  VerificationCodeResult,
} from './auth'
import type { ChatApiContract } from './chat'
import type { NotificationContentFormat, NotificationPriority, NotificationStatus, NotificationType, TenantMemberType } from './enums'
import type {
  ApiCredentialItem,
  ApiCredentialSecret,
  ChangeEmailParams,
  ChangePasswordParams,
  ChangePhoneParams,
  ChangeUserNameParams,
  ExternalLoginItem,
  LoginLogPage,
  MyOAuthAppCreateInput,
  MyOAuthAppItem,
  MyOAuthAppSecret,
  MyOAuthAppUpdateInput,
  NotificationPreference,
  TwoFactorSetupResult,
  UpdateProfileParams,
  UserActivity,
  UserProfile,
  UserSessionItem,
} from './profile'

/**
 * 租户切换器条目：控制中心 / 个人中心「我的租户」只读这些字段。
 *
 * 刻意是 src 侧 `TenantSwitcherDto` 的**窄投影**而非全量下沉——后者会连坐把
 * TenantStatus / TenantConfigStatus / TenantMemberInviteStatus 三个纯业务枚举拖进契约层。
 * src 的 DTO 字段更多，结构上可直接赋值给它。
 */
export interface AppTenantSwitcherItem {
  membershipId: string
  tenantId: string
  tenantCode: string
  tenantName: string
  tenantShortName?: null | string
  logo?: null | string
  domain?: null | string
  memberType: TenantMemberType
  isCurrent: boolean
  joinedTime: string
  membershipExpirationTime?: null | string
}

/**
 * shell 需要跳转、但**由应用侧定义**的路由路径。
 *
 * 底层包不该认识 `/workbench/profile`、`/message/chat` 这类业务路径——它们由 src 的路由表
 * 或后端 PageRegistry 定义，换一个应用就不存在，硬编码后点击即 404。
 * 未配置的项，对应入口应隐藏（而不是留一个点了没反应的死按钮）。
 */
export interface AppShellRoutes {
  /** 个人中心 */
  profile?: string
  /** 控制中心（登录后未进入租户时的落点；视图在 packages，但路由由应用注册） */
  controlCenter?: string
  /** 收件箱 / 消息中心 */
  inbox?: string
  /** 聊天全屏页 */
  chat?: string
}

export interface AppBackendDependency {
  packageName: string
  packageVersion: string
}

export interface AppEnumOption {
  name: string
  value: boolean | number | object | string
  valueText: string
  label: string
  description: string
  theme?: string
  icon?: string
  order: number
  disabled: boolean
  source: 'dict' | 'enum' | string
  extra?: Record<string, unknown>
}

export interface AppEnumDefinition {
  enumName: string
  fullName: string
  displayName: string
  cultureName: string
  isFlags: boolean
  underlyingTypeName: string
  items: AppEnumOption[]
}

export interface AppEnumBatchQuery {
  enumNames: string[]
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCodes?: string[]
  tenantId?: string
}

export interface AppEnumNameQuery {
  enumName: string
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCode?: string
  tenantId?: string
}

export interface AppUserInboxItem {
  basicId: string
  title: string
  content?: null | string
  contentFormat?: NotificationContentFormat
  notificationType: NotificationType
  notificationStatus: NotificationStatus
  sendTime: string
  readTime?: null | string
  confirmTime?: null | string
  isGlobal?: boolean
  needConfirm?: boolean
  icon?: null | string
  link?: null | string
}

/** 展示分级（横幅 / 弹窗 / 强制阅读）所需的收件箱条目，较 AppUserInboxItem 多带优先级与正文格式等展示字段 */
export interface AppUserInboxDisplayItem extends AppUserInboxItem {
  notificationId: string
  priority: NotificationPriority
  contentFormat: NotificationContentFormat
  isMandatory: boolean
  isBanner: boolean
  isPopup: boolean
}

// 刻意**不带**索引签名（`extends Record<string, unknown>`）：带上之后任何未声明的 key 都能塞进
// ctx.apis 且类型检查通过（历史上 createMenuPageApis 的整块死注册就是这么活下来的），
// 同时 `apis.typo` 也不报错（推成 unknown）。去掉后 packages 侧上百个 ctx.apis.* 调用点获得拼写保护。
export interface AppContextApis {
  changePasswordApi: (input: ChangePasswordParams) => Promise<unknown>
  changeUserNameApi: (input: ChangeUserNameParams) => Promise<unknown>
  /** 在线聊天（会话/消息/成员 + 选人/部门树/附件上传，契约见 ~/types/chat） */
  chatApi: ChatApiContract
  confirmChangeEmailApi: (code: string) => Promise<unknown>
  confirmChangePhoneApi: (code: string) => Promise<unknown>
  deactivateAccountApi: (password: string) => Promise<unknown>
  deleteAccountApi: (password: string) => Promise<unknown>
  disable2FAApi: (method: number | string, code: string) => Promise<unknown>
  enable2FAApi: (method: number | string, code: string) => Promise<unknown>
  enumApi: {
    getBatch: (query: AppEnumBatchQuery) => Promise<Record<string, AppEnumDefinition>>
    getByName: (query: AppEnumNameQuery) => Promise<AppEnumDefinition>
  }
  /** 用户设置（按 用户 × 场景 × 设置键 全场景跨端同步，载荷为 JSON 字符串；clientId 供后端推送回显过滤） */
  userSettingApi: {
    get: (input: { scene: number, settingKey: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
    save: (input: { scene: number, settingKey: string, settingValue?: null | string, clientId?: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
  }
  /** 字段权限（按资源下发当前用户的有效 FLS 规则：可读/可编辑/脱敏策略） */
  fieldSecurityApi: {
    getMine: (resourceCode: string) => Promise<Array<{ fieldName: string, isReadable: boolean, isEditable: boolean, maskStrategy: number, maskPattern?: null | string }>>
  }
  /** 导入历史（Schema 页面导入留痕：执行完毕上报 + 当前用户最近导入记录） */
  importHistoryApi: {
    create: (input: { pageCode: string, resourceCode?: null | string, fileName: string, totalCount: number, successCount: number, failCount: number, errorSummary?: null | string }) => Promise<unknown>
    recent: (pageCode: string, count?: number) => Promise<Array<{ basicId: number | string, pageCode: string, resourceCode?: null | string, fileName: string, totalCount: number, successCount: number, failCount: number, errorSummary?: null | string, createdTime: string }>>
  }
  /** 导出中心（提交异步导出任务，后台 worker 执行；进度经灵动岛、产物在导出中心下载） */
  exportTaskApi: {
    submit: (input: { businessType: string, taskName?: string, scope: number, format: number, querySnapshot?: null | string, columns: Array<{ key: string, title: string, valueMap?: Record<string, string> }> }) => Promise<unknown>
  }
  getActivityApi: () => Promise<UserActivity>
  /** 个人 API 凭证（开发者设置；Secret 明文仅创建/滚动时返回一次） */
  getApiCredentialsApi: () => Promise<ApiCredentialItem[]>
  createApiCredentialApi: (credentialName?: string) => Promise<ApiCredentialSecret>
  rotateApiCredentialSecretApi: (id: number | string) => Promise<ApiCredentialSecret>
  updateApiCredentialStatusApi: (id: number | string, status: 'Disabled' | 'Enabled') => Promise<ApiCredentialItem>
  deleteApiCredentialApi: (id: number | string) => Promise<unknown>
  /** 我的 OAuth 应用（开发者设置；ClientSecret 明文仅创建/重置机密客户端时返回一次） */
  getMyOAuthAppsApi: () => Promise<MyOAuthAppItem[]>
  createMyOAuthAppApi: (input: MyOAuthAppCreateInput) => Promise<MyOAuthAppSecret>
  updateMyOAuthAppApi: (input: MyOAuthAppUpdateInput) => Promise<MyOAuthAppItem>
  regenerateMyOAuthAppSecretApi: (id: number | string) => Promise<MyOAuthAppSecret>
  updateMyOAuthAppStatusApi: (id: number | string, status: 'Disabled' | 'Enabled') => Promise<MyOAuthAppItem>
  deleteMyOAuthAppApi: (id: number | string) => Promise<unknown>
  getNotificationPreferenceApi: () => Promise<NotificationPreference>
  updateNotificationPreferenceApi: (input: NotificationPreference) => Promise<NotificationPreference>
  /** 由文件主键(fileId)换取对象存储预签名访问 URL（<img> 可直接用、无需 token，会过期） */
  getFilePresignedUrlApi: (fileId: string) => Promise<string>
  /** 上传头像，返回文件主键(fileId)。访问级别/存储目录属应用策略，由 src 决定，契约层不暴露业务枚举 */
  uploadAvatarApi: (file: File, onProgress?: (percent: number) => void) => Promise<{ fileId: string }>
  /** 租户切换（控制中心 / 个人中心「我的租户」）：tenantId 传 null → 退回平台运维态 */
  tenantApi: {
    myAvailableTenants: () => Promise<AppTenantSwitcherItem[]>
    switchTenant: (input: SwitchTenantParams) => Promise<LoginToken>
  }
  getLinkedAccountsApi: () => Promise<ExternalLoginItem[]>
  getLoginConfigApi: () => Promise<LoginConfig>
  getLoginLogsApi: (page: number, pageSize: number) => Promise<LoginLogPage & { page: number, pageSize: number }>
  getPermissionsApi: () => Promise<PermissionInfo>
  getProfileApi: () => Promise<UserProfile>
  getSessionsApi: () => Promise<UserSessionItem[]>
  getUserInfoApi: () => Promise<UserInfo>
  emailLoginApi: (input: EmailLoginParams) => Promise<LoginToken>
  loginApi: (input: LoginParams) => Promise<LoginResponse>
  logoutApi: () => Promise<unknown>
  /** 锁屏（服务端强制）：设置会话级一次性口令并置位锁屏标记 */
  lockSessionApi: (input: { password: string }) => Promise<unknown>
  /** 解锁：口令由服务端 PBKDF2 校验；连续失败 5 次服务端会直接吊销会话 */
  unlockSessionApi: (input: { password: string }) => Promise<unknown>
  phoneLoginApi: (input: PhoneLoginParams) => Promise<LoginToken>
  sendEmailLoginCodeApi: (email: string) => Promise<VerificationCodeResult>
  registerApi: (input: unknown) => Promise<unknown>
  createOAuthBindTicketApi: () => Promise<string>
  requestPasswordResetApi: (email: string) => Promise<PasswordResetResult>
  consumePasswordResetTokenApi: (token: string, newPassword: string) => Promise<PasswordResetConfirmResult>
  revokeOtherSessionsApi: () => Promise<unknown>
  revokeSessionApi: (sessionId: string) => Promise<unknown>
  send2FASetupCodeApi: (method: number | string) => Promise<VerificationCodeResult>
  sendChangeEmailCodeApi: (input: ChangeEmailParams) => Promise<VerificationCodeResult>
  sendChangePhoneCodeApi: (input: ChangePhoneParams) => Promise<VerificationCodeResult>
  sendEmailVerifyCodeApi: () => Promise<VerificationCodeResult>
  sendPhoneLoginCodeApi: (phone: string) => Promise<VerificationCodeResult>
  sendPhoneVerifyCodeApi: () => Promise<VerificationCodeResult>
  serverApi: {
    getNuGetPackages: () => Promise<AppBackendDependency[]>
  }
  setup2FAApi: () => Promise<TwoFactorSetupResult>
  unlinkAccountApi: (provider: string) => Promise<unknown>
  updateProfileApi: (input: UpdateProfileParams) => Promise<UserProfile>
  userInboxApi: {
    banner: () => Promise<AppUserInboxDisplayItem[]>
    confirm: (id: string, userId?: string, tenantId?: null | string) => Promise<unknown>
    list: (userId?: string, unreadOnly?: boolean, tenantId?: null | string) => Promise<AppUserInboxItem[]>
    mandatoryUnread: () => Promise<AppUserInboxDisplayItem[]>
    markAllRead: (userId?: string, tenantId?: null | string) => Promise<unknown>
    markPopupShown: (id: string) => Promise<unknown>
    markRead: (id: string, userId?: string, tenantId?: null | string) => Promise<unknown>
    popup: () => Promise<AppUserInboxDisplayItem[]>
  }
  verifyEmailApi: (code: string) => Promise<unknown>
  verifyPhoneApi: (code: string) => Promise<unknown>
}
