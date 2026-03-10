import type { PageResult, SysOperationLog } from '~/types'
import { buildPageRequest, normalizePageResult, toId } from '../helpers'
import requestClient from '../request'

const API = '/api/OperationLog'

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

export function getOperationLogPageApi(params: Record<string, any>): Promise<PageResult<SysOperationLog>> {
  return requestClient
    .post<any>(`${API}/Page`, buildPageRequest(params))
    .then(data => normalizePageResult(data, normalize))
}

export function clearOperationLogApi() {
  return requestClient.delete<boolean>(`${API}/Clear`)
}
