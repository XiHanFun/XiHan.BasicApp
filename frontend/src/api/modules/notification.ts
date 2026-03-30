import type { NotificationPageQuery, PageResult, SysNotification } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const NOTIFICATION_API = '/api/Notification'

function normalizeNotification(raw: Record<string, any>): SysNotification {
  return {
    basicId: toId(raw.basicId),
    recipientUserId: raw.recipientUserId === null || raw.recipientUserId === undefined
      ? undefined
      : toId(raw.recipientUserId),
    sendUserId: raw.sendUserId === null || raw.sendUserId === undefined
      ? undefined
      : toId(raw.sendUserId),
    notificationType: toNumber(raw.notificationType, 0),
    title: raw.title ?? '',
    content: raw.content ?? '',
    notificationStatus: toNumber(raw.notificationStatus, 0),
    readTime: raw.readTime ?? undefined,
    sendTime: raw.sendTime ?? '',
    expireTime: raw.expireTime ?? undefined,
    isGlobal: raw.isGlobal ?? undefined,
    needConfirm: raw.needConfirm ?? undefined,
    status: raw.status === null || raw.status === undefined ? undefined : toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toNotificationCreatePayload(data: Partial<SysNotification>) {
  return {
    recipientUserId: data.recipientUserId ?? null,
    sendUserId: data.sendUserId ?? null,
    notificationType: toNumber(data.notificationType, 0),
    title: data.title ?? '',
    content: data.content ?? '',
    isGlobal: Boolean(data.isGlobal),
    needConfirm: Boolean(data.needConfirm),
    sendTime: data.sendTime ?? new Date().toISOString(),
    expireTime: data.expireTime ?? null,
    remark: data.remark ?? '',
  }
}

function toNotificationUpdatePayload(id: string, data: Partial<SysNotification>) {
  return {
    ...toNotificationCreatePayload(data),
    notificationStatus: toNumber(data.notificationStatus, 0),
    readTime: data.readTime ?? null,
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

export async function getNotificationPageApi(
  params: NotificationPageQuery,
): Promise<PageResult<SysNotification>> {
  const data = await requestClient.post<Record<string, unknown>>(
    `${NOTIFICATION_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['Title', 'Content'],
      filterFieldMap: {
        notificationType: 'NotificationType',
        notificationStatus: 'NotificationStatus',
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeNotification)
}

export function getNotificationDetailApi(id: string) {
  return requestClient
    .get<any>(`${NOTIFICATION_API}/ById`, { params: { id } })
    .then(raw => normalizeNotification(raw))
}

export function createNotificationApi(data: Partial<SysNotification>) {
  return requestClient.post<void>(`${NOTIFICATION_API}/Create`, toNotificationCreatePayload(data))
}

export function updateNotificationApi(id: string, data: Partial<SysNotification>) {
  return requestClient.put<void>(`${NOTIFICATION_API}/Update`, toNotificationUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteNotificationApi(id: string) {
  return requestClient.delete<void>(`${NOTIFICATION_API}/Delete`, {
    params: { id },
  })
}

export function getUserNotificationsApi(userId: string | number, includeRead = true, tenantId?: number) {
  return requestClient
    .get<any[]>(`${NOTIFICATION_API}/UserNotifications/${userId}/${tenantId ?? 0}`, {
      params: { includeRead },
    })
    .then(list => (Array.isArray(list) ? list.map(item => normalizeNotification(item)) : []))
}

export function getUnreadNotificationCountApi(userId: string | number, tenantId?: number) {
  return requestClient.get<number>(`${NOTIFICATION_API}/UnreadCount/${userId}/${tenantId ?? 0}`)
}

export function markNotificationReadApi(notificationId: string, userId: string | number, tenantId?: number) {
  return requestClient.post<boolean>(`${NOTIFICATION_API}/MarkAsRead`, undefined, {
    params: {
      notificationId,
      userId,
      tenantId: tenantId ?? 0,
    },
  })
}

export function markAllNotificationsReadApi(userId: string | number, tenantId?: number) {
  return requestClient.post<number>(`${NOTIFICATION_API}/MarkAllAsRead`, undefined, {
    params: {
      userId,
      tenantId: tenantId ?? 0,
    },
  })
}

export function confirmNotificationApi(notificationId: string, userId: string | number, tenantId?: number) {
  return requestClient.post<boolean>(`${NOTIFICATION_API}/Confirm`, undefined, {
    params: {
      notificationId,
      userId,
      tenantId: tenantId ?? 0,
    },
  })
}

export interface PushNotificationPayload {
  title: string
  content?: string
  notificationType: number
  isGlobal: boolean
  recipientUserIds: string[]
  sendUserId?: string
  needConfirm?: boolean
  icon?: string
  link?: string
  businessType?: string
  businessId?: string
  expireTime?: string
  tenantId?: number
  remark?: string
}

export function pushNotificationApi(data: PushNotificationPayload) {
  return requestClient.post<number>(`${NOTIFICATION_API}/Push`, data)
}
