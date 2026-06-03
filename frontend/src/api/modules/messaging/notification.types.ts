import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import { NotificationStatus, NotificationTargetType, NotificationType } from '~/types/enums'

// 通知契约枚举已下沉到 packages/types/enums（供布局通知中心等 shell 功能复用），此处 re-export 保持 `@/api` 入口不变
export { NotificationStatus, NotificationTargetType, NotificationType }

export interface NotificationPageQueryDto extends PageRequest {
  businessId?: ApiId | null
  businessType?: string | null
  expirationTimeEnd?: DateTimeString | null
  expirationTimeStart?: DateTimeString | null
  isPublished?: boolean | null
  keyword?: string | null
  needConfirm?: boolean | null
  notificationType?: NotificationType | null
  sendTimeEnd?: DateTimeString | null
  sendTimeStart?: DateTimeString | null
  sendUserId?: ApiId | null
  targetType?: NotificationTargetType | null
}

export interface NotificationListItemDto extends BasicDto {
  businessId?: ApiId | null
  businessType?: string | null
  content?: string | null
  createdTime: DateTimeString
  expirationTime?: DateTimeString | null
  icon?: string | null
  isPublished: boolean
  link?: string | null
  modifiedTime?: DateTimeString | null
  needConfirm: boolean
  notificationType: NotificationType
  remark?: string | null
  sendTime: DateTimeString
  sendUserId?: ApiId | null
  targetType: NotificationTargetType
  title: string
}

export interface NotificationDetailDto extends NotificationListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

export interface UserNotificationPageQueryDto extends PageRequest {
  confirmTimeEnd?: DateTimeString | null
  confirmTimeStart?: DateTimeString | null
  notificationId?: ApiId | null
  notificationStatus?: NotificationStatus | null
  readTimeEnd?: DateTimeString | null
  readTimeStart?: DateTimeString | null
  userId?: ApiId | null
}

export interface UserNotificationListItemDto extends BasicDto {
  confirmTime?: DateTimeString | null
  content?: string | null
  createdTime: DateTimeString
  icon?: string | null
  link?: string | null
  needConfirm: boolean
  notificationId: ApiId
  notificationStatus: NotificationStatus
  notificationType?: NotificationType | null
  readTime?: DateTimeString | null
  sendTime?: DateTimeString | null
  title?: string | null
  userId: ApiId
}

export interface UserNotificationDetailDto extends UserNotificationListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
