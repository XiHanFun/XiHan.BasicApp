import type { ApiId, PageResult } from '../../types'
import type {
  TenantMemberDetailDto,
  TenantMemberInviteStatusUpdateDto,
  TenantMemberListItemDto,
  TenantMemberPageQueryDto,
  TenantMemberStatusUpdateDto,
  TenantMemberUpdateDto,
} from './types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const tenantMemberQueryApi = createDynamicApiClient('TenantMemberQuery')
const tenantMemberCommandApi = createDynamicApiClient('TenantMember')
const tenantMemberReadApi = createReadApi<TenantMemberListItemDto, TenantMemberDetailDto, TenantMemberPageQueryDto>(
  'TenantMemberQuery',
  'TenantMember',
)

export const tenantMemberApi = {
  detail(id: ApiId) {
    return tenantMemberReadApi.detail(id)
  },
  page(input: TenantMemberPageQueryDto) {
    return tenantMemberQueryApi.get<PageResult<TenantMemberListItemDto>>(
      'TenantMemberPage',
      toTenantMemberPageParams(input),
    )
  },
  revoke(id: ApiId) {
    return tenantMemberCommandApi.delete(`TenantMember/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: TenantMemberUpdateDto) {
    return tenantMemberCommandApi.put<TenantMemberDetailDto, TenantMemberUpdateDto>('TenantMember', input)
  },
  updateInviteStatus(input: TenantMemberInviteStatusUpdateDto) {
    return tenantMemberCommandApi.put<TenantMemberDetailDto, TenantMemberInviteStatusUpdateDto>(
      'TenantMemberInviteStatus',
      input,
    )
  },
  updateStatus(input: TenantMemberStatusUpdateDto) {
    return tenantMemberCommandApi.put<TenantMemberDetailDto, TenantMemberStatusUpdateDto>(
      'TenantMemberStatus',
      input,
    )
  },
}

function toTenantMemberPageParams(input: TenantMemberPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'ExpirationTimeEnd', input.expirationTimeEnd)
  appendDynamicApiParam(params, 'ExpirationTimeStart', input.expirationTimeStart)
  appendDynamicApiParam(params, 'InviteStatus', input.inviteStatus)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'MemberType', input.memberType)
  appendDynamicApiParam(params, 'Status', input.status)
  appendDynamicApiParam(params, 'UserId', input.userId)

  return params
}
