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
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
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
    return oauthAppQueryApi.get<PageResult<OAuthAppListItemDto>>(
      'OAuthAppPage',
      toOAuthAppPageParams(input),
    )
  },
  // Commands
  create(input: OAuthAppCreateDto) {
    return oauthAppCommandApi.post<OAuthAppSecretDto, OAuthAppCreateDto>('CreateOAuthApp', input)
  },
  delete(id: OAuthAppDetailDto['basicId']) {
    return oauthAppCommandApi.delete(`DeleteOAuthApp/${formatDynamicApiRouteValue(id)}`)
  },
  regenerateSecret(id: OAuthAppDetailDto['basicId']) {
    return oauthAppCommandApi.post<OAuthAppSecretDto>(`RegenerateOAuthAppSecret/${formatDynamicApiRouteValue(id)}`)
  },
  update(input: OAuthAppUpdateDto) {
    return oauthAppCommandApi.put<OAuthAppDetailDto, OAuthAppUpdateDto>('UpdateOAuthApp', input)
  },
  updateStatus(input: OAuthAppStatusUpdateDto) {
    return oauthAppCommandApi.put<OAuthAppDetailDto, OAuthAppStatusUpdateDto>('UpdateOAuthAppStatus', input)
  },
}

function toOAuthAppPageParams(input: OAuthAppPageQueryDto) {
  const params = createPageRequestParams(input)

  appendDynamicApiParam(params, 'AppType', input.appType)
  appendDynamicApiParam(params, 'Keyword', input.keyword)
  appendDynamicApiParam(params, 'SkipConsent', input.skipConsent)
  appendDynamicApiParam(params, 'Status', input.status)

  return params
}
