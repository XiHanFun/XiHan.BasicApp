/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatConversationType, ChatMemberRole, components, RequestOf, ResponseOf } from '~/contracts'

/**
 * 在线聊天的类型：网络契约一律取生成的 `~/contracts`（后端 DTO 是真源），这里只起聊天侧的别名；
 * 推送载荷（后端 ChatPush.cs）不在 openapi.json 里，按后端记录逐字段写出；助手扩展点的类型随 AI 模块（N4）定
 * 主键都是 GUID 字符串；消息在会话内的先后看 `seq`（会话内序号）
 */
type Schemas = components['schemas']

/** 会话打开 / 创建结果 */
export type ChatConversationOpenResult = ResponseOf<'chat-conversations.open-single'>

/** 我的会话列表项（含未读数、最后消息冗余与最大序号 lastSeq） */
export type ChatConversationListItem = Schemas['ChatConversationListItem']

/** 表情回应项 */
export type ChatReactionItem = Schemas['ChatReactionItem']

/** 消息附件（图片 / 文件 / 语音消息先上传换 fileId） */
export type ChatMessageAttachment = Schemas['ChatMessageAttachment']

/** 消息项（`seq` 是会话内序号；系统提示的 senderUserId 为空即中性时间线，有值即带操作人的公告） */
export type ChatMessageItem = Schemas['ChatMessageItem']

/** 一页消息（历史按序号正序，搜索按时间倒序） */
export type ChatMessageHistoryResult = ResponseOf<'chat-messages.history'>

/** 会话成员项 */
export type ChatMemberItem = Schemas['ChatMemberItem']

/** 群信息更新入参（只改给了的字段；公告、描述与头像给空串即清空） */
export type ChatConversationInfoUpdateInput = RequestOf<'chat-conversations.update-info'>

/** 发送消息入参 */
export type ChatMessageSendInput = RequestOf<'chat-messages.send'>

/** 成员已读位（群已读回执）：已读到的消息与它的序号（没读过为 0） */
export type ChatReadPosition = Schemas['ChatReadPosition']

/** 消息历史查询（aroundMessageId 定位模式优先，否则取 beforeMessageId 之前的一页） */
export type ChatMessageHistoryQuery = RequestOf<'chat-messages.history'>

/** 会话内消息搜索查询 */
export type ChatMessageSearchQuery = RequestOf<'chat-messages.search'>

/** 选人候选（发起单聊 / 建群 / 加成员）：头像是文件主键，主部门名称没有任职时为空 */
export type ChatUserOption = Schemas['ChatUserOption']

/** 选人查询：关键字、部门（含全部下级）与分页 */
export type ChatUserOptionsQuery = RequestOf<'chat-conversations.user-options'>

/** 一页选人候选（按显示名排序） */
export type ChatUserOptionsResult = ResponseOf<'chat-conversations.user-options'>

/** 部门树节点（发起部门群） */
export interface ChatDepartmentPickerNode {
  departmentId: string
  departmentName: string
  children?: ChatDepartmentPickerNode[] | null
}

/** 聊天附件上传结果（发图片 / 文件 / 语音消息前先上传换 fileId） */
export interface ChatAttachmentUploadResult {
  fileId: string
  fileName: string
  fileSize: number
}

/** 附件的签名地址：按会话签出（成员凭会话换），到期前可复用，原样使用、不能再拼参数 */
export interface ChatFileUrl {
  url: string
  /** 到期时间（UTC，ISO 8601） */
  expiresAt: string
}

/** 推送里的会话摘要（后端 ChatConversationSnapshot） */
export interface ChatConversationSnapshot {
  conversationId: string
  conversationType: ChatConversationType
  conversationName?: null | string
  lastMessageAt?: null | string
  lastMessagePreview?: null | string
}

/** SignalR ReceiveChatMessage 载荷 */
export interface ChatMessagePushPayload {
  message: ChatMessageItem
  conversation: ChatConversationSnapshot
}

/** SignalR ChatMessageRecalled 载荷 */
export interface ChatRecalledPushPayload {
  conversationId: string
  messageId: string
}

/** SignalR ChatConversationChanged 载荷（变更种类见后端 ChatRealtimeMethods.ChangeTypes） */
export interface ChatConversationChangedPushPayload {
  conversationId: string
  changeType: 'created' | 'member-added' | 'member-removed' | 'pinned-changed' | (string & {})
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
  editedAt?: null | string
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
  lastReadSeq: number
}

/** SignalR ChatAssistantDelta 载荷 */
export interface ChatAssistantDeltaPushPayload {
  conversationId: string
  replyId: string
  delta: string
}

/** SignalR ChatAssistantCompleted 载荷 */
export interface ChatAssistantCompletedPushPayload {
  conversationId: string
  replyId: string
  messageId?: null | string
  /** 失败时的错误码（CHAT_UNAVAILABLE_002 等） */
  errorCode?: null | string
  /** 失败时的本地化正文 */
  error?: null | string
}

