import type { DynamicApiParams } from '../../base'
import type { UserSelectItemDto } from '../identity/user.types'
import type { DepartmentTreeNodeDto } from '../organization/department.types'
import type {
  ChatAssistantConversationResult,
  ChatAssistantOption,
  ChatAssistantReplyResult,
  ChatConversationInfoUpdateInput,
  ChatConversationListItem,
  ChatConversationOpenResult,
  ChatMemberItem,
  ChatMessageHistoryQuery,
  ChatMessageHistoryResult,
  ChatMessageItem,
  ChatMessageSearchQuery,
  ChatMessageSendInput,
  ChatReadPosition,
} from '~/types'
import { appendDynamicApiParam, createDynamicApiClient } from '../../base'

const chatCommandApi = createDynamicApiClient('Chat')
const chatQueryApi = createDynamicApiClient('ChatQuery')
const chatAssistantApi = createDynamicApiClient('ChatAssistant')
const aiAssistantQueryApi = createDynamicApiClient('AiAssistantQuery')

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
    return chatCommandApi.delete<void>('Member', { conversationId, userId })
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
  /** EditMessageAsync(dto)：Edit 前缀→PUT 且剥离 → PUT /Chat/Message */
  editMessage(messageId: string, content: string) {
    return chatCommandApi.put<ChatMessageItem>('Message', { messageId, content })
  },
  /** ToggleReactionAsync → POST /Chat/ToggleReaction */
  toggleReaction(messageId: string, emoji: string) {
    return chatCommandApi.post<{ added: boolean }>('ToggleReaction', { messageId, emoji })
  },
  /** PinMessageAsync → POST /Chat/PinMessage */
  pinMessage(messageId: string) {
    return chatCommandApi.post<void>('PinMessage', { messageId })
  },
  /** UnpinMessageAsync → POST /Chat/UnpinMessage */
  unpinMessage(messageId: string) {
    return chatCommandApi.post<void>('UnpinMessage', { messageId })
  },
  /** TogglePinConversationAsync → POST /Chat/TogglePinConversation */
  togglePinConversation(conversationId: string) {
    return chatCommandApi.post<{ isOn: boolean }>('TogglePinConversation', { conversationId })
  },
  /** ToggleMuteConversationAsync → POST /Chat/ToggleMuteConversation */
  toggleMuteConversation(conversationId: string) {
    return chatCommandApi.post<{ isOn: boolean }>('ToggleMuteConversation', { conversationId })
  },
  /** UpdateConversationInfoAsync(dto)：Update 前缀→PUT 且剥离 → PUT /Chat/ConversationInfo */
  updateConversationInfo(input: ChatConversationInfoUpdateInput) {
    return chatCommandApi.put<void, ChatConversationInfoUpdateInput>('ConversationInfo', input)
  },
  /** TransferOwnerAsync → POST /Chat/TransferOwner */
  transferOwner(conversationId: string, newOwnerUserId: string) {
    return chatCommandApi.post<void>('TransferOwner', { conversationId, newOwnerUserId })
  },
  /** SetMemberSilenceAsync → POST /Chat/SetMemberSilence */
  setMemberSilence(conversationId: string, userId: string, isSilenced: boolean) {
    return chatCommandApi.post<void>('SetMemberSilence', { conversationId, userId, isSilenced })
  },
  /** SetMemberRoleAsync → POST /Chat/SetMemberRole（仅群主；Admin ↔ Member） */
  setMemberRole(conversationId: string, userId: string, memberRole: 'Admin' | 'Member') {
    return chatCommandApi.post<void>('SetMemberRole', { conversationId, userId, memberRole })
  },
  /** GetMyConversationsAsync：Get 前缀剥离 → GET /ChatQuery/MyConversations */
  myConversations() {
    return chatQueryApi.get<ChatConversationListItem[]>('MyConversations')
  },
  /** GetMessageHistoryAsync：[HttpPost] 显式 POST（游标分页走 body）→ POST /ChatQuery/MessageHistory */
  messageHistory(query: ChatMessageHistoryQuery) {
    return chatQueryApi.post<ChatMessageHistoryResult, ChatMessageHistoryQuery>('MessageHistory', query)
  },
  /** GetMessageSearchAsync：Get 前缀剥离 + [HttpPost] → POST /ChatQuery/MessageSearch */
  searchMessages(query: ChatMessageSearchQuery) {
    return chatQueryApi.post<ChatMessageHistoryResult, ChatMessageSearchQuery>('MessageSearch', query)
  },
  /** GetMembersAsync(long conversationId)：GET 的 Id 参数拼路由段 → GET /ChatQuery/Members/{conversationId} */
  members(conversationId: string) {
    return chatQueryApi.get<ChatMemberItem[]>('Members', { conversationId })
  },
  /** GetUserOptionsAsync：Get 前缀剥离 → GET /ChatQuery/UserOptions；仅需 chat:read 的轻量选人（启用用户+超管隐藏） */
  /** 当前作用域内可参与聊天的部门树（聊天严格租户隔离，不复用读共享的通用部门树） */
  departmentTree() {
    return chatQueryApi.get<DepartmentTreeNodeDto[]>('DepartmentTree')
  },
  userOptions(keyword: string, limit = 20) {
    const params: DynamicApiParams = { Limit: limit }
    appendDynamicApiParam(params, 'Keyword', keyword)
    return chatQueryApi.get<UserSelectItemDto[]>('UserOptions', params)
  },
  /** GetReadPositionsAsync(long conversationId) → GET /ChatQuery/ReadPositions/{id}（群已读回执） */
  readPositions(conversationId: string) {
    return chatQueryApi.get<ChatReadPosition[]>('ReadPositions', { conversationId })
  },
  /** GetPinnedMessagesAsync(long conversationId) → GET /ChatQuery/PinnedMessages/{id} */
  pinnedMessages(conversationId: string) {
    return chatQueryApi.get<ChatMessageItem[]>('PinnedMessages', { conversationId })
  },
  /** GetAvailableAsync：Get 前缀剥离 → GET /AiAssistantQuery/Available（仅登录态，不看助手管理权限） */
  availableAssistants() {
    return aiAssistantQueryApi.get<ChatAssistantOption[]>('Available')
  },
  /** OpenConversationAsync → POST /ChatAssistant/OpenConversation */
  openAssistantConversation(assistantId: string) {
    return chatAssistantApi.post<ChatAssistantConversationResult>('OpenConversation', { assistantId })
  },
  /** ReplyAsync → POST /ChatAssistant/Reply；生成期间增量走 SignalR，此调用返回即已落库 */
  replyAssistant(conversationId: string, replyId: string) {
    return chatAssistantApi.post<ChatAssistantReplyResult>('Reply', { conversationId, replyId })
  },
}
