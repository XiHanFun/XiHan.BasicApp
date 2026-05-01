import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export enum OAuthAppType {
  Web = 0,
  Mobile = 1,
  Desktop = 2,
  Service = 3,
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
