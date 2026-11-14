/**
 * chat-record.ts：聊天记录（合并转发出来的消息）的解析与文案
 *
 * 职责边界：正文是服务端写的 JSON，读不出来的一律当没有记录（不抛错）；标题按来自什么会话写；
 * 卡片只预览前几行、内容用各类型的简短标签；系统提示、通话记录、已撤回与没落库的消息不能选去转发。
 * 翻译函数用回显键的替身，断言落在键与参数上
 */
import type { ChatRecord } from '@/modules/chat/components/chat-record'
import { describe, expect, it } from 'vitest'
import { CHAT_MAX_FORWARD_MESSAGES, chatRecordPreviewLines, chatRecordTitle, isForwardableMessage, parseChatRecord } from '@/modules/chat/components/chat-record'

/** 回显键与参数的翻译替身 */
function t(key: string, named?: Record<string, unknown>): string {
  return named
    ? `${key}${JSON.stringify(named)}`
    : key
}

function item(patch: Record<string, unknown> = {}) {
  return {
    senderUserId: 'u-1',
    senderUserName: 'alice',
    createdAt: '2026-11-14T08:00:00Z',
    messageType: 'Text',
    content: 'hello',
    attachments: [],
    ...patch,
  }
}

function content(patch: Record<string, unknown> = {}): string {
  return JSON.stringify({
    sourceType: 'Single',
    sourceName: null,
    participants: ['alice', 'bob'],
    items: [
      item(),
    ],
    ...patch,
  })
}

describe('解析聊天记录', () => {
  it('服务端写的正文解得出来自哪个会话与里面的每条消息', () => {
    const parsed = parseChatRecord(content({
      items: [
        item(),
        item({
          senderUserId: null,
          senderUserName: 'bob',
          messageType: 'File',
          content: null,
          attachments: [
            {
              fileId: 'f-1',
              fileName: 'spec.pdf',
              fileSize: 12,
              durationSeconds: null,
            },
          ],
        }),
      ],
    }))

    expect(parsed).toStrictEqual({
      sourceType: 'Single',
      sourceName: null,
      participants: ['alice', 'bob'],
      items: [
        {
          senderUserId: 'u-1',
          senderUserName: 'alice',
          createdAt: '2026-11-14T08:00:00Z',
          messageType: 'Text',
          content: 'hello',
          attachments: [],
        },
        {
          senderUserId: null,
          senderUserName: 'bob',
          createdAt: '2026-11-14T08:00:00Z',
          messageType: 'File',
          content: null,
          attachments: [
            {
              fileId: 'f-1',
              fileName: 'spec.pdf',
              fileSize: 12,
              durationSeconds: null,
            },
          ],
        },
      ],
    })
  })

  it.each([
    ['空', null],
    ['空串', ''],
    ['不是 JSON', '聊天记录'],
    ['不是对象', '[]'],
    [
      '会话类型不认识',
      content({ sourceType: 'Channel' }),
    ],
    [
      '没有消息这一项',
      content({ items: undefined }),
    ],
    [
      '参与者里有不是字符串的',
      content({
        participants: ['alice', 7],
      }),
    ],
    [
      '消息的类型不认识',
      content({
        items: [
          item({ messageType: 'Sticker' }),
        ],
      }),
    ],
    [
      '消息没有发送时间',
      content({
        items: [
          item({ createdAt: undefined }),
        ],
      }),
    ],
    [
      '附件不成样子',
      content({
        items: [
          item({
            attachments: [
              { fileName: 'x' },
            ],
          }),
        ],
      }),
    ],
  ])('%s：读不出记录', (_, value) => {
    expect(parseChatRecord(value)).toBeNull()
  })
})

describe('标题与预览', () => {
  const base = parseChatRecord(content()) as ChatRecord

  it('群的带群名，单聊带两个人的名字，只有一个名字的带它，都没有只说聊天记录', () => {
    expect(chatRecordTitle(t, {
      ...base,
      sourceType: 'Group',
      sourceName: 'Team',
      participants: [],
    })).toBe('chat.record.title_group{"name":"Team"}')
    expect(chatRecordTitle(t, base)).toBe('chat.record.title_pair{"a":"alice","b":"bob"}')
    expect(chatRecordTitle(t, {
      ...base,
      participants: ['alice'],
    })).toBe('chat.record.title_group{"name":"alice"}')
    expect(chatRecordTitle(t, { ...base, participants: [] })).toBe('chat.record.title')
  })

  it('卡片预览前四行「名字: 内容」：附件与套着的聊天记录用简短标签，正文里的换行并成一行', () => {
    const parsed = parseChatRecord(content({
      items: [
        item({ content: 'line one\nline two' }),
        item({
          senderUserName: 'bob',
          messageType: 'Image',
          content: null,
          attachments: [
            { fileId: 'f-1', fileName: 'a.png' },
            { fileId: 'f-2', fileName: 'b.png' },
          ],
        }),
        item({
          messageType: 'Record',
          content: content(),
        }),
        item({ senderUserName: null, content: 'no name' }),
        item({ content: 'fifth' }),
      ],
    })) as ChatRecord

    expect(chatRecordPreviewLines(parsed)).toStrictEqual([
      'alice: line one line two',
      'bob: [图片] 2张',
      'alice: [聊天记录]',
      'no name',
    ])
  })
})

describe('能不能选去转发', () => {
  it('系统提示、通话记录、已撤回与还没落库的不能；别的（含聊天记录与助手回复）都能', () => {
    const message = (patch: Record<string, unknown>) => ({ isRecalled: false, messageType: 'Text' as const, ...patch })

    expect(isForwardableMessage(message({}))).toBe(true)
    expect(isForwardableMessage(message({ messageType: 'Record' }))).toBe(true)
    expect(isForwardableMessage(message({ messageType: 'Assistant' }))).toBe(true)
    expect(isForwardableMessage(message({ messageType: 'System' }))).toBe(false)
    expect(isForwardableMessage(message({ messageType: 'Call' }))).toBe(false)
    expect(isForwardableMessage(message({ isRecalled: true }))).toBe(false)
    expect(isForwardableMessage(message({ pending: true }))).toBe(false)
    expect(isForwardableMessage(message({ failed: true }))).toBe(false)
    expect(CHAT_MAX_FORWARD_MESSAGES).toBe(50)
  })
})
