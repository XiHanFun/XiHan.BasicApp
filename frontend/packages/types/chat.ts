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
  lastMessageTime?: null | string
  lastMessagePreview?: null | string
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
  joinTime: string
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
}

/** 消息历史查询（游标：取 beforeMessageId 之前不含的历史；空取最新一页） */
export interface ChatMessageHistoryQuery {
  conversationId: string
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

/** 聊天业务常量（与后端 ChatDomainService 不变量一致，前端仅做前置校验/文案） */
export const CHAT_RECALL_WINDOW_MINUTES = 2
export const CHAT_MAX_CONTENT_LENGTH = 4000
export const CHAT_MAX_GROUP_NAME_LENGTH = 100

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
  myConversations: () => Promise<ChatConversationListItem[]>
  messageHistory: (query: ChatMessageHistoryQuery) => Promise<ChatMessageHistoryResult>
  members: (conversationId: string) => Promise<ChatMemberItem[]>
  /** 选人（keyword 模糊匹配），复用 UserSelect 轻量端点 */
  selectUsers: (keyword: string, limit?: number) => Promise<ChatUserPickerItem[]>
  /** 部门树（发起部门群用） */
  departmentTree: () => Promise<ChatDepartmentPickerNode[]>
  /** 上传聊天附件（图片/文件消息先上传换 fileId） */
  uploadAttachment: (file: File, onProgress?: (percent: number) => void) => Promise<ChatAttachmentUploadResult>
  /** fileId 换预签名访问 URL（图片内联/文件下载，会过期） */
  getFileUrl: (fileId: string) => Promise<string>
}
