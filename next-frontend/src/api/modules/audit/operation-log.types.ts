import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'

export { EnableStatus } from '../shared'

/** 与后端 OperationType 一致 */
export enum OperationType {
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

/** 与后端 OperationExecuteResult 一致（操作执行结果：成功/失败/部分成功） */
export enum OperationExecuteResult {
  Success = 'Success',
  Failed = 'Failed',
  PartialSuccess = 'PartialSuccess',
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
  result?: OperationExecuteResult | null
  sessionId?: string | null
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
  executionTime: NumericString
  function?: string | null
  method?: string | null
  module?: string | null
  operationIp?: string | null
  operationLocation?: string | null
  operationTime: DateTimeString
  operationType: OperationType
  os?: string | null
  requestUrl?: string | null
  result: OperationExecuteResult
  sessionId?: string | null
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
