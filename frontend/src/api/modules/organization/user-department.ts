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
// 命令端控制器为 UserDepartmentAppService → 动态 API 控制器名 "UserDepartment"（此前误写为 "User" 导致 404）
const userDepartmentCommandApi = createDynamicApiClient('UserDepartment')

/** 用户部门归属接口 */
export const userDepartmentApi = {
  assign(input: UserDepartmentAssignDto) {
    return userDepartmentCommandApi.post<UserDepartmentListItemDto, UserDepartmentAssignDto>('UserDepartment', input)
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
    return userDepartmentCommandApi.delete(`UserDepartment/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: UserDepartmentUpdateDto) {
    return userDepartmentCommandApi.put<UserDepartmentListItemDto, UserDepartmentUpdateDto>('UserDepartment', input)
  },
  updateStatus(input: UserDepartmentStatusUpdateDto) {
    return userDepartmentCommandApi.put<UserDepartmentListItemDto, UserDepartmentStatusUpdateDto>(
      'UserDepartmentStatus',
      input,
    )
  },
}
