import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserPermissionDetailDto,
  UserPermissionGrantDto,
  UserPermissionListItemDto,
  UserPermissionStatusUpdateDto,
  UserPermissionUpdateDto,
} from './user-permission.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const userPermissionQueryApi = createDynamicApiClient('UserPermissionQuery')
const userPermissionCommandApi = createDynamicApiClient('UserPermission')

export const userPermissionApi = {
  detail(id: ApiId) {
    return userPermissionQueryApi.get<UserPermissionDetailDto | null>(
      `UserPermissionDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  grant(input: UserPermissionGrantDto) {
    return userPermissionCommandApi.post<UserPermissionDetailDto, UserPermissionGrantDto>('UserPermission', input)
  },
  list(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return userPermissionQueryApi.get<UserPermissionListItemDto[]>(
      `UserPermissions/${formatDynamicApiRouteValue(userId)}`,
      params,
    )
  },
  revoke(id: ApiId) {
    return userPermissionCommandApi.delete(`UserPermission/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: UserPermissionUpdateDto) {
    return userPermissionCommandApi.put<UserPermissionDetailDto, UserPermissionUpdateDto>('UserPermission', input)
  },
  updateStatus(input: UserPermissionStatusUpdateDto) {
    return userPermissionCommandApi.put<UserPermissionDetailDto, UserPermissionStatusUpdateDto>(
      'UserPermissionStatus',
      input,
    )
  },
}
