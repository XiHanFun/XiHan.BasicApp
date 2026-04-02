import type {
  ChangePasswordParams,
  LoginConfig,
  LoginParams,
  LoginResponse,
  LoginToken,
  PasswordResetResult,
  PermissionInfo,
  PhoneLoginParams,
  RegisterParams,
  TwoFactorSetupResult,
  UpdateProfileParams,
  UserInfo,
  UserProfile,
  UserSessionItem,
  VerificationCodeResult,
} from '~/types'
import { unwrapPayload } from '../helpers'
import requestClient from '../request'

const AUTH = '/Auth'

function normalizeUserInfo(raw: any): UserInfo {
  const payload = unwrapPayload<any>(raw)
  return {
    basicId: payload?.basicId ?? 0,
    userName: payload?.userName ?? '',
    nickName: payload?.nickName ?? '',
    avatar: payload?.avatar ?? '',
    email: payload?.email ?? '',
    phone: payload?.phone ?? '',
    tenantId: payload?.tenantId ?? null,
    roles: payload?.roles ?? [],
    permissions: payload?.permissions ?? [],
  }
}

function normalizeToken(raw: any): LoginToken {
  const payload = unwrapPayload<any>(raw)
  return {
    accessToken: payload?.accessToken ?? '',
    refreshToken: payload?.refreshToken ?? '',
    tokenType: payload?.tokenType ?? 'Bearer',
    expiresIn: payload?.expiresIn ?? 0,
    issuedAt: payload?.issuedAt ?? new Date().toISOString(),
    expiresAt: payload?.expiresAt ?? '',
  }
}

function normalizeLoginResponse(raw: any): LoginResponse {
  const payload = unwrapPayload<any>(raw)
  const requiresTwoFactor = payload?.requiresTwoFactor === true
  const tokenRaw = payload?.token
  return {
    requiresTwoFactor,
    token: tokenRaw ? normalizeToken(tokenRaw) : null,
  }
}

function normalizeLoginConfig(raw: any): LoginConfig {
  const payload = unwrapPayload<any>(raw)
  const rawProviders = payload?.oauthProviders ?? []
  const oauthProviders = rawProviders.map((p: any) =>
    typeof p === 'string'
      ? { name: p, displayName: p }
      : { name: p?.name ?? '', displayName: p?.displayName ?? p?.name ?? '' },
  )
  return {
    loginMethods: payload?.loginMethods ?? ['password'],
    tenantEnabled: payload?.tenantEnabled ?? true,
    oauthProviders,
  }
}

function normalizePermission(raw: any): PermissionInfo {
  const payload = unwrapPayload<any>(raw)
  return {
    roles: payload?.roles ?? payload?.Roles ?? [],
    permissions: payload?.permissions ?? payload?.Permissions ?? [],
    menus: payload?.menus ?? payload?.Menus ?? [],
  }
}

function normalizeVerificationCode(raw: any): VerificationCodeResult {
  const payload = unwrapPayload<any>(raw)
  return {
    expiresInSeconds: payload?.expiresInSeconds ?? 300,
    debugCode: payload?.debugCode || undefined,
  }
}

function normalizePasswordReset(raw: any): PasswordResetResult {
  const payload = unwrapPayload<any>(raw)
  return {
    accepted: payload?.accepted ?? true,
    temporaryPassword: payload?.temporaryPassword || undefined,
  }
}

function normalizeProfile(raw: any): UserProfile {
  const p = unwrapPayload<any>(raw)
  return {
    userId: p?.userId ?? 0,
    userName: p?.userName ?? '',
    realName: p?.realName ?? '',
    nickName: p?.nickName ?? '',
    avatar: p?.avatar ?? '',
    email: p?.email ?? '',
    phone: p?.phone ?? '',
    gender: p?.gender ?? 0,
    birthday: p?.birthday || undefined,
    timeZone: p?.timeZone ?? '',
    language: p?.language ?? 'zh-CN',
    country: p?.country ?? '',
    remark: p?.remark ?? '',
    tenantId: p?.tenantId ?? null,
    lastLoginTime: p?.lastLoginTime || undefined,
    lastLoginIp: p?.lastLoginIp ?? '',
    twoFactorEnabled: p?.twoFactorEnabled ?? false,
    emailVerified: p?.emailVerified ?? false,
    phoneVerified: p?.phoneVerified ?? false,
    lastPasswordChangeTime: p?.lastPasswordChangeTime || undefined,
  }
}

function normalizeSessonItem(item: any): UserSessionItem {
  return {
    sessionId: item?.sessionId ?? '',
    deviceName: item?.deviceName ?? '',
    deviceType: item?.deviceType ?? 0,
    browser: item?.browser ?? '',
    operatingSystem: item?.operatingSystem ?? '',
    ipAddress: item?.ipAddress ?? '',
    location: item?.location ?? '',
    loginTime: item?.loginTime ?? '',
    lastActivityTime: item?.lastActivityTime ?? '',
    isCurrent: item?.isCurrent ?? false,
  }
}

