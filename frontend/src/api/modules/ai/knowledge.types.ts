import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { KnowledgeIndexStatus, KnowledgeSourceType } from './knowledge.enums'

/** 知识文档摄取 DTO（后端 KnowledgeIngestDto；文件读取为文本或粘贴，均以文本提交） */
export interface KnowledgeIngestDto {
  title: string
  sourceType: KnowledgeSourceType
  source?: string | null
  text: string
  embeddingProviderCode?: string | null
  remark?: string | null
}

/** 知识文档分页查询 DTO */
export interface KnowledgePageQueryDto extends PageRequest {
  keyword?: string | null
  sourceType?: KnowledgeSourceType | null
  status?: KnowledgeIndexStatus | null
}

/** 知识文档列表项 DTO（不含原文） */
export interface KnowledgeListItemDto extends BasicDto {
  title: string
  sourceType: KnowledgeSourceType
  source?: string | null
  chunkCount: number
  embeddingProviderCode?: string | null
  status: KnowledgeIndexStatus
  errorMessage?: string | null
  sort: number
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
}

/** 知识文档详情 DTO（含原文） */
export interface KnowledgeDetailDto extends KnowledgeListItemDto {
  rawContent?: string | null
  remark?: string | null
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
}

/** 知识检索/问答请求 DTO */
export interface KnowledgeQueryDto {
  query: string
  topK?: number | null
  provider?: string | null
  answer: boolean
}

/** 知识检索命中片段 DTO */
export interface KnowledgeCitationDto {
  documentId: string
  index: number
  title?: string | null
  source?: string | null
  score?: number | null
  text: string
}

/** 知识检索/问答结果 DTO */
export interface KnowledgeQueryResultDto {
  answer?: string | null
  citations: KnowledgeCitationDto[]
}
