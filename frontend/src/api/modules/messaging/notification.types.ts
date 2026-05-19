import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum NotificationType {
  System = 'System',
  User = 'User',
  Announcement = 'Announcement',
  Warning = 'Warning',
  Error = 'Error',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum NotificationTargetType {
  All = 'All',
  Role = 'Role',
  Department = 'Department',
  User = 'User',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum NotificationStatus {
  Unread = 'Unread',
  Read = 'Read',
  Deleted = 'Deleted',
}

export interface NotificationPageQueryDto extends PageRequest {
  businessId?: ApiId | null
  businessType?: string | null
  expireTimeEnd?: DateTimeString | null
  expireTimeStart?: DateTimeString | null
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
  expireTime?: DateTimeString | null
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
