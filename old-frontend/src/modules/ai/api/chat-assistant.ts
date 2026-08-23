/**
 * 聊天 AI 助手 Dynamic API 客户端（ChatAssistantAppService / AiAssistantQueryService）。
 * 职责：助手列表、开会话与触发回复；经 registerChatAssistantProvider 注册进聊天壳层的助手扩展点。
 */
import type {
  ChatAssistantConversationResult,
  ChatAssistantOption,
  ChatAssistantReplyResult,
} from '~/chat'
import { createDynamicApiClient } from '@/api/base'

const chatAssistantApi = createDynamicApiClient('ChatAssistant')
const aiAssistantQueryApi = createDynamicApiClient('AiAssistantQuery')

export const chatAssistantProviderApi = {
  /** GetAvailableAsync：Get 前缀剥离 → GET /AiAssistantQuery/Available（仅登录态，不看助手管理权限） */
  availableAssistants() {
    return aiAssistantQueryApi.get<ChatAssistantOption[]>('Available')
  },
  /** OpenConversationAsync → POST /ChatAssistant/OpenConversation */
  openConversation(assistantId: string) {
    return chatAssistantApi.post<ChatAssistantConversationResult>('OpenConversation', { assistantId })
  },
  /** ReplyAsync → POST /ChatAssistant/Reply；生成期间增量走 SignalR，此调用返回即已落库 */
  reply(conversationId: string, replyId: string) {
    return chatAssistantApi.post<ChatAssistantReplyResult>('Reply', { conversationId, replyId })
  },
}
