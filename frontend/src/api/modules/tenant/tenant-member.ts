import type { ApiId, PageResult } from '../../types'
import type {
  TenantMemberDetailDto,
  TenantMemberInviteStatusUpdateDto,
  TenantMemberListItemDto,
  TenantMemberPageQueryDto,
  TenantMemberStatusUpdateDto,
  TenantMemberUpdateDto,
} from './tenant-member.types'
import {
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const tenantMemberQueryApi = createDynamicApiClient('TenantMemberQuery')
const tenantMemberCommandApi = createDynamicApiClient('Tenant')
const tenantMemberReadApi = createReadApi<TenantMemberListItemDto, TenantMemberDetailDto, TenantMemberPageQueryDto>(
  'TenantMemberQuery',
  'TenantMember',
)

export const tenantMemberApi = {
  detail(id: ApiId) {
    return tenantMemberReadApi.detail(id)
  },
  page(input: TenantMemberPageQueryDto) {
    return tenantMemberQueryApi.post<PageResult<TenantMemberListItemDto>>('TenantMemberPage', input)
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
