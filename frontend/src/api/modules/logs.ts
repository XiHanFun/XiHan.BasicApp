import type { PageResult, SysLogItem } from '~/types'
import requestClient from '../request'
import { API_CONTRACT } from '../contract'
import { normalizePageResult } from '../helpers'

async function getLogPage(url: string, payload: Record<string, any>) {
  const data = await requestClient.post<any>(url, payload)
  return normalizePageResult<SysLogItem>(data)
}

export function getAccessLogsApi(payload: Record<string, any>): Promise<PageResult<SysLogItem>> {
  return getLogPage(API_CONTRACT.logs.access, payload)
}

export function getOperationLogsApi(payload: Record<string, any>): Promise<PageResult<SysLogItem>> {
  return getLogPage(API_CONTRACT.logs.operation, payload)
}

export function getExceptionLogsApi(payload: Record<string, any>): Promise<PageResult<SysLogItem>> {
  return getLogPage(API_CONTRACT.logs.exception, payload)
}

export function getAuditLogsApi(payload: Record<string, any>): Promise<PageResult<SysLogItem>> {
  return getLogPage(API_CONTRACT.logs.audit, payload)
}
