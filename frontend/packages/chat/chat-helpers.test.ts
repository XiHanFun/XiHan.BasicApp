/**
 * packages/chat/components/chat-helpers.ts 纯函数行为。
 *
 * 职责边界：只测消息摘要标签、会话/消息时间格式化与文件大小格式化这四个导出函数的全分支与边界。
 * 时间相关用例一律 vi.setSystemTime 固定“现在”，并用不带时区标记的 ISO 串作为输入
 * （`2026-08-27T09:05:00` 按本地时区解析），因此结果与本机时区无关。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { formatConversationTime, formatFileSize, formatMessageTime, messageBodyLabel } from './components/chat-helpers'
import { ChatMessageType } from './enums'

/** 测试用翻译器：直接回显 key，便于断言走到了 i18n 分支 */
function echoTranslate(key: string): string {
  return key
}

beforeEach(() => {
  vi.useFakeTimers()
  // 2026-08-27 15:30:00（本地时区）
  vi.setSystemTime(new Date(2026, 7, 27, 15, 30, 0))
})

afterEach(() => {
  vi.useRealTimers()
})

describe('messageBodyLabel 摘要标签', () => {
  it('有正文时原样返回正文，图片类型也不改写', () => {
    const label = messageBodyLabel({
      content: '看这张图',
      messageType: ChatMessageType.Image,
      attachments: [{ fileId: '1', fileName: 'a.png' }],
    })

    expect(label).toBe('看这张图')
  })

  it('正文为空串按无正文处理，落到附件分支', () => {
    const label = messageBodyLabel({
      content: '',
      messageType: ChatMessageType.Image,
      attachments: [{ fileId: '1', fileName: 'a.png' }],
    })

    expect(label).toBe('[图片]')
  })

  it('正文为 null 且无附件的文本消息返回空串', () => {
    expect(messageBodyLabel({ content: null, messageType: ChatMessageType.Text, attachments: null })).toBe('')
  })

  it('多图消息带出张数，单图不带', () => {
    const many = messageBodyLabel({
      messageType: ChatMessageType.Image,
      attachments: [
        { fileId: '1', fileName: 'a.png' },
        { fileId: '2', fileName: 'b.png' },
      ],
    })

    expect(many).toBe('[图片] 2张')
    expect(messageBodyLabel({ messageType: ChatMessageType.Image, attachments: [] })).toBe('[图片]')
  })

  it('语音消息取首个附件时长，缺时长按 0 秒展示', () => {
    const withDuration = messageBodyLabel({
      messageType: ChatMessageType.Voice,
      attachments: [{ fileId: '1', fileName: 'v.webm', durationSeconds: 12 }],
    })

    expect(withDuration).toBe('[语音] 12"')
    expect(messageBodyLabel({ messageType: ChatMessageType.Voice, attachments: [] })).toBe('[语音] 0"')
    expect(messageBodyLabel({ messageType: ChatMessageType.Voice })).toBe('[语音] 0"')
  })

  it('语音分支排在附件计数之前，带多个附件的语音仍显示时长而非文件数', () => {
    const label = messageBodyLabel({
      messageType: ChatMessageType.Voice,
      attachments: [
        { fileId: '1', fileName: 'v.webm', durationSeconds: 3 },
        { fileId: '2', fileName: 'v2.webm', durationSeconds: 9 },
      ],
    })

    expect(label).toBe('[语音] 3"')
  })

  it('单文件带文件名、多文件只给个数', () => {
    const single = messageBodyLabel({
      messageType: ChatMessageType.File,
      attachments: [{ fileId: '1', fileName: '合同.pdf' }],
    })
    const many = messageBodyLabel({
      messageType: ChatMessageType.File,
      attachments: [
        { fileId: '1', fileName: '合同.pdf' },
        { fileId: '2', fileName: '附件.docx' },
      ],
    })

    expect(single).toBe('[文件] 合同.pdf')
    expect(many).toBe('[文件] 2个')
  })

  it('单文件缺文件名时尾部空格被裁掉，不留下“[文件] ”', () => {
    const label = messageBodyLabel({
      messageType: ChatMessageType.File,
      attachments: [{ fileId: '1', fileName: '' }],
    })

    expect(label).toBe('[文件]')
  })

  it('文本消息若挂了附件，按文件分支给出个数（附件计数不看消息类型）', () => {
    const label = messageBodyLabel({
      messageType: ChatMessageType.Text,
      attachments: [
        { fileId: '1', fileName: 'a.txt' },
        { fileId: '2', fileName: 'b.txt' },
      ],
    })

    expect(label).toBe('[文件] 2个')
  })

  it('emoji 与超长正文原样返回，不做截断（截断是会话预览 previewOf 的职责）', () => {
    const long = '一'.repeat(500)

    expect(messageBodyLabel({ content: '🎉🎉', messageType: ChatMessageType.Text })).toBe('🎉🎉')
    expect(messageBodyLabel({ content: long, messageType: ChatMessageType.Text })).toBe(long)
  })
})

