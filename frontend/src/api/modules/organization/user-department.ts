import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserDepartmentAssignDto,
  UserDepartmentListItemDto,
  UserDepartmentStatusUpdateDto,
  UserDepartmentUpdateDto,
} from './user-department.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  formatDynamicApiRouteValue,
} from '../../base'

const userDepartmentQueryApi = createDynamicApiClient('UserDepartmentQuery')
const userCommandApi = createDynamicApiClient('User')

/** 用户部门归属接口 */
export const userDepartmentApi = {
  assign(input: UserDepartmentAssignDto) {
    return userCommandApi.post<UserDepartmentListItemDto, UserDepartmentAssignDto>('UserDepartment', input)
  },
  departmentUsers(departmentId: ApiId, includeChildren = true, onlyValid = true) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'IncludeChildren', includeChildren)
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)
    return userDepartmentQueryApi.get<UserDepartmentListItemDto[]>(
      `DepartmentUsers/${formatDynamicApiRouteValue(departmentId)}`,
      params,
    )
  },
  listByUser(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)
    return userDepartmentQueryApi.get<UserDepartmentListItemDto[]>(
      `UserDepartments/${formatDynamicApiRouteValue(userId)}`,
      params,
    )
  },
  revoke(id: ApiId) {
    return userCommandApi.delete(`UserDepartment/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: UserDepartmentUpdateDto) {
    return userCommandApi.put<UserDepartmentListItemDto, UserDepartmentUpdateDto>('UserDepartment', input)
  },
  updateStatus(input: UserDepartmentStatusUpdateDto) {
    return userCommandApi.put<UserDepartmentListItemDto, UserDepartmentStatusUpdateDto>(
      'UserDepartmentStatus',
      input,
    )
  },
}
