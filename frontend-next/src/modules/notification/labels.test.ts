import { describe, expect, it, vi } from 'vitest'
import { createI18n } from 'vue-i18n'
import { LOCALES } from '~/locales'
import { assertBatch, enabledOptions, MESSAGING_CODE_PATTERN, messagingFailureMessage, sendStatusLabel, sendStatusTone, smsProviderLabel, templateChannelLabel } from './labels'

/** 假翻译：键原样返回 */
const t = (key: string) => key

type Messages = Record<string, unknown>
const locales = import.meta.glob<{ default: Messages }>('./locales/*.ts', { eager: true })

function flatten(value: Messages, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return child && typeof child === 'object' ? flatten(child as Messages, path) : [path]
  })
}

describe('发送记录、模板与配置共用的文案', () => {
  it('发送状态的文案与语气（4.x 同）', () => {
    expect(sendStatusLabel(t, 'Pending')).toBe('notification.messaging.statuses.pending')
    expect(sendStatusLabel(t, 'Cancelled')).toBe('notification.messaging.statuses.cancelled')
    expect(sendStatusTone('Pending')).toBe('info')
    expect(sendStatusTone('Sending')).toBe('warning')
    expect(sendStatusTone('Success')).toBe('success')
    expect(sendStatusTone('Failed')).toBe('danger')
    expect(sendStatusTone('Cancelled')).toBe('neutral')
    expect(templateChannelLabel(t, 'Sms')).toBe('notification.messaging.channels.sms')
    expect(smsProviderLabel(t, 'Tencent')).toBe('notification.messaging.providers.tencent')
    expect(enabledOptions(t).map(option => option.value)).toEqual(['true', 'false'])
  })

  it('编码文法与后端一致：小写字母或数字开头，只含小写字母、数字、. _ -，最长 64', () => {
    expect(MESSAGING_CODE_PATTERN.test('auth-email-login-code')).toBe(true)
    expect(MESSAGING_CODE_PATTERN.test('smtp.main_2')).toBe(true)
    expect(MESSAGING_CODE_PATTERN.test('-smtp')).toBe(false)
    expect(MESSAGING_CODE_PATTERN.test('Smtp')).toBe(false)
    expect(MESSAGING_CODE_PATTERN.test('smtp main')).toBe(false)
    expect(MESSAGING_CODE_PATTERN.test('a'.repeat(64))).toBe(true)
    expect(MESSAGING_CODE_PATTERN.test('a'.repeat(65))).toBe(false)
  })

  it('批量结果逐项拒绝即抛错：认得的错误码翻成文案、去重后拼成一条，其余原样', () => {
    expect(messagingFailureMessage(t, 'NOTIFICATION_CONFLICT_005')).toBe('notification.messaging.failures.default_locked')
    expect(messagingFailureMessage(t, 'NOTIFICATION_CONFLICT_006')).toBe('notification.messaging.failures.record_busy')
    expect(messagingFailureMessage(t, 'NOTIFICATION_CONFLICT_007')).toBe('notification.messaging.failures.record_not_pending')
    expect(messagingFailureMessage(t, 'NOTIFICATION_NOTFOUND_007')).toBe('notification.messaging.failures.record_not_found')
    expect(messagingFailureMessage(t, 'KERNEL_UNEXPECTED_000')).toBe('KERNEL_UNEXPECTED_000')
    expect(() => assertBatch(t, { failed: [] })).not.toThrow()
    expect(() => assertBatch(t, { failed: [{ code: 'NOTIFICATION_CONFLICT_006' }, { code: 'NOTIFICATION_CONFLICT_006' }, { code: 'X_1' }] }))
      .toThrow('notification.messaging.failures.record_busy; X_1')
  })
})

describe('通知包的文案', () => {
  // 用独立的实例只装本包文案：不依赖测试初始化里合并好的全局实例
  const i18n = createI18n({ legacy: false, locale: 'zh-CN', messages: {} })
  for (const locale of LOCALES)
    i18n.global.mergeLocaleMessage(locale, locales[`./locales/${locale}.ts`]!.default)

  it('十种区域的每条文案都能编译：花括号与 @ 已转义、占位符照常插值', () => {
    const warn = vi.spyOn(console, 'warn')
    for (const locale of LOCALES) {
      const messages = locales[`./locales/${locale}.ts`]!.default
      for (const key of flatten(messages)) {
        const text = i18n.global.t(key, { name: 'N', title: 'T', count: 1, n: 1, target: 'X', time: 'Z' }, { locale })
        expect(text, `${locale} ${key}`).not.toBe(key)
        expect(text, `${locale} ${key}`).not.toContain('{\'')
      }
    }
    expect(warn).not.toHaveBeenCalled()
    expect(i18n.global.t('notification.templates.form_content_hint', {}, { locale: 'zh-CN' })).toContain('{{ code }}')
    expect(i18n.global.t('notification.email_configs.form.from_email_placeholder', {}, { locale: 'en-US' })).toBe('e.g. noreply@example.com')
  })
})
