/**
 * 模仿登录 API
 *
 * 发起与结束都返回一枚新的登录令牌，调用方换令牌后整页重载即可切换身份。
 * 候选目标：平台账号复用启用用户选择项（需 saas:user:read）；租户成员复用租户成员分页（需 saas:tenant-member:read）。
 */
import type {
  ImpersonationCandidate,
  ImpersonationCandidateQuery,
  ImpersonationTenantOption,
  LoginToken,
  StartImpersonationParams,
} from '~/types'
import { createDynamicApiClient } from '../../base'
import { createPageRequest } from '../../helpers'
import { ValidityStatus } from '../shared'
import { tenantApi, TenantConfigStatus, tenantMemberApi, TenantMemberInviteStatus, TenantStatus } from '../tenant'
import { userApi } from './user'

const authCommandApi = createDynamicApiClient('Auth')

/** 平台下拉一次取的租户数 */
const TENANT_OPTION_LIMIT = 100

export const impersonationApi = {
  /** 平台可在其中发起模仿的租户：正常且已完成初始化 */
  async tenants(): Promise<ImpersonationTenantOption[]> {
    const result = await tenantApi.page({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: TENANT_OPTION_LIMIT } }),
      configStatus: TenantConfigStatus.Configured,
      tenantStatus: TenantStatus.Normal,
    })
    return result.items.map(tenant => ({ tenantId: String(tenant.basicId), tenantName: tenant.tenantName }))
  },
  /** 可模仿的候选（服务端另有准入判定，这里只做检索） */
  async candidates(input: ImpersonationCandidateQuery): Promise<ImpersonationCandidate[]> {
    if (!input.tenantId) {
      const items = await userApi.select({ keyword: input.keyword, limit: 20, isSystemAccount: false })
      return (items ?? []).map(item => ({
        basicId: String(item.basicId),
        userName: item.userName,
        nickName: item.nickName,
        realName: item.realName,
        avatar: item.avatar,
      }))
    }

    const result = await tenantMemberApi.page({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: 20 } }),
      inviteStatus: TenantMemberInviteStatus.Accepted,
      keyword: input.keyword,
      status: ValidityStatus.Valid,
      tenantId: input.tenantId,
    })
    return result.items.map(member => ({
      basicId: String(member.userId),
      userName: member.userName ?? '',
      nickName: member.displayName || member.nickName,
      realName: member.realName,
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
