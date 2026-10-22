import type { ApiId, PageResult } from '../../types'
import type {
  AccessPolicyActionUpdateDto,
  AccessPolicyAttributeDto,
  AccessPolicyCreateDto,
  AccessPolicyDetailDto,
  AccessPolicyListItemDto,
  AccessPolicyPageQueryDto,
  AccessPolicyStatusUpdateDto,
  AccessPolicyTemplateDto,
  AccessPolicyTrialInputDto,
  AccessPolicyTrialResultDto,
  AccessPolicyUpdateDto,
  LoginBanDto,
  LoginUnbanDto,
} from './access-policy.types'
import { createDynamicApiClient } from '../../base'

const accessPolicyQueryApi = createDynamicApiClient('AccessPolicyQuery')
const accessPolicyCommandApi = createDynamicApiClient('AccessPolicy')

/** 访问策略：当前层（平台或本租户）的策略、试运行、模板与自动封禁 */
export const accessPolicyApi = {
  page(input: AccessPolicyPageQueryDto) {
    return accessPolicyQueryApi.post<PageResult<AccessPolicyListItemDto>, AccessPolicyPageQueryDto>('AccessPolicyPage', input)
  },
  detail(id: ApiId) {
    return accessPolicyQueryApi.get<AccessPolicyDetailDto | null>('AccessPolicyDetail', { id })
  },
  /** 条件编辑器能用的属性 */
  attributes() {
    return accessPolicyQueryApi.get<AccessPolicyAttributeDto[]>('AccessPolicyAttributes')
  },
  /** 预置模板 */
  templates() {
    return accessPolicyQueryApi.get<AccessPolicyTemplateDto[]>('AccessPolicyTemplates')
  },
  /** 试运行：已保存的策略或条件草稿，按模拟的请求上下文求值 */
  trial(input: AccessPolicyTrialInputDto) {
    return accessPolicyQueryApi.post<AccessPolicyTrialResultDto, AccessPolicyTrialInputDto>('AccessPolicyTrial', input)
  },
  /** 当前的自动封禁（租户上下文为空） */
  bans() {
    return accessPolicyQueryApi.get<LoginBanDto[]>('LoginBans')
  },
  create(input: AccessPolicyCreateDto) {
    return accessPolicyCommandApi.post<AccessPolicyDetailDto, AccessPolicyCreateDto>('AccessPolicy', input)
  },
  update(input: AccessPolicyUpdateDto) {
    return accessPolicyCommandApi.put<AccessPolicyDetailDto, AccessPolicyUpdateDto>('AccessPolicy', input)
  },
  /** 改动作（含启用拦截）：会拦下当前请求的拒绝策略保存不了 */
  updateAction(input: AccessPolicyActionUpdateDto) {
    return accessPolicyCommandApi.put<AccessPolicyDetailDto, AccessPolicyActionUpdateDto>('AccessPolicyAction', input)
  },
  updateStatus(input: AccessPolicyStatusUpdateDto) {
    return accessPolicyCommandApi.put<AccessPolicyDetailDto, AccessPolicyStatusUpdateDto>('AccessPolicyStatus', input)
  },
  delete(id: ApiId) {
    return accessPolicyCommandApi.delete('AccessPolicy', { id })
  },
  /** 手动解封（只在平台） */
  unban(input: LoginUnbanDto) {
    return accessPolicyCommandApi.post<boolean, LoginUnbanDto>('UnbanLogin', input)
  },
}
