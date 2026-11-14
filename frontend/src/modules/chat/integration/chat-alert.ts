/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatConversationSnapshot, ChatMessageItem } from '../types'
import { isMissedCallRecord } from '../call/call-helpers'

/**
 * 推来的消息要不要提醒当前用户（提示音、岛提示、桌面通知）
 * 自己发的、系统提示、不是自己没接到的通话记录不提醒；免打扰的会话也不提醒，未读照记
 * @param message 推来的消息
 * @param currentUserId 当前用户
 * @param muted 这个会话是不是免打扰
 */
export function shouldAlertChatMessage(message: ChatMessageItem, currentUserId: string, muted: boolean): boolean {
  if (muted || message.senderUserId === currentUserId || message.messageType === 'System') {
    return false
  }
  return message.messageType !== 'Call' || isMissedCallRecord(message.content, currentUserId)
}

/**
 * 桌面通知的标题与正文：群里是「群名 / 谁: 内容」，单聊与助手会话是「对方 / 内容」
 * @param conversation 推送里的会话摘要
 * @param sender 发送人的名字
 * @param preview 消息预览
 */
export function chatDesktopNotificationText(conversation: ChatConversationSnapshot, sender: string, preview: string): { title: string, body: string } {
  const grouped = conversation.conversationType === 'Group' || conversation.conversationType === 'Department'
  return grouped && conversation.conversationName
    ? {
        title: conversation.conversationName,
        body: `${sender}: ${preview}`,
      }
    : {
        title: sender,
        body: preview,
      }
}
