import type { LoginConfig, LoginParams, LoginResult, PermissionInfo, UserInfo } from '~/types'
import { API_CONTRACT } from '../contract'
import { unwrapPayload } from '../helpers'
import requestClient from '../request'

function normalizeUserInfo(raw: any): UserInfo {
  const payload = unwrapPayload<any>(raw)
  return {
    basicId: payload?.basicId ?? payload?.userId ?? 0,
    userName: payload?.userName ?? payload?.username ?? '',
    nickName: payload?.nickName ?? payload?.nickname ?? '',
    avatar: payload?.avatar ?? '',
    email: payload?.email ?? '',
    phone: payload?.phone ?? '',
    tenantId: payload?.tenantId ?? null,
    roles: payload?.roles ?? [],
    permissions: payload?.permissions ?? [],
  }
}

function normalizeToken(raw: any): LoginResult {
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

function normalizeLoginConfig(raw: any): LoginConfig {
  const payload = unwrapPayload<any>(raw)
  return {
    loginMethods: payload?.loginMethods ?? ['password'],
    tenantEnabled: payload?.tenantEnabled ?? true,
    oauthProviders: payload?.oauthProviders ?? [],
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

export function getLoginConfigApi() {
  return requestClient.get<any>(API_CONTRACT.auth.loginConfig).then(normalizeLoginConfig)
}

export async function loginApi(data: LoginParams): Promise<LoginResult> {
  const raw = await requestClient.post<any>(API_CONTRACT.auth.login, {
    userName: data.username,
    password: data.password,
    tenantId: data.tenantId,
  })
  return normalizeToken(raw)
}

export async function refreshTokenApi(refreshToken: string): Promise<LoginResult> {
  const raw = await requestClient.post<any>(API_CONTRACT.auth.refreshToken, {
    refreshToken,
  })
  return normalizeToken(raw)
}

export async function getUserInfoApi(): Promise<UserInfo> {
  const raw = await requestClient.get<any>(API_CONTRACT.auth.currentUser)
  return normalizeUserInfo(raw)
}

export async function getPermissionsApi(): Promise<PermissionInfo> {
  const raw = await requestClient.get<any>(API_CONTRACT.auth.permissions)
  return normalizePermission(raw)
}

export async function getAccessCodesApi() {
  try {
    return await requestClient.get<string[]>(API_CONTRACT.auth.codes)
  }
  catch {
    const authPermission = await getPermissionsApi()
    return authPermission.permissions
  }
}

export function logoutApi() {
  return requestClient.post(API_CONTRACT.auth.logout)
}
