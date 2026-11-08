/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 聊天展示辅助：时间与文件大小格式化、报错文案（纯函数，i18n 由调用方注入） */

import type {
  ChatMessageAttachment,
} from '../types'
import type { ChatConversationType, ChatMessageType } from '~/contracts'
import { ApiError } from '~/request'
import { formatDateTime, getDisplayTimeZone } from '~/utils'

type Translate = (key: string, named?: Record<string, unknown>) => string

/**
 * 报错文案：问题详情取正文（已本地化），其余取消息
 * 都没有时为空串，由调用点补兜底文案
 * 请求层不弹统一提示，用户操作的失败由聊天界面自己提示
 */
export function chatErrorText(error: unknown): string {
  if (error instanceof ApiError)
    return error.detail
  return error instanceof Error
    ? error.message
    : ''
}

/** 是否显示已读回执：助手不读消息，助手会话显示了会永远「未读」 */
export function hasReadReceipt(conversationType: ChatConversationType | undefined): boolean {
  return conversationType !== 'Assistant'
}

/**
 * 助手回复渲染前把图片语法降成链接
 * 回复受知识库片段与工具结果影响，自动加载的图片地址能把对话内容带到外部
 */
export function assistantMarkdownSource(text: string): string {
  return text.replace(/!\[/g, '[')
}

/** 会话列表与消息提醒的一行预览：助手回复去掉 Markdown 标记，别的会话照原文 */
export function previewText(conversationType: ChatConversationType | undefined, text: string): string {
  if (conversationType !== 'Assistant') {
    return text
  }

  return text
    .replace(/```[^\n]*\n?/g, '')
    .replace(/!?\[([^\]]*)\]\([^)]*\)?/g, '$1')
    .replace(/^[ \t:|-]+$/gm, '')
    .replace(/^[ \t]*(?:#{1,6}[ \t]+|[-*+][ \t]+|>[ \t]?)/gm, '')
    .replace(/[*~`]|__/g, '')
    .replace(/\|/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

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
  if (message.messageType === 'Image') {
    return count > 1
      ? `[图片] ${count}张`
      : '[图片]'
  }
  if (message.messageType === 'Voice') {
    return `[语音] ${message.attachments?.[0]?.durationSeconds ?? 0}"`
  }
  if (count > 0) {
    return count > 1
      ? `[文件] ${count}个`
      : `[文件] ${message.attachments?.[0]?.fileName ?? ''}`.trimEnd()
  }
  return ''
}

/** 一个时刻在显示时区里的日历日（YYYY-MM-DD）；空值与解析不出的为 null */
function calendarDayOf(time: string | Date, zone: string): null | string {
  const day = formatDateTime(time, zone, 'YYYY-MM-DD')
  return day === '-'
    ? null
    : day
}

/** 日历日的前一天（按日历算，不受夏令时影响） */
function previousDay(day: string): string {
  const [year, month, date] = day.split('-').map(Number)
  return new Date(Date.UTC(year!, month! - 1, date! - 1)).toISOString().slice(0, 10)
}

/**
 * 会话列表时间：今天 HH:mm、昨天、同年 M/D、跨年 YYYY/M/D
 * 按应用时区，与审计页的 formatDate 一致
 */
export function formatConversationTime(t: Translate, time?: null | string): string {
  const zone = getDisplayTimeZone()
  const day = time
    ? calendarDayOf(time, zone)
    : null
  if (!time || !day) {
    return ''
  }
  const today = calendarDayOf(new Date(), zone)!
  if (day === today) {
    return formatDateTime(time, zone, 'HH:mm')
  }
  if (day === previousDay(today)) {
    return t('chat.time.yesterday')
  }
  return formatDateTime(time, zone, day.slice(0, 4) === today.slice(0, 4)
    ? 'M/D'
    : 'YYYY/M/D')
}

/** 消息时间：今天 HH:mm；否则 M/D HH:mm（跨年带年份；按应用时区） */
export function formatMessageTime(time?: null | string): string {
  const zone = getDisplayTimeZone()
  const day = time
    ? calendarDayOf(time, zone)
    : null
  if (!time || !day) {
    return ''
  }
  const today = calendarDayOf(new Date(), zone)!
  if (day === today) {
    return formatDateTime(time, zone, 'HH:mm')
  }
  return formatDateTime(time, zone, day.slice(0, 4) === today.slice(0, 4)
    ? 'M/D HH:mm'
    : 'YYYY/M/D HH:mm')
}

/** 文件大小：B/KB/MB/GB（保留一位小数） */
export function formatFileSize(bytes?: null | number): string {
  // NaN / ±Infinity 也算无效输入：后端字段缺失被 Number() 化即为 NaN，
  // 不挡住会一路渲染成 "NaN KB" / "Infinity TB" 摆到界面上
  if (bytes == null || !Number.isFinite(bytes) || bytes < 0) {
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
