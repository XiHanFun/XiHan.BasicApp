import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  RolePermissionBatchUpdateDto,
  RolePermissionDetailDto,
  RolePermissionListItemDto,
  RolePermissionStatusUpdateDto,
  RolePermissionUpdateDto,
} from './role-permission.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const rolePermissionQueryApi = createDynamicApiClient('RolePermissionQuery')
const rolePermissionCommandApi = createDynamicApiClient('Role')

export const rolePermissionApi = {
  detail(id: ApiId) {
    return rolePermissionQueryApi.get<RolePermissionDetailDto | null>(
      'RolePermissionDetail',
      { id },
    )
  },
  batchUpdate(input: RolePermissionBatchUpdateDto) {
    return rolePermissionCommandApi.post<void, RolePermissionBatchUpdateDto>('BatchUpdateRolePermissions', input)
  },
  list(roleId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return rolePermissionQueryApi.get<RolePermissionListItemDto[]>(
      'RolePermissions',
      { ...params, roleId },
    )
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
