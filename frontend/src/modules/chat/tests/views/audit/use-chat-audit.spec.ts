import type { SchemaQueryParams } from '~/components'
import { describe, expect, it } from 'vitest'
import { auditContentText, CHAT_MESSAGE_TYPES, chatAuditResource, conversationTypeLabel, memberRoleTone, messageTypeLabel, messageTypeTone, useAuditLoader } from '@/modules/chat/views/audit/use-chat-audit'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

function row(id: string) {
  return {
    id,
    conversationId: 'c-1',
    conversationName: '研发群',
    conversationType: 'Group',
    senderUserId: null,
    senderUserName: null,
    messageType: 'System',
    content: '张三加入了群聊',
    fileName: null,
    isRecalled: false,
    editedAt: null,
    createdAt: '2026-11-01T00:00:00Z',
  }
}

const t = (key: string) => key

describe('chatAuditResource', () => {
  it('把 SchemaPage 的查询换成 chat-audit.list 的分页请求：多选合成 in、发送时间拆成 ge / le、会话与发送人精确匹配', async () => {
    const calls = record(() => new Response(JSON.stringify({
      items: [
        row('m-1'),
      ],
      total: 1,
      page: { index: 1, size: 20 },
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    const params: SchemaQueryParams = {
      page: 1,
      pageSize: 20,
      filters: {
        keyword: ' 加入 ',
        conversationId: 'c-1',
        senderUserId: 'u-1',
        includeRecalled: 'true',
        messageType: ['Text', 'System'],
        createdAt: ['2026-10-01T00:00:00Z', '2026-11-01T00:00:00Z'],
      },
      conditionFilters: [
        {
          field: 'messageType',
          operator: QueryOperator.In,
          values: ['Text', 'System'],
        },
        {
          field: 'createdAt',
          operator: QueryOperator.Between,
          values: ['2026-10-01T00:00:00Z', '2026-11-01T00:00:00Z'],
        },
      ],
      sorts: [
        { field: 'createdAt', order: 'desc' },
      ],
    }

    const result = await chatAuditResource().page!(params)

    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/chat-audit:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 1, size: 20 },
      keyword: '加入',
      filters: [
        { field: 'messageType', operator: 'in', value: 'Text,System' },
        { field: 'createdAt', operator: 'ge', value: '2026-10-01T00:00:00Z' },
        { field: 'createdAt', operator: 'le', value: '2026-11-01T00:00:00Z' },
        { field: 'conversationId', operator: 'eq', value: 'c-1' },
        { field: 'senderUserId', operator: 'eq', value: 'u-1' },
        { field: 'includeRecalled', operator: 'eq', value: 'true' },
      ],
      sorts: [
        { field: 'createdAt', descending: true },
      ],
    })
    expect(result.items.map(item => item.id)).toEqual(['m-1'])
    expect(result.page.totalCount).toBe(1)
  })

  it('没登记的筛选字段直接报错，而不是静默丢掉条件', async () => {
    record(() => new Response('{}', { status: 200 }))

    await expect(chatAuditResource().page!({
      page: 1,
      pageSize: 20,
      filters: { content: 'x' },
    })).rejects.toThrow('content')
  })
})

describe('枚举文案与语气', () => {
  it('通话记录的正文写成「媒体 · 结果」，口吻随发起人；别的类型照原文，读不出记录的原样给', () => {
    const record = JSON.stringify({
      callId: 'k-1',
      media: 'Video',
      reason: 'Missed',
      durationSeconds: 0,
    })

    expect(auditContentText(t, 'Call', record)).toBe('chat.call.video · chat.call.record_missed_by_peer')
    expect(auditContentText(t, 'Call', '坏掉的正文')).toBe('坏掉的正文')
    expect(auditContentText(t, 'Text', record)).toBe(record)
    expect(auditContentText(t, 'Text', null)).toBe('')
  })

  it('七种消息类型都有文案（含助手与通话），会话类型含助手会话', () => {
    expect(CHAT_MESSAGE_TYPES).toHaveLength(7)
    expect(messageTypeLabel(t, 'Call')).toBe('chat.audit.type_call')
    expect(messageTypeTone('Call')).toBe('info')
    expect(messageTypeLabel(t, 'Assistant')).toBe('chat.audit.type_assistant')
    expect(messageTypeLabel(t, 'Voice')).toBe('chat.audit.type_voice')
    expect(messageTypeTone('System')).toBe('warning')
    expect(messageTypeTone('Text')).toBe('neutral')
    expect(conversationTypeLabel(t, 'Assistant')).toBe('chat.type.assistant')
    expect(conversationTypeLabel(t, 'Single')).toBe('chat.type.single')
    expect(memberRoleTone('Owner')).toBe('warning')
  })
})

describe('useAuditLoader', () => {
  it('连着打开两条时只认最后一条：先发后到的旧结果不回写', async () => {
    const pending = new Map<string, (value: string) => void>()
    const loader = useAuditLoader((key: string) => new Promise<string>((resolve) => {
      pending.set(key, resolve)
    }))

    const first = loader.show('a')
    const second = loader.show('b')
    pending.get('b')!('B')
    await second
    pending.get('a')!('A')
    await first

    expect(loader.open.value).toBe(true)
    expect(loader.value.value).toBe('B')
    expect(loader.loading.value).toBe(false)
  })

  it('加载失败抛给调用方提示，loading 复位', async () => {
    const loader = useAuditLoader(() => Promise.reject(new Error('denied')))

    await expect(loader.show('a')).rejects.toThrow('denied')
    expect(loader.loading.value).toBe(false)
    expect(loader.value.value).toBeNull()
  })
})
