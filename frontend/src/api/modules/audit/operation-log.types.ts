import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

export enum OperationType {
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

export interface OperationLogPageQueryDto extends PageRequest {
  function?: string | null
  keyword?: string | null
  maxExecutionTime?: number | null
  method?: string | null
  minExecutionTime?: number | null
  module?: string | null
  operationTimeEnd?: DateTimeString | null
  operationTimeStart?: DateTimeString | null
  operationType?: OperationType | null
  sessionId?: string | null
  status?: EnableStatus | null
  title?: string | null
  traceId?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface OperationLogListItemDto extends BasicDto {
  browser?: string | null
  createdTime: DateTimeString
  description?: string | null
  errorMessage?: string | null
  executionTime: number
  function?: string | null
  method?: string | null
  module?: string | null
  operationIp?: string | null
  operationLocation?: string | null
  operationTime: DateTimeString
  operationType: OperationType
  os?: string | null
  requestUrl?: string | null
  sessionId?: string | null
  status: EnableStatus
  title?: string | null
  traceId?: string | null
  userAgent?: string | null
  userId?: ApiId | null
  userName?: string | null
}

export interface OperationLogDetailDto extends OperationLogListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
}
