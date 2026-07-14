import type { Router } from 'vue-router'
import type { EnumMetadata } from '@/api/modules/metadata/enum-metadata'
import type { DepartmentTreeNodeDto } from '@/api/modules/organization'
import type {
  ApiCredentialItem,
  ApiCredentialSecret,
  AppBackendDependency,
  AppEnumBatchQuery,
  AppEnumDefinition,
  AppEnumNameQuery,
  AppPageSummary,
  ChangeEmailParams,
  ChangePasswordParams,
  ChangePhoneParams,
  ChangeUserNameParams,
  ChatApiContract,
  ChatDepartmentPickerNode,
  EmailLoginParams,
  ExternalLoginItem,
  LoginConfig,
  LoginLogPage,
  LoginParams,
  LoginResponse,
  LoginToken,
  MyOAuthAppCreateInput,
  MyOAuthAppItem,
  MyOAuthAppSecret,
  MyOAuthAppUpdateInput,
  NotificationPreference,
  PasswordResetConfirmResult,
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
import { chatApi } from '@/api/modules/messaging'
import { enumMetadataApi } from '@/api/modules/metadata/enum-metadata'
import { departmentApi } from '@/api/modules/organization'
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
  oAuthProviders: [],
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

// 将后端枚举元数据（EnumMetadata）映射为前端枚举定义（AppEnumDefinition）。
// 后端业务枚举均为 Int32 底层、非 Flags；显示文案由后端按 X-Language 文化解析。
// 关键：后端实例数据经 JsonStringEnumConverter 序列化为「成员名字符串」（如 "Field"/"Pending"），
// 故选项 value 必须用成员名 item.name，才能与表格行数据匹配；item.value(整数)放入 valueText 备用。
function mapEnumMetadata(meta: EnumMetadata): AppEnumDefinition {
  return {
    cultureName: 'zh-CN',
    displayName: meta.displayName || meta.enumTypeName,
    enumName: meta.enumTypeName,
    fullName: meta.enumTypeName,
    isFlags: false,
    underlyingTypeName: 'Int32',
    items: (meta.items ?? []).map((item, index) => ({
      name: item.name,
      value: item.name,
      valueText: String(item.value),
      label: item.displayName || item.name,
      description: item.description ?? '',
      order: index,
      disabled: false,
      source: 'enum',
    })),
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
    lockSessionApi(input: { password: string }) {
      return requestClient.post<unknown>('/Auth/LockSession', input)
    },
    unlockSessionApi(input: { password: string }) {
      return requestClient.post<unknown>('/Auth/UnlockSession', input)
    },
    phoneLoginApi(input: PhoneLoginParams) {
      return requestClient.post<LoginToken>('/Auth/PhoneLogin', input)
    },
    emailLoginApi(input: EmailLoginParams) {
      return requestClient.post<LoginToken>('/Auth/EmailLogin', input)
    },
    sendEmailLoginCodeApi(email: string) {
      return requestClient.post<VerificationCodeResult>('/Auth/EmailLoginCode', { email })
    },
    registerApi(input: unknown) {
      return requestClient.post('/Auth/Register', input)
    },
    createOAuthBindTicketApi() {
      // 动态 API 约定会剥离方法名的 Create 前缀，实际路由为 /Auth/OAuthBindTicket
      return requestClient.post<string>('/Auth/OAuthBindTicket')
    },
    requestPasswordResetApi(email: string) {
      return requestClient.post<PasswordResetResult>('/Auth/PasswordResetRequest', { email })
    },
    consumePasswordResetTokenApi(token: string, newPassword: string) {
      return requestClient.post<PasswordResetConfirmResult>('/Auth/ConsumePasswordResetToken', { token, newPassword })
    },
    sendPhoneLoginCodeApi(phone: string) {
      return requestClient.post<VerificationCodeResult>('/Auth/PhoneLoginCode', { phone })
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
    getApiCredentialsApi() {
      return requestClient.get<ApiCredentialItem[]>('/Profile/ApiCredentials')
    },
    createApiCredentialApi(credentialName?: string) {
      return requestClient.post<ApiCredentialSecret>('/Profile/ApiCredential', { credentialName })
    },
    rotateApiCredentialSecretApi(id: number | string) {
      return requestClient.post<ApiCredentialSecret>('/Profile/RotateApiCredentialSecret', { basicId: id })
    },
    updateApiCredentialStatusApi(id: number | string, status: 'Disabled' | 'Enabled') {
      return requestClient.put<ApiCredentialItem>('/Profile/ApiCredentialStatus', { basicId: id, status })
    },
    deleteApiCredentialApi(id: number | string) {
      return requestClient.delete(`/Profile/ApiCredential/${id}`)
    },
    getMyOAuthAppsApi() {
      return requestClient.get<MyOAuthAppItem[]>('/MyOAuthApp/MyOAuthApps')
    },
    createMyOAuthAppApi(input: MyOAuthAppCreateInput) {
      return requestClient.post<MyOAuthAppSecret>('/MyOAuthApp/MyOAuthApp', input)
    },
    updateMyOAuthAppApi(input: MyOAuthAppUpdateInput) {
      return requestClient.put<MyOAuthAppItem>('/MyOAuthApp/MyOAuthApp', input)
    },
    regenerateMyOAuthAppSecretApi(id: number | string) {
      return requestClient.post<MyOAuthAppSecret>(`/MyOAuthApp/RegenerateMyOAuthAppSecret/${id}`)
    },
    updateMyOAuthAppStatusApi(id: number | string, status: 'Disabled' | 'Enabled') {
      return requestClient.put<MyOAuthAppItem>('/MyOAuthApp/MyOAuthAppStatus', { basicId: id, status })
    },
    deleteMyOAuthAppApi(id: number | string) {
      return requestClient.delete(`/MyOAuthApp/MyOAuthApp/${id}`)
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
      return requestClient.get<ExternalLoginItem[]>('/Profile/LinkedAccounts')
    },
    getLoginLogsApi(page: number, pageSize: number) {
      return requestClient.get<LoginLogPage>('/Profile/LoginLogs', { params: { page, pageSize } })
        .then(result => ({ ...result, page, pageSize }))
    },
    getProfileApi() {
      return requestClient.get<UserProfile>('/Profile/Profile')
    },
    getSessionsApi() {
      return requestClient.get<UserSessionItem[]>('/Profile/Sessions')
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
      // 复用既有可用端点 /EnumMetadata/AllEnums（DynamicApi 约定去掉 Get 前缀），按名筛选并映射为
      // AppEnumDefinition；淘汰原先 404 的 /Enum/Batch、/Enum/ByName 重复实现。结果由 useEnumService 缓存。
      async getBatch(query: AppEnumBatchQuery) {
        const names = query.enumNames ?? []
        const fallback = Object.fromEntries(names.map(name => [name, emptyEnum(name)]))
        try {
          const all = await enumMetadataApi.getAll()
          const wanted = new Set(names)
          const result: Record<string, AppEnumDefinition> = {}
          for (const meta of all) {
            if (wanted.size === 0 || wanted.has(meta.enumTypeName)) {
              result[meta.enumTypeName] = mapEnumMetadata(meta)
            }
          }
          // 保证请求的每个名称都有值（缺失回退空定义），维持「键齐全」契约
          for (const name of names) {
            result[name] ??= emptyEnum(name)
          }
          return result
        }
        catch {
          return fallback
        }
      },
      async getByName(query: AppEnumNameQuery) {
        const name = query.enumName
        try {
          const all = await enumMetadataApi.getAll()
          const meta = all.find(item => item.enumTypeName === name)
          return meta ? mapEnumMetadata(meta) : emptyEnum(name)
        }
        catch {
          return emptyEnum(name)
        }
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
      save(input: { scene: number, settingKey: string, settingValue?: null | string, clientId?: string }) {
        return requestClient.post<{ scene: number, settingKey: string, settingValue?: null | string }>('/UserSetting/Save', input)
      },
    },
    fieldSecurityApi: {
      getMine(resourceCode: string) {
        return getWithFallback<Array<{ fieldName: string, isReadable: boolean, isEditable: boolean, maskStrategy: number, maskPattern?: null | string }>>(
          '/MyFieldSecurity/Mine',
          [],
          { params: { resourceCode } },
        )
      },
    },
    importHistoryApi: {
      create(input: { pageCode: string, resourceCode?: null | string, fileName: string, totalCount: number, successCount: number, failCount: number, errorSummary?: null | string }) {
        return requestClient.post<unknown>('/ImportHistory/Create', input)
      },
      recent(pageCode: string, count = 10) {
        return getWithFallback<Array<{ basicId: number | string, pageCode: string, resourceCode?: null | string, fileName: string, totalCount: number, successCount: number, failCount: number, errorSummary?: null | string, createdTime: string }>>(
          '/ImportHistoryQuery/Mine',
          [],
          { params: { pageCode, count } },
        )
      },
    },
    exportTaskApi: {
      submit(input: { businessType: string, taskName?: string, scope: number, format: number, querySnapshot?: null | string, columns: Array<{ key: string, title: string, valueMap?: Record<string, string> }> }) {
        return requestClient.post<unknown>('/ExportTask/Submit', input)
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
      banner() {
        return workbenchApi.inbox.banner()
      },
      confirm(id: string, _userId?: string, _tenantId?: null | string) {
        return workbenchApi.inbox.confirm(id)
      },
      list(_userId?: string, unreadOnly = false, _tenantId?: null | string) {
        return workbenchApi.inbox.list(unreadOnly)
      },
      mandatoryUnread() {
        return workbenchApi.inbox.mandatoryUnread()
      },
      markAllRead(_userId?: string, _tenantId?: null | string) {
        return workbenchApi.inbox.markAllRead()
      },
      markPopupShown(id: string) {
        return workbenchApi.inbox.markPopupShown(id)
      },
      markRead(id: string, _userId?: string, _tenantId?: null | string) {
        return workbenchApi.inbox.markRead(id)
      },
      popup() {
        return workbenchApi.inbox.popup()
      },
    },
    userSessionApi: {
      page(input: { page?: number, pageSize?: number }) {
        return getWithFallback<AppPageSummary>('/UserSessionQuery/UserSessionPage', emptyPage(input))
      },
    },
  }
}

function mapDepartmentNode(node: DepartmentTreeNodeDto): ChatDepartmentPickerNode {
  return {
    departmentId: node.basicId,
    departmentName: node.departmentName,
    children: node.children?.map(mapDepartmentNode) ?? null,
  }
}

function createChatApis() {
  const composed: ChatApiContract = {
    ...chatApi,
    async selectUsers(keyword: string, limit = 20) {
      // 走聊天专属选人端点（仅需 saas:chat:read）；用户管理的 UserSelect 端点要求 saas:user:read，普通聊天用户会 403
      const items = await chatApi.userOptions(keyword, limit)
      return items.map(u => ({
        userId: u.basicId,
        userName: u.userName,
        nickName: u.nickName ?? u.realName,
        avatar: u.avatar,
      }))
    },
    async departmentTree() {
      const nodes = await departmentApi.tree({ limit: 500, onlyEnabled: true })
      return nodes.map(mapDepartmentNode)
    },
    async uploadAttachment(file: File, onProgress?: (percent: number) => void) {
      // 秒传：SHA-256（与后端 FileTransferService 同算法，hex 小写）命中已有文件直接复用 fileId；
      // 未命中/不支持/超大文件（整读内存有风险）回退普通上传
      const FAST_UPLOAD_MAX_BYTES = 200 * 1024 * 1024
      if (file.size > 0 && file.size <= FAST_UPLOAD_MAX_BYTES && globalThis.crypto?.subtle) {
        try {
          const digest = await crypto.subtle.digest('SHA-256', await file.arrayBuffer())
          const fileHash = [...new Uint8Array(digest)].map(b => b.toString(16).padStart(2, '0')).join('')
          // 探测语义：命中返回详情，未命中返回 null（不再抛 4xx）
          const fast = await fileApi.fastUpload({
            fileHash,
            originalName: file.name,
            fileSize: file.size,
            mimeType: file.type || null,
          })
          if (fast) {
            onProgress?.(100)
            return {
              fileId: fast.basicId,
              fileName: fast.originalName || fast.fileName,
              fileSize: fast.fileSize,
            }
          }
        }
        catch {
          // 探测失败不阻断：回退普通上传
        }
      }
      const detail = await fileApi.upload({ file, directory: 'chat' }, onProgress)
      return {
        fileId: detail.basicId,
        fileName: detail.originalName || detail.fileName,
        fileSize: detail.fileSize,
      }
    },
    getFileUrl(fileId: string) {
      return fileApi.generatePresignedUrl(fileId)
    },
  }
  return { chatApi: composed }
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
    ...createChatApis(),
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
