import type { EnableStatus } from '@/api/modules/shared'
import type { ApiId, BasicDto, DateTimeString, PageRequest } from '@/api/types'

/** AI 助手分页查询 DTO（后端 AiAssistantPageQueryDto） */
export interface AiAssistantPageQueryDto extends PageRequest {
  keyword?: string | null
  isDefault?: boolean | null
  isEnabled?: boolean | null
  status?: EnableStatus | null
}

/** AI 助手列表项 DTO（后端 AiAssistantListItemDto） */
export interface AiAssistantListItemDto extends BasicDto {
  assistantCode: string
  assistantName: string
  avatar?: string | null
  description?: string | null
  promptCode?: string | null
  providerCode?: string | null
  enableKnowledge: boolean
  knowledgeProviderCode?: string | null
  knowledgeTopK: number
  historyRounds: number
  isDefault: boolean
  isEnabled: boolean
  sort: number
  status: EnableStatus
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
}

/** AI 助手详情 DTO（后端 AiAssistantDetailDto） */
export interface AiAssistantDetailDto extends AiAssistantListItemDto {
  greeting?: string | null
  remark?: string | null
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
}

/** AI 助手创建 DTO（后端 AiAssistantCreateDto） */
export interface AiAssistantCreateDto {
  assistantCode: string
  assistantName: string
  avatar?: string | null
  description?: string | null
  greeting?: string | null
  promptCode?: string | null
  providerCode?: string | null
  enableKnowledge: boolean
  knowledgeProviderCode?: string | null
  knowledgeTopK: number
  historyRounds: number
  isDefault: boolean
  isEnabled: boolean
  sort: number
  status: EnableStatus
  remark?: string | null
}

/** AI 助手更新 DTO（后端 AiAssistantUpdateDto，assistantCode 不可变） */
export interface AiAssistantUpdateDto {
  basicId: ApiId
  assistantName: string
  avatar?: string | null
  description?: string | null
  greeting?: string | null
  promptCode?: string | null
  providerCode?: string | null
  enableKnowledge: boolean
  knowledgeProviderCode?: string | null
  knowledgeTopK: number
  historyRounds: number
  isDefault: boolean
  isEnabled: boolean
  sort: number
  remark?: string | null
}

/** AI 助手状态更新 DTO（后端 AiAssistantStatusUpdateDto） */
export interface AiAssistantStatusUpdateDto {
  basicId: ApiId
  status: EnableStatus
  remark?: string | null
}

/** AI 助手单体动作 DTO（设为默认，后端 AiAssistantActionDto，仅携带主键） */
export interface AiAssistantActionDto {
  basicId: ApiId
}
