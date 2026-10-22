import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { AccessPolicyAction, AccessPolicyHitSource, AccessPolicyPhase } from '../authorization/access-policy.types'

export interface AccessPolicyHitPageQueryDto extends PageRequest {
  action?: AccessPolicyAction | null
  hitTimeEnd?: DateTimeString | null
  hitTimeStart?: DateTimeString | null
  ip?: string | null
  isBlocked?: boolean | null
  path?: string | null
  phase?: AccessPolicyPhase | null
  policyCode?: string | null
  policyId?: ApiId | null
  source?: AccessPolicyHitSource | null
  traceId?: string | null
  userName?: string | null
}

/** 一行命中记录：同一键在同一分钟里合并，带次数 */
export interface AccessPolicyHitListItemDto extends BasicDto {
  action: AccessPolicyAction
  clientKind?: string | null
  hitCount: number
  hitTime: DateTimeString
  ip?: string | null
  isBlocked: boolean
  location?: string | null
  method?: string | null
  path?: string | null
  permissionCodes?: string | null
  phase: AccessPolicyPhase
  policyCode: string
  policyId?: ApiId | null
  policyName?: string | null
  source: AccessPolicyHitSource
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}
