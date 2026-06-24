import type { ApiId, PageResult } from '../../types'
import type {
  ReviewAuditDto,
  ReviewCreateDto,
  ReviewDetailDto,
  ReviewListItemDto,
  ReviewPageQueryDto,
  ReviewStatusUpdateDto,
  ReviewUpdateDto,
  ReviewWithdrawDto,
} from './review.types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const reviewQueryApi = createDynamicApiClient('ReviewQuery')
const reviewCommandApi = createDynamicApiClient('Review')

export const reviewApi = {
  // Query
  detail(id: ApiId) {
    return reviewQueryApi.get<ReviewDetailDto | null>(
      `ReviewDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: ReviewPageQueryDto) {
    return reviewQueryApi.post<PageResult<ReviewListItemDto>>('ReviewPage', input)
  },
  // Commands
  audit(input: ReviewAuditDto) {
    return reviewCommandApi.post<ReviewDetailDto, ReviewAuditDto>('AuditReview', input)
  },
  create(input: ReviewCreateDto) {
    return reviewCommandApi.post<ReviewDetailDto, ReviewCreateDto>('Review', input)
  },
  delete(id: ApiId) {
    return reviewCommandApi.delete(`Review/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: ReviewUpdateDto) {
    return reviewCommandApi.put<ReviewDetailDto, ReviewUpdateDto>('Review', input)
  },
  updateStatus(input: ReviewStatusUpdateDto) {
    return reviewCommandApi.put<ReviewDetailDto, ReviewStatusUpdateDto>('ReviewStatus', input)
  },
  withdraw(input: ReviewWithdrawDto) {
    return reviewCommandApi.post<ReviewDetailDto, ReviewWithdrawDto>('WithdrawReview', input)
  },
}
