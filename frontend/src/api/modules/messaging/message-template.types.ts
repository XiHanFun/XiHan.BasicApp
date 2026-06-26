import type { ApiId, BasicDto, PageRequest } from '../../types'
import type { EnableStatus } from '../shared/types'

/** 消息渠道（与后端 MessageChannel 数值一致） */
export enum MessageChannel {
  /** 站内通知 */
  SiteNotification = 1,
  /** 邮件 */
  Email = 2,
  /** 短信 */
  Sms = 4,
}

export interface MessageTemplateListItemDto extends BasicDto {
  templateCode: string
  channel: MessageChannel
  templateName: string
  subject?: string | null
  isHtml: boolean
  /** 是否平台级全局模板（仅平台运维态可维护，租户可建同编码覆盖） */
  isGlobal: boolean
  description?: string | null
  status: EnableStatus
  sort: number
  remark?: string | null
}

export interface MessageTemplateDetailDto extends MessageTemplateListItemDto {
  /** 内容模板（Scriban 语法） */
  content: string
}

export interface MessageTemplateCreateDto {
  templateCode: string
  channel: MessageChannel
  templateName: string
  subject?: string | null
  content: string
  isHtml: boolean
  description?: string | null
  status: EnableStatus
  sort: number
  remark?: string | null
}

export interface MessageTemplateUpdateDto {
  basicId: ApiId
  templateName: string
  subject?: string | null
  content: string
  isHtml: boolean
  description?: string | null
  sort: number
  remark?: string | null
}

export interface MessageTemplateStatusUpdateDto {
  basicId: ApiId
  status: EnableStatus
  remark?: string | null
}

export interface MessageTemplatePageQueryDto extends PageRequest {
  keyword?: string | null
  channel?: MessageChannel | null
  status?: EnableStatus | null
}
