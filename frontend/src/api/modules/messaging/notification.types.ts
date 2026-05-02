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
  createdTime: DateTimeString
  expireTime?: DateTimeString | null
  hasAction: boolean
  hasBody: boolean
  hasNote: boolean
  hasVisualMark: boolean
  isBroadcast: boolean
  isPublished: boolean
  modifiedTime?: DateTimeString | null
  needConfirm: boolean
  notificationType: NotificationType
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
  createdTime: DateTimeString
  notificationId: ApiId
  notificationStatus: NotificationStatus
  readTime?: DateTimeString | null
  userId: ApiId
}

export interface UserNotificationDetailDto extends UserNotificationListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
