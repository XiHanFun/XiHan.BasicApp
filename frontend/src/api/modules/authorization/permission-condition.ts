import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  PermissionConditionCreateDto,
  PermissionConditionDetailDto,
  PermissionConditionListItemDto,
  PermissionConditionStatusUpdateDto,
  PermissionConditionUpdateDto,
} from './permission-condition.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const permissionConditionQueryApi = createDynamicApiClient('PermissionConditionQuery')
const permissionConditionCommandApi = createDynamicApiClient('PermissionCondition')

function buildOnlyValidParams(onlyValid: boolean) {
  const params: DynamicApiParams = {}
  appendDynamicApiParam(params, 'OnlyValid', onlyValid)
  return params
}

export const permissionConditionApi = {
  create(input: PermissionConditionCreateDto) {
    return permissionConditionCommandApi.post<PermissionConditionDetailDto, PermissionConditionCreateDto>(
      'PermissionCondition',
      input,
    )
  },
  delete(id: ApiId) {
    return permissionConditionCommandApi.delete(`PermissionCondition/${formatDynamicApiRouteValue(id)}`)
  },
  detail(id: ApiId) {
    return permissionConditionQueryApi.get<PermissionConditionDetailDto | null>(
      `PermissionConditionDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  rolePermissionConditions(rolePermissionId: ApiId, onlyValid = false) {
    return permissionConditionQueryApi.get<PermissionConditionListItemDto[]>(
      `RolePermissionConditions/${formatDynamicApiRouteValue(rolePermissionId)}`,
      buildOnlyValidParams(onlyValid),
    )
  },
  update(input: PermissionConditionUpdateDto) {
    return permissionConditionCommandApi.put<PermissionConditionDetailDto, PermissionConditionUpdateDto>(
      'PermissionCondition',
      input,
    )
  },
  updateStatus(input: PermissionConditionStatusUpdateDto) {
    return permissionConditionCommandApi.put<PermissionConditionDetailDto, PermissionConditionStatusUpdateDto>(
      'PermissionConditionStatus',
      input,
    )
  },
  userPermissionConditions(userPermissionId: ApiId, onlyValid = false) {
    return permissionConditionQueryApi.get<PermissionConditionListItemDto[]>(
      `UserPermissionConditions/${formatDynamicApiRouteValue(userPermissionId)}`,
      buildOnlyValidParams(onlyValid),
    )
  },
}
