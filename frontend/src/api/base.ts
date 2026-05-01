import type { AxiosRequestConfig } from 'axios'
import type { ApiId, PageRequest, PageResult } from './types'
import { requestClient } from './request'

export type DynamicApiParams = Record<string, boolean | number | string | undefined>

export interface DynamicApiClient {
  delete<TResult = void>(action: string, config?: AxiosRequestConfig): Promise<TResult>
  get<TResult>(action: string, params?: DynamicApiParams, config?: AxiosRequestConfig): Promise<TResult>
  post<TResult, TBody = unknown>(action: string, body?: TBody, config?: AxiosRequestConfig): Promise<TResult>
  put<TResult, TBody = unknown>(action: string, body?: TBody, config?: AxiosRequestConfig): Promise<TResult>
}

export interface ReadApi<TListItem, TDetail, TQuery extends PageRequest = PageRequest> {
  detail(id: ApiId): Promise<TDetail | null>
  page(input: TQuery): Promise<PageResult<TListItem>>
}

export interface CommandApi<TCreate, TUpdate, TDetail> {
  create(input: TCreate): Promise<TDetail>
  update(input: TUpdate): Promise<TDetail>
}

export function createDynamicApiClient(controllerName: string): DynamicApiClient {
  const normalizedControllerName = normalizeSegment(controllerName)

  return {
    get<TResult>(action: string, params?: DynamicApiParams, config?: AxiosRequestConfig) {
      return requestClient.get<TResult>(buildActionUrl(normalizedControllerName, action), {
        ...config,
        params,
      })
    },
    post<TResult, TBody = unknown>(action: string, body?: TBody, config?: AxiosRequestConfig) {
      return requestClient.post<TResult>(buildActionUrl(normalizedControllerName, action), body, config)
    },
    put<TResult, TBody = unknown>(action: string, body?: TBody, config?: AxiosRequestConfig) {
      return requestClient.put<TResult>(buildActionUrl(normalizedControllerName, action), body, config)
    },
    delete<TResult = void>(action: string, config?: AxiosRequestConfig) {
      return requestClient.delete<TResult>(buildActionUrl(normalizedControllerName, action), config)
    },
  }
}

export function createReadApi<TListItem, TDetail, TQuery extends PageRequest = PageRequest>(
  controllerName: string,
  resourceName: string,
): ReadApi<TListItem, TDetail, TQuery> {
  const api = createDynamicApiClient(controllerName)
  const normalizedResourceName = normalizeSegment(resourceName)

  return {
    page(input: TQuery) {
      return api.get<PageResult<TListItem>>(`${normalizedResourceName}Page`, undefined, { params: input })
    },
    detail(id: ApiId) {
      return api.get<TDetail | null>(`${normalizedResourceName}Detail`, { id })
    },
  }
}

export function createCommandApi<TCreate, TUpdate, TDetail>(
  controllerName: string,
  resourceName: string,
): CommandApi<TCreate, TUpdate, TDetail> {
  const api = createDynamicApiClient(controllerName)
  const normalizedResourceName = normalizeSegment(resourceName)

  return {
    create(input: TCreate) {
      return api.post<TDetail, TCreate>(normalizedResourceName, input)
    },
    update(input: TUpdate) {
      return api.put<TDetail, TUpdate>(normalizedResourceName, input)
    },
  }
}

function buildActionUrl(controllerName: string, action: string) {
  return `/${controllerName}/${normalizeSegment(action)}`
}

function normalizeSegment(value: string) {
  const normalized = value.trim().replace(/^\/+|\/+$/g, '')
  if (!normalized) {
    throw new Error('Dynamic API 路由段不能为空')
  }
  return normalized
}
