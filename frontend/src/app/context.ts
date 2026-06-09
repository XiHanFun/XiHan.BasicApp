import type { Router } from 'vue-router'
import type {
  AppBackendDependency,
  AppEnumBatchQuery,
  AppEnumDefinition,
  AppEnumNameQuery,
  AppPageSummary,
  ChangeEmailParams,
  ChangePasswordParams,
  ChangePhoneParams,
  ChangeUserNameParams,
  EmailLoginParams,
  ExternalLoginItem,
  LoginConfig,
  LoginLogPage,
  LoginParams,
  LoginResponse,
  LoginToken,
  NotificationPreference,
  PasswordResetResult,
  PermissionInfo,
  PhoneLoginParams,
  TwoFactorSetupResult,
  UpdateProfileParams,
  UserActivity,
  UserInfo,
  UserProfile,
  UserSessionItem,
  VerificationCodeResult,
} from '~/types'
import { fileApi } from '@/api/modules/files'
import { logManagementApi } from '@/api/modules/log'
import {
  appManagementApi,
  approvalManagementApi,
  cacheManagementApi,
  configManagementApi,
  dictManagementApi,
  fileManagementApi,
  jobManagementApi,
  menuManagementApi,
  serverManagementApi,
  tenantManagementApi,
} from '@/api/modules/platform'
import {
  messageCenterApi,
  orgManagementApi,
  permissionCenterApi,
  roleManagementApi,
  userManagementApi,
} from '@/api/modules/system'
import { workbenchApi } from '@/api/modules/workbench'
import { requestClient } from '@/api/request'
import { router } from '@/router'
import { staticRoutes } from '@/router/routes'
import { registerAppContext } from '~/stores/app-context'

const viewModules = import.meta.glob('/src/views/**/*.vue')

const defaultLoginConfig: LoginConfig = {
  loginMethods: ['password'],
  oauthProviders: [],
  tenantEnabled: true,
}

function emptyPage(input?: { page?: number, pageSize?: number }): AppPageSummary {
  return {
    items: [],
    page: input?.page ?? 1,
    pageSize: input?.pageSize ?? 20,
    total: 0,
  }
}

function emptyEnum(name: string): AppEnumDefinition {
  return {
    cultureName: 'zh-CN',
    displayName: name,
    enumName: name,
    fullName: name,
    isFlags: false,
    items: [],
    underlyingTypeName: 'Int32',
  }
}

