/** 聊天展示辅助：时间与文件大小格式化（纯函数，i18n 由调用方注入） */

import type { ChatMessageAttachment } from '~/types'
import { ChatMessageType } from '~/types/enums'

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 消息正文/附件的简短标签（回复引用、搜索命中、置顶预览用）：多图/多文件带数量 */
export function messageBodyLabel(message: {
  content?: null | string
  messageType: ChatMessageType
  attachments?: ChatMessageAttachment[] | null
}): string {
  if (message.content) {
    return message.content
  }
  const count = message.attachments?.length ?? 0
  if (message.messageType === ChatMessageType.Image) {
    return count > 1 ? `[图片] ${count}张` : '[图片]'
  }
  if (count > 0) {
    return count > 1 ? `[文件] ${count}个` : `[文件] ${message.attachments?.[0]?.fileName ?? ''}`.trimEnd()
  }
  return ''
}

function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
}

function pad(n: number): string {
  return n.toString().padStart(2, '0')
}

/** 会话列表时间：今天 HH:mm；昨天；同年 M/D；跨年 YYYY/M/D */
export function formatConversationTime(t: Translate, time?: null | string): string {
  if (!time) {
    return ''
  }
  const date = new Date(time)
  if (Number.isNaN(date.getTime())) {
    return ''
  }
  const now = new Date()
  if (isSameDay(date, now)) {
    return `${pad(date.getHours())}:${pad(date.getMinutes())}`
  }
  const yesterday = new Date(now)
  yesterday.setDate(now.getDate() - 1)
  if (isSameDay(date, yesterday)) {
    return t('chat.time.yesterday')
  }
  if (date.getFullYear() === now.getFullYear()) {
    return `${date.getMonth() + 1}/${date.getDate()}`
  }
  return `${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()}`
}

/** 消息时间：今天 HH:mm；否则 M/D HH:mm（跨年带年份） */
export function formatMessageTime(time?: null | string): string {
  if (!time) {
    return ''
  }
  const date = new Date(time)
  if (Number.isNaN(date.getTime())) {
    return ''
  }
  const now = new Date()
  const hm = `${pad(date.getHours())}:${pad(date.getMinutes())}`
  if (isSameDay(date, now)) {
    return hm
  }
  if (date.getFullYear() === now.getFullYear()) {
    return `${date.getMonth() + 1}/${date.getDate()} ${hm}`
  }
  return `${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()} ${hm}`
}

/** 文件大小：B/KB/MB/GB（保留一位小数） */
export function formatFileSize(bytes?: null | number): string {
  if (bytes == null || bytes < 0) {
    return ''
  }
  if (bytes < 1024) {
    return `${bytes} B`
  }
  const units = ['KB', 'MB', 'GB', 'TB']
  let value = bytes / 1024
  let unitIndex = 0
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex += 1
  }
  return `${value.toFixed(1)} ${units[unitIndex]}`
}
