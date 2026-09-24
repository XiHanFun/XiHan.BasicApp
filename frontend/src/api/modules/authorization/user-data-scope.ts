import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserDataScopeBatchUpdateDto,
  UserDataScopeDetailDto,
  UserDataScopeListItemDto,
  UserDataScopeStatusUpdateDto,
  UserDataScopeUpdateDto,
} from './user-data-scope.types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const userDataScopeQueryApi = createDynamicApiClient('UserDataScopeQuery')
const userDataScopeCommandApi = createDynamicApiClient('UserDataScope')

export const userDataScopeApi = {
  /** 一次性提交本次授予与撤销（单事务） */
  batchUpdate(input: UserDataScopeBatchUpdateDto) {
    return userDataScopeCommandApi.post<void, UserDataScopeBatchUpdateDto>('BatchUpdateUserDataScopes', input)
  },
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
  update(input: UserDataScopeUpdateDto) {
    return userDataScopeCommandApi.put<UserDataScopeDetailDto, UserDataScopeUpdateDto>('UserDataScope', input)
  },
  updateStatus(input: UserDataScopeStatusUpdateDto) {
    return userDataScopeCommandApi.put<UserDataScopeDetailDto, UserDataScopeStatusUpdateDto>(
      'UserDataScopeStatus',
      input,
    )
  },
}
