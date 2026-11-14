import type { ChatAuditCall } from '@/modules/chat/views/audit/use-chat-audit'
import type { SchemaQueryParams } from '~/components'
import { describe, expect, it } from 'vitest'
import { auditContentText, callConversationLabel, callEndReasonLabel, callOutcomeLabel, callOutcomeTone, callStatusLabel, CHAT_MESSAGE_TYPES, chatAuditCallResource, chatAuditResource, conversationTypeLabel, memberRoleTone, messageTypeLabel, messageTypeTone, participantName, participantStatusLabel, participantStatusTone, toChatAuditCallRow, useAuditLoader } from '@/modules/chat/views/audit/use-chat-audit'
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

function callRow(overrides: Partial<ChatAuditCall> = {}): ChatAuditCall {
  return {
    id: 'k-1',
    conversationId: 'c-1',
    conversationName: null,
    conversationType: 'Single',
    initiatorUserId: 'u-1',
    initiatorUserName: 'alice',
    media: 'Voice',
    status: 'Ended',
    endReason: 'Completed',
    startedAt: '2026-11-01T00:00:00Z',
    answeredAt: '2026-11-01T00:00:05Z',
    endedAt: '2026-11-01T00:02:20Z',
    durationSeconds: 135,
    participants: [
      {
        userId: 'u-1',
        userName: 'alice',
        displayName: 'Alice',
        avatarFileId: null,
        status: 'Left',
        joinedAt: '2026-11-01T00:00:00Z',
        leftAt: '2026-11-01T00:02:20Z',
      },
      {
        userId: 'u-2',
        userName: 'bob',
        displayName: null,
        avatarFileId: null,
        status: 'Left',
        joinedAt: '2026-11-01T00:00:05Z',
        leftAt: '2026-11-01T00:02:20Z',
      },
    ],
    ...overrides,
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

describe('chatAuditCallResource', () => {
  it('把 SchemaPage 的查询换成 chat-audit.list-calls 的分页请求：结束原因多选合成 in、发起时间拆成 ge / le，其余精确匹配，不带关键字', async () => {
    const calls = record(() => new Response(JSON.stringify({
      items: [
        callRow(),
      ],
      total: 1,
      page: { index: 2, size: 10 },
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))
    const params: SchemaQueryParams = {
      page: 2,
      pageSize: 10,
      filters: {
        conversationId: 'c-1',
        initiatorUserId: 'u-1',
        participantUserId: 'u-2',
        media: 'Voice',
        status: 'Ended',
        endReason: ['Completed', 'Interrupted'],
        startedAt: ['2026-10-01T00:00:00Z', '2026-11-02T00:00:00Z'],
      },
      conditionFilters: [
        {
          field: 'endReason',
          operator: QueryOperator.In,
          values: ['Completed', 'Interrupted'],
        },
        {
          field: 'startedAt',
          operator: QueryOperator.Between,
          values: ['2026-10-01T00:00:00Z', '2026-11-02T00:00:00Z'],
        },
      ],
      sorts: [
        { field: 'startedAt', order: 'asc' },
      ],
    }

    const result = await chatAuditCallResource().page!(params)

    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/chat-audit:list-calls' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 10 },
      filters: [
        { field: 'endReason', operator: 'in', value: 'Completed,Interrupted' },
        { field: 'startedAt', operator: 'ge', value: '2026-10-01T00:00:00Z' },
        { field: 'startedAt', operator: 'le', value: '2026-11-02T00:00:00Z' },
        { field: 'conversationId', operator: 'eq', value: 'c-1' },
        { field: 'initiatorUserId', operator: 'eq', value: 'u-1' },
        { field: 'participantUserId', operator: 'eq', value: 'u-2' },
        { field: 'media', operator: 'eq', value: 'Voice' },
        { field: 'status', operator: 'eq', value: 'Ended' },
      ],
      sorts: [
        { field: 'startedAt', descending: false },
      ],
      keyword: null,
    })
    expect(result.items.map(item => item.id)).toEqual(['k-1'])
    expect(result.page.totalCount).toBe(1)
    // 行上带着换算好的文字：列、速览卡与导出读的是同一份
    expect(result.items[0]).toMatchObject({ conversationLabel: 'Alice / bob', participantNames: 'Alice、bob', durationText: '02:15' })
  })

  it('没登记的筛选字段直接报错，而不是静默丢掉条件', async () => {
    record(() => new Response('{}', { status: 200 }))

    await expect(chatAuditCallResource().page!({
      page: 1,
      pageSize: 20,
      filters: { content: 'x' },
    })).rejects.toThrow('content')
  })
})

describe('通话记录的文案与语气', () => {
  it('结果：结束了的说怎么结束的，还没结束的说状态；正常打完成功、没接上警告、中断危险、进行中提示', () => {
    const ended = (endReason: ChatAuditCall['endReason']) => ({ status: 'Ended' as const, endReason })

    expect([
      'Completed',
      'Cancelled',
      'Rejected',
      'Missed',
      'Interrupted',
    ].map(reason => callOutcomeLabel(t, ended(reason as ChatAuditCall['endReason'])))).toEqual([
      'chat.audit.call_reason_completed',
      'chat.audit.call_reason_cancelled',
      'chat.audit.call_reason_rejected',
      'chat.audit.call_reason_missed',
      'chat.audit.call_reason_interrupted',
    ])
    expect([
      'Completed',
      'Cancelled',
      'Rejected',
      'Missed',
      'Interrupted',
    ].map(reason => callOutcomeTone(ended(reason as ChatAuditCall['endReason'])))).toEqual([
      'success',
      'warning',
      'warning',
      'warning',
      'danger',
    ])
    expect([
      callOutcomeLabel(t, { status: 'Ringing', endReason: null }),
      callOutcomeLabel(t, { status: 'Ongoing', endReason: null }),
    ]).toEqual(['chat.audit.call_status_ringing', 'chat.audit.call_status_ongoing'])
    expect(callOutcomeTone({ status: 'Ongoing', endReason: null })).toBe('info')
    expect(callStatusLabel(t, 'Ended')).toBe('chat.audit.call_status_ended')
    expect(callEndReasonLabel(t, 'Completed')).toBe('chat.audit.call_reason_completed')
  })

  it('参与者的状态各有文案；在里面成功、响铃中提示、拒绝与没接到警告、离开了中性', () => {
    const statuses = [
      'Invited',
      'Joined',
      'Rejected',
      'Missed',
      'Left',
    ] as const

    expect(statuses.map(status => participantStatusLabel(t, status))).toEqual([
      'chat.audit.participant_invited',
      'chat.audit.participant_joined',
      'chat.audit.participant_rejected',
      'chat.audit.participant_missed',
      'chat.audit.participant_left',
    ])
    expect(statuses.map(participantStatusTone)).toEqual([
      'info',
      'success',
      'warning',
      'warning',
      'neutral',
    ])
  })

  it('参与者的名字依次取显示名、用户名、主键；会话有名字用名字，单聊没有名字就写出两个人', () => {
    const [alice, bob] = callRow().participants

    expect([
      participantName(alice!),
      participantName(bob!),
      participantName({ userId: 'u-3', userName: null, displayName: null }),
    ]).toEqual(['Alice', 'bob', 'u-3'])
    expect(callConversationLabel(callRow())).toBe('Alice / bob')
    expect(callConversationLabel(callRow({ conversationName: '研发群', conversationType: 'Group' }))).toBe('研发群')
  })

  it('换算成列表的一行：会话、参与者与时长各成一段文字；没接通的时长为空串，群用会话名', () => {
    expect(toChatAuditCallRow(callRow({
      conversationName: '研发群',
      conversationType: 'Group',
      endReason: 'Missed',
      answeredAt: null,
      durationSeconds: null,
    })))
      .toMatchObject({
        id: 'k-1',
        conversationLabel: '研发群',
        participantNames: 'Alice、bob',
        durationText: '',
      })
    expect(toChatAuditCallRow(callRow({ durationSeconds: 0 })).durationText).toBe('00:00')
  })
})

describe('枚举文案与语气', () => {
  it('通话记录的正文写成「媒体 · 结果」，口吻随发起人，群里的按多人通话说；别的类型照原文，读不出记录的原样给', () => {
    const record = JSON.stringify({
      callId: 'k-1',
      media: 'Video',
      reason: 'Missed',
      durationSeconds: 0,
      missedUserIds: ['bob'],
    })

    expect(auditContentText(t, 'Call', record, 'Single')).toBe('chat.call.video · chat.call.record_missed_by_peer')
    expect(auditContentText(t, 'Call', record, 'Group')).toBe('chat.call.video · chat.call.record_no_answer')
    expect(auditContentText(t, 'Call', '坏掉的正文', 'Single')).toBe('坏掉的正文')
    expect(auditContentText(t, 'Text', record, 'Single')).toBe(record)
    expect(auditContentText(t, 'Text', null, 'Group')).toBe('')
  })

  it('聊天记录的正文写成「标题 · 几条」，读不出记录的原样给', () => {
    const record = JSON.stringify({
      sourceType: 'Group',
      sourceName: 'Team',
      participants: [],
      items: [
        {
          senderUserName: 'alice',
          createdAt: '2026-11-14T08:00:00Z',
          messageType: 'Text',
          content: 'one',
          attachments: [],
        },
        {
          senderUserName: 'bob',
          createdAt: '2026-11-14T08:00:01Z',
          messageType: 'Text',
          content: 'two',
          attachments: [],
        },
      ],
    })

    expect(auditContentText(t, 'Record', record, 'Group')).toBe('chat.record.title_group · chat.record.count')
    expect(auditContentText(t, 'Record', '坏掉的正文', 'Group')).toBe('坏掉的正文')
    expect(messageTypeLabel(t, 'Record')).toBe('chat.audit.type_record')
    expect(messageTypeTone('Record')).toBe('info')
  })

  it('八种消息类型都有文案（含助手、通话与聊天记录），会话类型含助手会话', () => {
    expect(CHAT_MESSAGE_TYPES).toHaveLength(8)
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
