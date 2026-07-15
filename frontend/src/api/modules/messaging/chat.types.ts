/**
 * 聊天契约类型已下沉到 packages 底层（`~/types/chat`，被 packages 的聊天 store/组件复用），
 * 此处反向 re-export 以保持 `@/api` 入口不变（同 src/api/types.ts 范式）。
 */
export type {
  ChatApiContract,
  ChatAttachmentUploadResult,
  ChatConversationChangedPushPayload,
  ChatConversationListItem,
  ChatConversationOpenResult,
  ChatDepartmentPickerNode,
  ChatMemberItem,
  ChatMessageEditedPushPayload,
  ChatMessageHistoryQuery,
  ChatMessageHistoryResult,
  ChatMessageItem,
  ChatMessagePushPayload,
  ChatMessageSendInput,
  ChatReactionChangedPushPayload,
  ChatReactionItem,
  ChatReadPosition,
  ChatReadPositionChangedPushPayload,
  ChatRecalledPushPayload,
  ChatTypingPushPayload,
  ChatUserPickerItem,
} from '~/types'

// 聊天契约**枚举**（运行时值）：`export type` 带不出枚举值，故单独做值 re-export，补齐 `@/api` 入口。
// 缺这行时 src 侧只能绕过 `@/api` 直取 '~/types/enums'（chat-audit.types.ts 曾如此）。
export { ChatConversationType, ChatMemberRole, ChatMessageType } from '~/types/enums'
