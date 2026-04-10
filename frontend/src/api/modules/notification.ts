import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Notification')

export interface SysNotification {
  basicId: string
  recipientUserId?: string
  sendUserId?: string
  notificationType: number
  title: string
  content?: string
  notificationStatus: number
  readTime?: string
  sendTime: string
  expireTime?: string
  isGlobal?: boolean
  needConfirm?: boolean
  status?: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface NotificationPageQuery extends PageQuery {
  notificationType?: number
  notificationStatus?: number
  status?: number
}

export interface PushNotificationPayload {
  title: string
  content?: string
  notificationType: number
  isGlobal: boolean
  recipientUserIds: Array<number | string>
  sendUserId?: number | string
  needConfirm?: boolean
  icon?: string
  link?: string
  businessType?: string
  businessId?: string
  expireTime?: string
  tenantId?: number | string
  remark?: string
}

function toLongOrNull(value: unknown): null | number {
  if (value === undefined || value === null || value === '')
    return null
  const parsed = toNumber(value, 0)
  return parsed > 0 ? parsed : null
}

function toLongArray(values: Array<number | string>): number[] {
  return values
    .map(item => toNumber(item, 0))
    .filter(item => item > 0)
}

function toTenantSegment(value?: null | number | string): number {
  const parsed = toNumber(value, 0)
  return parsed > 0 ? parsed : 0
}

function normalizeNotification(raw: Record<string, any>): SysNotification {
  return {
    basicId: toId(raw.basicId),
    recipientUserId: raw.recipientUserId == null ? undefined : toId(raw.recipientUserId),
    sendUserId: raw.sendUserId == null ? undefined : toId(raw.sendUserId),
    notificationType: toNumber(raw.notificationType, 0),
    title: raw.title ?? '',
    content: raw.content ?? '',
    notificationStatus: toNumber(raw.notificationStatus, 0),
    readTime: raw.readTime ?? undefined,
    sendTime: raw.sendTime ?? '',
    expireTime: raw.expireTime ?? undefined,
    isGlobal: raw.isGlobal ?? undefined,
    needConfirm: raw.needConfirm ?? undefined,
    status: raw.status == null ? undefined : toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysNotification>) {
  const isGlobal = Boolean(data.isGlobal)
  return {
    recipientUserId: isGlobal ? null : toLongOrNull(data.recipientUserId),
    sendUserId: toLongOrNull(data.sendUserId),
    notificationType: toNumber(data.notificationType, 0),
    title: data.title ?? '',
    content: data.content ?? '',
    isGlobal,
    needConfirm: Boolean(data.needConfirm),
    sendTime: data.sendTime ?? new Date().toISOString(),
    expireTime: data.expireTime ?? null,
    remark: data.remark ?? '',
  }
}

function toUpdatePayload(id: string, data: Partial<SysNotification>) {
  return {
    ...toCreatePayload(data),
    notificationStatus: toNumber(data.notificationStatus, 0),
    readTime: data.readTime ?? null,
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

export const notificationApi = {
  page: (params: NotificationPageQuery) =>
    api.page(params, {
      keywordFields: ['Title', 'Content'],
      filterFieldMap: {
        notificationType: 'NotificationType',
        notificationStatus: 'NotificationStatus',
        status: 'Status',
      },
    }).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalizeNotification(item)),
    })),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeNotification),

  create: (data: Partial<SysNotification>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysNotification>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getUserNotifications: (userId: string | number, includeRead = true, tenantId?: null | number | string) =>
    api.request.get<any[]>(`${api.baseUrl}UserNotifications/${userId}/${toTenantSegment(tenantId)}`, { params: { includeRead } })
      .then((list: any[]) => Array.isArray(list) ? list.map(normalizeNotification) : []),

  getUnreadCount: (userId: string | number, tenantId?: null | number | string) =>
    api.request.get<number>(`${api.baseUrl}UnreadCount/${userId}/${toTenantSegment(tenantId)}`),

  markRead: (notificationId: string, userId: string | number, tenantId?: null | number | string) =>
    api.request.post<boolean>(`${api.baseUrl}MarkAsRead`, undefined, { params: { notificationId, userId, tenantId: toTenantSegment(tenantId) } }),

  markAllRead: (userId: string | number, tenantId?: null | number | string) =>
    api.request.post<number>(`${api.baseUrl}MarkAllAsRead`, undefined, { params: { userId, tenantId: toTenantSegment(tenantId) } }),

  confirm: (notificationId: string, userId: string | number, tenantId?: null | number | string) =>
    api.request.post<boolean>(`${api.baseUrl}Confirm`, undefined, { params: { notificationId, userId, tenantId: toTenantSegment(tenantId) } }),

  push: (data: PushNotificationPayload) => {
    const isGlobal = Boolean(data.isGlobal)
    return api.request.post<number>(`${api.baseUrl}Push`, {
      ...data,
      isGlobal,
      title: (data.title ?? '').trim(),
      content: data.content ?? '',
      recipientUserIds: isGlobal ? [] : toLongArray(data.recipientUserIds),
      sendUserId: toLongOrNull(data.sendUserId),
      tenantId: toLongOrNull(data.tenantId),
      remark: data.remark ?? '',
    })
  },
}
