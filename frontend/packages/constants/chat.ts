// ==================== 在线聊天 ====================

/** 聊天 Hub 路径（后端 SignalRConstants.HubPaths.Chat） */
export const CHAT_HUB_PATH = '/hubs/chat'

/** 服务端 → 客户端推送方法名（后端 ChatRealtimeMethods，按同名字符串订阅，禁止内联） */
export const CHAT_REALTIME_METHODS = {
  receiveChatMessage: 'ReceiveChatMessage',
  chatMessageRecalled: 'ChatMessageRecalled',
  chatConversationChanged: 'ChatConversationChanged',
  chatTyping: 'ChatTyping',
  chatMessageEdited: 'ChatMessageEdited',
  chatReactionChanged: 'ChatReactionChanged',
  chatReadPositionChanged: 'ChatReadPositionChanged',
} as const

/** 会话草稿本地存储键（值为 conversationId → 草稿文本 的 JSON） */
export const CHAT_DRAFTS_STORAGE_KEY = 'chat-drafts'

/** 客户端 → 服务端 Hub 方法名（后端 BasicAppChatHub；conversationId 一律传字符串形态雪花 ID） */
export const CHAT_HUB_METHODS = {
  joinConversation: 'JoinConversation',
  leaveConversation: 'LeaveConversation',
  typing: 'Typing',
} as const

/** 聊天权限码（后端 SaasPermissionCodes.Chat） */
export const CHAT_PERMISSIONS = {
  read: 'saas:chat:read',
  send: 'saas:chat:send',
  manage: 'saas:chat:manage',
} as const
