import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum DeviceType {
  Unknown = 'Unknown',
  Web = 'Web',
  iOS = 'iOS',
  Android = 'Android',
  Windows = 'Windows',
  macOS = 'macOS',
  Linux = 'Linux',
  Tablet = 'Tablet',
  MiniProgram = 'MiniProgram',
  Api = 'Api',
}

/** 会话状态（与后端 SessionStatus 枚举序列化值一致） */
export enum SessionStatus {
  Active = 'Active',
  Offline = 'Offline',
  Revoked = 'Revoked',
  Expired = 'Expired',
}

export interface UserSessionPageQueryDto extends PageRequest {
  deviceType?: DeviceType
  keyword?: string
  lastActivityTimeEnd?: DateTimeString
  lastActivityTimeStart?: DateTimeString
  loginTimeEnd?: DateTimeString
  loginTimeStart?: DateTimeString
  status?: SessionStatus
  userId?: ApiId
}

export interface UserSessionListItemDto extends BasicDto {
  browser?: string | null
  createdTime: DateTimeString
  deviceIdMasked?: string | null
  deviceName?: string | null
  deviceType: DeviceType
  expirationTime?: DateTimeString | null
  ipAddressMasked?: string | null
  isExpired: boolean
  lastActivityTime: DateTimeString
  loginTime: DateTimeString
  logoutTime?: DateTimeString | null
  modifiedTime?: DateTimeString | null
  nickName?: string | null
  operatingSystem?: string | null
  realName?: string | null
  revokedTime?: DateTimeString | null
  status: SessionStatus
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
