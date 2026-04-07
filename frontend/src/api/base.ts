import type { AnyRecord, BuildPageRequestOptions } from './helpers'
import { buildPageRequest, flattenPageResponse } from './helpers'
import requestClient from './request'

export function useBaseApi(module: string) {
  const baseUrl = `/api/${module}/`

  return {
    baseUrl,

    page: (params: Record<string, any>, options?: BuildPageRequestOptions) =>
      requestClient
        .post<AnyRecord>(`${baseUrl}Page`, buildPageRequest(params, options))
        .then(flattenPageResponse),

    list: (params?: Record<string, any>) =>
      requestClient.post(`${baseUrl}List`, params ?? {}),

    detail: (id: string) =>
      requestClient.get(`${baseUrl}ById`, { params: { id } }),

    create: (data: Record<string, any>) =>
      requestClient.post(`${baseUrl}Create`, data),

    update: (data: Record<string, any>) =>
      requestClient.put(`${baseUrl}Update`, data),

    delete: (id: string) =>
      requestClient.delete(`${baseUrl}Delete`, { params: { id } }),

    deletePath: (id: string) =>
      requestClient.delete(`${baseUrl}Delete/${id}`),

    clear: () =>
      requestClient.delete(`${baseUrl}Clear`),

    request: requestClient,
  }
}
