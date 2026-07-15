// ==================== 通用类型 ====================
//
// 这里只放**当前真有消费者**的通用类型。
// 分页契约不在此处：真源是 `~/types/contracts` 的 PageResult（{ items, page: PageResultMetadata }，
// 与后端 PageResultDtoBase 对齐）。本文件曾有一份遗留的 PageResult（{ items, total, page, pageSize }），
// 形状已与后端分叉，且因 `packages/types/index.ts` 导出 './common' 而不导出 './contracts'，
// `import { PageResult } from '~/types'` 拿到的正是错的那份——已删除，勿再添加。

export interface ApiResponse<T = unknown> {
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
