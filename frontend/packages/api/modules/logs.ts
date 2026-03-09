import type { PageResult, SysLogItem } from '~/types'
import { API_CONTRACT } from '../contract'
import { buildPageRequest, normalizePageResult, toNumber } from '../helpers'
import requestClient from '../request'

async function getLogPage(url: string, payload: Record<string, any>) {
  const page = Math.max(1, toNumber(payload.page, 1))
  const pageSize = Math.max(1, toNumber(payload.pageSize, 20))

  try {
    const data = await requestClient.post<any>(url, buildPageRequest(payload))
    return normalizePageResult<SysLogItem>(data)
  }
  catch {
    return {
      items: [],
      total: 0,
      page,
      pageSize,
    }
  }
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