async function getWithFallback<T>(url: string, fallback: T, config?: Parameters<typeof requestClient.get>[1]): Promise<T> {
  try {
    return await requestClient.get<T>(url, config)
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
    emailLoginApi(input: EmailLoginParams) {
      return requestClient.post<LoginToken>('/Auth/EmailLogin', input)
    },
    sendEmailLoginCodeApi(email: string, tenantId?: null | string) {
      return requestClient.post<VerificationCodeResult>('/Auth/EmailLoginCode', { email, tenantId })
    },
    registerApi(input: unknown) {
      return requestClient.post('/Auth/Register', input)
    },
    requestPasswordResetApi(email: string, scopeId?: string) {
      return requestClient.post<PasswordResetResult>('/Auth/PasswordResetRequest', { email, scopeId })
    },
    sendPhoneLoginCodeApi(phone: string, scopeId?: string) {
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
    disable2FAApi(method: number | string, code: string) {
      return requestClient.post('/Profile/Disable2FA', { code, method })
    },
    enable2FAApi(method: number | string, code: string) {
      return requestClient.post('/Profile/Enable2FA', { code, method })
    },
    getActivityApi() {
      return requestClient.get<UserActivity>('/Profile/Activity')
    },
    getNotificationPreferenceApi() {
      return requestClient.get<NotificationPreference>('/Profile/NotificationPreference')
    },
    updateNotificationPreferenceApi(input: NotificationPreference) {
      return requestClient.put<NotificationPreference>('/Profile/NotificationPreference', input)
    },
    getFilePresignedUrlApi(fileId: string) {
      return fileApi.generatePresignedUrl(fileId)
    },
    getLinkedAccountsApi() {
      return getWithFallback<ExternalLoginItem[]>('/Profile/LinkedAccounts', [])
    },
    getLoginLogsApi(page: number, pageSize: number) {
      return getWithFallback<LoginLogPage>('/Profile/LoginLogs', { items: [], total: 0 }, { params: { page, pageSize } })
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
    send2FASetupCodeApi(method: number | string) {
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
        return getWithFallback<AppPageSummary>('/AccessLogQuery/AccessLogPage', emptyPage(input))
      },
    },
    enumApi: {
      getBatch(query: AppEnumBatchQuery) {
        const fallback = Object.fromEntries(
          (query.enumNames ?? []).map(name => [name, emptyEnum(name)]),
        )
        return getWithFallback<Record<string, AppEnumDefinition>>('/Enum/Batch', fallback, { params: query })
      },
      getByName(query: AppEnumNameQuery) {
        const name = query.enumName
        return getWithFallback<AppEnumDefinition>('/Enum/ByName', emptyEnum(name), { params: query })
      },
    },
    userSettingApi: {
      get(input: { scene: number, settingKey: string }) {
        return getWithFallback<{ scene: number, settingKey: string, settingValue?: null | string }>(
          '/UserSettingQuery/Get',
          { scene: input.scene, settingKey: input.settingKey, settingValue: null },
          { params: input },
        )
      },
      save(input: { scene: number, settingKey: string, settingValue?: null | string }) {
        return requestClient.post<{ scene: number, settingKey: string, settingValue?: null | string }>('/UserSetting/Save', input)
      },
    },
    fieldSecurityApi: {
      getMine(resourceCode: string) {
        return getWithFallback<Array<{ fieldName: string, isReadable: boolean, isEditable: boolean, maskStrategy: number, maskPattern?: null | string }>>(
          '/MyFieldSecurity/GetMine',
          [],
          { params: { resourceCode } },
        )
      },
    },
    operationLogApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<AppPageSummary>('/OperationLogQuery/OperationLogPage', emptyPage(input))
      },
    },
    serverApi: {
      getNuGetPackages() {
        return getWithFallback<AppBackendDependency[]>('/Server/NuGetPackages', [])
      },
    },
    userApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<AppPageSummary>('/UserQuery/UserPage', emptyPage(input))
      },
    },
    userInboxApi: {
      confirm(id: string, _userId?: string, _tenantId?: null | string) {
        return workbenchApi.inbox.confirm(id)
      },
      list(_userId?: string, unreadOnly = false, _tenantId?: null | string) {
        return workbenchApi.inbox.list(unreadOnly)
      },
      markAllRead(_userId?: string, _tenantId?: null | string) {
        return workbenchApi.inbox.markAllRead()
      },
      markRead(id: string, _userId?: string, _tenantId?: null | string) {
        return workbenchApi.inbox.markRead(id)
      },
    },
    userSessionApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<AppPageSummary>('/UserSessionQuery/UserSessionPage', emptyPage(input))
      },
    },
  }
}

function createMenuPageApis() {
  return {
    logManagementApi,
    platformApi: {
      app: appManagementApi,
      approval: approvalManagementApi,
      cache: cacheManagementApi,
      config: configManagementApi,
      dict: dictManagementApi,
      file: fileManagementApi,
      job: jobManagementApi,
      menu: menuManagementApi,
      server: serverManagementApi,
      tenant: tenantManagementApi,
    },
    systemApi: {
      message: messageCenterApi,
      org: orgManagementApi,
      permission: permissionCenterApi,
      role: roleManagementApi,
      user: userManagementApi,
    },
    workbenchApi,
  }
}

export function createApplicationApis() {
  return {
    ...createAuthApis(),
    ...createProfileApis(),
    ...createShellApis(),
    ...createMenuPageApis(),
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
