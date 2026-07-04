import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'
import type { EnableStatus } from '../shared'

export { EnableStatus } from '../shared'

/** AI Provider 分页查询 DTO（后端 AiProviderPageQueryDto） */
export interface AiProviderPageQueryDto extends PageRequest {
  keyword?: string | null
  provider?: string | null
  isDefault?: boolean | null
  isEnabled?: boolean | null
  status?: EnableStatus | null
}

/** AI Provider 列表项 DTO（不含密钥；后端 AiProviderListItemDto） */
export interface AiProviderListItemDto extends BasicDto {
  configCode: string
  configName: string
  provider: string
  model: string
  embeddingModel?: string | null
  baseUrl?: string | null
  maxOutputTokens?: number | null
  temperature?: number | null
  timeoutSeconds?: number | null
  isDefault: boolean
  isEnabled: boolean
  /** 是否已配置密钥（仅布尔标志，密钥永不回读） */
  hasApiKey: boolean
  sort: number
  status: EnableStatus
  createdTime: DateTimeString
  modifiedTime?: DateTimeString | null
}

/** AI Provider 详情 DTO（后端 AiProviderDetailDto，不含密钥明文） */
export interface AiProviderDetailDto extends AiProviderListItemDto {
  extraJson?: string | null
  remark?: string | null
  createdId?: ApiId | null
  createdBy?: string | null
  modifiedId?: ApiId | null
  modifiedBy?: string | null
}

/** AI Provider 创建 DTO（后端 AiProviderCreateDto） */
export interface AiProviderCreateDto {
  configCode: string
  configName: string
  provider: string
  model: string
  embeddingModel?: string | null
  baseUrl?: string | null
  /** API 密钥（明文提交，服务端加密落库） */
  apiKey?: string | null
  maxOutputTokens?: number | null
  temperature?: number | null
  timeoutSeconds?: number | null
  extraJson?: string | null
  isDefault: boolean
  isEnabled: boolean
  sort: number
  status: EnableStatus
  remark?: string | null
}

/** AI Provider 更新 DTO（后端 AiProviderUpdateDto；ConfigCode 不可变，状态走独立 Status 接口） */
export interface AiProviderUpdateDto extends BasicDto {
  configName: string
  provider: string
  model: string
  embeddingModel?: string | null
  baseUrl?: string | null
  /** API 密钥（留空保留原密钥；非空则替换并加密） */
  apiKey?: string | null
  maxOutputTokens?: number | null
  temperature?: number | null
  timeoutSeconds?: number | null
  extraJson?: string | null
  isDefault: boolean
  isEnabled: boolean
  sort: number
  remark?: string | null
}

/** AI Provider 状态更新 DTO（后端 AiProviderStatusUpdateDto） */
export interface AiProviderStatusUpdateDto extends BasicDto {
  status: EnableStatus
  remark?: string | null
}

/** AI Provider 单体动作 DTO（设为默认 / 测试连接，后端 AiProviderActionDto，仅携带主键） */
export interface AiProviderActionDto extends BasicDto {
}

/** AI Provider 连接测试结果 DTO（后端 AiProviderTestConnectionResultDto，应用级结果，包裹在成功信封内） */
export interface AiProviderTestConnectionResultDto {
  success: boolean
  message?: string | null
  latencyMs: number
  model?: string | null
}
