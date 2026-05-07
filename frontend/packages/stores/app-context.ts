import type { Router, RouteRecordRaw } from 'vue-router'
import type {
  ChangeEmailParams,
  ChangePasswordParams,
  ChangePhoneParams,
  ChangeUserNameParams,
  ExternalLoginItem,
  LoginConfig,
  LoginLogPage,
  LoginParams,
  LoginResponse,
  LoginToken,
  PasswordResetResult,
  PermissionInfo,
  PhoneLoginParams,
  TwoFactorSetupResult,
  UpdateProfileParams,
  UserInfo,
  UserProfile,
  UserSessionItem,
  VerificationCodeResult,
} from '~/types'
import { reactive } from 'vue'

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
  notificationType: number
  notificationStatus: number
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
  getLinkedAccountsApi: () => Promise<ExternalLoginItem[]>
  getLoginConfigApi: () => Promise<LoginConfig>
  getLoginLogsApi: (page: number, pageSize: number) => Promise<LoginLogPage & { page: number, pageSize: number }>
  getPermissionsApi: () => Promise<PermissionInfo>
  getProfileApi: () => Promise<UserProfile>
  getSessionsApi: () => Promise<UserSessionItem[]>
  getUserInfoApi: () => Promise<UserInfo>
  loginApi: (input: LoginParams) => Promise<LoginResponse>
  logoutApi: () => Promise<unknown>
  operationLogApi: {
    page: (input: { page?: number, pageSize?: number }) => Promise<AppPageSummary>
  }
  phoneLoginApi: (input: PhoneLoginParams) => Promise<LoginToken>
  registerApi: (input: unknown) => Promise<unknown>
  requestPasswordResetApi: (email: string, scopeId?: string) => Promise<PasswordResetResult>
  revokeOtherSessionsApi: () => Promise<unknown>
  revokeSessionApi: (sessionId: string) => Promise<unknown>
  send2FASetupCodeApi: (method: number | string) => Promise<VerificationCodeResult>
  sendChangeEmailCodeApi: (input: ChangeEmailParams) => Promise<VerificationCodeResult>
  sendChangePhoneCodeApi: (input: ChangePhoneParams) => Promise<VerificationCodeResult>
  sendEmailVerifyCodeApi: () => Promise<VerificationCodeResult>
  sendPhoneLoginCodeApi: (phone: string, scopeId?: string) => Promise<VerificationCodeResult>
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

/**
 * 应用上下文：packages（核心包）不直接依赖 src（应用层），
 * 而是由 src 在启动时注册 API 实现、路由实例、视图模块等。
 *
 * 这样 packages 只通过 ~/ 引用自身，src 通过 @/ 引用自身并注入到此处。
 */
export interface AppContext {
  /** src 注册的 API 函数集合，packages 按名称取用 */
  apis: AppContextApis
  /** 获取路由实例（惰性，避免循环依赖） */
  getRouter: () => Promise<Router>
  /** 获取静态路由定义（用于静态路由模式） */
  getStaticRoutes: () => RouteRecordRaw[]
  /** src/views 下 import.meta.glob 的结果 */
  viewModules: Record<string, () => Promise<unknown>>
  /** src 注册的显式组件映射（PascalCase 路径 → 懒加载函数） */
  explicitComponentMap: Record<string, () => Promise<unknown>>
}

const context = reactive<AppContext>({
  apis: {} as AppContextApis,
  getRouter: () => Promise.reject(new Error('[app-context] Router not registered')),
  getStaticRoutes: () => [],
  viewModules: {},
  explicitComponentMap: {},
})

/** src 启动时调用，注入 API 实现和路由等 */
export function registerAppContext(partial: Partial<AppContext>) {
  if (partial.apis) {
    Object.assign(context.apis, partial.apis)
  }
  if (partial.getRouter) {
    context.getRouter = partial.getRouter
  }
  if (partial.getStaticRoutes) {
    context.getStaticRoutes = partial.getStaticRoutes
  }
  if (partial.viewModules) {
    context.viewModules = partial.viewModules
  }
  if (partial.explicitComponentMap) {
    context.explicitComponentMap = partial.explicitComponentMap
  }
}

/** packages 内部使用，获取应用上下文 */
export function useAppContext(): AppContext {
  return context
}
