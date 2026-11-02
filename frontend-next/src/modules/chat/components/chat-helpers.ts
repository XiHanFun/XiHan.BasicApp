/** 聊天展示辅助：时间与文件大小格式化、报错文案（纯函数，i18n 由调用方注入） */

import type {
  ChatMessageAttachment,
} from '../types'
import type { ChatMessageType } from '~/contracts'
import { ApiError } from '~/request'
import { formatDateTime, getDisplayTimeZone } from '~/utils'

type Translate = (key: string, named?: Record<string, unknown>) => string

/**
 * 报错文案：后端的问题详情只取正文（已按请求语言本地化），其余取消息；都没有时为空串，由调用点补上兜底文案。
 * 6.0 的请求层不弹统一提示（4.x 的请求层会弹），用户操作的失败由聊天界面自己提示。
 */
export function chatErrorText(error: unknown): string {
  if (error instanceof ApiError)
    return error.detail
  return error instanceof Error ? error.message : ''
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
    return count > 1 ? `[图片] ${count}张` : '[图片]'
  }
  if (message.messageType === 'Voice') {
    return `[语音] ${message.attachments?.[0]?.durationSeconds ?? 0}"`
  }
  if (count > 0) {
    return count > 1 ? `[文件] ${count}个` : `[文件] ${message.attachments?.[0]?.fileName ?? ''}`.trimEnd()
  }
  return ''
}

/** 一个时刻在显示时区里的日历日（YYYY-MM-DD）；空值与解析不出的为 null */
function calendarDayOf(time: string | Date, zone: string): null | string {
  const day = formatDateTime(time, zone, 'YYYY-MM-DD')
  return day === '-' ? null : day
}

/** 日历日的前一天（按日历算，不受夏令时影响） */
function previousDay(day: string): string {
  const [year, month, date] = day.split('-').map(Number)
  return new Date(Date.UTC(year!, month! - 1, date! - 1)).toISOString().slice(0, 10)
}

/** 会话列表时间：今天 HH:mm；昨天；同年 M/D；跨年 YYYY/M/D（按应用时区，同审计页的 formatDate） */
export function formatConversationTime(t: Translate, time?: null | string): string {
  const zone = getDisplayTimeZone()
  const day = time ? calendarDayOf(time, zone) : null
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
  return formatDateTime(time, zone, day.slice(0, 4) === today.slice(0, 4) ? 'M/D' : 'YYYY/M/D')
}

/** 消息时间：今天 HH:mm；否则 M/D HH:mm（跨年带年份；按应用时区） */
export function formatMessageTime(time?: null | string): string {
  const zone = getDisplayTimeZone()
  const day = time ? calendarDayOf(time, zone) : null
  if (!time || !day) {
    return ''
  }
  const today = calendarDayOf(new Date(), zone)!
  if (day === today) {
    return formatDateTime(time, zone, 'HH:mm')
  }
  return formatDateTime(time, zone, day.slice(0, 4) === today.slice(0, 4) ? 'M/D HH:mm' : 'YYYY/M/D HH:mm')
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
