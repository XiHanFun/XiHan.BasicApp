import type {
  LoginConfig,
  LoginParams,
  LoginResponse,
  LoginToken,
  PasswordResetResult,
  PermissionInfo,
  PhoneLoginParams,
  RegisterParams,
  UserInfo,
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
    availableTwoFactorMethods: payload?.availableTwoFactorMethods ?? undefined,
    twoFactorMethod: payload?.twoFactorMethod ?? undefined,
    codeSent: payload?.codeSent ?? false,
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

export const authApi = {
  getLoginConfig: () => requestClient.get<any>(`${AUTH}/LoginConfig`).then(normalizeLoginConfig),

  login: async (data: LoginParams): Promise<LoginResponse> => {
    const raw = await requestClient.post<any>(`${AUTH}/Login`, {
      userName: data.username,
      password: data.password,
      tenantId: data.tenantId,
      twoFactorCode: data.twoFactorCode || undefined,
      twoFactorMethod: data.twoFactorMethod || undefined,
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
