import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserDataScopeDetailDto,
  UserDataScopeListItemDto,
  UserDataScopeSetDto,
  UserDataScopeSettingDto,
} from './user-data-scope.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const userDataScopeQueryApi = createDynamicApiClient('UserDataScopeQuery')
const userDataScopeCommandApi = createDynamicApiClient('UserDataScope')

export const userDataScopeApi = {
  detail(id: ApiId) {
    return userDataScopeQueryApi.get<UserDataScopeDetailDto | null>(
      'UserDataScopeDetail',
      { id },
    )
  },
  list(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return userDataScopeQueryApi.get<UserDataScopeListItemDto[]>(
      'UserDataScopes',
      { ...params, userId },
    )
  },
  /** 覆盖档位与自定义部门一次提交（单事务） */
  set(input: UserDataScopeSetDto) {
    return userDataScopeCommandApi.post<void, UserDataScopeSetDto>('SetUserDataScope', input)
  },
  /** 成员在本租户的数据范围设置（覆盖档位 + 当前生效的自定义部门） */
  setting(userId: ApiId) {
    return userDataScopeQueryApi.get<UserDataScopeSettingDto>('UserDataScopeSetting', { userId })
  },
}
