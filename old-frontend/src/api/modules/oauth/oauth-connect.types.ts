/** OAuth 授权请求（同意页把 /connect/authorize 查询参数原样上送后端） */
export interface OAuthAuthorizeRequestDto {
  responseType?: string | null
  clientId?: string | null
  redirectUri?: string | null
  scope?: string | null
  state?: string | null
  codeChallenge?: string | null
  codeChallengeMethod?: string | null
}

/** OAuth 同意页预览（客户端信息 + 授权范围；非法请求携带错误码） */
export interface OAuthConsentPreviewDto {
  valid: boolean
  error?: string | null
  errorDescription?: string | null
  clientId?: string | null
  appName?: string | null
  appDescription?: string | null
  logo?: string | null
  homepage?: string | null
  skipConsent: boolean
  scopes: string[]
}

/** OAuth 同意结果（成功返回携带 code 的最终跳转地址） */
export interface OAuthConsentResultDto {
  success: boolean
  error?: string | null
  errorDescription?: string | null
  redirectUri?: string | null
}
