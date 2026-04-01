import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('AccessLog')

// -------- 类型 --------
export interface SysAccessLog {
  basicId?: string
  userId?: string
  userName?: string
  sessionId?: string
  resourcePath?: string
  resourceName?: string
  resourceType?: string
  method?: string
  accessResult?: string
  statusCode?: number
  accessIp?: string
  accessLocation?: string
  userAgent?: string
  browser?: string
  os?: string
  device?: string
  referer?: string
  responseTime?: number
  responseSize?: number
  accessTime?: string
  leaveTime?: string
  stayTime?: number
  errorMessage?: string
  extendData?: string
  remark?: string
  createdTime?: string
}

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

// -------- API --------
export const accessLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalize(item)),
    })),
  clear: () => api.clear(),
}
