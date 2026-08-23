import type { ApiId, PageResult } from '../../types'
import type {
  UserSessionDetailDto,
  UserSessionListItemDto,
  UserSessionPageQueryDto,
  UserSessionRevokeDto,
  UserSessionsRevokeDto,
} from './user-session.types'
import { createDynamicApiClient, createReadApi } from '../../base'

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
    return userSessionQueryApi.post<PageResult<UserSessionListItemDto>>('UserSessionPage', input)
  },
  revokeSession(input: UserSessionRevokeDto) {
    // Revoke 前缀不在动态 API 动词表内：路由保留完整方法名（POST RevokeUserSession）
    return userSessionCommandApi.post<UserSessionDetailDto, UserSessionRevokeDto>(
      'RevokeUserSession',
      input,
    )
  },
  revokeUserSessions(input: UserSessionsRevokeDto) {
    return userSessionCommandApi.post<number, UserSessionsRevokeDto>('RevokeUserSessions', input)
  },
}
