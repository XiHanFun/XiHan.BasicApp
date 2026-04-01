import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId, toNumber } from '../helpers'

const api = useBaseApi('Review')

export interface SysReview {
  basicId: string
  reviewCode: string
  reviewTitle: string
  reviewType: string
  reviewContent?: string
  reviewStatus: number
  reviewResult?: number
  priority: number
  submitUserId?: string
  submitTime: string
  currentReviewUserId?: string
  reviewLevel: number
  currentLevel: number
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface ReviewPageQuery extends PageQuery {
  reviewStatus?: number
  reviewResult?: number
  status?: number
}

function normalizeReview(raw: Record<string, any>): SysReview {
  return {
    basicId: toId(raw.basicId),
    reviewCode: raw.reviewCode ?? '',
    reviewTitle: raw.reviewTitle ?? '',
    reviewType: raw.reviewType ?? '',
    reviewContent: raw.reviewContent ?? '',
    reviewStatus: toNumber(raw.reviewStatus, 0),
    reviewResult: raw.reviewResult == null ? undefined : toNumber(raw.reviewResult, 0),
    priority: toNumber(raw.priority, 3),
    submitUserId: raw.submitUserId == null ? undefined : toId(raw.submitUserId),
    submitTime: raw.submitTime ?? '',
    currentReviewUserId:
      raw.currentReviewUserId == null ? undefined : toId(raw.currentReviewUserId),
    reviewLevel: toNumber(raw.reviewLevel, 1),
    currentLevel: toNumber(raw.currentLevel, 1),
    status: toNumber(raw.status, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? undefined,
    remark: raw.remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysReview>) {
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

function toUpdatePayload(id: string, data: Partial<SysReview>) {
  return {
    ...toCreatePayload(data),
    reviewStatus: toNumber(data.reviewStatus, 0),
    reviewResult: data.reviewResult ?? null,
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

export const reviewApi = {
  page: (params: Record<string, any>) =>
    api.page(params, {
      keywordFields: ['ReviewCode', 'ReviewTitle', 'ReviewType'],
      filterFieldMap: {
        reviewStatus: 'ReviewStatus',
        reviewResult: 'ReviewResult',
        status: 'Status',
      },
    }),

  detail: (id: string) =>
    api.request.get<any>(`${api.baseUrl}ById`, { params: { id } }).then(normalizeReview),

  create: (data: Partial<SysReview>) => api.create(toCreatePayload(data)),

  update: (id: string, data: Partial<SysReview>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),

  delete: (id: string) => api.delete(id),

  getByCode: (reviewCode: string, tenantId?: number) =>
    api.request
      .get<any>(`${api.baseUrl}ByReviewCode/${tenantId ?? 0}`, { params: { reviewCode } })
      .then((raw: any) => (raw ? normalizeReview(raw) : null)),
}
