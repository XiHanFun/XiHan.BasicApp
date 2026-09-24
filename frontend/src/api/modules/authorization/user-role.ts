import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RoleMemberBatchUpdateDto,
  RoleMemberDto,
  UserRoleBatchUpdateDto,
  UserRoleDetailDto,
  UserRoleListItemDto,
  UserRoleStatusUpdateDto,
  UserRoleUpdateDto,
} from './user-role.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const userRoleQueryApi = createDynamicApiClient('UserRoleQuery')
const userRoleCommandApi = createDynamicApiClient('UserRole')

export const userRoleApi = {
  detail(id: ApiId) {
    return userRoleQueryApi.get<UserRoleDetailDto | null>('UserRoleDetail', { id })
  },
  /** 批量提交角色直授改动（授予/撤销一次性下发，后端单事务） */
  batchUpdate(input: UserRoleBatchUpdateDto) {
    return userRoleCommandApi.post<void, UserRoleBatchUpdateDto>('BatchUpdateUserRoles', input)
  },
  /** 以角色为中心批量维护成员（加入/移出一次性下发，后端单事务） */
  batchUpdateRoleMembers(input: RoleMemberBatchUpdateDto) {
    return userRoleCommandApi.post<void, RoleMemberBatchUpdateDto>('BatchUpdateRoleMembers', input)
  },
  /** 角色在本租户此刻生效的成员 */
  roleMembers(roleId: ApiId) {
    return userRoleQueryApi.get<RoleMemberDto[]>('RoleMembers', { roleId })
  },
  list(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return userRoleQueryApi.get<UserRoleListItemDto[]>('UserRoles', { ...params, userId })
  },
  update(input: UserRoleUpdateDto) {
    return userRoleCommandApi.put<UserRoleDetailDto, UserRoleUpdateDto>('UserRole', input)
  },
  updateStatus(input: UserRoleStatusUpdateDto) {
    return userRoleCommandApi.put<UserRoleDetailDto, UserRoleStatusUpdateDto>('UserRoleStatus', input)
  },
}
