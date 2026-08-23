import type { ApiId, BasicDto, DateTimeString, PageRequest } from '../../types'

export interface TelegramBotPageQueryDto extends PageRequest {
  isEnabled?: boolean | null
  keyword?: string | null
}

/** Token 为敏感字段不下发，仅以 hasToken 标识是否已配置 */
export interface TelegramBotListItemDto extends BasicDto {
  botName: string
  createdTime: DateTimeString
  enableFallbackReply: boolean
  hasToken: boolean
  isEnabled: boolean
  modifiedTime?: DateTimeString | null
  sort: number
}

export interface TelegramBotDetailDto extends TelegramBotListItemDto {
  /** 逗号分隔的 Telegram 用户 Id */
  adminUsers?: string | null
  /** 逗号分隔的命令白名单；空 = 不限制命令 */
  allowedCommands?: string | null
  /** 逗号分隔的群组 ChatId 白名单；空 = 拒收所有群消息（fail-closed） */
  allowedGroupChatIds?: string | null
  createdBy?: string | null
  createdId?: ApiId | null
  modifiedBy?: string | null
  modifiedId?: ApiId | null
  remark?: string | null
}

export interface TelegramBotCreateDto {
  adminUsers?: string | null
  allowedCommands?: string | null
  allowedGroupChatIds?: string | null
  botName: string
  enableFallbackReply: boolean
  isEnabled: boolean
  remark?: string | null
  sort: number
  /** 仅写入，读侧不回显 */
  token: string
}

export interface TelegramBotUpdateDto extends BasicDto {
  adminUsers?: string | null
  allowedCommands?: string | null
  allowedGroupChatIds?: string | null
  botName: string
  enableFallbackReply: boolean
  remark?: string | null
  sort: number
  /** 留空表示保留原 Token */
  token?: string | null
}

export interface TelegramBotStatusUpdateDto extends BasicDto {
  isEnabled: boolean
}
