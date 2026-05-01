import type { Router } from 'vue-router'
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
import * as authorizationApi from '@/api/modules/authorization'
import * as tenantApi from '@/api/modules/tenant'
import { requestClient } from '@/api/request'
import { router } from '@/router'
import { staticRoutes } from '@/router/routes'
import { registerAppContext } from '~/stores/app-context'

interface PageSummary {
  items: unknown[]
  page: number
  pageSize: number
  total: number
}

interface EnumQuery {
  enumName?: string
  name?: string
}

interface EnumDefinition {
  items: unknown[]
  name: string
}

interface BackendDependency {
  packageName: string
  packageVersion: string
}

const viewModules = import.meta.glob('/src/views/**/*.vue')

const defaultLoginConfig: LoginConfig = {
  loginMethods: ['password'],
  oauthProviders: [],
  tenantEnabled: true,
}

function emptyPage(input?: { page?: number, pageSize?: number }): PageSummary {
  return {
    items: [],
    page: input?.page ?? 1,
    pageSize: input?.pageSize ?? 20,
    total: 0,
  }
}

async function getWithFallback<T>(url: string, fallback: T): Promise<T> {
  try {
    return await requestClient.get<T>(url)
  }
  catch {
    return fallback
  }
}

async function postWithFallback<T>(url: string, data: unknown, fallback: T): Promise<T> {
  try {
    return await requestClient.post<T>(url, data)
  }
  catch {
    return fallback
  }
}

function createAuthApis() {
  return {
    getLoginConfigApi() {
      return getWithFallback<LoginConfig>('/Auth/LoginConfig', defaultLoginConfig)
    },
    getPermissionsApi() {
      return requestClient.get<PermissionInfo>('/Auth/Permissions')
    },
    getUserInfoApi() {
      return requestClient.get<UserInfo>('/Auth/UserInfo')
    },
    loginApi(input: LoginParams) {
      return requestClient.post<LoginResponse>('/Auth/Login', input)
    },
    logoutApi() {
      return postWithFallback('/Auth/Logout', undefined, undefined)
    },
    phoneLoginApi(input: PhoneLoginParams) {
      return requestClient.post<LoginToken>('/Auth/PhoneLogin', input)
    },
    registerApi(input: unknown) {
      return requestClient.post('/Auth/Register', input)
    },
    requestPasswordResetApi(email: string, scopeId?: number) {
      return requestClient.post<PasswordResetResult>('/Auth/PasswordResetRequest', { email, scopeId })
    },
    sendPhoneLoginCodeApi(phone: string, scopeId?: number) {
      return requestClient.post<VerificationCodeResult>('/Auth/PhoneLoginCode', { phone, scopeId })
    },
  }
}

