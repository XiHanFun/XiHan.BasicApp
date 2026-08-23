import type { ChatAuditListItemDto, ChatAuditPageQueryDto } from './chat-audit.types'
import type { PageResult } from '@/api/types'
import { createDynamicApiClient } from '@/api/base'

const chatAuditQueryApi = createDynamicApiClient('ChatAuditQuery')

/** 聊天审计 API（管理侧跨会话合规查询，权限 chat:audit） */
export const chatAuditApi = {
  /** GetChatMessagePageAsync：[HttpPost] → POST /ChatAuditQuery/ChatMessagePage */
  page(input: ChatAuditPageQueryDto) {
    return chatAuditQueryApi.post<PageResult<ChatAuditListItemDto>, ChatAuditPageQueryDto>('ChatMessagePage', input)
  },
}
