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
  createPageRequestParams,
  createReadApi,
} from '../../base'

const userQueryApi = createDynamicApiClient('UserQuery')
const userCommandApi = createDynamicApiClient('User')
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
    return userQueryApi.get<PageResult<UserListItemDto>>('UserPage', toUserPageParams(input))
  },
  resetPassword(input: UserPasswordResetDto) {
    return userCommandApi.put<UserDetailDto, UserPasswordResetDto>('UserPassword', input)
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

function toUserPageParams(input: UserPageQueryDto) {
  const params = createPageRequestParams(input)
  appendDynamicApiParam(params, 'Country', input.country)
  appendDynamicApiParam(params, 'Gender', input.gender)
  appendDynamicApiParam(params, 'IsSystemAccount', input.isSystemAccount)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'Language', input.language)
  appendDynamicApiParam(params, 'Status', input.status)
  return params
}
