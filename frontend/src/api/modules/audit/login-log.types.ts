import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum LoginResult {
  Success = 'Success',
  InvalidCredentials = 'InvalidCredentials',
  AccountLocked = 'AccountLocked',
  AccountDisabled = 'AccountDisabled',
  RequiresTwoFactor = 'RequiresTwoFactor',
  TwoFactorFailed = 'TwoFactorFailed',
  Logout = 'Logout',
  Failed = 'Failed',
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
  browser?: string | null
  createdTime: DateTimeString
  device?: string | null
  deviceId?: string | null
  isRiskLogin: boolean
  loginIp?: string | null
  loginLocation?: string | null
  loginResult: LoginResult
  loginTime: DateTimeString
  message?: string | null
  os?: string | null
  sessionId?: string | null
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface LoginLogDetailDto extends LoginLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
