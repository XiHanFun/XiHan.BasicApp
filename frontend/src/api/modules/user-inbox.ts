import type { SysNotification } from './notification'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('UserInbox')

const NOTIFICATION_TYPE_FROM_NAME: Record<string, number> = {
  System: 0, User: 1, Announcement: 2, Warning: 3, Error: 4,
}
const NOTIFICATION_STATUS_FROM_NAME: Record<string, number> = {
  Unread: 0, Read: 1, Deleted: 2,
}
const YES_NO_FROM_NAME: Record<string, number> = { No: 0, Yes: 1 }

function parseEnum(value: unknown, nameMap: Record<string, number>, fallback: number): number {
  if (typeof value === 'number') return value
  if (typeof value === 'string') return nameMap[value] ?? toNumber(value, fallback)
  return fallback
}

function toTenantSegment(value?: null | number | string): string {
  if (value === undefined || value === null || value === '') return '0'
  return String(value)
}

function normalizeNotification(raw: Record<string, any>): SysNotification {
  return {
    basicId: toId(raw.basicId),
    sendUserId: raw.sendUserId == null ? undefined : toId(raw.sendUserId),
    notificationType: parseEnum(raw.notificationType, NOTIFICATION_TYPE_FROM_NAME, 0),
    title: raw.title ?? '',
    content: raw.content ?? '',
    notificationStatus: parseEnum(raw.notificationStatus, NOTIFICATION_STATUS_FROM_NAME, 0),
    readTime: raw.readTime ?? undefined,
    confirmTime: raw.confirmTime ?? undefined,
    sendTime: raw.sendTime ?? '',
    expireTime: raw.expireTime ?? undefined,
    isGlobal: raw.isGlobal ?? undefined,
    needConfirm: raw.needConfirm ?? undefined,
    isPublished: raw.isPublished ?? false,
    status: raw.status == null ? undefined : parseEnum(raw.status, YES_NO_FROM_NAME, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

export const userInboxApi = {
  list: (userId: string | number, includeRead = true, tenantId?: null | number | string) =>
    api.request.get<any[]>(
      `${api.baseUrl}UserNotifications/${userId}/${toTenantSegment(tenantId)}`,
      { params: { includeRead } },
    ).then((list: any[]) => Array.isArray(list) ? list.map(normalizeNotification) : []),

  unreadCount: (userId: string | number, tenantId?: null | number | string) =>
    api.request.get<number>(`${api.baseUrl}UnreadCount/${userId}/${toTenantSegment(tenantId)}`),

  markRead: (notificationId: string, userId: string | number, tenantId?: null | number | string) =>
    api.request.post<boolean>(`${api.baseUrl}MarkAsRead`, undefined, {
      params: { notificationId, userId, tenantId: toTenantSegment(tenantId) },
    }),

  markAllRead: (userId: string | number, tenantId?: null | number | string) =>
    api.request.post<number>(`${api.baseUrl}MarkAllAsRead`, undefined, {
      params: { userId, tenantId: toTenantSegment(tenantId) },
    }),

  confirm: (notificationId: string, userId: string | number, tenantId?: null | number | string) =>
    api.request.post<boolean>(`${api.baseUrl}Confirm`, undefined, {
      params: { notificationId, userId, tenantId: toTenantSegment(tenantId) },
    }),
}
