import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserPermissionBatchUpdateDto,
  UserPermissionDetailDto,
  UserPermissionListItemDto,
  UserPermissionStatusUpdateDto,
  UserPermissionUpdateDto,
} from './user-permission.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const userPermissionQueryApi = createDynamicApiClient('UserPermissionQuery')
const userPermissionCommandApi = createDynamicApiClient('UserPermission')

export const userPermissionApi = {
  detail(id: ApiId) {
    return userPermissionQueryApi.get<UserPermissionDetailDto | null>(
      'UserPermissionDetail',
      { id },
    )
  },
  /** 批量提交直授改动（授予/拒绝/撤销一次性下发，后端单事务） */
  batchUpdate(input: UserPermissionBatchUpdateDto) {
    return userPermissionCommandApi.post<void, UserPermissionBatchUpdateDto>('BatchUpdateUserPermissions', input)
  },
  list(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return userPermissionQueryApi.get<UserPermissionListItemDto[]>(
      'UserPermissions',
      { ...params, userId },
    )
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
