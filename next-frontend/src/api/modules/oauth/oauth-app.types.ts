import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum OAuthAppType {
  Web = 'Web',
  Mobile = 'Mobile',
  Desktop = 'Desktop',
  Service = 'Service',
}

export interface OAuthAppPageQueryDto extends PageRequest {
  appType?: OAuthAppType | null
  keyword?: string | null
  skipConsent?: boolean | null
  status?: EnableStatus | null
}

export interface OAuthAppListItemDto extends BasicDto {
  accessTokenLifetime: number
  appDescription?: string | null
  appName: string
  appType: OAuthAppType
  authorizationCodeLifetime: number
  clientId: string
  createdTime: DateTimeString
  grantTypes: string
  modifiedTime?: DateTimeString | null
  refreshTokenLifetime: number
  scopes?: string | null
  skipConsent: boolean
  status: EnableStatus
}

export interface OAuthAppDetailDto extends OAuthAppListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  homepage?: string | null
  logo?: string | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  redirectUris?: string | null
  remark?: string | null
}

// ---- Command DTOs ----

export interface OAuthAppCreateDto {
  appName: string
  appDescription?: string | null
  clientId: string
  clientSecret?: string | null
  appType: OAuthAppType
  grantTypes: string
  redirectUris?: string | null
  scopes?: string | null
  accessTokenLifetime: number
  refreshTokenLifetime: number
  authorizationCodeLifetime: number
  logo?: string | null
  homepage?: string | null
  skipConsent: boolean
  status: EnableStatus
  remark?: string | null
}

export interface OAuthAppUpdateDto extends BasicDto {
  appName: string
  appDescription?: string | null
  appType: OAuthAppType
  grantTypes: string
  redirectUris?: string | null
  scopes?: string | null
  accessTokenLifetime: number
  refreshTokenLifetime: number
  authorizationCodeLifetime: number
  logo?: string | null
  homepage?: string | null
  skipConsent: boolean
  remark?: string | null
}

export interface OAuthAppStatusUpdateDto {
  basicId: ApiId
  status: EnableStatus
  remark?: string | null
}

export interface OAuthAppSecretDto {
  basicId: ApiId
  clientId: string
  clientSecret: string
}
