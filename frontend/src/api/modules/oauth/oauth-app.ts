import type { PageResult } from '../../types'
import type {
  OAuthAppDetailDto,
  OAuthAppListItemDto,
  OAuthAppPageQueryDto,
} from './oauth-app.types'
import {
  appendDynamicApiParam,
  createDynamicApiClient,
  createPageRequestParams,
  createReadApi,
} from '../../base'

const oauthAppQueryApi = createDynamicApiClient('OAuthAppQuery')
const oauthAppReadApi = createReadApi<OAuthAppListItemDto, OAuthAppDetailDto, OAuthAppPageQueryDto>(
  'OAuthAppQuery',
  'OAuthApp',
)

export const oauthAppApi = {
  detail(id: OAuthAppDetailDto['basicId']) {
    return oauthAppReadApi.detail(id)
  },
  page(input: OAuthAppPageQueryDto) {
    return oauthAppQueryApi.get<PageResult<OAuthAppListItemDto>>(
      'OAuthAppPage',
      toOAuthAppPageParams(input),
    )
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
