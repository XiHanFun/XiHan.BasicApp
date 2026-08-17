import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum BotProviderType {
  DingTalk = 'DingTalk',
  Lark = 'Lark',
  WeCom = 'WeCom',
}

export interface BotConfigPageQueryDto extends PageRequest {
  isDefault?: boolean | null
  isEnabled?: boolean | null
  keyword?: string | null
  provider?: BotProviderType | null
}

/** Secret 为敏感字段不下发，仅以 hasSecret 标识是否已配置 */
export interface BotConfigListItemDto extends BasicDto {
  configCode: string
  configName: string
  createdTime: DateTimeString
  hasSecret: boolean
  isDefault: boolean
  isEnabled: boolean
  /** 钉钉/飞书安全关键词 */
  keyword?: string | null
  modifiedTime?: DateTimeString | null
  provider: BotProviderType
  sort: number
}

export interface BotConfigDetailDto extends BotConfigListItemDto {
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
  /** 含凭证的完整 Webhook 地址 */
  webhookUrl: string
}

export interface BotConfigCreateDto {
  configCode: string
  configName: string
  isDefault: boolean
  isEnabled: boolean
  /** 钉钉/飞书安全关键词 */
  keyword?: string | null
  provider: BotProviderType
  remark?: string | null
  /** 仅写入，读侧不回显；企业微信不使用 */
  secret?: string | null
  sort: number
  webhookUrl: string
}

export interface BotConfigUpdateDto extends BasicDto {
  configName: string
  /** 钉钉/飞书安全关键词 */
  keyword?: string | null
  provider: BotProviderType
  remark?: string | null
  /** 留空表示保留原秘钥 */
  secret?: string | null
  sort: number
  webhookUrl: string
}

export interface BotConfigStatusUpdateDto extends BasicDto {
  isEnabled: boolean
}

export type BotConfigDefaultUpdateDto = BasicDto
