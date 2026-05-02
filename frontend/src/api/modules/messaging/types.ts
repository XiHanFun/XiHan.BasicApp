import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum EmailStatus {
  Pending = 0,
  Sending = 1,
  Success = 2,
  Failed = 3,
  Cancelled = 4,
}

export enum EmailType {
  System = 0,
  Verification = 1,
  Notification = 2,
  Marketing = 3,
  Custom = 99,
}

export enum SmsStatus {
  Pending = 0,
  Sending = 1,
  Success = 2,
  Failed = 3,
  Cancelled = 4,
}

export enum SmsType {
  VerificationCode = 0,
  Notification = 1,
  Marketing = 2,
  Custom = 99,
}

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

export interface EmailPageQueryDto extends PageRequest {
  businessId?: ApiId | null
  businessType?: string | null
  emailStatus?: EmailStatus | null
  emailType?: EmailType | null
  keyword?: string | null
  receiveUserId?: ApiId | null
  scheduledTimeEnd?: DateTimeString | null
  scheduledTimeStart?: DateTimeString | null
  sendTimeEnd?: DateTimeString | null
  sendTimeStart?: DateTimeString | null
  sendUserId?: ApiId | null
  templateId?: ApiId | null
}

export interface EmailListItemDto extends BasicDto {
  businessId?: ApiId | null
  businessType?: string | null
  createdTime: DateTimeString
  emailStatus: EmailStatus
  emailType: EmailType
  hasAttachment: boolean
  hasBlindRecipient: boolean
  hasBody: boolean
  hasCopyRecipient: boolean
  hasFailureDetail: boolean
  hasNote: boolean
  hasRecipientAddress: boolean
  hasSenderAddress: boolean
  hasTemplateData: boolean
  isHtml: boolean
  maxRetryCount: number
  modifiedTime?: DateTimeString | null
  receiveUserId?: ApiId | null
  retryCount: number
  scheduledTime?: DateTimeString | null
  sendTime?: DateTimeString | null
  sendUserId?: ApiId | null
  subject: string
  templateId?: ApiId | null
}

export interface EmailDetailDto extends EmailListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

export interface SmsPageQueryDto extends PageRequest {
  businessId?: ApiId | null
  businessType?: string | null
  keyword?: string | null
  provider?: string | null
  receiverId?: ApiId | null
  scheduledTimeEnd?: DateTimeString | null
  scheduledTimeStart?: DateTimeString | null
  sendTimeEnd?: DateTimeString | null
  sendTimeStart?: DateTimeString | null
  senderId?: ApiId | null
  smsStatus?: SmsStatus | null
  smsType?: SmsType | null
  templateId?: ApiId | null
}

export interface SmsListItemDto extends BasicDto {
  businessId?: ApiId | null
  businessType?: string | null
  cost?: number | null
  createdTime: DateTimeString
  hasBody: boolean
  hasFailureDetail: boolean
  hasNote: boolean
  hasProviderReceipt: boolean
  hasRecipientPhone: boolean
  hasTemplateData: boolean
  maxRetryCount: number
  modifiedTime?: DateTimeString | null
  provider?: string | null
  receiverId?: ApiId | null
  retryCount: number
  scheduledTime?: DateTimeString | null
  sendTime?: DateTimeString | null
  senderId?: ApiId | null
  smsStatus: SmsStatus
  smsType: SmsType
  templateId?: ApiId | null
}

export interface SmsDetailDto extends SmsListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
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
