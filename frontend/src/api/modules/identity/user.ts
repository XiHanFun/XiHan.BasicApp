import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  UserCreateDto,
  UserDetailDto,
  UserListItemDto,
  UserPageQueryDto,
  UserPasswordResetDto,
  UserSelectItemDto,
  UserSelectQueryDto,
  UserStatusUpdateDto,
  UserUpdateDto,
} from './user.types'
import {
  appendDynamicApiParam,
  createCommandApi,
  createDynamicApiClient,
  createReadApi,
} from '../../base'

const userQueryApi = createDynamicApiClient('UserQuery')
const userCommandApi = createDynamicApiClient('User')
// 安全类命令在 UserSecurityAppService（控制器 UserSecurity）
const userSecurityCommandApi = createDynamicApiClient('UserSecurity')
const userReadApi = createReadApi<UserListItemDto, UserDetailDto, UserPageQueryDto>('UserQuery', 'User')
const userBaseCommandApi = createCommandApi<UserCreateDto, UserUpdateDto, UserDetailDto>('User', 'User')

export const userApi = {
  create(input: UserCreateDto) {
    return userBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return userCommandApi.delete(`User/${id}`)
  },
  detail(id: ApiId) {
    return userReadApi.detail(id)
  },
  page(input: UserPageQueryDto) {
    return userQueryApi.post<PageResult<UserListItemDto>, UserPageQueryDto>('UserPage', input)
  },
  resetPassword(input: UserPasswordResetDto) {
    // 后端为 UserSecurityAppService.ResetUserPasswordAsync：Reset 前缀不剥离、动词 POST
    return userSecurityCommandApi.post<UserDetailDto, UserPasswordResetDto>('ResetUserPassword', input)
  },
  select(input: UserSelectQueryDto) {
    const params: DynamicApiParams = { Limit: input.limit }
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    appendDynamicApiParam(params, 'Gender', input.gender)
    appendDynamicApiParam(params, 'IsSystemAccount', input.isSystemAccount)
    return userQueryApi.get<UserSelectItemDto[]>('UserSelect', params)
  },
  update(input: UserUpdateDto) {
    return userBaseCommandApi.update(input)
  },
  updateStatus(input: UserStatusUpdateDto) {
    return userCommandApi.put<UserDetailDto, UserStatusUpdateDto>('UserStatus', input)
  },
}
