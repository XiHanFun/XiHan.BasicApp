/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Permission } from '~/contracts'

// ==================== 在线聊天 ====================
// Hub 路径、推送方法名与客户端可调的 Hub 方法名一律取 ~/contracts 的 RealtimeHubs / RealtimeMethods / RealtimeInvocations（后端导出、生成入库）

/** 「助手不可用」的错误码（后端没有装配 AI）：聊天列表只认这个码隐藏助手入口，别的错误照常报 */
export const CHAT_ASSISTANT_UNAVAILABLE_CODE = 'CHAT_UNAVAILABLE_001'

/** 聊天全屏页路径（后端 ChatPages 登记的 chat.conversations 页面） */
export const CHAT_PAGE_PATH = '/chat/conversations'

/** 会话草稿本地存储键（值为 conversationId → 草稿文本 的 JSON） */
export const CHAT_DRAFTS_STORAGE_KEY = 'chat-drafts'

/** 发送键偏好本地存储键（'enter' | 'ctrl-enter'） */
export const CHAT_SEND_KEY_STORAGE_KEY = 'chat-send-key'

/** 语音已听记录本地存储键（值为 `用户ID:消息ID` 字符串数组，仅本机有效） */
export const CHAT_VOICE_PLAYED_STORAGE_KEY = 'chat-voice-played'

/** 语音已听记录保留条数上限，超出丢最旧的 */
export const CHAT_VOICE_PLAYED_CAP = 500

/**
 * 聊天界面用到的权限码（后端 ChatPermissions）：
 * 会话读管打开会话、退群、置顶、免打扰与已读，消息发送管发、撤回、编辑、回应、置顶消息与转发，会话管理管建群与群治理
 * 只做显隐，安全边界在后端；类型保证每个码都在生成的权限表里
 */
export const CHAT_PERMISSIONS = {
  read: 'chat.chat-conversations.read',
  send: 'chat.chat-messages.send',
  manage: 'chat.chat-conversations.manage',
} as const satisfies Record<string, Permission>
