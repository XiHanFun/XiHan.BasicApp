import type { ApiId, ApiPrimitive, PageRequest, PageResult } from './types'
import type { AxiosRequestConfig } from '~/request'
import { requestClient } from './request'

export type DynamicApiParamValue = boolean | number | string | undefined
export type DynamicApiParams = Record<string, DynamicApiParamValue>

export interface DynamicApiClient {
  delete: <TResult = void>(action: string, config?: AxiosRequestConfig) => Promise<TResult>
  get: <TResult>(action: string, params?: DynamicApiParams, config?: AxiosRequestConfig) => Promise<TResult>
  post: <TResult, TBody = unknown>(action: string, body?: TBody, config?: AxiosRequestConfig) => Promise<TResult>
  put: <TResult, TBody = unknown>(action: string, body?: TBody, config?: AxiosRequestConfig) => Promise<TResult>
}

export interface ReadApi<TListItem, TDetail, TQuery extends PageRequest = PageRequest> {
  detail: (id: ApiId) => Promise<TDetail | null>
  page: (input: TQuery) => Promise<PageResult<TListItem>>
}

export interface CommandApi<TCreate, TUpdate, TDetail> {
  create: (input: TCreate) => Promise<TDetail>
  update: (input: TUpdate) => Promise<TDetail>
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
      // 分页统一走 POST：整个请求对象作为 JSON body 上送（后端复杂参数自动从 body 绑定）
      return api.post<PageResult<TListItem>, TQuery>(`${normalizedResourceName}Page`, input)
    },
    detail(id: ApiId) {
      return api.get<TDetail | null>(`${normalizedResourceName}Detail/${formatDynamicApiRouteValue(id)}`)
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

// 分页查询已统一改走 POST（整个查询对象作 JSON body 上送），旧的「查询串序列化」函数
// （createPageRequestParams / appendKeyword/Filter/Sort Params）已移除。
// appendDynamicApiParam 仍保留：供非分页的下拉/统计等 GET 接口拼装简单查询参数。
export function appendDynamicApiParam(params: DynamicApiParams, key: string, value: ApiPrimitive | undefined) {
  if (value === undefined || value === null || value === '') {
    return
  }

  params[key] = value
}

export function formatDynamicApiRouteValue(value: ApiId) {
  return encodeURIComponent(value)
}

function normalizeSegment(value: string) {
  const normalized = value.trim().replace(/^\/+|\/+$/g, '')
  if (!normalized) {
    throw new Error('Dynamic API 路由段不能为空')
  }
  return normalized
}
