import type { DynamicApiParams } from '../../base'
import type { ApiId, PageResult } from '../../types'
import type {
  UserSessionDetailDto,
  UserSessionListItemDto,
  UserSessionPageQueryDto,
  UserSessionRevokeDto,
  UserSessionsRevokeDto,
} from './user-session.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const userSessionQueryApi = createDynamicApiClient('UserSessionQuery')
const userSessionCommandApi = createDynamicApiClient('UserSession')
const userSessionReadApi = createReadApi<
  UserSessionListItemDto,
  UserSessionDetailDto,
  UserSessionPageQueryDto
>('UserSessionQuery', 'UserSession')

export const userSessionApi = {
  detail(id: ApiId) {
    return userSessionReadApi.detail(id)
  },
  page(input: UserSessionPageQueryDto) {
    return userSessionQueryApi.get<PageResult<UserSessionListItemDto>>(
      'UserSessionPage',
      toUserSessionPageParams(input),
    )
  },
  revokeSession(input: UserSessionRevokeDto) {
    return userSessionCommandApi.post<UserSessionDetailDto, UserSessionRevokeDto>(
      'UserSession',
      input,
    )
  },
  revokeUserSessions(input: UserSessionsRevokeDto) {
    return userSessionCommandApi.post<number, UserSessionsRevokeDto>('UserSessions', input)
  },
}

function toUserSessionPageParams(input: UserSessionPageQueryDto) {
  const params: DynamicApiParams = createPageRequestParams(input)

  appendDynamicApiParam(params, 'DeviceType', input.deviceType)
  appendDynamicApiParam(params, 'IsOnline', input.isOnline)
  appendDynamicApiParam(params, 'IsRevoked', input.isRevoked)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'LastActivityTimeEnd', input.lastActivityTimeEnd)
  appendDynamicApiParam(params, 'LastActivityTimeStart', input.lastActivityTimeStart)
  appendDynamicApiParam(params, 'LoginTimeEnd', input.loginTimeEnd)
  appendDynamicApiParam(params, 'LoginTimeStart', input.loginTimeStart)
  appendDynamicApiParam(params, 'UserId', input.userId)

  return params
}
