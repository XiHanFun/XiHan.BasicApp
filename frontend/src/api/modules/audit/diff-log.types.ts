import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export enum AuditRiskLevel {
  Low = 1,
  Medium = 2,
  High = 3,
  VeryHigh = 4,
  Critical = 5,
}

export enum AuditOperationType {
  Login = 0,
  Logout = 1,
  Query = 2,
  Create = 3,
  Update = 4,
  Delete = 5,
  Import = 6,
  Export = 7,
  Other = 99,
}

export interface DiffLogPageQueryDto extends PageRequest {
  auditTimeEnd?: DateTimeString | null
  auditTimeStart?: DateTimeString | null
  auditType?: string | null
  entityId?: string | null
  entityName?: string | null
  entityType?: string | null
  isSuccess?: boolean | null
  keyword?: string | null
  maxExecutionTime?: number | null
  minExecutionTime?: number | null
  operationType?: AuditOperationType | null
  requestId?: string | null
  riskLevel?: AuditRiskLevel | null
  sessionId?: string | null
  tableName?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface DiffLogListItemDto extends BasicDto {
  auditTime: DateTimeString
  auditType: string
  createdTime: DateTimeString
  entityId?: string | null
  entityName?: string | null
  entityType?: string | null
  executionTime: number
  isSuccess: boolean
  operationType: AuditOperationType
  primaryKey?: string | null
  primaryKeyValue?: string | null
  requestId?: string | null
  riskLevel: AuditRiskLevel
  sessionId?: string | null
  tableName?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}
