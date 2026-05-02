import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RolePermissionDetailDto,
  RolePermissionGrantDto,
  RolePermissionListItemDto,
  RolePermissionStatusUpdateDto,
  RolePermissionUpdateDto,
} from './role-permission.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const rolePermissionQueryApi = createDynamicApiClient('RolePermissionQuery')
const rolePermissionCommandApi = createDynamicApiClient('RolePermission')

export const rolePermissionApi = {
  detail(id: ApiId) {
    return rolePermissionQueryApi.get<RolePermissionDetailDto | null>(
      `RolePermissionDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  grant(input: RolePermissionGrantDto) {
    return rolePermissionCommandApi.post<RolePermissionDetailDto, RolePermissionGrantDto>('RolePermission', input)
  },
  list(roleId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return rolePermissionQueryApi.get<RolePermissionListItemDto[]>(
      `RolePermissions/${formatDynamicApiRouteValue(roleId)}`,
      params,
    )
  },
  revoke(id: ApiId) {
    return rolePermissionCommandApi.delete(`RolePermission/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: RolePermissionUpdateDto) {
    return rolePermissionCommandApi.put<RolePermissionDetailDto, RolePermissionUpdateDto>('RolePermission', input)
  },
  updateStatus(input: RolePermissionStatusUpdateDto) {
    return rolePermissionCommandApi.put<RolePermissionDetailDto, RolePermissionStatusUpdateDto>(
      'RolePermissionStatus',
      input,
    )
  },
}
