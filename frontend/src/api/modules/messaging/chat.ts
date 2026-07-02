import type {
  ChatConversationListItem,
  ChatConversationOpenResult,
  ChatMemberItem,
  ChatMessageHistoryQuery,
  ChatMessageHistoryResult,
  ChatMessageItem,
  ChatMessageSendInput,
} from '~/types'
import { createDynamicApiClient, formatDynamicApiRouteValue } from '../../base'

const chatCommandApi = createDynamicApiClient('Chat')
const chatQueryApi = createDynamicApiClient('ChatQuery')

/**
 * 在线聊天 REST API（ChatAppService / ChatQueryService）
 *
 * 动态 API 路由推导注意：HttpMethodConventions 的动词前缀（Create/Add/Remove/Get 等）
 * 既决定谓词也会从路由段剥离，非 CRUD 前缀（Open/Send/Recall/Mark）不剥离且默认 POST。
 */
export const chatApi = {
  /** OpenSingleConversationAsync → POST /Chat/OpenSingleConversation */
  openSingleConversation(peerUserId: string) {
    return chatCommandApi.post<ChatConversationOpenResult>('OpenSingleConversation', { peerUserId })
  },
  /** CreateGroupConversationAsync：Create 前缀剥离 → POST /Chat/GroupConversation */
  createGroupConversation(conversationName: string, memberUserIds: string[]) {
    return chatCommandApi.post<ChatConversationOpenResult>('GroupConversation', { conversationName, memberUserIds })
  },
  /** OpenDepartmentConversationAsync → POST /Chat/OpenDepartmentConversation */
  openDepartmentConversation(departmentId: string) {
    return chatCommandApi.post<ChatConversationOpenResult>('OpenDepartmentConversation', { departmentId })
  },
  /** AddMembersAsync：Add 前缀剥离 → POST /Chat/Members */
  addMembers(conversationId: string, userIds: string[]) {
    return chatCommandApi.post<void>('Members', { conversationId, userIds })
  },
  /** RemoveMemberAsync：Remove 前缀剥离 → DELETE /Chat/Member；DELETE 无 body，复杂 DTO 兜底 Query 绑定 */
  removeMember(conversationId: string, userId: string) {
    return chatCommandApi.delete<void>('Member', { params: { conversationId, userId } })
  },
  /** SendMessageAsync → POST /Chat/SendMessage */
  sendMessage(input: ChatMessageSendInput) {
    return chatCommandApi.post<ChatMessageItem, ChatMessageSendInput>('SendMessage', input)
  },
  /** RecallMessageAsync(long messageId)：POST 不把 id 拼路由段，走 query（同 Notification Remind 模式） */
  recallMessage(messageId: string) {
    return chatCommandApi.post<void>('RecallMessage', undefined, { params: { messageId } })
  },
  /** MarkReadAsync → POST /Chat/MarkRead */
  markRead(conversationId: string, upToMessageId?: null | string) {
    return chatCommandApi.post<void>('MarkRead', { conversationId, upToMessageId })
  },
  /** GetMyConversationsAsync：Get 前缀剥离 → GET /ChatQuery/MyConversations */
  myConversations() {
    return chatQueryApi.get<ChatConversationListItem[]>('MyConversations')
  },
  /** GetMessageHistoryAsync：[HttpPost] 显式 POST（游标分页走 body）→ POST /ChatQuery/MessageHistory */
  messageHistory(query: ChatMessageHistoryQuery) {
    return chatQueryApi.post<ChatMessageHistoryResult, ChatMessageHistoryQuery>('MessageHistory', query)
  },
  /** GetMembersAsync(long conversationId)：GET 的 Id 参数拼路由段 → GET /ChatQuery/Members/{conversationId} */
  members(conversationId: string) {
    return chatQueryApi.get<ChatMemberItem[]>(`Members/${formatDynamicApiRouteValue(conversationId)}`)
  },
}
