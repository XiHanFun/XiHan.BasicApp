import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum AuditResult {
  Pass = 'Pass',
  Reject = 'Reject',
  Return = 'Return',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum AuditStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Withdrawn = 'Withdrawn',
}

export interface ReviewPageQueryDto extends PageRequest {
  currentReviewUserId?: ApiId | null
  entityId?: string | null
  entityType?: string | null
  keyword?: string | null
  reviewCode?: string | null
  reviewEndTimeEnd?: DateTimeString | null
  reviewEndTimeStart?: DateTimeString | null
  reviewResult?: AuditResult | null
  reviewStartTimeEnd?: DateTimeString | null
  reviewStartTimeStart?: DateTimeString | null
  reviewStatus?: AuditStatus | null
  reviewType?: string | null
  status?: EnableStatus | null
  submitTimeEnd?: DateTimeString | null
  submitTimeStart?: DateTimeString | null
  submitUserId?: ApiId | null
}

export interface ReviewListItemDto extends BasicDto {
  createdTime: DateTimeString
  currentLevel: number
  currentReviewUserId?: ApiId | null
  entityId?: string | null
  entityType?: string | null
  modifiedTime?: DateTimeString | null
  priority: number
  reviewCode: string
  reviewEndTime?: DateTimeString | null
  reviewLevel: number
  reviewResult?: AuditResult | null
  reviewStartTime?: DateTimeString | null
  reviewStatus: AuditStatus
  reviewTitle: string
  reviewType: string
  status: EnableStatus
  submitTime: DateTimeString
  submitUserId?: ApiId | null
}

export interface ReviewDetailDto extends ReviewListItemDto {
  attachments?: string | null
  businessData?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  extendData?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  reviewContent?: string | null
  reviewDescription?: string | null
  reviewUserIds?: string | null
}

// ---- Command DTOs ----

export interface ReviewCreateDto {
  reviewCode: string
  reviewTitle: string
  reviewType: string
  reviewContent?: string | null
  reviewDescription?: string | null
  entityType?: string | null
  entityId?: string | null
  businessData?: string | null
  reviewStatus: AuditStatus
  reviewResult?: AuditResult | null
  priority: number
  submitUserId?: ApiId | null
  submitTime?: DateTimeString | null
  currentReviewUserId?: ApiId | null
  reviewUserIds?: string | null
  reviewLevel: number
  currentLevel: number
  reviewStartTime?: DateTimeString | null
  reviewEndTime?: DateTimeString | null
  attachments?: string | null
  extendData?: string | null
  status: EnableStatus
  remark?: string | null
}

export interface ReviewUpdateDto extends BasicDto {
  reviewTitle: string
  reviewType: string
  reviewContent?: string | null
  reviewDescription?: string | null
  entityType?: string | null
  entityId?: string | null
  businessData?: string | null
  priority: number
  submitUserId?: ApiId | null
  submitTime?: DateTimeString | null
  currentReviewUserId?: ApiId | null
  reviewUserIds?: string | null
  reviewLevel: number
  currentLevel: number
  reviewStartTime?: DateTimeString | null
  reviewEndTime?: DateTimeString | null
  attachments?: string | null
  extendData?: string | null
  remark?: string | null
}

export interface ReviewStatusUpdateDto {
  basicId: ApiId
  status: EnableStatus
  remark?: string | null
}

export interface ReviewAuditDto {
  basicId: ApiId
  reviewResult: AuditResult
  reviewUserId?: ApiId | null
  nextReviewUserId?: ApiId | null
  reviewComment?: string | null
  reviewAction?: string | null
  reviewTime?: DateTimeString | null
  attachments?: string | null
  extendData?: string | null
  remark?: string | null
}

export interface ReviewWithdrawDto {
  basicId: ApiId
  reason?: string | null
  withdrawTime?: DateTimeString | null
}
