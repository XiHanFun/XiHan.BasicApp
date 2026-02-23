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
  code: number
  message: string
  data: T
  success: boolean
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
