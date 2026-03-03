import type { OAuthAppPageQuery, PageResult, SysOAuthApp } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getOAuthAppPageApi(
  params: OAuthAppPageQuery,
): Promise<PageResult<SysOAuthApp>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.oauthApps, { params })
  return normalizePageResult<SysOAuthApp>(data)
}

export function getOAuthAppDetailApi(id: string) {
  return requestClient.get<SysOAuthApp>(`${API_CONTRACT.system.oauthApps}/${id}`)
}

export function createOAuthAppApi(data: Partial<SysOAuthApp>) {
  return requestClient.post<void>(API_CONTRACT.system.oauthApps, data)
}

export function updateOAuthAppApi(id: string, data: Partial<SysOAuthApp>) {
  return requestClient.put<void>(`${API_CONTRACT.system.oauthApps}/${id}`, data)
}

export function deleteOAuthAppApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.oauthApps}/${id}`)
}

export function getOAuthAppByClientIdApi(clientId: string, tenantId?: number) {
  return requestClient.get<SysOAuthApp | null>(`${API_CONTRACT.system.oauthApps}/by-client-id`, {
    params: { clientId, tenantId },
  })
}
