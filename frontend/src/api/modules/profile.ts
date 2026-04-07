import type {
  ChangeEmailParams,
  ChangePasswordParams,
  ChangePhoneParams,
  ChangeUserNameParams,
  ExternalLoginItem,
  LoginLogItem,
  LoginLogPage,
  TwoFactorSetupResult,
  UpdateProfileParams,
  UserProfile,
  UserSessionItem,
  VerificationCodeResult,
} from '~/types'
import { unwrapPayload } from '../helpers'
import requestClient from '../request'

const PROFILE = '/Profile'

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
    isSystemAccount: p?.isSystemAccount ?? false,
    twoFactorEnabled: p?.twoFactorEnabled ?? false,
    twoFactorMethod: p?.twoFactorMethod ?? 0,
    emailVerified: p?.emailVerified ?? false,
    phoneVerified: p?.phoneVerified ?? false,
    lastPasswordChangeTime: p?.lastPasswordChangeTime || undefined,
    lastUserNameChangeTime: p?.lastUserNameChangeTime || undefined,
    canChangeUserName: p?.canChangeUserName ?? false,
  }
}

function normalizeSessionItem(item: any): UserSessionItem {
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

function normalizeVerificationCode(raw: any): VerificationCodeResult {
  const payload = unwrapPayload<any>(raw)
  return {
    expiresInSeconds: payload?.expiresInSeconds ?? 300,
    debugCode: payload?.debugCode || undefined,
  }
}

function normalizeExternalLogin(item: any): ExternalLoginItem {
  return {
    provider: item?.provider ?? '',
    providerDisplayName: item?.providerDisplayName ?? '',
    email: item?.email ?? '',
    avatarUrl: item?.avatarUrl ?? '',
    lastLoginTime: item?.lastLoginTime || undefined,
  }
}

export const profileApi = {
  getProfile: async (): Promise<UserProfile> => {
    const raw = await requestClient.get<any>(`${PROFILE}/Profile`)
    return normalizeProfile(raw)
  },

  updateProfile: (data: UpdateProfileParams) =>
    requestClient.put(`${PROFILE}/Profile`, data),

  changePassword: (data: ChangePasswordParams) =>
    requestClient.post(`${PROFILE}/ChangePassword`, data),

  changeUserName: (data: ChangeUserNameParams) =>
    requestClient.post(`${PROFILE}/ChangeUserName`, data),

  getSessions: async (): Promise<UserSessionItem[]> => {
    const raw = await requestClient.get<any>(`${PROFILE}/Sessions`)
    const payload = unwrapPayload<any[]>(raw)
    return (payload ?? []).map(normalizeSessionItem)
  },

  revokeSession: (sessionId: string) =>
    requestClient.post(`${PROFILE}/RevokeSession`, { sessionId }),

  revokeOtherSessions: () =>
    requestClient.post(`${PROFILE}/RevokeOtherSessions`),

  // ==================== 2FA ====================

  setup2FA: async (): Promise<TwoFactorSetupResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/Setup2FA`)
    const payload = unwrapPayload<any>(raw)
    return {
      sharedKey: payload?.sharedKey ?? '',
      authenticatorUri: payload?.authenticatorUri ?? '',
    }
  },

  /** 发送 2FA 设置验证码（邮箱/手机方式） */
  send2FASetupCode: async (method: number): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/Send2FASetupCode`, { method })
    return normalizeVerificationCode(raw)
  },

  enable2FA: (method: number, code: string) =>
    requestClient.post(`${PROFILE}/Enable2FA`, { method, code }),

  disable2FA: (method: number, code: string) =>
    requestClient.post(`${PROFILE}/Disable2FA`, { method, code }),

  // ==================== 邮箱/手机验证 ====================

  sendEmailVerifyCode: async (): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/SendEmailVerifyCode`)
    return normalizeVerificationCode(raw)
  },

  verifyEmail: (code: string) =>
    requestClient.post(`${PROFILE}/VerifyEmail`, { code }),

  sendPhoneVerifyCode: async (): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/SendPhoneVerifyCode`)
    return normalizeVerificationCode(raw)
  },

  verifyPhone: (code: string) =>
    requestClient.post(`${PROFILE}/VerifyPhone`, { code }),

  // ==================== 换绑邮箱/手机 ====================

  sendChangeEmailCode: async (data: ChangeEmailParams): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/SendChangeEmailCode`, data)
    return normalizeVerificationCode(raw)
  },

  confirmChangeEmail: (code: string) =>
    requestClient.post(`${PROFILE}/ConfirmChangeEmail`, { code }),

  sendChangePhoneCode: async (data: ChangePhoneParams): Promise<VerificationCodeResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/SendChangePhoneCode`, data)
    return normalizeVerificationCode(raw)
  },

  confirmChangePhone: (code: string) =>
    requestClient.post(`${PROFILE}/ConfirmChangePhone`, { code }),

  // ==================== 登录日志 ====================

  getLoginLogs: async (pageIndex = 1, pageSize = 20): Promise<LoginLogPage> => {
    const raw = await requestClient.get<any>(`${PROFILE}/LoginLogs`, {
      params: { pageIndex, pageSize },
    })
    const payload = unwrapPayload<any>(raw)
    const items = (payload?.items ?? []).map((item: any): LoginLogItem => ({
      loginTime: item?.loginTime ?? '',
      loginIp: item?.loginIp ?? '',
      loginLocation: item?.loginLocation ?? '',
      browser: item?.browser ?? '',
      os: item?.os ?? '',
      loginResult: item?.loginResult ?? 0,
      message: item?.message ?? '',
    }))
    return { items, total: payload?.total ?? 0 }
  },

  // ==================== 第三方账号 ====================

  getLinkedAccounts: async (): Promise<ExternalLoginItem[]> => {
    const raw = await requestClient.get<any>(`${PROFILE}/LinkedAccounts`)
    const payload = unwrapPayload<any[]>(raw)
    return (payload ?? []).map(normalizeExternalLogin)
  },

  unlinkAccount: (provider: string) =>
    requestClient.post(`${PROFILE}/UnlinkExternalLogin`, { provider }),

  // ==================== 账号管理 ====================

  deactivateAccount: (password: string) =>
    requestClient.post(`${PROFILE}/DeactivateAccount`, { password }),

  deleteAccount: (password: string) =>
    requestClient.delete(`${PROFILE}/Account`, { data: { password } }),
}

