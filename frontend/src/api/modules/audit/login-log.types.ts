import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum LoginResult {
  Success = 0,
  InvalidCredentials = 1,
  AccountLocked = 2,
  AccountDisabled = 3,
  RequiresTwoFactor = 4,
  TwoFactorFailed = 5,
  Failed = 99,
}

export interface LoginLogPageQueryDto extends PageRequest {
  isRiskLogin?: boolean | null
  keyword?: string | null
  loginResult?: LoginResult | null
  loginTimeEnd?: DateTimeString | null
  loginTimeStart?: DateTimeString | null
  sessionId?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface LoginLogListItemDto extends BasicDto {
  createdTime: DateTimeString
  isRiskLogin: boolean
  loginResult: LoginResult
  loginTime: DateTimeString
  sessionId?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}
