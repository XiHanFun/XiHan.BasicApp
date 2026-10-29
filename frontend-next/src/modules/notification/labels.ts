/**
 * 通知包两页（通知公告、我的消息）共用的枚举文案、语气与报错文案。6.0 没有枚举元数据接口，取值是契约字面量，
 * 文案键按字面量逐个静态写出，供 i18n 门禁核对。
 */
import type { Tone } from '@xihan-ui/core'
import type { NotificationContentFormat, NotificationPriority, NotificationTargetType, NotificationType } from '~/contracts'
import { ApiError } from '~/request'

export type Translate = (key: string, values?: Record<string, unknown>) => string

/** 报错文案：后端的问题详情只取正文（已按请求语言本地化），其余取消息；都没有时为空串，由调用点补上兜底文案 */
export function errorText(error: unknown): string {
  if (error instanceof ApiError)
    return error.detail
  return error instanceof Error ? error.message : String(error ?? '')
}

export function notificationTypeLabel(t: Translate, type: NotificationType): string {
  switch (type) {
    case 'Security': return t('notification.notifications.type_security')
    case 'Business': return t('notification.notifications.type_business')
    case 'Todo': return t('notification.notifications.type_todo')
    case 'Emergency': return t('notification.notifications.type_emergency')
    default: return t('notification.notifications.type_system')
  }
}

/** 类型标签的语气（4.x 同）：系统 info、安全 warning、业务 success、待办 neutral、紧急 danger */
export function notificationTypeTone(type: NotificationType): Tone {
  switch (type) {
    case 'Security': return 'warning'
    case 'Business': return 'success'
    case 'Todo': return 'neutral'
    case 'Emergency': return 'danger'
    default: return 'info'
  }
}

export function targetTypeLabel(t: Translate, type: NotificationTargetType): string {
  switch (type) {
    case 'Role': return t('notification.notifications.target_role')
    case 'Department': return t('notification.notifications.target_department')
    case 'User': return t('notification.notifications.target_user')
    default: return t('notification.notifications.target_all')
  }
}

export function priorityLabel(t: Translate, priority: NotificationPriority): string {
  switch (priority) {
    case 'Low': return t('notification.notifications.priority_low')
    case 'High': return t('notification.notifications.priority_high')
    case 'Urgent': return t('notification.notifications.priority_urgent')
    default: return t('notification.notifications.priority_normal')
  }
}

export function contentFormatLabel(t: Translate, format: NotificationContentFormat): string {
  switch (format) {
    case 'Markdown': return t('notification.notifications.format_markdown')
    case 'Html': return t('notification.notifications.format_html')
    default: return t('notification.notifications.format_text')
  }
}
