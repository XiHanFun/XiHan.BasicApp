import type { ApiId, BasicDto, DateTimeString, PageRequest } from '@/api/types'

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

/** 审批业务流程变量（设计器分支条件与表达式提示） */
export interface WorkflowBusinessVariableDto {
  name: string
  label: string
  /** string / number / boolean / user（user 为用户主键字符串） */
  valueType: string
  description?: string | null
}

/** 审批业务（流程编码等于业务编码即为该业务的审批流程） */
export interface WorkflowBusinessTypeDto {
  code: string
  name: string
  description?: string | null
  formComponent: string
  outcomes: string[]
  variables: WorkflowBusinessVariableDto[]
}