function createProfileApis() {
  return {
    changePasswordApi(input: ChangePasswordParams) {
      return requestClient.post('/Profile/ChangePassword', input)
    },
    changeUserNameApi(input: ChangeUserNameParams) {
      return requestClient.post('/Profile/ChangeUserName', input)
    },
    confirmChangeEmailApi(code: string) {
      return requestClient.post('/Profile/ConfirmChangeEmail', { code })
    },
    confirmChangePhoneApi(code: string) {
      return requestClient.post('/Profile/ConfirmChangePhone', { code })
    },
    deactivateAccountApi(password: string) {
      return requestClient.post('/Profile/DeactivateAccount', { password })
    },
    deleteAccountApi(password: string) {
      return requestClient.post('/Profile/DeleteAccount', { password })
    },
    disable2FAApi(method: string, code: string) {
      return requestClient.post('/Profile/Disable2FA', { code, method })
    },
    enable2FAApi(method: string, code: string) {
      return requestClient.post('/Profile/Enable2FA', { code, method })
    },
    getLinkedAccountsApi() {
      return getWithFallback<ExternalLoginItem[]>('/Profile/LinkedAccounts', [])
    },
    getLoginLogsApi(page: number, pageSize: number) {
      return getWithFallback<LoginLogPage>('/Profile/LoginLogs', { items: [], total: 0 })
        .then(result => ({ ...result, page, pageSize }))
    },
    getProfileApi() {
      return requestClient.get<UserProfile>('/Profile/Profile')
    },
    getSessionsApi() {
      return getWithFallback<UserSessionItem[]>('/Profile/Sessions', [])
    },
    revokeOtherSessionsApi() {
      return requestClient.post('/Profile/RevokeOtherSessions')
    },
    revokeSessionApi(sessionId: string) {
      return requestClient.post('/Profile/RevokeSession', { sessionId })
    },
    send2FASetupCodeApi(method: string) {
      return requestClient.post<VerificationCodeResult>('/Profile/Send2FASetupCode', { method })
    },
    sendChangeEmailCodeApi(input: ChangeEmailParams) {
      return requestClient.post<VerificationCodeResult>('/Profile/SendChangeEmailCode', input)
    },
    sendChangePhoneCodeApi(input: ChangePhoneParams) {
      return requestClient.post<VerificationCodeResult>('/Profile/SendChangePhoneCode', input)
    },
    sendEmailVerifyCodeApi() {
      return requestClient.post<VerificationCodeResult>('/Profile/SendEmailVerifyCode')
    },
    sendPhoneVerifyCodeApi() {
      return requestClient.post<VerificationCodeResult>('/Profile/SendPhoneVerifyCode')
    },
    setup2FAApi() {
      return requestClient.post<TwoFactorSetupResult>('/Profile/Setup2FA')
    },
    unlinkAccountApi(provider: string) {
      return requestClient.post('/Profile/UnlinkAccount', { provider })
    },
    updateProfileApi(input: UpdateProfileParams) {
      return requestClient.put<UserProfile>('/Profile/Profile', input)
    },
    verifyEmailApi(code: string) {
      return requestClient.post('/Profile/VerifyEmail', { code })
    },
    verifyPhoneApi(code: string) {
      return requestClient.post('/Profile/VerifyPhone', { code })
    },
  }
}

function createShellApis() {
  return {
    accessLogApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<PageSummary>('/AccessLogQuery/AccessLogPage', emptyPage(input))
      },
    },
    enumApi: {
      getBatch(query: EnumQuery[]) {
        return getWithFallback<EnumDefinition[]>('/Enum/Batch', query.map(item => ({
          items: [],
          name: item.enumName ?? item.name ?? '',
        })))
      },
      getByName(query: EnumQuery) {
        return getWithFallback<EnumDefinition>('/Enum/ByName', {
          items: [],
          name: query.enumName ?? query.name ?? '',
        })
      },
    },
    operationLogApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<PageSummary>('/OperationLogQuery/OperationLogPage', emptyPage(input))
      },
    },
    serverApi: {
      getNuGetPackages() {
        return getWithFallback<BackendDependency[]>('/Server/NuGetPackages', [])
      },
    },
    userApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<PageSummary>('/UserQuery/UserPage', emptyPage(input))
      },
    },
    userInboxApi: {
      confirm(_id: string, _userId: string) {
        return Promise.resolve()
      },
      list(_userId: string, _unreadOnly = false) {
        return Promise.resolve([])
      },
      markAllRead(_userId: string) {
        return Promise.resolve()
      },
      markRead(_id: string, _userId: string) {
        return Promise.resolve()
      },
    },
    userSessionApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<PageSummary>('/UserSessionQuery/UserSessionPage', emptyPage(input))
      },
    },
  }
}

export function createApplicationApis() {
  return {
    ...authorizationApi,
    ...createAuthApis(),
    ...createProfileApis(),
    ...createShellApis(),
    ...tenantApi,
  }
}

export function registerApplicationContext(appRouter: Router = router) {
  registerAppContext({
    apis: createApplicationApis(),
    explicitComponentMap: {},
    getRouter: () => Promise.resolve(appRouter),
    getStaticRoutes: () => staticRoutes,
    viewModules,
  })
}
