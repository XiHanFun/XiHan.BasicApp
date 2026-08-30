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
    return oauthAppCommandApi.delete('OAuthApp', { id })
  },
  regenerateSecret(id: OAuthAppDetailDto['basicId']) {
    // RegenerateOAuthAppSecretAsync(long id)：简单类型参数被动态 API 推断为 query，
    // 放进请求体绑不上（会静默变成 0），故 id 走 query
    return oauthAppCommandApi.post<OAuthAppSecretDto>('RegenerateOAuthAppSecret', undefined, { params: { id } })
  },
  update(input: OAuthAppUpdateDto) {
    return oauthAppCommandApi.put<OAuthAppDetailDto, OAuthAppUpdateDto>('OAuthApp', input)
  },
  updateStatus(input: OAuthAppStatusUpdateDto) {
    return oauthAppCommandApi.put<OAuthAppDetailDto, OAuthAppStatusUpdateDto>('OAuthAppStatus', input)
  },
}
