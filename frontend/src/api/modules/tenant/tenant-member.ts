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
  TenantOwnerTransferDto,
  TenantSupportMemberAddDto,
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
  /** 租户侧：把已有用户加入当前租户（后端取当前租户，不接受指定租户） */
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
  /** 平台侧：支持人员入驻（把平台账号以支持成员身份加入指定租户） */
  addSupport(input: TenantSupportMemberAddDto) {
    return tenantMemberCommandApi.post<TenantMemberDetailDto, TenantSupportMemberAddDto>('TenantSupportMember', input)
  },
  /** 平台侧：支持人员移除 */
  removeSupport(tenantId: ApiId, memberId: ApiId) {
    return tenantMemberCommandApi.delete('TenantSupportMember', { tenantId, memberId })
  },
  /** 平台侧：所有权转移，原所有者改为管理员，所有者角色随之移交 */
  transferOwner(input: TenantOwnerTransferDto) {
    // Transfer 不在动态 API 的动词前缀表内：方法名整体作为路由
    return tenantMemberCommandApi.post<TenantMemberDetailDto, TenantOwnerTransferDto>('TransferTenantOwner', input)
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
