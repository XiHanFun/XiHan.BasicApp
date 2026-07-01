import type {
  OAuthAuthorizeRequestDto,
  OAuthConsentPreviewDto,
  OAuthConsentResultDto,
} from './oauth-connect.types'
import { createDynamicApiClient } from '../../base'

// 与后端 OAuthConsentAppService（RouteTemplate=api/OAuthConnect）对应
const oauthConnectApi = createDynamicApiClient('OAuthConnect')

export const oauthConsentApi = {
  // 预览：校验授权请求并返回客户端信息与授权范围（POST，携带完整授权参数）
  resolve(input: OAuthAuthorizeRequestDto) {
    return oauthConnectApi.post<OAuthConsentPreviewDto, OAuthAuthorizeRequestDto>('ResolveAuthorization', input)
  },
  // 同意：创建授权码并返回携带 code 的最终跳转地址
  authorize(input: OAuthAuthorizeRequestDto) {
    return oauthConnectApi.post<OAuthConsentResultDto, OAuthAuthorizeRequestDto>('Authorize', input)
  },
}
