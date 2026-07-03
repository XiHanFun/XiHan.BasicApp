import type { ChatConversationType, ChatMemberRole, ChatMessageType } from './enums'

/**
 * 在线聊天契约类型（后端 ChatDtos 的前端镜像）。
 *
 * packages 的聊天 store/组件依赖这些契约与 `ChatApiContract`，实现由 src 注册进
 * `appContext.apis.chatApi`（packages 不反向依赖 src，同 userInboxApi 范式）。
 * 所有雪花 ID 均为字符串形态（REST 与 SignalR 载荷已统一投影）。
 */

/** 会话打开/创建结果 */
export interface ChatConversationOpenResult {
  conversationId: string
  conversationType: ChatConversationType
  /** 会话名称（单聊为空） */
  conversationName?: null | string
  /** 是否本次新建 */
  created: boolean
}

/** 我的会话列表项（含未读数与最后消息冗余） */
export interface ChatConversationListItem {
  conversationId: string
  conversationType: ChatConversationType
  /** 展示名称（单聊=对端用户名；群聊/部门群=会话名） */
  displayName: string
  /** 会话头像（单聊=对端头像） */
  avatar?: null | string
  /** 单聊对端用户ID */
  peerUserId?: null | string
  /** 部门ID（部门群） */
  departmentId?: null | string
  memberCount: number
  /** 我在会话中的角色 */
  memberRole: ChatMemberRole
  /** 我的未读消息数 */
  unreadCount: number
  isMuted: boolean
  /** 是否置顶会话（个人维度，列表置顶优先） */
  isPinned: boolean
  /** 我是否被禁言（输入区置灰） */
  isSilenced: boolean
  /** 群公告 */
  announcement?: null | string
  /** 群描述 */
  description?: null | string
  lastMessageTime?: null | string
  lastMessagePreview?: null | string
}

/** 表情回应项 */
export interface ChatReactionItem {
  emoji: string
  userId: string
  userName?: null | string
}

/** 消息项 */
export interface ChatMessageItem {
  messageId: string
  conversationId: string
  senderUserId: string
  /** 发送人用户名（快照） */
  senderUserName?: null | string
  messageType: ChatMessageType
  /** 消息内容（已撤回为空） */
  content?: null | string
  fileId?: null | string
  fileName?: null | string
  /** 文件大小（字节） */
  fileSize?: null | number
  isRecalled: boolean
  /** 客户端消息ID（乐观上屏去重） */
  clientMessageId?: null | string
  createdTime: string
  /** 被回复消息ID */
  replyToMessageId?: null | string
  /** 回复快照「{发送人}: {内容截断}」 */
  replyPreview?: null | string
  /** 编辑时间（非空即"已编辑"） */
  editedTime?: null | string
  /** 被 @ 用户ID集合 */
  mentionedUserIds: string[]
  /** 是否被 Pin */
  isPinned: boolean
  /** 表情回应列表 */
  reactions: ChatReactionItem[]
}

/** 消息历史（游标分页，Items 按时间正序） */
export interface ChatMessageHistoryResult {
  items: ChatMessageItem[]
  /** 是否还有更早的历史 */
  hasMore: boolean
}

/** 会话成员项 */
export interface ChatMemberItem {
  userId: string
  userName?: null | string
  memberRole: ChatMemberRole
  /** 是否被禁言 */
  isSilenced: boolean
  joinTime: string
}

/** 群信息更新入参（null 字段不改） */
export interface ChatConversationInfoUpdateInput {
  conversationId: string
  conversationName?: null | string
  announcement?: null | string
  description?: null | string
  /** 群头像（文件主键或直链；null 不改；空串清空） */
  avatar?: null | string
}

/** 发送消息入参 */
export interface ChatMessageSendInput {
  conversationId: string
  messageType: ChatMessageType
  /** 文本正文；图片/文件为可选说明 */
  content?: null | string
  /** 图片/文件消息必填 */
  fileId?: null | string
  fileName?: null | string
  fileSize?: null | number
  /** 客户端消息ID（乐观上屏去重） */
  clientMessageId?: null | string
  /** 被回复消息ID */
  replyToMessageId?: null | string
  /** 被 @ 用户ID集合（须均为会话成员） */
  mentionedUserIds?: null | string[]
}

/** 成员已读位（群已读回执） */
export interface ChatReadPosition {
  userId: string
  lastReadMessageId?: null | string
}

/** 消息历史查询（游标：取 beforeMessageId 之前不含的历史；aroundMessageId 定位模式优先） */
export interface ChatMessageHistoryQuery {
  conversationId: string
  beforeMessageId?: null | string
  /** 定位模式：以该消息为中心取前后各半页（搜索命中跳转用） */
  aroundMessageId?: null | string
  take?: number
}

/** 会话内消息搜索查询 */
export interface ChatMessageSearchQuery {
  conversationId: string
  keyword: string
  beforeMessageId?: null | string
  take?: number
}

/** 选人（发起单聊/建群）轻量条目 */
export interface ChatUserPickerItem {
  userId: string
  userName: string
  nickName?: null | string
  avatar?: null | string
}

/** 部门树节点（发起部门群） */
export interface ChatDepartmentPickerNode {
  departmentId: string
  departmentName: string
  children?: ChatDepartmentPickerNode[] | null
}

