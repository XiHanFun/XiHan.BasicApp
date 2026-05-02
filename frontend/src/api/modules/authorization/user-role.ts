import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserRoleDetailDto,
  UserRoleGrantDto,
  UserRoleListItemDto,
  UserRoleStatusUpdateDto,
  UserRoleUpdateDto,
} from './user-role.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const userRoleQueryApi = createDynamicApiClient('UserRoleQuery')
const userRoleCommandApi = createDynamicApiClient('UserRole')

export const userRoleApi = {
  detail(id: ApiId) {
    return userRoleQueryApi.get<UserRoleDetailDto | null>(`UserRoleDetail/${formatDynamicApiRouteValue(id)}`)
  },
  grant(input: UserRoleGrantDto) {
    return userRoleCommandApi.post<UserRoleDetailDto, UserRoleGrantDto>('UserRole', input)
  },
  list(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return userRoleQueryApi.get<UserRoleListItemDto[]>(`UserRoles/${formatDynamicApiRouteValue(userId)}`, params)
  },
  revoke(id: ApiId) {
    return userRoleCommandApi.delete(`UserRole/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: UserRoleUpdateDto) {
    return userRoleCommandApi.put<UserRoleDetailDto, UserRoleUpdateDto>('UserRole', input)
  },
  updateStatus(input: UserRoleStatusUpdateDto) {
    return userRoleCommandApi.put<UserRoleDetailDto, UserRoleStatusUpdateDto>('UserRoleStatus', input)
  },
}
