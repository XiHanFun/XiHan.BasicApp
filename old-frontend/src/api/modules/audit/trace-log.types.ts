import type { ApiId, DateTimeString, NumericString } from '../../types'

/** 追踪维度（与后端 JsonStringEnumConverter 序列化值一致） */
export enum TraceDimension {
  UserName = 'UserName',
  SessionId = 'SessionId',
  TraceId = 'TraceId',
  Ip = 'Ip',
  UserId = 'UserId',
}

/** 追踪日志类型（与后端 JsonStringEnumConverter 序列化值一致） */
export enum TraceLogType {
  Access = 'Access',
  Api = 'Api',
  Operation = 'Operation',
  Login = 'Login',
  Exception = 'Exception',
  Diff = 'Diff',
  PermissionChange = 'PermissionChange',
}

export interface TraceTimelineQueryDto {
  dimension: TraceDimension
  endTime?: DateTimeString | null
  logTypes?: TraceLogType[] | null
  maxPerType?: number | null
  startTime?: DateTimeString | null
  value: string
}

export interface TraceTimelineItemDto {
  basicId: ApiId
  executionTime?: NumericString | null
  ip?: string | null
  location?: string | null
  logType: TraceLogType
  method?: string | null
  path?: string | null
  sessionId?: string | null
  status: string
  statusCode?: number | null
  summary?: string | null
  time: DateTimeString
  title?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface TraceTimelineResultDto {
  items: TraceTimelineItemDto[]
  totalCount: number
  truncated: boolean
  typeCounts: Record<string, number>
}