export const getProfileApi = profileApi.getProfile
export const updateProfileApi = profileApi.updateProfile
export const changePasswordApi = profileApi.changePassword
export const changeUserNameApi = profileApi.changeUserName
export const getSessionsApi = profileApi.getSessions
export const revokeSessionApi = profileApi.revokeSession
export const revokeOtherSessionsApi = profileApi.revokeOtherSessions
export const setup2FAApi = profileApi.setup2FA
export const send2FASetupCodeApi = profileApi.send2FASetupCode
export const enable2FAApi = profileApi.enable2FA
export const disable2FAApi = profileApi.disable2FA
export const sendEmailVerifyCodeApi = profileApi.sendEmailVerifyCode
export const verifyEmailApi = profileApi.verifyEmail
export const sendPhoneVerifyCodeApi = profileApi.sendPhoneVerifyCode
export const verifyPhoneApi = profileApi.verifyPhone
export const sendChangeEmailCodeApi = profileApi.sendChangeEmailCode
export const confirmChangeEmailApi = profileApi.confirmChangeEmail
export const sendChangePhoneCodeApi = profileApi.sendChangePhoneCode
export const confirmChangePhoneApi = profileApi.confirmChangePhone
export const getLoginLogsApi = profileApi.getLoginLogs
export const getLinkedAccountsApi = profileApi.getLinkedAccounts
export const unlinkAccountApi = profileApi.unlinkAccount
export const deactivateAccountApi = profileApi.deactivateAccount
export const deleteAccountApi = profileApi.deleteAccount
