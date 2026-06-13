import type { ApiId, PageResult } from '../../types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

/** 导出任务状态（与后端 ExportTaskStatus 对齐） */
export enum ExportTaskStatus {
  Pending = 0,
  Processing = 1,
  Success = 2,
  Failed = 3,
}

/** 导出范围（与后端 ExportScope 对齐） */
export enum ExportScope {
  CurrentPage = 0,
  SearchResult = 1,
  All = 2,
}

/** 导出格式（与后端 ExportFormat 对齐） */
export enum ExportFormat {
  Csv = 0,
  Xlsx = 1,
}

/** 导出列定义（随提交快照上送；valueMap 用于枚举/字典原始值 → label） */
export interface ExportColumn {
  key: string
  title: string
  valueMap?: Record<string, string>
}

/** 提交导出任务入参 */
export interface ExportTaskSubmitInput {
  businessType: string
  taskName?: string
  scope: ExportScope
  format: ExportFormat
  /** 资源自身分页查询 DTO 的 JSON 字符串（由对应后端 Provider 反序列化） */
  querySnapshot?: string
  columns: ExportColumn[]
}

/** 导出任务 DTO */
export interface ExportTaskDto {
  basicId: ApiId
  businessType: string
  taskName: string
  scope: ExportScope
  format: ExportFormat
  status: ExportTaskStatus
  progress: number
  totalCount: number
  processedCount: number
  fileId?: ApiId | null
  fileName?: null | string
  fileSize: number
  errorMessage?: null | string
  createdTime: string
  startedTime?: null | string
  finishedTime?: null | string
}

const exportCommandApi = createDynamicApiClient('ExportTask')
const exportQueryApi = createDynamicApiClient('ExportTaskQuery')

export const exportTaskApi = {
  /** 提交导出任务 → POST /ExportTask/Submit */
  submit(input: ExportTaskSubmitInput) {
    return exportCommandApi.post<ExportTaskDto, ExportTaskSubmitInput>('Submit', input)
  },
  /** 我的导出任务分页 → GET /ExportTaskQuery/Mine */
  mine(pageIndex = 1, pageSize = 10) {
    return exportQueryApi.get<PageResult<ExportTaskDto>>('Mine', { pageIndex, pageSize })
  },
  /** 任务详情（轮询用） → GET /ExportTaskQuery/Detail/{id} */
  detail(id: ApiId) {
    return exportQueryApi.get<ExportTaskDto | null>(`Detail/${formatDynamicApiRouteValue(id)}`)
  },
  /** 取消待执行任务 → POST /ExportTask/Cancel?id= */
  cancel(id: ApiId) {
    return exportCommandApi.post<void>('Cancel', undefined, { params: { id } })
  },
  /** 删除任务记录 → DELETE /ExportTask/Delete/{id} */
  remove(id: ApiId) {
    return exportCommandApi.delete<void>(`Delete/${formatDynamicApiRouteValue(id)}`)
  },
}
