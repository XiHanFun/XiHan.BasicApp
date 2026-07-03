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
