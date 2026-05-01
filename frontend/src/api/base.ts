import type { AxiosRequestConfig } from 'axios'
import type { ApiId, ApiPrimitive, PageRequest, PageResult } from './types'
import { requestClient } from './request'

export type DynamicApiParamValue = boolean | number | string | undefined
export type DynamicApiParams = Record<string, DynamicApiParamValue>

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
      return api.get<PageResult<TListItem>>(`${normalizedResourceName}Page`, createPageRequestParams(input))
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

export function createPageRequestParams(input: PageRequest): DynamicApiParams {
  const params: DynamicApiParams = {
    'Behavior.DisableDefaultSort': input.behavior.disableDefaultSort,
    'Behavior.DisablePaging': input.behavior.disablePaging,
    'Behavior.EnableSplitQuery': input.behavior.enableSplitQuery,
    'Behavior.IgnoreSoftDelete': input.behavior.ignoreSoftDelete,
    'Behavior.IgnoreTenant': input.behavior.ignoreTenant,
    'Page.PageIndex': input.page.pageIndex,
    'Page.PageSize': input.page.pageSize,
  }

  appendDynamicApiParam(params, 'Behavior.QueryTimeout', input.behavior.queryTimeout)
  appendKeywordParams(params, input)
  appendFilterParams(params, input)
  appendSortParams(params, input)

  return params
}

export function appendDynamicApiParam(params: DynamicApiParams, key: string, value: ApiPrimitive | undefined) {
  if (value === undefined || value === null || value === '') {
    return
  }

  params[key] = value
}

function appendKeywordParams(params: DynamicApiParams, input: PageRequest) {
  appendDynamicApiParam(params, 'Conditions.Keyword.Value', input.conditions.keyword?.value)
  input.conditions.keyword?.fields.forEach((field, index) => {
    appendDynamicApiParam(params, `Conditions.Keyword.Fields[${index}]`, field)
  })
}

function appendFilterParams(params: DynamicApiParams, input: PageRequest) {
  input.conditions.filters.forEach((filter, index) => {
    appendDynamicApiParam(params, `Conditions.Filters[${index}].Field`, filter.field)
    appendDynamicApiParam(params, `Conditions.Filters[${index}].Operator`, filter.operator)
    appendDynamicApiParam(params, `Conditions.Filters[${index}].Value`, filter.value)
    filter.values?.forEach((value, valueIndex) => {
      appendDynamicApiParam(params, `Conditions.Filters[${index}].Values[${valueIndex}]`, value)
    })
  })
}

function appendSortParams(params: DynamicApiParams, input: PageRequest) {
  input.conditions.sorts.forEach((sort, index) => {
    appendDynamicApiParam(params, `Conditions.Sorts[${index}].Direction`, sort.direction)
    appendDynamicApiParam(params, `Conditions.Sorts[${index}].Field`, sort.field)
    appendDynamicApiParam(params, `Conditions.Sorts[${index}].Priority`, sort.priority)
  })
}

export function formatDynamicApiRouteValue(value: ApiId) {
  return encodeURIComponent(String(value))
}

function normalizeSegment(value: string) {
  const normalized = value.trim().replace(/^\/+|\/+$/g, '')
  if (!normalized) {
    throw new Error('Dynamic API 路由段不能为空')
  }
  return normalized
}
