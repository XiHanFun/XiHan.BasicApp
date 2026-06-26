import type { DynamicApiParams } from '../../base'
import type { ApiId } from '../../types'
import type {
  UserDataScopeDetailDto,
  UserDataScopeGrantDto,
  UserDataScopeListItemDto,
  UserDataScopeStatusUpdateDto,
  UserDataScopeUpdateDto,
} from './user-data-scope.types'
import { appendDynamicApiParam, createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const userDataScopeQueryApi = createDynamicApiClient('UserDataScopeQuery')
const userDataScopeCommandApi = createDynamicApiClient('UserDataScope')

export const userDataScopeApi = {
  detail(id: ApiId) {
    return userDataScopeQueryApi.get<UserDataScopeDetailDto | null>(
      `UserDataScopeDetail/${formatDynamicApiRouteValue(id)}`,
    )
  },
  grant(input: UserDataScopeGrantDto) {
    return userDataScopeCommandApi.post<UserDataScopeDetailDto, UserDataScopeGrantDto>('UserDataScope', input)
  },
  list(userId: ApiId, onlyValid = false) {
    const params: DynamicApiParams = {}
    appendDynamicApiParam(params, 'OnlyValid', onlyValid)

    return userDataScopeQueryApi.get<UserDataScopeListItemDto[]>(
      `UserDataScopes/${formatDynamicApiRouteValue(userId)}`,
      params,
    )
  },
  revoke(id: ApiId) {
    return userDataScopeCommandApi.delete(`UserDataScope/${formatDynamicApiRouteValue(id)}`)
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
