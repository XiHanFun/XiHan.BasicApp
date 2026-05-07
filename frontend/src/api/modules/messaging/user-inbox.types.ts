import type { DateTimeString } from '../../types'
import type { NotificationStatus, NotificationType } from './notification.types'

export interface UserInboxItemDto {
  basicId: string
  notificationId: string
  title: string
  content?: string | null
  notificationType: NotificationType
  notificationStatus: NotificationStatus
  sendTime: DateTimeString
  readTime?: DateTimeString | null
  confirmTime?: DateTimeString | null
  isGlobal: boolean
  needConfirm: boolean
  icon?: string | null
  link?: string | null
}

export interface UserInboxUpdateDto {
  basicId: string
}
