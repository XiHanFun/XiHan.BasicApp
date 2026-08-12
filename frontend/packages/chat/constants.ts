// ==================== 在线聊天 ====================

/** 聊天全屏页路径（对应后端 Chat 模块 PageRegistry 的 message.chat 页面） */
export const CHAT_PAGE_PATH = '/message/chat'

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
  chatAssistantDelta: 'ChatAssistantDelta',
  chatAssistantCompleted: 'ChatAssistantCompleted',
} as const

/** 会话草稿本地存储键（值为 conversationId → 草稿文本 的 JSON） */
export const CHAT_DRAFTS_STORAGE_KEY = 'chat-drafts'

/** 发送键偏好本地存储键（'enter' | 'ctrl-enter'） */
export const CHAT_SEND_KEY_STORAGE_KEY = 'chat-send-key'

/** 语音已听记录本地存储键（值为 `用户ID:消息ID` 字符串数组，仅本机有效） */
export const CHAT_VOICE_PLAYED_STORAGE_KEY = 'chat-voice-played'

/** 语音已听记录保留条数上限，超出丢最旧的 */
export const CHAT_VOICE_PLAYED_CAP = 500

/** 客户端 → 服务端 Hub 方法名（后端 BasicAppChatHub；conversationId 一律传字符串形态雪花 ID） */
export const CHAT_HUB_METHODS = {
  joinConversation: 'JoinConversation',
  leaveConversation: 'LeaveConversation',
  typing: 'Typing',
} as const

/** 聊天权限码（后端 SaasPermissionCodes.Chat） */
export const CHAT_PERMISSIONS = {
  read: 'chat:read',
  send: 'chat:send',
  manage: 'chat:manage',
} as const