/** 聊天附件上传结果（发图片/文件消息前先上传换 fileId） */
export interface ChatAttachmentUploadResult {
  fileId: string
  fileName: string
  fileSize: number
}

/** SignalR ReceiveChatMessage 载荷 */
export interface ChatMessagePushPayload {
  message: ChatMessageItem
  conversation: {
    conversationId: string
    conversationType: ChatConversationType
    conversationName?: null | string
    lastMessageTime?: null | string
    lastMessagePreview?: null | string
  }
}

/** SignalR ChatMessageRecalled 载荷 */
export interface ChatRecalledPushPayload {
  conversationId: string
  messageId: string
}

/** SignalR ChatConversationChanged 载荷 */
export interface ChatConversationChangedPushPayload {
  conversationId: string
  changeType: 'created' | 'member-added' | 'member-removed' | (string & {})
}

/** SignalR ChatTyping 载荷 */
export interface ChatTypingPushPayload {
  conversationId: string
  userId: string
  userName?: null | string
}

/** SignalR ChatMessageEdited 载荷 */
export interface ChatMessageEditedPushPayload {
  conversationId: string
  messageId: string
  content?: null | string
  editedTime?: null | string
}

/** SignalR ChatReactionChanged 载荷（delta） */
export interface ChatReactionChangedPushPayload {
  conversationId: string
  messageId: string
  emoji: string
  userId: string
  userName?: null | string
  added: boolean
}

/** SignalR ChatReadPositionChanged 载荷 */
export interface ChatReadPositionChangedPushPayload {
  conversationId: string
  userId: string
  lastReadMessageId?: null | string
}

/** 聊天业务常量（与后端 ChatDomainService 不变量一致，前端仅做前置校验/文案） */
export const CHAT_RECALL_WINDOW_MINUTES = 2
export const CHAT_EDIT_WINDOW_MINUTES = 5
export const CHAT_MAX_CONTENT_LENGTH = 4000
export const CHAT_MAX_GROUP_NAME_LENGTH = 100
export const CHAT_MAX_MENTION_COUNT = 20

/**
 * 聊天 API 契约：src 实现并注册进 appContext.apis.chatApi，packages 只依赖此契约
 */
export interface ChatApiContract {
  openSingleConversation: (peerUserId: string) => Promise<ChatConversationOpenResult>
  createGroupConversation: (conversationName: string, memberUserIds: string[]) => Promise<ChatConversationOpenResult>
  openDepartmentConversation: (departmentId: string) => Promise<ChatConversationOpenResult>
  addMembers: (conversationId: string, userIds: string[]) => Promise<void>
  removeMember: (conversationId: string, userId: string) => Promise<void>
  sendMessage: (input: ChatMessageSendInput) => Promise<ChatMessageItem>
  recallMessage: (messageId: string) => Promise<void>
  markRead: (conversationId: string, upToMessageId?: null | string) => Promise<void>
  /** 编辑消息（仅文本、仅本人、5 分钟窗） */
  editMessage: (messageId: string, content: string) => Promise<ChatMessageItem>
  /** 表情回应 toggle */
  toggleReaction: (messageId: string, emoji: string) => Promise<{ added: boolean }>
  /** Pin/取消 Pin 消息 */
  pinMessage: (messageId: string) => Promise<void>
  unpinMessage: (messageId: string) => Promise<void>
  /** 会话置顶/免打扰 toggle（个人维度），返回新状态 */
  togglePinConversation: (conversationId: string) => Promise<{ isOn: boolean }>
  toggleMuteConversation: (conversationId: string) => Promise<{ isOn: boolean }>
  /** 群治理：信息编辑/转让群主/成员禁言（群主与管理员） */
  updateConversationInfo: (input: ChatConversationInfoUpdateInput) => Promise<void>
  transferOwner: (conversationId: string, newOwnerUserId: string) => Promise<void>
  setMemberSilence: (conversationId: string, userId: string, isSilenced: boolean) => Promise<void>
  /** 设置成员角色（仅群主；Admin ↔ Member） */
  setMemberRole: (conversationId: string, userId: string, memberRole: 'Admin' | 'Member') => Promise<void>
  myConversations: () => Promise<ChatConversationListItem[]>
  messageHistory: (query: ChatMessageHistoryQuery) => Promise<ChatMessageHistoryResult>
  /** 会话内消息搜索（关键字匹配正文与文件名，排除已撤回） */
  searchMessages: (query: ChatMessageSearchQuery) => Promise<ChatMessageHistoryResult>
  members: (conversationId: string) => Promise<ChatMemberItem[]>
  /** 成员已读位（群已读回执） */
  readPositions: (conversationId: string) => Promise<ChatReadPosition[]>
  /** 会话内被 Pin 的消息 */
  pinnedMessages: (conversationId: string) => Promise<ChatMessageItem[]>
  /** 选人（keyword 模糊匹配），复用 UserSelect 轻量端点 */
  selectUsers: (keyword: string, limit?: number) => Promise<ChatUserPickerItem[]>
  /** 部门树（发起部门群用） */
  departmentTree: () => Promise<ChatDepartmentPickerNode[]>
  /** 上传聊天附件（图片/文件消息先上传换 fileId） */
  uploadAttachment: (file: File, onProgress?: (percent: number) => void) => Promise<ChatAttachmentUploadResult>
  /** fileId 换预签名访问 URL（图片内联/文件下载，会过期） */
  getFileUrl: (fileId: string) => Promise<string>
}
