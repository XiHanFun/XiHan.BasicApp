import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface AuditLogPageQueryDto extends PageRequest {
  auditTimeEnd?: DateTimeString | null
  auditTimeStart?: DateTimeString | null
  auditType?: string | null
  entityId?: string | null
  entityType?: string | null
  isSuccess?: boolean | null
  keyword?: string | null
  operationType?: number | null
  riskLevel?: number | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface AuditLogListItemDto extends BasicDto {
  auditTime: DateTimeString
  auditType: string
  createdTime: DateTimeString
  description?: string | null
  entityId?: string | null
  entityName?: string | null
  entityType?: string | null
  hasAfterData: boolean
  hasBeforeData: boolean
  hasChangedFields: boolean
  isSuccess: boolean
  operationIp?: string | null
  operationType: number
  requestId?: string | null
  riskLevel: number
  sessionId?: string | null
  tableName?: string | null
  tenantId?: ApiId | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}
