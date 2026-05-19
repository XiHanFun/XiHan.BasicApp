import type { UserLockUpdateDto, UserLoginPolicyUpdateDto, UserSecurityDetailDto } from './user-security.types'
import { createDynamicApiClient } from '../../base'

const userCommandApi = createDynamicApiClient('User')

/** 用户安全相关命令接口 */
export const userSecurityApi = {
  updateLock(input: UserLockUpdateDto) {
    return userCommandApi.put<UserSecurityDetailDto, UserLockUpdateDto>('UserLock', input)
  },
  updateLoginPolicy(input: UserLoginPolicyUpdateDto) {
    return userCommandApi.put<UserSecurityDetailDto, UserLoginPolicyUpdateDto>('UserLoginPolicy', input)
  },
}
