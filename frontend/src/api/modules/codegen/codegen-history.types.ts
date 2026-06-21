import type { ApiId, BasicDto, DateTimeString, NumericString, PageRequest } from '../../types'
import type { GenStatus, GenType } from './codegen.enums'

/**
 * 代码生成历史分页查询 DTO（镜像后端 CodeGenHistoryPageQueryDto）
 */
export interface CodeGenHistoryPageQueryDto extends PageRequest {
  tableId?: ApiId | null
  tableName?: string | null
  batchNumber?: string | null
  genStatus?: GenStatus | null
  startTime?: DateTimeString | null
  endTime?: DateTimeString | null
}

/**
 * 代码生成历史列表项 DTO（镜像后端 CodeGenHistoryListItemDto）
 *
 * - `tableId` 后端为 long，经 LongJsonConverter 序列化为字符串 → ApiId(string)
 * - `duration`/`totalSize` 后端为 long → NumericString，展示需 Number() + 格式化
 */
export interface CodeGenHistoryListItemDto extends BasicDto {
  tableId: ApiId
  tableName: string
  batchNumber?: string | null
  genStatus: GenStatus
  genType: GenType
  genTime: DateTimeString
  duration: NumericString
  fileCount: number
  totalSize: NumericString
  operatorName?: string | null
}

/**
 * 代码生成历史详情 DTO（镜像后端 CodeGenHistoryDetailDto，含产物清单与错误快照）
 *
 * `generatedFiles`/`usedTemplates`/`tableSnapshot` 后端为 JSON 字符串原样透传。
 */
export interface CodeGenHistoryDetailDto extends CodeGenHistoryListItemDto {
  genPath?: string | null
  downloadPath?: string | null
  generatedFiles?: string | null
  usedTemplates?: string | null
  tableSnapshot?: string | null
  errorMessage?: string | null
  operatorIp?: string | null
  remark?: string | null
}
