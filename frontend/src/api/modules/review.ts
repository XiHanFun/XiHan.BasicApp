import type { PageResult, ReviewPageQuery, SysReview } from '~/types'
import { buildPageRequest, normalizePageResult, toId, toNumber } from '../helpers'
import requestClient from '../request'

const REVIEW_API = '/api/Review'

function normalizeReview(raw: Record<string, any>): SysReview {
  return {
    basicId: toId(raw.basicId),
    reviewCode: raw.reviewCode ?? '',
    reviewTitle: raw.reviewTitle ?? '',
    reviewType: raw.reviewType ?? '',
    reviewContent: raw.reviewContent ?? '',
    reviewStatus: toNumber(raw.reviewStatus, 0),
    reviewResult: raw.reviewResult === null || raw.reviewResult === undefined
      ? undefined
      : toNumber(raw.reviewResult, 0),
    priority: toNumber(raw.priority, 3),
    submitUserId: raw.submitUserId === null || raw.submitUserId === undefined
      ? undefined
      : toId(raw.submitUserId),
    submitTime: raw.submitTime ?? '',
    currentReviewUserId: raw.currentReviewUserId === null || raw.currentReviewUserId === undefined
      ? undefined
      : toId(raw.currentReviewUserId),
    reviewLevel: toNumber(raw.reviewLevel, 1),
    currentLevel: toNumber(raw.currentLevel, 1),
    status: toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toReviewCreatePayload(data: Partial<SysReview>) {
  return {
    reviewCode: data.reviewCode ?? '',
    reviewTitle: data.reviewTitle ?? '',
    reviewType: data.reviewType ?? '',
    reviewContent: data.reviewContent ?? '',
    priority: toNumber(data.priority, 3),
    submitUserId: data.submitUserId ?? null,
    submitTime: data.submitTime ?? new Date().toISOString(),
    currentReviewUserId: data.currentReviewUserId ?? null,
    reviewLevel: toNumber(data.reviewLevel, 1),
    currentLevel: toNumber(data.currentLevel, 1),
    remark: data.remark ?? '',
  }
}

function toReviewUpdatePayload(id: string, data: Partial<SysReview>) {
  return {
    ...toReviewCreatePayload(data),
    reviewStatus: toNumber(data.reviewStatus, 0),
    reviewResult: data.reviewResult ?? null,
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

export async function getReviewPageApi(params: ReviewPageQuery): Promise<PageResult<SysReview>> {
  const data = await requestClient.post<any>(
    `${REVIEW_API}/Page`,
    buildPageRequest(params, {
      keywordFields: ['ReviewCode', 'ReviewTitle', 'ReviewType'],
      filterFieldMap: {
        reviewStatus: 'ReviewStatus',
        reviewResult: 'ReviewResult',
        status: 'Status',
      },
    }),
  )
  return normalizePageResult(data, normalizeReview)
}

export function getReviewDetailApi(id: string) {
  return requestClient
    .get<any>(`${REVIEW_API}/ById`, { params: { id } })
    .then(raw => normalizeReview(raw))
}

export function createReviewApi(data: Partial<SysReview>) {
  return requestClient.post<void>(`${REVIEW_API}/Create`, toReviewCreatePayload(data))
}

export function updateReviewApi(id: string, data: Partial<SysReview>) {
  return requestClient.put<void>(`${REVIEW_API}/Update`, toReviewUpdatePayload(id, data), {
    params: { id },
  })
}

export function deleteReviewApi(id: string) {
  return requestClient.delete<void>(`${REVIEW_API}/Delete`, {
    params: { id },
  })
}

export function getReviewByCodeApi(reviewCode: string, tenantId?: number) {
  return requestClient
    .get<any>(`${REVIEW_API}/ByReviewCode/${tenantId ?? 0}`, {
      params: { reviewCode },
    })
    .then(raw => (raw ? normalizeReview(raw) : null))
}
