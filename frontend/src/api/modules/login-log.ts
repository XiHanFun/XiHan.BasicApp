import type { AnyRecord } from '../helpers'
import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { toId } from '../helpers'

const api = useBaseApi('LoginLog')

// -------- 类型 --------
export interface SysLoginLog {
  basicId?: string
  userId?: string
  userName?: string
  loginIp?: string
  loginLocation?: string
  browser?: string
  os?: string
  loginResult?: number
  message?: string
  loginTime?: string
  createdTime?: string
}

function normalize(raw: Record<string, any>): SysLoginLog {
  return {
    basicId: toId(raw.basicId),
    userId: toId(raw.userId),
    userName: raw.userName ?? '',
    loginIp: raw.loginIp ?? '',
    loginLocation: raw.loginLocation ?? '',
    browser: raw.browser ?? '',
    os: raw.os ?? '',
    loginResult: raw.loginResult ?? 0,
    message: raw.message ?? '',
    loginTime: raw.loginTime ?? '',
    createdTime: raw.createdTime ?? '',
  }
}

// -------- API --------
export const loginLogApi = {
  page: (params: PageQuery & Record<string, any>) =>
    api.page(params).then(res => ({
      total: res.total,
      items: (res.items as AnyRecord[]).map(item => normalize(item)),
    })),
  clear: () => api.clear(),
}
