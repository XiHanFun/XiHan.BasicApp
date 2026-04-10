import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Notification')

// 后端 JsonStringEnumConverter：API 收发枚举均为字符串名称
const NOTIFICATION_TYPE_TO_NAME: Record<number, string> = {
  0: 'System', 1: 'User', 2: 'Announcement', 3: 'Warning', 4: 'Error',
}
const NOTIFICATION_TYPE_FROM_NAME: Record<string, number> = {
  System: 0, User: 1, Announcement: 2, Warning: 3, Error: 4,
}
const NOTIFICATION_STATUS_TO_NAME: Record<number, string> = {
  0: 'Unread', 1: 'Read', 2: 'Deleted',
}
const NOTIFICATION_STATUS_FROM_NAME: Record<string, number> = {
  Unread: 0, Read: 1, Deleted: 2,
}
const YES_NO_TO_NAME: Record<number, string> = { 0: 'No', 1: 'Yes' }
const YES_NO_FROM_NAME: Record<string, number> = { No: 0, Yes: 1 }

/** 将后端枚举字符串或数字统一转为前端数字 */
function parseEnum(value: unknown, nameMap: Record<string, number>, fallback: number): number {
  if (typeof value === 'number') return value
  if (typeof value === 'string') return nameMap[value] ?? toNumber(value, fallback)
  return fallback
}

/** 前端数字转后端枚举名称字符串 */
function toEnumName(value: unknown, nameMap: Record<number, string>, fallback: string): string {
  const num = typeof value === 'number' ? value : toNumber(value, -1)
  return nameMap[num] ?? fallback
}

/** 保持字符串 ID，避免 JS Number 丢失雪花 ID 精度 */
function toStringIdOrNull(value: unknown): null | string {
  if (value === undefined || value === null || value === '') return null
  const s = String(value)
  return s === '0' ? null : s
}

function toStringIdArray(values: Array<number | string>): string[] {
  return values.map(v => String(v)).filter(s => s !== '' && s !== '0')
}

function toTenantSegment(value?: null | number | string): string {
  if (value === undefined || value === null || value === '') return '0'
  return String(value)
}

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

function normalizeNotification(raw: Record<string, any>): SysNotification {
  return {
    basicId: toId(raw.basicId),
    recipientUserId: raw.recipientUserId == null ? undefined : toId(raw.recipientUserId),
    sendUserId: raw.sendUserId == null ? undefined : toId(raw.sendUserId),
    notificationType: parseEnum(raw.notificationType, NOTIFICATION_TYPE_FROM_NAME, 0),
    title: raw.title ?? '',
    content: raw.content ?? '',
    notificationStatus: parseEnum(raw.notificationStatus, NOTIFICATION_STATUS_FROM_NAME, 0),
    readTime: raw.readTime ?? undefined,
    sendTime: raw.sendTime ?? '',
    expireTime: raw.expireTime ?? undefined,
    isGlobal: raw.isGlobal ?? undefined,
    needConfirm: raw.needConfirm ?? undefined,
    status: raw.status == null ? undefined : parseEnum(raw.status, YES_NO_FROM_NAME, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysNotification>) {
  const isGlobal = Boolean(data.isGlobal)
  return {
    recipientUserId: isGlobal ? null : toStringIdOrNull(data.recipientUserId),
    sendUserId: toStringIdOrNull(data.sendUserId),
    notificationType: toEnumName(data.notificationType, NOTIFICATION_TYPE_TO_NAME, 'System'),
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
    notificationStatus: toEnumName(data.notificationStatus, NOTIFICATION_STATUS_TO_NAME, 'Unread'),
    readTime: data.readTime ?? null,
    status: toEnumName(data.status, YES_NO_TO_NAME, 'Yes'),
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
      notificationType: toEnumName(data.notificationType, NOTIFICATION_TYPE_TO_NAME, 'System'),
      recipientUserIds: isGlobal ? [] : toStringIdArray(data.recipientUserIds),
      sendUserId: toStringIdOrNull(data.sendUserId),
      tenantId: toStringIdOrNull(data.tenantId),
      remark: data.remark ?? '',
    })
  },
}
