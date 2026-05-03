import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface LoginLogPageQueryDto extends PageRequest {
  keyword?: string | null
  loginResult?: string | null
  loginTimeEnd?: DateTimeString | null
  loginTimeStart?: DateTimeString | null
  loginType?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface LoginLogListItemDto extends BasicDto {
  browser?: string | null
  createdTime: DateTimeString
  device?: string | null
  failReason?: string | null
  loginIp?: string | null
  loginLocation?: string | null
  loginResult: string
  loginTime: DateTimeString
  loginType: string
  logoutTime?: DateTimeString | null
  os?: string | null
  sessionDuration?: number | null
  tenantId?: ApiId | null
  userId?: ApiId | null
  userName?: string | null
}
