import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum DeviceType {
  Unknown = 0,
  Web = 1,
  IOS = 2,
  Android = 3,
  Windows = 4,
  MacOS = 5,
  Linux = 6,
  Tablet = 7,
  MiniProgram = 8,
  Api = 9,
}

export interface UserSessionPageQueryDto extends PageRequest {
  deviceType?: DeviceType
  isOnline?: boolean
  isRevoked?: boolean
  keyword?: string
  lastActivityTimeEnd?: DateTimeString
  lastActivityTimeStart?: DateTimeString
  loginTimeEnd?: DateTimeString
  loginTimeStart?: DateTimeString
  userId?: ApiId
}

export interface UserSessionListItemDto extends BasicDto {
  browser?: string | null
  createdTime: DateTimeString
  deviceIdMasked?: string | null
  deviceName?: string | null
  deviceType: DeviceType
  expiresAt?: DateTimeString | null
  ipAddressMasked?: string | null
  isExpired: boolean
  isOnline: boolean
  isRevoked: boolean
  lastActivityTime: DateTimeString
  loginTime: DateTimeString
  logoutTime?: DateTimeString | null
  modifiedTime?: DateTimeString | null
  nickName?: string | null
  operatingSystem?: string | null
  realName?: string | null
  revokedAt?: DateTimeString | null
  userId: ApiId
  userName?: string | null
  userSessionId: string
}

export interface UserSessionDetailDto extends UserSessionListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  revokedReason?: string | null
}

export interface UserSessionRevokeDto extends BasicDto {
  reason: string
}

export interface UserSessionsRevokeDto {
  reason: string
  userId: ApiId
}
