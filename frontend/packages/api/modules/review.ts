import type { PageResult, ReviewPageQuery, SysReview } from '~/types'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'
import requestClient from '../request'

export async function getReviewPageApi(params: ReviewPageQuery): Promise<PageResult<SysReview>> {
  const data = await requestClient.get<any>(API_CONTRACT.system.reviews, { params })
  return normalizePageResult<SysReview>(data)
}

export function getReviewDetailApi(id: string) {
  return requestClient.get<SysReview>(`${API_CONTRACT.system.reviews}/${id}`)
}

export function createReviewApi(data: Partial<SysReview>) {
  return requestClient.post<void>(API_CONTRACT.system.reviews, data)
}

export function updateReviewApi(id: string, data: Partial<SysReview>) {
  return requestClient.put<void>(`${API_CONTRACT.system.reviews}/${id}`, data)
}

export function deleteReviewApi(id: string) {
  return requestClient.delete<void>(`${API_CONTRACT.system.reviews}/${id}`)
}

export function getReviewByCodeApi(reviewCode: string, tenantId?: number) {
  return requestClient.get<SysReview | null>(`${API_CONTRACT.system.reviews}/by-code`, {
    params: { reviewCode, tenantId },
  })
}