describe('formatConversationTime 会话列表时间', () => {
  it('空值与非法时间串返回空串', () => {
    expect(formatConversationTime(echoTranslate, null)).toBe('')
    expect(formatConversationTime(echoTranslate, undefined)).toBe('')
    expect(formatConversationTime(echoTranslate, '')).toBe('')
    expect(formatConversationTime(echoTranslate, '不是时间')).toBe('')
  })

  it('今天的消息只显示 HH:mm 且补零', () => {
    expect(formatConversationTime(echoTranslate, '2026-08-27T09:05:00')).toBe('09:05')
    expect(formatConversationTime(echoTranslate, '2026-08-27T00:00:00')).toBe('00:00')
  })

  it('昨天的消息走 i18n 的“昨天”键而非日期', () => {
    expect(formatConversationTime(echoTranslate, '2026-08-26T23:59:00')).toBe('chat.time.yesterday')
  })

  it('同年非昨天显示 M/D，月日不补零', () => {
    expect(formatConversationTime(echoTranslate, '2026-01-05T08:00:00')).toBe('1/5')
  })

  it('跨年显示 YYYY/M/D', () => {
    expect(formatConversationTime(echoTranslate, '2024-12-31T08:00:00')).toBe('2024/12/31')
  })

  it('跨月的“昨天”按自然日回退一天，2026-03-01 的昨天是 2026-02-28', () => {
    vi.setSystemTime(new Date(2026, 2, 1, 10, 0, 0))

    expect(formatConversationTime(echoTranslate, '2026-02-28T22:00:00')).toBe('chat.time.yesterday')
  })

  it('跨年的“昨天”优先于跨年日期格式，元旦看昨天仍是“昨天”', () => {
    vi.setSystemTime(new Date(2026, 0, 1, 10, 0, 0))

    expect(formatConversationTime(echoTranslate, '2025-12-31T22:00:00')).toBe('chat.time.yesterday')
  })
})

describe('formatMessageTime 消息气泡时间', () => {
  it('空值与非法时间串返回空串', () => {
    expect(formatMessageTime(null)).toBe('')
    expect(formatMessageTime(undefined)).toBe('')
    expect(formatMessageTime('')).toBe('')
    expect(formatMessageTime('bad-input')).toBe('')
  })

  it('今天只显示 HH:mm', () => {
    expect(formatMessageTime('2026-08-27T07:08:00')).toBe('07:08')
  })

  it('同年非今天显示 M/D HH:mm，且不认“昨天”这一说', () => {
    expect(formatMessageTime('2026-08-26T23:59:00')).toBe('8/26 23:59')
  })

  it('跨年显示 YYYY/M/D HH:mm', () => {
    expect(formatMessageTime('2025-12-31T23:59:00')).toBe('2025/12/31 23:59')
  })
})

describe('formatFileSize 文件大小', () => {
  it('空值与负数返回空串', () => {
    expect(formatFileSize(null)).toBe('')
    expect(formatFileSize(undefined)).toBe('')
    expect(formatFileSize(-1)).toBe('')
  })

  // 回归锚点（缺陷 41）：守卫原先只挡 null 与负数，NaN / Infinity 会一路走到换算，
  // 渲染出 "NaN KB" / "Infinity TB" 摆到界面上（附件 fileSize 缺失被 Number() 化即为 NaN）
  it('非有限数（NaN / Infinity）按无效输入返回空串，不渲染出 "NaN KB"', () => {
    expect(formatFileSize(Number.NaN)).toBe('')
    expect(formatFileSize(Number.POSITIVE_INFINITY)).toBe('')
    expect(formatFileSize(Number.NEGATIVE_INFINITY)).toBe('')
  })

  it('小于 1024 字节保持整数 B，不换算', () => {
    expect(formatFileSize(0)).toBe('0 B')
    expect(formatFileSize(1)).toBe('1 B')
    expect(formatFileSize(1023)).toBe('1023 B')
  })

  it('恰好 1024 字节进位到 KB 并保留一位小数', () => {
    expect(formatFileSize(1024)).toBe('1.0 KB')
    expect(formatFileSize(1536)).toBe('1.5 KB')
  })

  it('按 1024 逐级进位到 MB / GB / TB', () => {
    expect(formatFileSize(1024 ** 2)).toBe('1.0 MB')
    expect(formatFileSize(1024 ** 3)).toBe('1.0 GB')
    expect(formatFileSize(1024 ** 4)).toBe('1.0 TB')
  })

  it('最高单位止于 TB，超出后继续用 TB 计数而不是溢出到未知单位', () => {
    expect(formatFileSize(1024 ** 5)).toBe('1024.0 TB')
  })

  it('小数位按四舍五入保留一位', () => {
    expect(formatFileSize(1280)).toBe('1.3 KB')
    expect(formatFileSize(1074)).toBe('1.0 KB')
  })
})
