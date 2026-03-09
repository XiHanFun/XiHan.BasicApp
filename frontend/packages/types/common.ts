// ==================== 通用类型 ====================

export interface Recordable<T = any> {
  [key: string]: T
}

export interface PageResult<T = any> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ApiResponse<T = any> {
  code: number | string
  message: string
  data: T
  isSuccess: boolean
  traceId?: string
  timestamp?: string
}

export type FrontendRequestLogStatus = 'pending' | 'success' | 'error'

export interface FrontendRequestLog {
  requestId: string
  method: string
  url: string
  startedAt: number
  finishedAt?: number
  duration?: number
  status: FrontendRequestLogStatus
  statusCode?: number
  responseCode?: number | string
  message?: string
  traceId?: string
}

export interface SelectOption {
  label: string
  value: string | number
  disabled?: boolean
}

// ==================== 分页查询参数 ====================

export interface PageQuery {
  page?: number
  pageSize?: number
  keyword?: string
}
