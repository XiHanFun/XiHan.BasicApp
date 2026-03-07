import type { LoginConfig, LoginParams, LoginResult, PermissionInfo, UserInfo } from '~/types'
import { API_CONTRACT } from '../contract'
import requestClient from '../request'

function unwrapPayload<T>(raw: any): T {
  return (raw?.data ?? raw?.Data ?? raw) as T
}

function normalizeUserInfo(raw: any): UserInfo {
  const payload = unwrapPayload<any>(raw)
  return {
    basicId: payload?.basicId ?? payload?.BasicId ?? payload?.userId ?? payload?.UserId ?? 0,
    userName:
      payload?.userName ?? payload?.UserName ?? payload?.username ?? payload?.Username ?? '',
    nickName:
      payload?.nickName ?? payload?.NickName ?? payload?.nickname ?? payload?.Nickname ?? '',
    avatar: payload?.avatar ?? payload?.Avatar ?? '',
    email: payload?.email ?? payload?.Email ?? '',
    phone: payload?.phone ?? payload?.Phone ?? '',
    tenantId: payload?.tenantId ?? payload?.TenantId ?? null,
    roles: payload?.roles ?? payload?.Roles ?? [],
    permissions: payload?.permissions ?? payload?.Permissions ?? [],
  }
}

function normalizeToken(raw: any): LoginResult {
  const payload = unwrapPayload<any>(raw)
  return {
    accessToken: payload?.accessToken ?? payload?.AccessToken ?? '',
    refreshToken: payload?.refreshToken ?? payload?.RefreshToken ?? '',
    tokenType: payload?.tokenType ?? payload?.TokenType ?? 'Bearer',
    expiresIn: payload?.expiresIn ?? payload?.ExpiresIn ?? 0,
    issuedAt: payload?.issuedAt ?? payload?.IssuedAt ?? new Date().toISOString(),
    expiresAt: payload?.expiresAt ?? payload?.ExpiresAt ?? '',
  }
}

function normalizeLoginConfig(raw: any): LoginConfig {
  const payload = unwrapPayload<any>(raw)
  return {
    loginMethods: payload?.loginMethods ?? payload?.LoginMethods ?? ['password'],
    tenantEnabled: payload?.tenantEnabled ?? payload?.TenantEnabled ?? true,
    oauthProviders:
      payload?.oauthProviders ?? payload?.OauthProviders ?? payload?.OAuthProviders ?? [],
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
    UserName: data.username,
    Password: data.password,
    TenantId: data.tenantId,
  })
  return normalizeToken(raw)
}

export async function refreshTokenApi(refreshToken: string): Promise<LoginResult> {
  const raw = await requestClient.post<any>(API_CONTRACT.auth.refreshToken, {
    RefreshToken: refreshToken,
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
  const authPermission = await getPermissionsApi()
  return authPermission.permissions
}

export function logoutApi() {
  return requestClient.post(API_CONTRACT.auth.logout)
}
