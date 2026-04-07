import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('UserSession')

export interface SysUserSession {
  basicId: string
  userId: string
  userSessionId: string
  deviceType: number
  deviceName?: string
  browser?: string
  operatingSystem?: string
  ipAddress?: string
  location?: string
  loginTime: string
  lastActivityTime: string
  isOnline: boolean
  isRevoked: boolean
  revokedAt?: string
  logoutTime?: string
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface UserSessionPageQuery extends PageQuery {
  deviceType?: number
  isOnline?: boolean
  isRevoked?: boolean
}

function normalizeUserSession(raw: Record<string, any>): SysUserSession {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userSessionId: raw.userSessionId ?? '',
    deviceType: toNumber(raw.deviceType, 0),
    deviceName: raw.deviceName ?? undefined,
    browser: raw.browser ?? undefined,
    operatingSystem: raw.operatingSystem ?? undefined,
    ipAddress: raw.ipAddress ?? undefined,
    location: raw.location ?? undefined,
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

function toCreatePayload(data: Partial<SysUserSession>) {
  return {
    userId: toId(data.userId),
    sessionId: data.userSessionId ?? '',
    deviceType: toNumber(data.deviceType, 0),
    deviceName: data.deviceName ?? '',
    ipAddress: data.ipAddress ?? '',
    isOnline: Boolean(data.isOnline),
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysUserSession>) {
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

export const userSessionApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['UserSessionId', 'DeviceName', 'IpAddress', 'Browser'],
      filterFieldMap: { deviceType: 'DeviceType', isOnline: 'IsOnline', isRevoked: 'IsRevoked' },
    }),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeUserSession),

  create: (data: Partial<SysUserSession>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysUserSession>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getBySessionId: (sessionId: string, tenantId?: number) =>
    api.request
      .get<any>(`${api.baseUrl}BySessionId/${sessionId}/${tenantId ?? 0}`)
      .then((raw: any) => (raw ? normalizeUserSession(raw) : null)),

  revokeUserSessions: (userId: string | number, reason: string, tenantId?: number) =>
    api.request.post<number>(`${api.baseUrl}RevokeUserSessions/${userId}`, undefined, {
      params: { reason, tenantId: tenantId ?? 0 },
    }),
}