/** 聊天业务常量（与后端 ChatRules 的不变量一致，前端只做前置校验与文案） */
export const CHAT_RECALL_WINDOW_MINUTES = 2
export const CHAT_EDIT_WINDOW_MINUTES = 5
export const CHAT_MAX_CONTENT_LENGTH = 4000
export const CHAT_MAX_GROUP_NAME_LENGTH = 100
export const CHAT_MAX_MENTION_COUNT = 20

/**
 * 聊天 API 契约：本模块的 api.ts 用生成的操作客户端实现并经 setChatApi 注册，聊天 store 与组件只依赖这份契约
 * 置顶与免打扰传新值（不是切换）；转发是服务端操作；附件地址按会话签出
 */
export interface ChatApiContract {
  openSingleConversation: (peerUserId: string) => Promise<ChatConversationOpenResult>
  createGroupConversation: (conversationName: string, memberUserIds: string[]) => Promise<ChatConversationOpenResult>
  openDepartmentConversation: (departmentId: string) => Promise<ChatConversationOpenResult>
  /** 加成员；逐项失败的抛出（带原因） */
  addMembers: (conversationId: string, userIds: string[]) => Promise<void>
  /** 移出成员；逐项失败的抛出（带原因） */
  removeMember: (conversationId: string, userId: string) => Promise<void>
  /** 主动退群：退出对象恒为当前登录用户，只要会话读权限，不要群治理权限 */
  leaveConversation: (conversationId: string) => Promise<void>
  sendMessage: (input: ChatMessageSendInput) => Promise<ChatMessageItem>
  recallMessage: (messageId: string) => Promise<void>
  markRead: (conversationId: string, upToMessageId?: null | string) => Promise<void>
  /** 编辑消息（仅文本、仅本人、5 分钟窗） */
  editMessage: (messageId: string, content: string) => Promise<ChatMessageItem>
  /** 表情回应开关：再点同一个即取消 */
  toggleReaction: (messageId: string, emoji: string) => Promise<{ added: boolean }>
  /** 转发到一个或多个会话（服务端原样复制类型、正文与附件）；逐项失败的抛出（带原因） */
  forwardMessage: (messageId: string, conversationIds: string[]) => Promise<void>
  /** Pin / 取消 Pin 消息 */
  pinMessage: (messageId: string) => Promise<void>
  unpinMessage: (messageId: string) => Promise<void>
  /** 会话置顶 / 免打扰（个人维度）：传新值，返回生效后的状态 */
  setConversationPinned: (conversationId: string, isPinned: boolean) => Promise<{ isOn: boolean }>
  setConversationMuted: (conversationId: string, isMuted: boolean) => Promise<{ isOn: boolean }>
  /** 群治理：信息编辑 / 转让群主 / 成员禁言（群主与管理员） */
  updateConversationInfo: (input: ChatConversationInfoUpdateInput) => Promise<void>
  transferOwner: (conversationId: string, newOwnerUserId: string) => Promise<void>
  /** 禁言或解除；逐项失败的抛出（带原因） */
  setMemberSilence: (conversationId: string, userId: string, isSilenced: boolean) => Promise<void>
  /** 设置成员角色（仅群主；Admin ↔ Member） */
  setMemberRole: (conversationId: string, userId: string, memberRole: Exclude<ChatMemberRole, 'Owner'>) => Promise<void>
  myConversations: () => Promise<ChatConversationListItem[]>
  messageHistory: (query: ChatMessageHistoryQuery) => Promise<ChatMessageHistoryResult>
  /** 会话内消息搜索（关键字匹配正文与附件名，不含已撤回的） */
  searchMessages: (query: ChatMessageSearchQuery) => Promise<ChatMessageHistoryResult>
  members: (conversationId: string) => Promise<ChatMemberItem[]>
  /** 成员已读位（群已读回执） */
  readPositions: (conversationId: string) => Promise<ChatReadPosition[]>
  /** 会话内被 Pin 的消息 */
  pinnedMessages: (conversationId: string) => Promise<ChatMessageItem[]>
  /** 选人（关键字匹配用户名与显示名，可按部门筛、逐页取），聊天专属的轻量端点，只要会话读权限 */
  userOptions: (query: ChatUserOptionsQuery) => Promise<ChatUserOptionsResult>
  /** 当前一侧启用的部门树（发起部门群用；平台侧没有部门） */
  departmentTree: () => Promise<ChatDepartmentPickerNode[]>
  /** 上传聊天附件（先按 SHA-256 秒传，没命中再普通上传） */
  uploadAttachment: (file: File, onProgress?: (percent: number) => void) => Promise<ChatAttachmentUploadResult>
  /** 按会话换附件的签名地址（图片内联、语音、文件下载；会过期）；download 为真即以附件下载（带原始文件名） */
  getFileUrl: (conversationId: string, fileId: string, download?: boolean) => Promise<ChatFileUrl>
}
