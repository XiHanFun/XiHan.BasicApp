import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

export enum AuditResult {
  Pass = 0,
  Reject = 1,
  Return = 2,
}

export enum AuditStatus {
  Pending = 0,
  InProgress = 1,
  Approved = 2,
  Rejected = 3,
  Withdrawn = 4,
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
