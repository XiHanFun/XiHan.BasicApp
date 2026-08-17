import type { ApiId, DateTimeString } from '../../types'
import type { UserManagementSecurityDto } from '../system/system.types'

export type UserSecurityDetailDto = UserManagementSecurityDto

/** 用户锁定状态更新 */
export interface UserLockUpdateDto {
  userId: ApiId
  isLocked: boolean
  lockoutEndTime?: DateTimeString | null
  remark?: string | null
}

/** 用户登录策略更新 */
export interface UserLoginPolicyUpdateDto {
  userId: ApiId
  allowMultiLogin: boolean
  maxLoginDevices: number
  remark?: string | null
}

/** 用户双因素认证重置（清除 OTP 绑定） */
export interface UserTwoFactorResetDto {
  userId: ApiId
  remark?: string | null
}
