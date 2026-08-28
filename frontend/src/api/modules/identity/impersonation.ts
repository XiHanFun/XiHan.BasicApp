/**
 * 模仿登录 API
 *
 * 发起与结束都返回一枚新的登录令牌，调用方换令牌后整页重载即可切换身份。
 * 候选目标复用启用用户选择项接口（需 saas:user:read）。
 */
import type { ImpersonationCandidate, LoginToken, StartImpersonationParams } from '~/types'
import { createDynamicApiClient } from '../../base'
import { userApi } from './user'

const authCommandApi = createDynamicApiClient('Auth')

export const impersonationApi = {
  /** 可模仿的候选用户（服务端另有准入判定，这里只做检索） */
  async candidates(keyword?: string): Promise<ImpersonationCandidate[]> {
    const items = await userApi.select({ keyword, limit: 20, isSystemAccount: false })
    return (items ?? []).map(item => ({
      basicId: String(item.basicId),
      userName: item.userName,
      nickName: item.nickName,
      realName: item.realName,
      avatar: item.avatar,
    }))
  },
  /** 发起模仿登录 */
  start(input: StartImpersonationParams) {
    return authCommandApi.post<LoginToken, StartImpersonationParams>('StartImpersonation', input)
  },
  /** 结束模仿登录，回到发起人身份 */
  stop() {
    return authCommandApi.post<LoginToken>('StopImpersonation')
  },
}
