import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { DeviceType } from './user-session.types'

export interface OnlineUserPageQueryDto extends PageRequest {
  deviceType?: DeviceType
  keyword?: string
  userId?: ApiId
}

/** 一行 = 一个活跃会话（同一用户多端登录会出现多行） */
export interface OnlineUserListItemDto extends BasicDto {
  avatar?: string | null
  browser?: string | null
  deviceName?: string | null
  deviceType: DeviceType
  ipAddressMasked?: string | null
  /** 是否持有实时（SignalR）连接 */
  isRealtimeOnline: boolean
  lastActivityTime: DateTimeString
  location?: string | null
  loginTime: DateTimeString
  nickName?: string | null
  /** 本次会话在线时长（秒，登录时间至今） */
  onlineDurationSeconds: number
  operatingSystem?: string | null
  userId: ApiId
  userName?: string | null
  userSessionId: string
}

export interface OnlineUserSummaryDto {
  /** 活跃会话数（当前租户范围） */
  activeSessions: number
  /** 活跃用户数（活跃会话去重用户数） */
  activeUsers: number
  /** 实时在线用户数（持有 SignalR 连接的去重用户数） */
  realtimeOnlineUsers: number
}
