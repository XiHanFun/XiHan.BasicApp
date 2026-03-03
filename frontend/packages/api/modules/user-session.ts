import type { PageResult, SysUserSession, UserSessionPageQuery } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getUserSessionPageApi(
  params: UserSessionPageQuery,
): Promise<PageResult<SysUserSession>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.userSessions, { params })
  return normalizePageResult<SysUserSession>(data)
}

export function getUserSessionDetailApi(id: string) {
  return requestClient.get<SysUserSession>(`${API_CONTRACT.system.userSessions}/${id}`)
}

export function createUserSessionApi(data: Partial<SysUserSession>) {
  return requestClient.post<void>(API_CONTRACT.system.userSessions, data)
}

export function updateUserSessionApi(id: string, data: Partial<SysUserSession>) {
  return requestClient.put<void>(`${API_CONTRACT.system.userSessions}/${id}`, data)
}

export function deleteUserSessionApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.userSessions}/${id}`)
}

export function getUserSessionBySessionIdApi(sessionId: string, tenantId?: number) {
  return requestClient.get<SysUserSession | null>(`${API_CONTRACT.system.userSessions}/by-session-id`, {
    params: { sessionId, tenantId },
  })
}

export function revokeUserSessionsApi(userId: number, reason: string, tenantId?: number) {
  return requestClient.post<number>(`${API_CONTRACT.system.userSessions}/revoke`, {
    userId,
    reason,
    tenantId,
  })
}
