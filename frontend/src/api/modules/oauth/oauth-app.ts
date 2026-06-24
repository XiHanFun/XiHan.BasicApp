import type { PageResult } from '../../types'
import type {
  OAuthAppCreateDto,
  OAuthAppDetailDto,
  OAuthAppListItemDto,
  OAuthAppPageQueryDto,
  OAuthAppSecretDto,
  OAuthAppStatusUpdateDto,
  OAuthAppUpdateDto,
} from './oauth-app.types'
import {
  createDynamicApiClient,
  createReadApi,
  formatDynamicApiRouteValue,
} from '../../base'

const oauthAppQueryApi = createDynamicApiClient('OAuthAppQuery')
const oauthAppCommandApi = createDynamicApiClient('OAuthApp')
const oauthAppReadApi = createReadApi<OAuthAppListItemDto, OAuthAppDetailDto, OAuthAppPageQueryDto>(
  'OAuthAppQuery',
  'OAuthApp',
)

export const oauthAppApi = {
  // Query
  detail(id: OAuthAppDetailDto['basicId']) {
    return oauthAppReadApi.detail(id)
  },
  page(input: OAuthAppPageQueryDto) {
    return oauthAppQueryApi.post<PageResult<OAuthAppListItemDto>>('OAuthAppPage', input)
  },
  // Commands
  create(input: OAuthAppCreateDto) {
    return oauthAppCommandApi.post<OAuthAppSecretDto, OAuthAppCreateDto>('OAuthApp', input)
  },
  delete(id: OAuthAppDetailDto['basicId']) {
    return oauthAppCommandApi.delete(`OAuthApp/${formatDynamicApiRouteValue(id)}`)
  },
  regenerateSecret(id: OAuthAppDetailDto['basicId']) {
    return oauthAppCommandApi.post<OAuthAppSecretDto>(`RegenerateOAuthAppSecret/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: OAuthAppUpdateDto) {
    return oauthAppCommandApi.put<OAuthAppDetailDto, OAuthAppUpdateDto>('OAuthApp', input)
  },
  updateStatus(input: OAuthAppStatusUpdateDto) {
    return oauthAppCommandApi.put<OAuthAppDetailDto, OAuthAppStatusUpdateDto>('OAuthAppStatus', input)
  },
}
