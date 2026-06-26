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

export interface NotificationCreateDto {
  businessId?: ApiId | null
  businessType?: string | null
  content?: string | null
  expirationTime?: DateTimeString | null
  icon?: string | null
  link?: string | null
  needConfirm: boolean
  notificationType: NotificationType
  /** 创建后立即发布（默认 false，走「先建后发」流程） */
  publishImmediately: boolean
  remark?: string | null
  sendTime?: DateTimeString | null
  sendUserId?: ApiId | null
  targetType: NotificationTargetType
  /** 模板编码（站内通知渠道；提供时按模板渲染覆盖 title/content，缺失回退原值） */
  templateCode?: string | null
  /** 模板变量 */
  templateParams?: Record<string, string> | null
  title: string
  /** 目标类型为指定用户时的接收用户主键列表 */
  userIds: ApiId[]
}

export interface NotificationUpdateDto {
  basicId: ApiId
  businessId?: ApiId | null
  businessType?: string | null
  content?: string | null
  expirationTime?: DateTimeString | null
  icon?: string | null
  link?: string | null
  needConfirm: boolean
  notificationType: NotificationType
  remark?: string | null
  sendTime?: DateTimeString | null
  targetType: NotificationTargetType
  title: string
  /** 目标类型为指定用户时的接收用户主键列表（更新会整体覆盖原列表） */
  userIds: ApiId[]
}

export interface NotificationPublishDto {
  basicId: ApiId
  /** 留空沿用创建时设定的目标类型 */
  targetType?: NotificationTargetType | null
  /** 留空沿用创建时设定的指定用户列表 */
  userIds?: ApiId[]
}

export interface NotificationPublishResultDto {
  basicId: ApiId
  recipientCount: number
  sendTime: DateTimeString
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
