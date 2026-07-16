import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum WorkflowDefinitionStatus {
  Draft = 'Draft',
  Published = 'Published',
  Disabled = 'Disabled',
  Archived = 'Archived',
}

export interface WorkflowDefinitionPageQueryDto extends PageRequest {
  keyword?: string | null
  status?: WorkflowDefinitionStatus | null
  category?: string | null
}

export interface WorkflowDefinitionListItemDto extends BasicDto {
  code: string
  name: string
  version: number
  description?: string | null
  category?: string | null
  status: WorkflowDefinitionStatus
  enableCompensation: boolean
  publishTime?: DateTimeString | null
  createdTime: DateTimeString
}

export interface WorkflowDefinitionDetailDto extends WorkflowDefinitionListItemDto {
  definitionJson: string
}

export interface WorkflowDefinitionCreateDto {
  definitionJson: string
}

export interface WorkflowDefinitionUpdateDraftDto {
  basicId: ApiId
  definitionJson: string
}

export interface WorkflowDefinitionIdDto {
  basicId: ApiId
}

export interface WorkflowDefinitionNewVersionDto {
  code: string
}
