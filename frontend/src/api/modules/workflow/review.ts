import type { ApiId, PageResult } from '../../types'
import type { ReviewDetailDto, ReviewListItemDto, ReviewPageQueryDto } from './review.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  formatDynamicApiRouteValue,
} from '../../base'

const reviewQueryApi = createDynamicApiClient('ReviewQuery')

export const reviewApi = {
  detail(id: ApiId) {
    return reviewQueryApi.get<ReviewDetailDto | null>(
      `ReviewDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  page(input: ReviewPageQueryDto) {
    return reviewQueryApi.get<PageResult<ReviewListItemDto>>(
      'ReviewPage',
      toReviewPageParams(input),
    )
  },
}

function toReviewPageParams(input: ReviewPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'CurrentReviewUserId', input.currentReviewUserId)
  appendDynamicApiParam(params, 'EntityId', input.entityId)
  appendDynamicApiParam(params, 'EntityType', input.entityType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'ReviewCode', input.reviewCode)
  appendDynamicApiParam(params, 'ReviewEndTimeEnd', input.reviewEndTimeEnd)
  appendDynamicApiParam(params, 'ReviewEndTimeStart', input.reviewEndTimeStart)
  appendDynamicApiParam(params, 'ReviewResult', input.reviewResult)
  appendDynamicApiParam(params, 'ReviewStartTimeEnd', input.reviewStartTimeEnd)
  appendDynamicApiParam(params, 'ReviewStartTimeStart', input.reviewStartTimeStart)
  appendDynamicApiParam(params, 'ReviewStatus', input.reviewStatus)
  appendDynamicApiParam(params, 'ReviewType', input.reviewType)
  appendDynamicApiParam(params, 'Status', input.status)
  appendDynamicApiParam(params, 'SubmitTimeEnd', input.submitTimeEnd)
  appendDynamicApiParam(params, 'SubmitTimeStart', input.submitTimeStart)
  appendDynamicApiParam(params, 'SubmitUserId', input.submitUserId)
  return params
}
