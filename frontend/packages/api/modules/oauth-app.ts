import type { OAuthAppPageQuery, PageResult, SysOAuthApp } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const OAUTH_API = '/api/OAuth'

function normalizeOAuthApp(raw: Record<string, any>): SysOAuthApp {
  return {
    basicId: toId(raw.basicId),
    appName: raw.appName ?? '',
    appDescription: raw.appDescription ?? '',
    clientId: raw.clientId ?? '',
    clientSecret: raw.clientSecret ?? '',
    appType: toNumber(raw.appType, 0),
    grantTypes: raw.grantTypes ?? '',
    redirectUris: raw.redirectUris ?? '',
    scopes: raw.scopes ?? '',
    accessTokenLifetime: toNumber(raw.accessTokenLifetime, 3600),
    refreshTokenLifetime: toNumber(raw.refreshTokenLifetime, 2592000),
    authorizationCodeLifetime: toNumber(raw.authorizationCodeLifetime, 300),
    skipConsent: Boolean(raw.skipConsent),
    status: toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toOAuthCreatePayload(data: Partial<SysOAuthApp>) {
  return {
    appName: data.appName ?? '',
    appDescription: data.appDescription ?? '',
    clientId: data.clientId ?? '',
    clientSecret: data.clientSecret ?? '',
    appType: toNumber(data.appType, 0),
    grantTypes: data.grantTypes ?? '',
    redirectUris: data.redirectUris ?? '',
    scopes: data.scopes ?? '',
    accessTokenLifetime: toNumber(data.accessTokenLifetime, 3600),
    refreshTokenLifetime: toNumber(data.refreshTokenLifetime, 2592000),
    authorizationCodeLifetime: toNumber(data.authorizationCodeLifetime, 300),
    skipConsent: Boolean(data.skipConsent),
    remark: data.remark ?? '',
  }
}

function toOAuthUpdatePayload(id: string, data: Partial<SysOAuthApp>) {
  return {
    ...toOAuthCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toNumber(id, 0),
  }
}

export async function getOAuthAppPageApi(
  params: OAuthAppPageQuery,
): Promise<PageResult<SysOAuthApp>> {
  const data = await requestClient.post<any>(
    `${OAUTH_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['AppName', 'ClientId'],
      filterFieldMap: {
        appType: 'AppType',
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeOAuthApp)
}

export function getOAuthAppDetailApi(id: string) {
  return requestClient
    .get<any>(`${OAUTH_API}/ById`, { params: { id } })
    .then(raw => normalizeOAuthApp(raw))
}

export function createOAuthAppApi(data: Partial<SysOAuthApp>) {
  return requestClient.post<void>(`${OAUTH_API}/Create`, toOAuthCreatePayload(data))
}

export function updateOAuthAppApi(id: string, data: Partial<SysOAuthApp>) {
  return requestClient.put<void>(`${OAUTH_API}/Update`, toOAuthUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteOAuthAppApi(id: string) {
  return requestClient.delete<void>(`${OAUTH_API}/Delete`, {
    params: { id },
  })
}

export function getOAuthAppByClientIdApi(clientId: string, tenantId?: number) {
  return requestClient
    .get<any>(`${OAUTH_API}/ByClientId/${clientId}/${tenantId ?? 0}`)
    .then(raw => (raw ? normalizeOAuthApp(raw) : null))
}
