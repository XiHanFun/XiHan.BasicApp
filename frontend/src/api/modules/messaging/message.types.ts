import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum EmailStatus {
  Pending = 'Pending',
  Sending = 'Sending',
  Success = 'Success',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum EmailType {
  System = 'System',
  Verification = 'Verification',
  Notification = 'Notification',
  Marketing = 'Marketing',
  Custom = 'Custom',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum SmsStatus {
  Pending = 'Pending',
  Sending = 'Sending',
  Success = 'Success',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum SmsType {
  VerificationCode = 'VerificationCode',
  Notification = 'Notification',
  Marketing = 'Marketing',
  Custom = 'Custom',
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
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
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
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
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
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
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
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
}

export interface SmsDetailDto extends SmsListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
}

// ---- Command DTOs ----

export interface EmailCreateDto {
  sendUserId?: ApiId | null
  receiveUserId?: ApiId | null
  emailType: EmailType
  fromEmail: string
  fromName?: string | null
  toEmail: string
  ccEmail?: string | null
  bccEmail?: string | null
  subject: string
  content?: string | null
  isHtml: boolean
  attachments?: string | null
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
  templateParams?: string | null
  scheduledTime?: DateTimeString | null
  maxRetryCount: number
  businessType?: string | null
  businessId?: ApiId | null
  remark?: string | null
}

export interface EmailUpdateDto extends BasicDto {
  receiveUserId?: ApiId | null
  emailType: EmailType
  fromEmail: string
  fromName?: string | null
  toEmail: string
  ccEmail?: string | null
  bccEmail?: string | null
  subject: string
  content?: string | null
  isHtml: boolean
  attachments?: string | null
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
  templateParams?: string | null
  scheduledTime?: DateTimeString | null
  maxRetryCount: number
  businessType?: string | null
  businessId?: ApiId | null
  remark?: string | null
}

export interface EmailStatusUpdateDto {
  basicId: ApiId
  emailStatus: EmailStatus
  sendTime?: DateTimeString | null
  retryCount?: number | null
  errorMessage?: string | null
  remark?: string | null
}

export interface SmsCreateDto {
  senderId?: ApiId | null
  receiverId?: ApiId | null
  smsType: SmsType
  toPhone: string
  content: string
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
  templateParams?: string | null
  provider?: string | null
  scheduledTime?: DateTimeString | null
  maxRetryCount: number
  businessType?: string | null
  businessId?: ApiId | null
  remark?: string | null
}

export interface SmsUpdateDto extends BasicDto {
  receiverId?: ApiId | null
  smsType: SmsType
  toPhone: string
  content: string
  /** 模板编码（关联消息模板，发送时按编码渲染） */
  templateCode?: string | null
  templateParams?: string | null
  provider?: string | null
  scheduledTime?: DateTimeString | null
  maxRetryCount: number
  businessType?: string | null
  businessId?: ApiId | null
  remark?: string | null
}

export interface SmsStatusUpdateDto {
  basicId: ApiId
  smsStatus: SmsStatus
  sendTime?: DateTimeString | null
  providerMessageId?: string | null
  retryCount?: number | null
  cost?: number | null
  errorMessage?: string | null
  remark?: string | null
}
