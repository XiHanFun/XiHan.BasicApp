import type { LoginParams, LoginResult, UserInfo } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'

function normalizeUserInfo(raw: any): UserInfo {
  return {
    basicId: raw?.basicId ?? raw?.BasicId ?? 0,
    userName: raw?.userName ?? raw?.UserName ?? '',
    nickName: raw?.nickName ?? raw?.NickName ?? '',
    avatar: raw?.avatar ?? raw?.Avatar ?? '',
    email: raw?.email ?? raw?.Email ?? '',
    phone: raw?.phone ?? raw?.Phone ?? '',
    roles: raw?.roles ?? raw?.Roles ?? [],
    permissions: raw?.permissions ?? raw?.Permissions ?? [],
  }
}

export async function loginApi(data: LoginParams): Promise<LoginResult> {
  const raw = await requestClient.post<any>(API_CONTRACT.auth.login, {
    UserName: data.username,
    Password: data.password,
    CaptchaCode: data.captchaCode,
    CaptchaKey: data.captchaKey,
  })

  return {
    accessToken: raw?.accessToken ?? raw?.AccessToken ?? '',
    refreshToken: raw?.refreshToken ?? raw?.RefreshToken ?? '',
    tokenType: raw?.tokenType ?? raw?.TokenType ?? 'Bearer',
    expiresIn: raw?.expiresIn ?? raw?.ExpiresIn ?? 0,
    issuedAt: raw?.issuedAt ?? raw?.IssuedAt ?? '',
    expiresAt: raw?.expiresAt ?? raw?.ExpiresAt ?? '',
    user: normalizeUserInfo(raw?.user ?? raw?.User),
    roles: raw?.roles ?? raw?.Roles ?? [],
    permissions: raw?.permissions ?? raw?.Permissions ?? [],
  }
}

export function logoutApi() {
  return requestClient.post(API_CONTRACT.auth.logout)
}

export async function refreshTokenApi(refreshToken: string) {
  const raw = await requestClient.post<any>(API_CONTRACT.auth.refreshToken, {
    RefreshToken: refreshToken,
  })
  return {
    accessToken: raw?.accessToken ?? raw?.AccessToken ?? '',
    refreshToken: raw?.refreshToken ?? raw?.RefreshToken ?? '',
  }
}

export async function getUserInfoApi() {
  const raw = await requestClient.get<any>(API_CONTRACT.auth.currentUser)
  return normalizeUserInfo(raw)
}

export function getAccessCodesApi() {
  return requestClient.get<string[]>(API_CONTRACT.auth.codes)
}

export function getCaptchaApi() {
  return requestClient.get<{ key: string; image: string }>(API_CONTRACT.auth.captcha)
}
