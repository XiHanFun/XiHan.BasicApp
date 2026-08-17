import type { DateTimeString } from '../../types'
import type { NotificationContentFormat, NotificationPriority, NotificationStatus, NotificationType } from './notification.types'

export interface UserInboxItemDto {
  basicId: string
  notificationId: string
  title: string
  content?: string | null
  notificationType: NotificationType
  notificationStatus: NotificationStatus
  priority: NotificationPriority
  contentFormat: NotificationContentFormat
  sendTime: DateTimeString
  readTime?: DateTimeString | null
  confirmTime?: DateTimeString | null
  isGlobal: boolean
  needConfirm: boolean
  isMandatory: boolean
  isBanner: boolean
  isPopup: boolean
  icon?: string | null
  link?: string | null
}

export interface UserInboxUpdateDto {
  basicId: string
}
