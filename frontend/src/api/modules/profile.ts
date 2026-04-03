import type {
  ChangePasswordParams,
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
    twoFactorEnabled: p?.twoFactorEnabled ?? false,
    emailVerified: p?.emailVerified ?? false,
    phoneVerified: p?.phoneVerified ?? false,
    lastPasswordChangeTime: p?.lastPasswordChangeTime || undefined,
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

export const profileApi = {
  getProfile: async (): Promise<UserProfile> => {
    const raw = await requestClient.get<any>(`${PROFILE}/Profile`)
    return normalizeProfile(raw)
  },

  updateProfile: (data: UpdateProfileParams) =>
    requestClient.post(`${PROFILE}/UpdateProfile`, data),

  changePassword: (data: ChangePasswordParams) =>
    requestClient.post(`${PROFILE}/ChangePassword`, data),

  getSessions: async (): Promise<UserSessionItem[]> => {
    const raw = await requestClient.get<any>(`${PROFILE}/Sessions`)
    const payload = unwrapPayload<any[]>(raw)
    return (payload ?? []).map(normalizeSessionItem)
  },

  revokeSession: (sessionId: string) =>
    requestClient.post(`${PROFILE}/RevokeSession`, { sessionId }),

  revokeOtherSessions: () =>
    requestClient.post(`${PROFILE}/RevokeOtherSessions`),

  setup2FA: async (): Promise<TwoFactorSetupResult> => {
    const raw = await requestClient.post<any>(`${PROFILE}/Setup2FA`)
    const payload = unwrapPayload<any>(raw)
    return {
      sharedKey: payload?.sharedKey ?? '',
      authenticatorUri: payload?.authenticatorUri ?? '',
    }
  },

  enable2FA: (code: string) =>
    requestClient.post(`${PROFILE}/Enable2FA`, { code }),

  disable2FA: (code: string) =>
    requestClient.post(`${PROFILE}/Disable2FA`, { code }),

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

  deactivateAccount: (password: string) =>
    requestClient.post(`${PROFILE}/DeactivateAccount`, { password }),

  deleteAccount: (password: string) =>
    requestClient.post(`${PROFILE}/DeleteAccount`, { password }),
}

export const getProfileApi = profileApi.getProfile
export const updateProfileApi = profileApi.updateProfile
export const changePasswordApi = profileApi.changePassword
export const getSessionsApi = profileApi.getSessions
export const revokeSessionApi = profileApi.revokeSession
export const revokeOtherSessionsApi = profileApi.revokeOtherSessions
export const setup2FAApi = profileApi.setup2FA
export const enable2FAApi = profileApi.enable2FA
export const disable2FAApi = profileApi.disable2FA
export const sendEmailVerifyCodeApi = profileApi.sendEmailVerifyCode
export const verifyEmailApi = profileApi.verifyEmail
export const sendPhoneVerifyCodeApi = profileApi.sendPhoneVerifyCode
export const verifyPhoneApi = profileApi.verifyPhone
export const deactivateAccountApi = profileApi.deactivateAccount
export const deleteAccountApi = profileApi.deleteAccount
