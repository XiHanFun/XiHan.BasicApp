import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  UserCreateDto,
  UserDetailDto,
  UserListItemDto,
  UserPageQueryDto,
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
const userReadApi = createReadApi<UserListItemDto, UserDetailDto, UserPageQueryDto>('UserQuery', 'User')
const userBaseCommandApi = createCommandApi<UserCreateDto, UserUpdateDto, UserDetailDto>('User', 'User')

// 重置密码不在这里挂：后端实现在 UserSecurityAppService，返回 UserSecurityDetailDto，
// 唯一入口是 userSecurityApi.resetPassword（= userManagementApi.security.resetPassword）。
// 这里原先另有一条同端点、却声明返回 UserDetailDto 的重复方法，展开进 userManagementApi 后
// 形成两个语义不同的等价入口，已删除。
export const userApi = {
  create(input: UserCreateDto) {
    return userBaseCommandApi.create(input)
  },
  delete(id: ApiId) {
    return userCommandApi.delete('User', { id })
  },
  detail(id: ApiId) {
    return userReadApi.detail(id)
  },
  page(input: UserPageQueryDto) {
    return userQueryApi.post<PageResult<UserListItemDto>, UserPageQueryDto>('UserPage', input)
  },
  select(input: UserSelectQueryDto) {
    const params: DynamicApiParams = { Limit: input.limit }
    appendDynamicApiParam(params, 'Keyword', input.keyword)
    appendDynamicApiParam(params, 'Gender', input.gender)
    appendDynamicApiParam(params, 'IsSystemAccount', input.isSystemAccount)
    // 后端为 UserQueryService.GetEnabledUsersAsync(UserSelectQueryDto)：Get 前缀剥离 → GET /UserQuery/EnabledUsers，DTO 走 query 绑定
    return userQueryApi.get<UserSelectItemDto[]>('EnabledUsers', params)
  },
  update(input: UserUpdateDto) {
    return userBaseCommandApi.update(input)
  },
  updateStatus(input: UserStatusUpdateDto) {
    return userCommandApi.put<UserDetailDto, UserStatusUpdateDto>('UserStatus', input)
  },
}
