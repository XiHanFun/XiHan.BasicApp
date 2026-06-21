import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { TemplateEngine, TemplateType } from './codegen.enums'

export { EnableStatus } from '../shared'

/** 代码生成模板分页查询 DTO */
export interface CodeGenTemplatePageQueryDto extends PageRequest {
  keyword?: string | null
  templateGroup?: string | null
  templateType?: TemplateType | null
  templateEngine?: TemplateEngine | null
  isBuiltIn?: boolean | null
  isEnabled?: boolean | null
  status?: EnableStatus | null
}

/** 代码生成模板列表项 DTO */
export interface CodeGenTemplateListItemDto extends BasicDto {
  templateCode: string
  templateName: string
  templateDescription?: string | null
  templateGroup?: string | null
  templateType: TemplateType
  templateEngine: TemplateEngine
  fileExtension?: string | null
  isBuiltIn: boolean
  isEnabled: boolean
  sort: number
  status: EnableStatus
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
}

/** 代码生成模板详情 DTO（含模板正文） */
export interface CodeGenTemplateDetailDto extends CodeGenTemplateListItemDto {
  templateContent?: string | null
  templatePath?: string | null
  fileNameExpression?: string | null
  filePathExpression?: string | null
  remark?: string | null
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
}

/** 代码生成模板创建 DTO */
export interface CodeGenTemplateCreateDto {
  templateCode: string
  templateName: string
  templateDescription?: string | null
  templateGroup?: string | null
  templateType: TemplateType
  templateEngine: TemplateEngine
  templateContent?: string | null
  fileNameExpression?: string | null
  filePathExpression?: string | null
  fileExtension?: string | null
  sort: number
  status: EnableStatus
  remark?: string | null
}

/** 代码生成模板更新 DTO（后端 BasicAppUDto，状态变更走独立 Status 接口） */
export interface CodeGenTemplateUpdateDto extends BasicDto {
  templateName: string
  templateDescription?: string | null
  templateGroup?: string | null
  templateType: TemplateType
  templateEngine: TemplateEngine
  templateContent?: string | null
  fileNameExpression?: string | null
  filePathExpression?: string | null
  fileExtension?: string | null
  isEnabled: boolean
  sort: number
  remark?: string | null
}

/** 代码生成模板状态更新 DTO */
export interface CodeGenTemplateStatusUpdateDto extends BasicDto {
  status: EnableStatus
  remark?: string | null
}

/** 模板语法校验请求 DTO */
export interface CodeGenTemplateValidateDto {
  templateEngine: TemplateEngine
  templateContent: string
}

/** 模板语法校验结果 DTO */
export interface CodeGenTemplateValidateResultDto {
  isValid: boolean
  errors: string[]
}
