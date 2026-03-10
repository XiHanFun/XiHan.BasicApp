import type { PageResult, SysAccessLog } from '~/types'
import { buildPageRequest, normalizePageResult, toId } from '../helpers'
import requestClient from '../request'

const API = '/api/AccessLog'

function normalize(raw: Record<string, any>): SysAccessLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    sessionId: raw.sessionId ?? '',
    resourcePath: raw.resourcePath ?? '',
    resourceName: raw.resourceName ?? '',
    resourceType: raw.resourceType ?? '',
    method: raw.method ?? '',
    accessResult: String(raw.accessResult ?? ''),
    statusCode: raw.statusCode ?? 0,
    accessIp: raw.accessIp ?? '',
    accessLocation: raw.accessLocation ?? '',
    userAgent: raw.userAgent ?? '',
    browser: raw.browser ?? '',
    os: raw.os ?? '',
    device: raw.device ?? '',
    referer: raw.referer ?? '',
    responseTime: raw.responseTime ?? 0,
    responseSize: raw.responseSize ?? 0,
    accessTime: raw.accessTime ?? '',
    leaveTime: raw.leaveTime ?? '',
    stayTime: raw.stayTime ?? 0,
    errorMessage: raw.errorMessage ?? '',
    extendData: raw.extendData ?? '',
    remark: raw.remark ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

export function getAccessLogPageApi(params: Record<string, any>): Promise<PageResult<SysAccessLog>> {
  return requestClient
    .post<any>(`${API}/Page`, buildPageRequest(params))
    .then(data => normalizePageResult(data, normalize))
}

export function clearAccessLogApi() {
  return requestClient.delete<boolean>(`${API}/Clear`)
}
