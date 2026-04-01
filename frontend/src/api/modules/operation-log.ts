import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('OperationLog')

// -------- 类型 --------
export interface SysOperationLog {
  basicId?: string
  userId?: string
  userName?: string
  operationType?: string
  module?: string
  function?: string
  title?: string
  description?: string
  method?: string
  requestUrl?: string
  requestParams?: string
  responseResult?: string
  executionTime?: number
  operationIp?: string
  operationLocation?: string
  browser?: string
  os?: string
  status?: string
  errorMessage?: string
  operationTime?: string
  createdTime?: string
}

function normalize(raw: Record<string, any>): SysOperationLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    operationType: String(raw.operationType ?? ''),
    module: raw.module ?? '',
    function: raw.function ?? '',
    title: raw.title ?? '',
    description: raw.description ?? '',
    method: raw.method ?? '',
    requestUrl: raw.requestUrl ?? '',
    requestParams: raw.requestParams ?? '',
    responseResult: raw.responseResult ?? '',
    executionTime: raw.executionTime ?? 0,
    operationIp: raw.operationIp ?? '',
    operationLocation: raw.operationLocation ?? '',
    browser: raw.browser ?? '',
    os: raw.os ?? '',
    status: String(raw.status ?? ''),
    errorMessage: raw.errorMessage ?? '',
    operationTime: raw.operationTime ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

// -------- API --------
export const operationLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalize(item)),
    })),
  clear: () => api.clear(),
}
