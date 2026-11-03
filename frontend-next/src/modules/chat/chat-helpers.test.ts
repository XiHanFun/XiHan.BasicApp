/**
 * components/chat-helpers.ts 纯函数行为
 *
 * 职责边界：只测消息摘要标签、会话/消息时间格式化、文件大小格式化与报错文案这五个导出函数的全分支与边界
 * 时间相关用例一律 vi.setSystemTime 固定“现在”。应用时区没设时跟随浏览器，这时用不带时区标记的 ISO 串作为输入
 * （`2026-08-27T09:05:00` 按本地时区解析），结果与本机时区无关；设了应用时区的用例用 UTC 时刻作为输入
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '~/request'
import { setDisplayTimeZone } from '~/utils'
import { chatErrorText, formatConversationTime, formatFileSize, formatMessageTime, messageBodyLabel } from './components/chat-helpers'

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
      messageType: 'Image',
      attachments: [{ fileId: '1', fileName: 'a.png' }],
    })

    expect(label).toBe('看这张图')
  })

  it('正文为空串按无正文处理，落到附件分支', () => {
    const label = messageBodyLabel({
      content: '',
      messageType: 'Image',
      attachments: [{ fileId: '1', fileName: 'a.png' }],
    })

    expect(label).toBe('[图片]')
  })

  it('正文为 null 且无附件的文本消息返回空串', () => {
    expect(messageBodyLabel({ content: null, messageType: 'Text', attachments: null })).toBe('')
  })

  it('多图消息带出张数，单图不带', () => {
    const many = messageBodyLabel({
      messageType: 'Image',
      attachments: [
        { fileId: '1', fileName: 'a.png' },
        { fileId: '2', fileName: 'b.png' },
      ],
    })

    expect(many).toBe('[图片] 2张')
    expect(messageBodyLabel({ messageType: 'Image', attachments: [] })).toBe('[图片]')
  })

  it('语音消息取首个附件时长，缺时长按 0 秒展示', () => {
    const withDuration = messageBodyLabel({
      messageType: 'Voice',
      attachments: [{ fileId: '1', fileName: 'v.webm', durationSeconds: 12 }],
    })

    expect(withDuration).toBe('[语音] 12"')
    expect(messageBodyLabel({ messageType: 'Voice', attachments: [] })).toBe('[语音] 0"')
    expect(messageBodyLabel({ messageType: 'Voice' })).toBe('[语音] 0"')
  })

  it('语音分支排在附件计数之前，带多个附件的语音仍显示时长而非文件数', () => {
    const label = messageBodyLabel({
      messageType: 'Voice',
      attachments: [
        { fileId: '1', fileName: 'v.webm', durationSeconds: 3 },
        { fileId: '2', fileName: 'v2.webm', durationSeconds: 9 },
      ],
    })

    expect(label).toBe('[语音] 3"')
  })

  it('单文件带文件名、多文件只给个数', () => {
    const single = messageBodyLabel({
      messageType: 'File',
      attachments: [{ fileId: '1', fileName: '合同.pdf' }],
    })
    const many = messageBodyLabel({
      messageType: 'File',
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
      messageType: 'File',
      attachments: [{ fileId: '1', fileName: '' }],
    })

    expect(label).toBe('[文件]')
  })

  it('文本消息若挂了附件，按文件分支给出个数（附件计数不看消息类型）', () => {
    const label = messageBodyLabel({
      messageType: 'Text',
      attachments: [
        { fileId: '1', fileName: 'a.txt' },
        { fileId: '2', fileName: 'b.txt' },
      ],
    })

    expect(label).toBe('[文件] 2个')
  })

  it('emoji 与超长正文原样返回，不做截断（截断是会话预览 previewOf 的职责）', () => {
    const long = '一'.repeat(500)

    expect(messageBodyLabel({ content: '🎉🎉', messageType: 'Text' })).toBe('🎉🎉')
    expect(messageBodyLabel({ content: long, messageType: 'Text' })).toBe(long)
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

// 后端的时间是 UTC 时刻；聊天与审计页一样按偏好设置的应用时区显示，「今天 / 昨天」也按应用时区的日历日算
describe('按应用时区显示', () => {
  beforeEach(() => {
    // 2026-08-27 15:30 UTC：东京已是 28 日 00:30，纽约是 27 日 11:30
    vi.setSystemTime(new Date(Date.UTC(2026, 7, 27, 15, 30, 0)))
  })

  afterEach(() => {
    setDisplayTimeZone('')
  })

  it('会话列表时间随应用时区换日', () => {
    setDisplayTimeZone('Asia/Tokyo')
    expect(formatConversationTime(echoTranslate, '2026-08-27T15:10:00Z')).toBe('00:10')
    expect(formatConversationTime(echoTranslate, '2026-08-27T14:50:00Z')).toBe('chat.time.yesterday')
    expect(formatConversationTime(echoTranslate, '2026-08-26T14:50:00Z')).toBe('8/26')

    setDisplayTimeZone('America/New_York')
    expect(formatConversationTime(echoTranslate, '2026-08-27T15:10:00Z')).toBe('11:10')
    expect(formatConversationTime(echoTranslate, '2026-08-27T03:50:00Z')).toBe('chat.time.yesterday')
  })

  it('消息时间随应用时区换算时刻与日期', () => {
    setDisplayTimeZone('Asia/Tokyo')
    expect(formatMessageTime('2026-08-27T15:10:00Z')).toBe('00:10')
    expect(formatMessageTime('2026-08-27T14:50:00Z')).toBe('8/27 23:50')
    expect(formatMessageTime('2025-12-31T15:30:00Z')).toBe('1/1 00:30')
    expect(formatMessageTime('2025-12-31T14:30:00Z')).toBe('2025/12/31 23:30')

    setDisplayTimeZone('America/New_York')
    expect(formatMessageTime('2026-08-27T14:50:00Z')).toBe('10:50')
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

describe('chatErrorText 报错文案', () => {
  it('后端的问题详情只取已本地化的正文，不带错误码前缀', () => {
    const error = new ApiError(409, { code: 'CHAT_CONFLICT_001', detail: '对方不是会话成员' })

    expect(error.message).toContain('CHAT_CONFLICT_001')
    expect(chatErrorText(error)).toBe('对方不是会话成员')
  })

  it('普通异常取消息，非异常值给空串交由调用点兜底', () => {
    expect(chatErrorText(new Error('网络断了'))).toBe('网络断了')
    expect(chatErrorText('oops')).toBe('')
    expect(chatErrorText(undefined)).toBe('')
  })
})
