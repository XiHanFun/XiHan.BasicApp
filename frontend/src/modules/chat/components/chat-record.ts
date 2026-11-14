/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 聊天记录（合并转发出来的消息）的解析与文案，以及哪些消息能被选去转发（纯函数，i18n 由调用方注入） */

import type { ChatMessageAttachment, ChatMessageItem } from '../types'
import type { ChatConversationType, ChatMessageType } from '~/contracts'
import { ChatConversationType as ChatConversationTypes, ChatMessageType as ChatMessageTypes } from '~/contracts'
import { messageBodyLabel } from './chat-helpers'

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 一次最多转发多少条（与后端 ChatRules.MaxForwardMessages 一致） */
export const CHAT_MAX_FORWARD_MESSAGES = 50

/** 聊天记录卡片上预览几行 */
const RECORD_PREVIEW_LINES = 4

/** 聊天记录里的一条消息（后端 ChatRecordItem） */
export interface ChatRecordItem {
  senderUserId: null | string
  senderUserName: null | string
  /** 原来的发送时间（UTC，ISO 8601） */
  createdAt: string
  messageType: ChatMessageType
  /** 正文；聊天记录套着聊天记录时是里面那份的 JSON */
  content: null | string
  attachments: ChatMessageAttachment[]
}

/** 聊天记录消息的正文（后端 ChatRecord，合并转发时由服务端写进消息的 content） */
export interface ChatRecord {
  /** 来自什么会话 */
  sourceType: ChatConversationType
  /** 来自的群名；单聊与助手会话没有 */
  sourceName: null | string
  /** 单聊是两个人的名字，助手会话是里面消息的发送人；群为空 */
  participants: string[]
  items: ChatRecordItem[]
}

function attachmentsOf(value: unknown): ChatMessageAttachment[] | null {
  if (!Array.isArray(value)) {
    return null
  }
  const attachments: ChatMessageAttachment[] = []
  for (const entry of value) {
    if (typeof entry !== 'object' || entry === null) {
      return null
    }
    const attachment = entry as Record<string, unknown>
    if (typeof attachment.fileId !== 'string' || typeof attachment.fileName !== 'string') {
      return null
    }
    attachments.push({
      fileId: attachment.fileId,
      fileName: attachment.fileName,
      fileSize: typeof attachment.fileSize === 'number'
        ? attachment.fileSize
        : null,
      durationSeconds: typeof attachment.durationSeconds === 'number'
        ? attachment.durationSeconds
        : null,
    })
  }
  return attachments
}

function itemOf(value: unknown): ChatRecordItem | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }
  const item = value as Record<string, unknown>
  const messageType = ChatMessageTypes.find(type => type === item.messageType)
  const attachments = attachmentsOf(item.attachments)
  if (!messageType || !attachments || typeof item.createdAt !== 'string') {
    return null
  }
  return {
    senderUserId: typeof item.senderUserId === 'string'
      ? item.senderUserId
      : null,
    senderUserName: typeof item.senderUserName === 'string'
      ? item.senderUserName
      : null,
    createdAt: item.createdAt,
    messageType,
    content: typeof item.content === 'string'
      ? item.content
      : null,
    attachments,
  }
}

/**
 * 解析聊天记录消息的正文
 * @returns 聊天记录；正文为空、不是 JSON 或字段不对的为 null
 */
export function parseChatRecord(content?: null | string): ChatRecord | null {
  if (!content) {
    return null
  }
  let parsed: unknown
  try {
    parsed = JSON.parse(content)
  }
  catch {
    return null
  }
  if (typeof parsed !== 'object' || parsed === null) {
    return null
  }
  const record = parsed as Record<string, unknown>
  const sourceType = ChatConversationTypes.find(type => type === record.sourceType)
  if (!sourceType || !Array.isArray(record.items) || !Array.isArray(record.participants)) {
    return null
  }
  const items = record.items.map(itemOf)
  if (items.includes(null) || !record.participants.every(name => typeof name === 'string')) {
    return null
  }
  return {
    sourceType,
    sourceName: typeof record.sourceName === 'string'
      ? record.sourceName
      : null,
    participants: record.participants as string[],
    items: items as ChatRecordItem[],
  }
}

/** 聊天记录的标题：群的带群名，单聊带两个人的名字，都没有就只说聊天记录 */
export function chatRecordTitle(t: Translate, record: ChatRecord): string {
  if (record.sourceName) {
    return t('chat.record.title_group', { name: record.sourceName })
  }
  const [first, second] = record.participants
  if (first && second) {
    return t('chat.record.title_pair', { a: first, b: second })
  }
  return first
    ? t('chat.record.title_group', { name: first })
    : t('chat.record.title')
}

/** 聊天记录卡片上的几行预览：「名字: 内容」，内容用各类型的简短标签 */
export function chatRecordPreviewLines(record: ChatRecord): string[] {
  return record.items.slice(0, RECORD_PREVIEW_LINES).map((item) => {
    const body = messageBodyLabel(item).replace(/\s+/g, ' ').trim()
    return item.senderUserName
      ? `${item.senderUserName}: ${body}`
      : body
  })
}

/**
 * 这条消息能不能被选去转发：系统提示、通话记录、已撤回与还没落库的不能
 * 与后端转发的规则一致，前端据此不让选，免得一提交整批被拒
 */
export function isForwardableMessage(message: Pick<ChatMessageItem, 'isRecalled' | 'messageType'> & { pending?: boolean, failed?: boolean }): boolean {
  return !message.isRecalled && !message.pending && !message.failed
    && message.messageType !== 'System' && message.messageType !== 'Call'
}
