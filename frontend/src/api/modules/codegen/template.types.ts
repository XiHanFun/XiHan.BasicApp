import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'
import type { ArtifactWriteMode, TemplateEngine, TemplateType } from './codegen.enums'

export { EnableStatus } from '../shared'

/** 代码生成模板分页查询 DTO */
export interface CodeGenTemplatePageQueryDto extends PageRequest {
  keyword?: string | null
  templateGroup?: string | null
  templateType?: TemplateType | null
  templateEngine?: TemplateEngine | null
  writeMode?: ArtifactWriteMode | null
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
  /** 模板类型；为空表示通用模板，适用于全部类型（单表/树表/主子表） */
  templateType?: TemplateType | null
  templateEngine: TemplateEngine
  /** 写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建 */
  writeMode: ArtifactWriteMode
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
  /** 模板类型；为空表示通用模板，适用于全部类型（单表/树表/主子表） */
  templateType?: TemplateType | null
  templateEngine: TemplateEngine
  /** 写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建 */
  writeMode: ArtifactWriteMode
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
  /** 模板类型；为空表示通用模板，适用于全部类型（单表/树表/主子表） */
  templateType?: TemplateType | null
  templateEngine: TemplateEngine
  /** 写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建 */
  writeMode: ArtifactWriteMode
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
