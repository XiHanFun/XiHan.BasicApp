/**
 * 通知包各页（通知公告、发送记录、消息模板、邮件 / 短信 / 机器人配置、Telegram 机器人、我的消息）共用的枚举文案、语气与报错文案。
 * 6.0 没有枚举元数据接口，取值是契约字面量，文案键按字面量逐个静态写出，供 i18n 门禁核对。
 */
import type { Tone } from '@xihan-ui/core'
import type { BotProvider, DeliveryChannel, MessageSendStatus, NotificationContentFormat, NotificationPriority, NotificationTargetType, NotificationType, SmsProvider, TemplateChannel } from '~/contracts'
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

export function deliveryChannelLabel(t: Translate, channel: DeliveryChannel): string {
  switch (channel) {
    case 'Email': return t('notification.notifications.channel_email')
    case 'Sms': return t('notification.notifications.channel_sms')
    case 'Bot': return t('notification.notifications.channel_bot')
    default: return t('notification.notifications.channel_in_app')
  }
}

export function sendStatusLabel(t: Translate, status: MessageSendStatus): string {
  switch (status) {
    case 'Sending': return t('notification.messaging.statuses.sending')
    case 'Success': return t('notification.messaging.statuses.success')
    case 'Failed': return t('notification.messaging.statuses.failed')
    case 'Cancelled': return t('notification.messaging.statuses.cancelled')
    default: return t('notification.messaging.statuses.pending')
  }
}

/** 发送状态的语气（4.x 同）：排队中 info、发送中 warning、成功 success、失败 danger、已取消 neutral */
export function sendStatusTone(status: MessageSendStatus): Tone {
  switch (status) {
    case 'Sending': return 'warning'
    case 'Success': return 'success'
    case 'Failed': return 'danger'
    case 'Cancelled': return 'neutral'
    default: return 'info'
  }
}

export function templateChannelLabel(t: Translate, channel: TemplateChannel): string {
  switch (channel) {
    case 'InApp': return t('notification.messaging.channels.in_app')
    case 'Sms': return t('notification.messaging.channels.sms')
    case 'Bot': return t('notification.messaging.channels.bot')
    default: return t('notification.messaging.channels.email')
  }
}

export function smsProviderLabel(t: Translate, provider: SmsProvider): string {
  return provider === 'Tencent' ? t('notification.messaging.providers.tencent') : t('notification.messaging.providers.aliyun')
}

export function botProviderLabel(t: Translate, provider: BotProvider): string {
  switch (provider) {
    case 'Lark': return t('notification.messaging.bot_providers.lark')
    case 'WeCom': return t('notification.messaging.bot_providers.we_com')
    default: return t('notification.messaging.bot_providers.ding_talk')
  }
}

/** 启用状态的搜索选项：后端按 `isEnabled eq true / false` 精确匹配 */
export function enabledOptions(t: Translate): { label: string, value: string }[] {
  return [
    { label: t('notification.messaging.enabled'), value: 'true' },
    { label: t('notification.messaging.disabled'), value: 'false' },
  ]
}

export function enabledLabel(t: Translate, enabled: boolean): string {
  return enabled ? t('notification.messaging.enabled') : t('notification.messaging.disabled')
}

/** 模板与配置的编码文法（与后端 `MessagingCodes.Pattern` 一致） */
export const MESSAGING_CODE_PATTERN = /^[a-z0-9][a-z0-9._-]{0,63}$/

/** 发送记录、模板与配置的批量操作只回失败项的错误码：认得的翻成文案，其余原样 */
export function messagingFailureMessage(t: Translate, code: string): string {
  switch (code) {
    case 'NOTIFICATION_NOTFOUND_003': return t('notification.messaging.failures.template_not_found')
    case 'NOTIFICATION_DENIED_002': return t('notification.messaging.failures.template_other_side')
    case 'NOTIFICATION_NOTFOUND_004':
    case 'NOTIFICATION_NOTFOUND_005':
    case 'NOTIFICATION_NOTFOUND_008': return t('notification.messaging.failures.config_not_found')
    case 'NOTIFICATION_NOTFOUND_009': return t('notification.messaging.failures.bot_not_found')
    case 'NOTIFICATION_NOTFOUND_006':
    case 'NOTIFICATION_NOTFOUND_007': return t('notification.messaging.failures.record_not_found')
    case 'NOTIFICATION_CONFLICT_005': return t('notification.messaging.failures.default_locked')
    case 'NOTIFICATION_CONFLICT_006': return t('notification.messaging.failures.record_busy')
    case 'NOTIFICATION_CONFLICT_007': return t('notification.messaging.failures.record_not_pending')
    default: return code
  }
}

/** 批量结果里有失败项即抛错：逐项原因去重后拼成一条 */
export function assertBatch(t: Translate, result: { failed: readonly { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error([...new Set(result.failed.map(failure => messagingFailureMessage(t, failure.code)))].join('; '))
}
