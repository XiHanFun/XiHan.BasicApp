import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

/** AI 提示词分页查询 DTO */
export interface AiPromptPageQueryDto extends PageRequest {
  keyword?: string | null
  category?: string | null
  isEnabled?: boolean | null
  status?: EnableStatus | null
}

/** AI 提示词列表项 DTO（不含正文） */
export interface AiPromptListItemDto extends BasicDto {
  promptCode: string
  promptName: string
  category?: string | null
  version?: string | null
  isEnabled: boolean
  sort: number
  status: EnableStatus
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
}

/** AI 提示词详情 DTO（含正文） */
export interface AiPromptDetailDto extends AiPromptListItemDto {
  content?: string | null
  remark?: string | null
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
}

/** AI 提示词创建 DTO */
export interface AiPromptCreateDto {
  promptCode: string
  promptName: string
  category?: string | null
  version?: string | null
  content: string
  isEnabled: boolean
  sort: number
  status: EnableStatus
  remark?: string | null
}

/** AI 提示词更新 DTO（PromptCode 不可变，状态走独立 Status 接口） */
export interface AiPromptUpdateDto extends BasicDto {
  promptName: string
  category?: string | null
  version?: string | null
  content: string
  isEnabled: boolean
  sort: number
  remark?: string | null
}

/** AI 提示词状态更新 DTO */
export interface AiPromptStatusUpdateDto extends BasicDto {
  status: EnableStatus
  remark?: string | null
}
