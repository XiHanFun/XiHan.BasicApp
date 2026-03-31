import type { PageResult, SysUserSession, UserSessionPageQuery } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const USER_SESSION_API = '/api/UserSession'

function normalizeUserSession(raw: Record<string, any>): SysUserSession {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    sessionId: raw.sessionId ?? raw.userSessionId ?? '',
    userSessionId: raw.userSessionId ?? raw.sessionId ?? '',
    deviceType: toNumber(raw.deviceType, 0),
    deviceName: raw.deviceName ?? undefined,
    ipAddress: raw.ipAddress ?? undefined,
    loginTime: raw.loginTime ?? '',
    lastActivityTime: raw.lastActivityTime ?? '',
    isOnline: Boolean(raw.isOnline),
    isRevoked: Boolean(raw.isRevoked),
    revokedAt: raw.revokedAt ?? undefined,
    logoutTime: raw.logoutTime ?? undefined,
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toUserSessionCreatePayload(data: Partial<SysUserSession>) {
  return {
    userId: toId(data.userId),
    sessionId: data.sessionId ?? '',
    deviceType: toNumber(data.deviceType, 0),
    deviceName: data.deviceName ?? '',
    ipAddress: data.ipAddress ?? '',
    isOnline: Boolean(data.isOnline),
    remark: data.remark ?? '',
  }
}

function toUserSessionUpdatePayload(id: string, data: Partial<SysUserSession>) {
  return {
    deviceType: toNumber(data.deviceType, 0),
    deviceName: data.deviceName ?? '',
    ipAddress: data.ipAddress ?? '',
    lastActivityTime: data.lastActivityTime ?? new Date().toISOString(),
    isOnline: Boolean(data.isOnline),
    isRevoked: Boolean(data.isRevoked),
    revokedAt: data.revokedAt ?? null,
    logoutTime: data.logoutTime ?? null,
    remark: data.remark ?? '',
    basicId: toId(id),
  }
}

export async function getUserSessionPageApi(
  params: UserSessionPageQuery,
): Promise<PageResult<SysUserSession>> {
  const data = await requestClient.post<any>(
    `${USER_SESSION_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['SessionId', 'DeviceName', 'IpAddress'],
      filterFieldMap: {
        deviceType: 'DeviceType',
        isOnline: 'IsOnline',
        isRevoked: 'IsRevoked',
      },
    }),
  )
  return normalizePageResult(data, normalizeUserSession)
}

export function getUserSessionDetailApi(id: string) {
  return requestClient
    .get<any>(`${USER_SESSION_API}/ById`, { params: { id } })
    .then(raw => normalizeUserSession(raw))
}

export function createUserSessionApi(data: Partial<SysUserSession>) {
  return requestClient.post<void>(`${USER_SESSION_API}/Create`, toUserSessionCreatePayload(data))
}

export function updateUserSessionApi(id: string, data: Partial<SysUserSession>) {
  return requestClient.put<void>(`${USER_SESSION_API}/Update`, toUserSessionUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteUserSessionApi(id: string) {
  return requestClient.delete<void>(`${USER_SESSION_API}/Delete`, {
    params: { id },
  })
}

export function getUserSessionBySessionIdApi(sessionId: string, tenantId?: number) {
  return requestClient
    .get<any>(`${USER_SESSION_API}/BySessionId/${sessionId}/${tenantId ?? 0}`)
    .then(raw => (raw ? normalizeUserSession(raw) : null))
}

export function revokeUserSessionsApi(userId: string | number, reason: string, tenantId?: number) {
  return requestClient.post<number>(`${USER_SESSION_API}/RevokeUserSessions`, undefined, {
    params: {
      userId,
      reason,
      tenantId: tenantId ?? 0,
    },
  })
}
