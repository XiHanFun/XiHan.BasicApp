import type {
  UserLockUpdateDto,
  UserLoginPolicyUpdateDto,
  UserSecurityDetailDto,
  UserTwoFactorResetDto,
} from './user-security.types'
import type { UserPasswordResetDto } from './user.types'
import { createDynamicApiClient } from '../../base'

// 后端为 UserSecurityAppService → 控制器 UserSecurity；
// Reset* 不在动态 API 动词表内：前缀保留、动词为 POST（ResetUserPassword / ResetUserTwoFactor）
const userSecurityCommandApi = createDynamicApiClient('UserSecurity')

/** 用户安全相关命令接口 */
export const userSecurityApi = {
  resetPassword(input: UserPasswordResetDto) {
    return userSecurityCommandApi.post<UserSecurityDetailDto, UserPasswordResetDto>('ResetUserPassword', input)
  },
  resetTwoFactor(input: UserTwoFactorResetDto) {
    return userSecurityCommandApi.post<UserSecurityDetailDto, UserTwoFactorResetDto>('ResetUserTwoFactor', input)
  },
  updateLock(input: UserLockUpdateDto) {
    return userSecurityCommandApi.put<UserSecurityDetailDto, UserLockUpdateDto>('UserLock', input)
  },
  updateLoginPolicy(input: UserLoginPolicyUpdateDto) {
    return userSecurityCommandApi.put<UserSecurityDetailDto, UserLoginPolicyUpdateDto>('UserLoginPolicy', input)
  },
}