export const authApi = {
  getLoginConfig: () => requestClient.get<any>(`${AUTH}/LoginConfig`).then(normalizeLoginConfig),

  login: async (data: LoginParams): Promise<LoginResponse> => {
    const raw = await requestClient.post<any>(`${AUTH}/Login`, {
      userName: data.username,
      password: data.password,
      tenantId: data.tenantId,
      twoFactorCode: data.twoFactorCode || undefined,
    })
    return normalizeLoginResponse(raw)
  },

  register: async (data: RegisterParams): Promise<void> => {
    await requestClient.post(`${AUTH}/Register`, {
      userName: data.username,
      password: data.password,
      nickName: data.nickName,
      email: data.email,
      phone: data.phone,
      tenantId: data.tenantId,
    })
  },

  sendPhoneLoginCode: async (phone: string, tenantId?: null | number): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${AUTH}/SendPhoneLoginCode`, { phone, tenantId })
    return normalizeVerificationCode(raw)
  },

  phoneLogin: async (data: PhoneLoginParams): Promise<LoginToken> => {
    const raw = await requestClient.post<any>(`${AUTH}/PhoneLogin`, {
      phone: data.phone,
      code: data.code,
      tenantId: data.tenantId,
    })
    return normalizeToken(raw)
  },

  requestPasswordReset: async (email: string, tenantId?: null | number): Promise<PasswordResetResult> => {
    const raw = await requestClient.post<any>(`${AUTH}/RequestPasswordReset`, { email, tenantId })
    return normalizePasswordReset(raw)
  },

  refreshToken: async (refreshToken: string): Promise<LoginToken> => {
    const raw = await requestClient.post<any>(`${AUTH}/RefreshToken`, { refreshToken })
    return normalizeToken(raw)
  },

  getUserInfo: async (): Promise<UserInfo> => {
    const raw = await requestClient.get<any>(`${AUTH}/CurrentUser`)
    return normalizeUserInfo(raw)
  },

  getPermissions: async (): Promise<PermissionInfo> => {
    const raw = await requestClient.get<any>(`${AUTH}/Permissions`)
    return normalizePermission(raw)
  },

  getAccessCodes: async () => {
    try {
      return await requestClient.get<string[]>(`${AUTH}/PermissionCodes`)
    }
    catch {
      const perm = await authApi.getPermissions()
      return perm.permissions
    }
  },

  logout: () => requestClient.post(`${AUTH}/Logout`),

  // ==================== 个人中心 ====================

  getProfile: async (): Promise<UserProfile> => {
    const raw = await requestClient.get<any>(`${AUTH}/Profile`)
    return normalizeProfile(raw)
  },

  updateProfile: (data: UpdateProfileParams) =>
    requestClient.post(`${AUTH}/UpdateProfile`, data),

  changePassword: (data: ChangePasswordParams) =>
    requestClient.post(`${AUTH}/ChangePassword`, data),

  getSessions: async (): Promise<UserSessionItem[]> => {
    const raw = await requestClient.get<any>(`${AUTH}/Sessions`)
    const payload = unwrapPayload<any[]>(raw)
    return (payload ?? []).map(normalizeSessonItem)
  },

  revokeSession: (sessionId: string) =>
    requestClient.post(`${AUTH}/RevokeSession`, { sessionId }),

  revokeOtherSessions: () =>
    requestClient.post(`${AUTH}/RevokeOtherSessions`),

  setup2FA: async (): Promise<TwoFactorSetupResult> => {
    const raw = await requestClient.post<any>(`${AUTH}/Setup2FA`)
    const payload = unwrapPayload<any>(raw)
    return {
      sharedKey: payload?.sharedKey ?? '',
      authenticatorUri: payload?.authenticatorUri ?? '',
    }
  },

  enable2FA: (code: string) =>
    requestClient.post(`${AUTH}/Enable2FA`, { code }),

  disable2FA: (code: string) =>
    requestClient.post(`${AUTH}/Disable2FA`, { code }),

  // 邮箱验证
  sendEmailVerifyCode: async (): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${AUTH}/SendEmailVerifyCode`)
    return normalizeVerificationCode(raw)
  },

  verifyEmail: (code: string) =>
    requestClient.post(`${AUTH}/VerifyEmail`, { code }),

  // 手机号验证
  sendPhoneVerifyCode: async (): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${AUTH}/SendPhoneVerifyCode`)
    return normalizeVerificationCode(raw)
  },

  verifyPhone: (code: string) =>
    requestClient.post(`${AUTH}/VerifyPhone`, { code }),

  // 账号管理
  deactivateAccount: (password: string) =>
    requestClient.post(`${AUTH}/DeactivateAccount`, { password }),

  deleteAccount: (password: string) =>
    requestClient.post(`${AUTH}/DeleteAccount`, { password }),
}

export const getLoginConfigApi = authApi.getLoginConfig
export const loginApi = authApi.login
export const registerApi = authApi.register
export const sendPhoneLoginCodeApi = authApi.sendPhoneLoginCode
export const phoneLoginApi = authApi.phoneLogin
export const requestPasswordResetApi = authApi.requestPasswordReset
export const refreshTokenApi = authApi.refreshToken
export const getUserInfoApi = authApi.getUserInfo
export const getPermissionsApi = authApi.getPermissions
export const getAccessCodesApi = authApi.getAccessCodes
export const logoutApi = authApi.logout
export const getProfileApi = authApi.getProfile
export const updateProfileApi = authApi.updateProfile
export const changePasswordApi = authApi.changePassword
export const getSessionsApi = authApi.getSessions
export const revokeSessionApi = authApi.revokeSession
export const revokeOtherSessionsApi = authApi.revokeOtherSessions
export const setup2FAApi = authApi.setup2FA
export const enable2FAApi = authApi.enable2FA
export const disable2FAApi = authApi.disable2FA
export const sendEmailVerifyCodeApi = authApi.sendEmailVerifyCode
export const verifyEmailApi = authApi.verifyEmail
export const sendPhoneVerifyCodeApi = authApi.sendPhoneVerifyCode
export const verifyPhoneApi = authApi.verifyPhone
export const deactivateAccountApi = authApi.deactivateAccount
export const deleteAccountApi = authApi.deleteAccount
