import type { ApiId, DateTimeString, PageRequest } from '../../types'
import type { ChatConversationType, ChatMessageType } from './chat.types'

/** 聊天审计分页查询 DTO（管理侧跨会话，权限 saas:chat:audit） */
export interface ChatAuditPageQueryDto extends PageRequest {
  keyword?: string
  conversationId?: ApiId
  senderUserId?: ApiId
  createdTimeStart?: DateTimeString
  createdTimeEnd?: DateTimeString
  /** 是否包含已撤回消息（默认包含） */
  includeRecalled?: boolean
}

/** 聊天审计列表项 DTO */
export interface ChatAuditListItemDto {
  basicId: ApiId
  conversationId: ApiId
  conversationName?: null | string
  conversationType: ChatConversationType
  senderUserId: ApiId
  senderUserName?: null | string
  messageType: ChatMessageType
  content?: null | string
  fileName?: null | string
  isRecalled: boolean
  editedTime?: DateTimeString | null
  createdTime: DateTimeString
}
