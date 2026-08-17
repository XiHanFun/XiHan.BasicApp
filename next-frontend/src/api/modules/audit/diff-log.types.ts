import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum AuditRiskLevel {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  VeryHigh = 'VeryHigh',
  Critical = 'Critical',
}

/** 与后端 OperationType 一致 */
export enum AuditOperationType {
  Login = 'Login',
  Logout = 'Logout',
  Query = 'Query',
  Create = 'Create',
  Update = 'Update',
  Delete = 'Delete',
  Import = 'Import',
  Export = 'Export',
  Review = 'Review',
  Approve = 'Approve',
  StartTask = 'StartTask',
  Execute = 'Execute',
  Restore = 'Restore',
  Other = 'Other',
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
  afterData?: string | null
  beforeData?: string | null
  changeDescription?: string | null
  changedFields?: string | null
  createdTime: DateTimeString
  description?: string | null
  entityId?: string | null
  entityName?: string | null
  entityType?: string | null
  exceptionMessage?: string | null
  exceptionStackTrace?: string | null
  executionTime: NumericString
  extendData?: string | null
  isSuccess: boolean
  operationType: AuditOperationType
  operationIp?: string | null
  primaryKey?: string | null
  primaryKeyValue?: string | null
  remark?: string | null
  requestId?: string | null
  riskLevel: AuditRiskLevel
  sessionId?: string | null
  tableName?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface DiffLogDetailDto extends DiffLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
