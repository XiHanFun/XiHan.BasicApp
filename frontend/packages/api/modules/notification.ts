import type { NotificationPageQuery, PageResult, SysNotification } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getNotificationPageApi(
  params: NotificationPageQuery,
): Promise<PageResult<SysNotification>> {
  const data = await requestClient.get<Record<string, unknown>>(API_CONTRACT.system.notifications, {
    params,
  })
  return normalizePageResult<SysNotification>(data)
}

export function getNotificationDetailApi(id: string) {
  return requestClient.get<SysNotification>(`${API_CONTRACT.system.notifications}/${id}`)
}

export function createNotificationApi(data: Partial<SysNotification>) {
  return requestClient.post<void>(API_CONTRACT.system.notifications, data)
}

export function updateNotificationApi(id: string, data: Partial<SysNotification>) {
  return requestClient.put<void>(`${API_CONTRACT.system.notifications}/${id}`, data)
}

export function deleteNotificationApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.notifications}/${id}`)
}

export function getUserNotificationsApi(userId: number, includeRead = true, tenantId?: number) {
  return requestClient.get<SysNotification[]>(`${API_CONTRACT.system.notifications}/user`, {
    params: { userId, includeRead, tenantId },
  })
}

export function getUnreadNotificationCountApi(userId: number, tenantId?: number) {
  return requestClient.get<number>(`${API_CONTRACT.system.notifications}/unread-count`, {
    params: { userId, tenantId },
  })
}

export function markNotificationReadApi(notificationId: number, userId: number, tenantId?: number) {
  return requestClient.post<boolean>(`${API_CONTRACT.system.notifications}/mark-read`, {
    notificationId,
    userId,
    tenantId,
  })
}

export function markAllNotificationsReadApi(userId: number, tenantId?: number) {
  return requestClient.post<number>(`${API_CONTRACT.system.notifications}/mark-all-read`, {
    userId,
    tenantId,
  })
}

export function confirmNotificationApi(notificationId: number, userId: number, tenantId?: number) {
  return requestClient.post<boolean>(`${API_CONTRACT.system.notifications}/confirm`, {
    notificationId,
    userId,
    tenantId,
  })
}

export interface PushNotificationPayload {
  title: string
  content?: string
  notificationType: number
  isGlobal: boolean
  recipientUserIds: number[]
  sendUserId?: number
  needConfirm?: boolean
  icon?: string
  link?: string
  businessType?: string
  businessId?: number
  expireTime?: string
  tenantId?: number
  remark?: string
}

export function pushNotificationApi(data: PushNotificationPayload) {
  return requestClient.post<number>(`${API_CONTRACT.system.notifications}/push`, data)
}
