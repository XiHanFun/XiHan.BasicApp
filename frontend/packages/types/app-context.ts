import type {
  EmailLoginParams,
  LoginConfig,
  LoginParams,
  LoginResponse,
  LoginToken,
  PasswordResetResult,
  PermissionInfo,
  PhoneLoginParams,
  UserInfo,
  VerificationCodeResult,
} from './auth'
import type { NotificationStatus, NotificationType } from './enums'
import type {
  ApiCredentialItem,
  ApiCredentialSecret,
  ChangeEmailParams,
  ChangePasswordParams,
  ChangePhoneParams,
  ChangeUserNameParams,
  ExternalLoginItem,
  LoginLogPage,
  NotificationPreference,
  TwoFactorSetupResult,
  UpdateProfileParams,
  UserActivity,
  UserProfile,
  UserSessionItem,
} from './profile'

export interface AppPageSummary {
  items: unknown[]
  page: number
  pageSize: number
  total: number
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

export interface AppContextApis extends Record<string, unknown> {
  accessLogApi: {
    page: (input: { page?: number, pageSize?: number }) => Promise<AppPageSummary>
  }
  changePasswordApi: (input: ChangePasswordParams) => Promise<unknown>
  changeUserNameApi: (input: ChangeUserNameParams) => Promise<unknown>
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
  /** 用户设置（按 用户 × 场景 × 设置键 全场景跨端同步，载荷为 JSON 字符串） */
  userSettingApi: {
    get: (input: { scene: number, settingKey: string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
    save: (input: { scene: number, settingKey: string, settingValue?: null | string }) => Promise<{ scene: number, settingKey: string, settingValue?: null | string }>
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
  getActivityApi: () => Promise<UserActivity>
  /** 个人 API 凭证（开发者设置；Secret 明文仅创建/滚动时返回一次） */
  getApiCredentialsApi: () => Promise<ApiCredentialItem[]>
  createApiCredentialApi: (credentialName?: string) => Promise<ApiCredentialSecret>
  rotateApiCredentialSecretApi: (id: number | string) => Promise<ApiCredentialSecret>
  updateApiCredentialStatusApi: (id: number | string, status: 'Disabled' | 'Enabled') => Promise<ApiCredentialItem>
  deleteApiCredentialApi: (id: number | string) => Promise<unknown>
  getNotificationPreferenceApi: () => Promise<NotificationPreference>
  updateNotificationPreferenceApi: (input: NotificationPreference) => Promise<NotificationPreference>
  /** 由文件主键(fileId)换取对象存储预签名访问 URL（<img> 可直接用、无需 token，会过期） */
  getFilePresignedUrlApi: (fileId: string) => Promise<string>
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
  operationLogApi: {
    page: (input: { page?: number, pageSize?: number }) => Promise<AppPageSummary>
  }
  phoneLoginApi: (input: PhoneLoginParams) => Promise<LoginToken>
  sendEmailLoginCodeApi: (email: string) => Promise<VerificationCodeResult>
  registerApi: (input: unknown) => Promise<unknown>
  requestPasswordResetApi: (email: string) => Promise<PasswordResetResult>
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
  userApi: {
    page: (input: { page?: number, pageSize?: number }) => Promise<AppPageSummary>
  }
  userInboxApi: {
    confirm: (id: string, userId?: string, tenantId?: null | string) => Promise<unknown>
    list: (userId?: string, unreadOnly?: boolean, tenantId?: null | string) => Promise<AppUserInboxItem[]>
    markAllRead: (userId?: string, tenantId?: null | string) => Promise<unknown>
    markRead: (id: string, userId?: string, tenantId?: null | string) => Promise<unknown>
  }
  userSessionApi: {
    page: (input: { page?: number, pageSize?: number }) => Promise<AppPageSummary>
  }
  verifyEmailApi: (code: string) => Promise<unknown>
  verifyPhoneApi: (code: string) => Promise<unknown>
}
