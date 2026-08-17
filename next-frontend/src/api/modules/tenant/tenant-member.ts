import type { ApiId, PageResult } from '../../types'
import type {
  TenantMemberAddDto,
  TenantMemberDetailDto,
  TenantMemberInviteDto,
  TenantMemberInviteStatusUpdateDto,
  TenantMemberListItemDto,
  TenantMemberPageQueryDto,
  TenantMemberStatusUpdateDto,
  TenantMemberUpdateDto,
} from './tenant-member.types'
import {
  createDynamicApiClient,
  createReadApi,
} from '../../base'

const tenantMemberQueryApi = createDynamicApiClient('TenantMemberQuery')
const tenantMemberCommandApi = createDynamicApiClient('Tenant')
const tenantMemberReadApi = createReadApi<TenantMemberListItemDto, TenantMemberDetailDto, TenantMemberPageQueryDto>(
  'TenantMemberQuery',
  'TenantMember',
)

export const tenantMemberApi = {
  add(input: TenantMemberAddDto) {
    // POST /api/Tenant/TenantMember（Add 前缀被动态 API 剥离并推导为 POST）
    return tenantMemberCommandApi.post<TenantMemberDetailDto, TenantMemberAddDto>('TenantMember', input)
  },
  detail(id: ApiId) {
    return tenantMemberReadApi.detail(id)
  },
  invite(input: TenantMemberInviteDto) {
    // Invite 不在动态 API 的动词前缀表内：方法名整体作为路由，默认 POST
    return tenantMemberCommandApi.post<TenantMemberDetailDto, TenantMemberInviteDto>('InviteTenantMember', input)
  },
  page(input: TenantMemberPageQueryDto) {
    return tenantMemberQueryApi.post<PageResult<TenantMemberListItemDto>>('TenantMemberPage', input)
  },
  revoke(id: ApiId) {
    return tenantMemberCommandApi.delete('TenantMember', { id })
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
