import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum NotificationType {
  System = 0,
  User = 1,
  Announcement = 2,
  Warning = 3,
  Error = 4,
}

export enum NotificationStatus {
  Unread = 0,
  Read = 1,
  Deleted = 2,
}

export interface NotificationPageQueryDto extends PageRequest {
  businessId?: ApiId | null
  businessType?: string | null
  expireTimeEnd?: DateTimeString | null
  expireTimeStart?: DateTimeString | null
  isBroadcast?: boolean | null
  isPublished?: boolean | null
  keyword?: string | null
  needConfirm?: boolean | null
  notificationType?: NotificationType | null
  sendTimeEnd?: DateTimeString | null
  sendTimeStart?: DateTimeString | null
  sendUserId?: ApiId | null
}

export interface NotificationListItemDto extends BasicDto {
  businessId?: ApiId | null
  businessType?: string | null
  content?: string | null
  createdTime: DateTimeString
  expireTime?: DateTimeString | null
  icon?: string | null
  isBroadcast: boolean
  isPublished: boolean
  link?: string | null
  modifiedTime?: DateTimeString | null
  needConfirm: boolean
  notificationType: NotificationType
  remark?: string | null
  sendTime: DateTimeString
  sendUserId?: ApiId | null
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
