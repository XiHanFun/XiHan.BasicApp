/**
 * packages/chat/store.ts 的消息流维度行为。
 *
 * 职责边界：历史分页与合并去重、乐观上屏与回执收敛、失败重发、撤回/编辑/表情回应/已读位回灌、
 * 搜索定位造成的「视口分离态」、语音已听本地记录、群已读人数计算。
 * 会话列表相关在 chat-store-conversations，助手流在 chat-store-assistant。
 */
import type { ChatApiContract, ChatConversationListItem, ChatMessageItem } from './types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useUserStore } from '~/stores'
import { setChatApi } from './api-contract'
import { CHAT_VOICE_PLAYED_CAP, CHAT_VOICE_PLAYED_STORAGE_KEY } from './constants'
import { ChatConversationType, ChatMemberRole, ChatMessageType } from './enums'
import { useChatStore } from './store'

const hub = vi.hoisted(() => ({ calls: [] as string[] }))

vi.mock('~/composables/useSignalR', () => ({
  useSignalR: () => ({
    invoke: (method: string) => {
      hub.calls.push(method)
      return Promise.resolve()
    },
  }),
}))

function message(patch: Partial<ChatMessageItem> & { messageId: string }): ChatMessageItem {
  return {
    conversationId: 'a',
    senderUserId: 'other',
    messageType: ChatMessageType.Text,
    content: `正文 ${patch.messageId}`,
    isRecalled: false,
    createdTime: '2026-08-27T00:00:00Z',
    mentionedUserIds: [],
    isPinned: false,
    reactions: [],
    ...patch,
  }
}

function conversation(patch: Partial<ChatConversationListItem> & { conversationId: string }): ChatConversationListItem {
  return {
    conversationType: ChatConversationType.Single,
    displayName: '会话',
    memberCount: 2,
    memberRole: ChatMemberRole.Member,
    unreadCount: 0,
    isMuted: false,
    isPinned: false,
    isSilenced: false,
    ...patch,
  }
}

function createApi(overrides: Partial<ChatApiContract> = {}): ChatApiContract {
  const unexpected = (name: string) => () => Promise.reject(new Error(`未预期调用 ${name}`))
  return new Proxy({
    myConversations: () => Promise.resolve([]),
    markRead: () => Promise.resolve(),
    messageHistory: () => Promise.resolve({ items: [], hasMore: false }),
    readPositions: () => Promise.resolve([]),
    pinnedMessages: () => Promise.resolve([]),
    ...overrides,
  } as Partial<ChatApiContract>, {
    get(target, property: string) {
      return Reflect.get(target, property) ?? unexpected(property)
    },
  }) as ChatApiContract
}

function setup(overrides: Partial<ChatApiContract> = {}) {
  setActivePinia(createPinia())
  const userStore = useUserStore()
  userStore.userInfo = { basicId: 'me', userName: 'me', nickName: '我', roles: [], permissions: [] }
  setChatApi(createApi(overrides))
  return useChatStore()
}

beforeEach(() => {
  hub.calls = []
})

afterEach(() => {
  vi.useRealTimers()
})

describe('历史分页与合并', () => {
  it('加载中重复调用直接返回，同一会话只打一次历史接口', async () => {
    let calls = 0
    let release: (value: { hasMore: boolean, items: ChatMessageItem[] }) => void = () => undefined
    const store = setup({
      messageHistory: () => {
        calls += 1
        return new Promise((resolve) => {
          release = resolve
        })
      },
    })

    const first = store.loadHistory('a')
    await store.loadHistory('a')
    expect(calls).toBe(1)

    release({ items: [message({ messageId: 'm1' })], hasMore: true })
    await first

    expect(calls).toBe(1)
    expect(store.hasMoreOlder.a).toBe(true)
  })

  it('更早一页拼在现有消息之前，保持时间正序', async () => {
    const pages: Record<string, ChatMessageItem[]> = {
      latest: [message({ messageId: '300' }), message({ messageId: '400' })],
      older: [message({ messageId: '100' }), message({ messageId: '200' })],
    }
    const store = setup({
      messageHistory: (query: { beforeMessageId?: null | string }) => Promise.resolve({
        items: query.beforeMessageId ? pages.older! : pages.latest!,
        hasMore: !query.beforeMessageId,
      }),
    })

    await store.loadHistory('a')
    await store.loadOlder('a')

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['100', '200', '300', '400'])
  })

  it('推送先到的消息不会被历史页重复插入一遍', async () => {
    const store = setup({
      messageHistory: () => Promise.resolve({
        items: [message({ messageId: '100' }), message({ messageId: '200' })],
        hasMore: false,
      }),
    })
    store.applyIncomingMessage({
      message: message({ messageId: '200' }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })
    store.messages.a ??= []

    await store.loadHistory('a')

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['100', '200'])
  })

  it('翻更早一页时游标取第一条已落库消息，跳过本地乐观占位', async () => {
    const cursors: (null | string | undefined)[] = []
    const store = setup({
      messageHistory: (query: { beforeMessageId?: null | string }) => {
        cursors.push(query.beforeMessageId)
        return Promise.resolve({ items: [], hasMore: false })
      },
      sendMessage: () => new Promise<ChatMessageItem>(() => {}),
    })
    await store.loadHistory('a')
    store.messages.a = [message({ messageId: '500' })]
    void store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '在发' })
    store.hasMoreOlder.a = true

    await store.loadOlder('a')

    expect(store.messages.a?.[0]?.messageId.startsWith('local:')).toBe(false)
    expect(cursors[cursors.length - 1]).toBe('500')
  })

  it('已知没有更早历史时不再打接口', async () => {
    let calls = 0
    const store = setup({
      messageHistory: () => {
        calls += 1
        return Promise.resolve({ items: [message({ messageId: '1' })], hasMore: false })
      },
    })
    await store.loadHistory('a')
    expect(calls).toBe(1)

    await store.loadOlder('a')

    expect(calls).toBe(1)
  })

  it('会话尚无消息缓存时翻更早一页是空转', async () => {
    let calls = 0
    const store = setup({
      messageHistory: () => {
        calls += 1
        return Promise.resolve({ items: [], hasMore: true })
      },
    })

    await store.loadOlder('never-opened')

    expect(calls).toBe(0)
  })

  it('缓存里全是本地乐观消息时不发出翻页请求（没有可用游标）', async () => {
    let calls = 0
    const store = setup({
      messageHistory: () => {
        calls += 1
        return Promise.resolve({ items: [], hasMore: true })
      },
      sendMessage: () => new Promise<ChatMessageItem>(() => {}),
    })
    void store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '在发' })
    store.hasMoreOlder.a = true

    await store.loadOlder('a')

    expect(calls).toBe(0)
  })
})

describe('乐观上屏与回执收敛', () => {
  it('发送时立刻插入 pending 占位，回执到达后原位替换为正式消息且不留重影', async () => {
    let capturedClientId: null | string | undefined
    const store = setup({
      sendMessage: (input: { clientMessageId?: null | string }) => {
        capturedClientId = input.clientMessageId
        return Promise.resolve(message({
          messageId: '900',
          content: '你好',
          senderUserId: 'me',
          clientMessageId: input.clientMessageId,
        }))
      },
    })

    const pending = store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '你好' })
    expect(store.messages.a).toHaveLength(1)
    expect(store.messages.a?.[0]?.pending).toBe(true)
    expect(store.messages.a?.[0]?.messageId.startsWith('local:')).toBe(true)

    await pending

    expect(store.messages.a).toHaveLength(1)
    expect(store.messages.a?.[0]?.messageId).toBe('900')
    expect(store.messages.a?.[0]?.pending).toBeUndefined()
    expect(capturedClientId).toBeTruthy()
  })

  it('推送抢在 REST 回执之前到达时按 clientMessageId 原位替换占位，随后的回执是无操作', async () => {
    let resolveSend: (value: ChatMessageItem) => void = () => undefined
    const store = setup({
      sendMessage: (input: { clientMessageId?: null | string }) => new Promise<ChatMessageItem>((resolve) => {
        resolveSend = () => resolve(message({
          messageId: '900',
          senderUserId: 'me',
          clientMessageId: input.clientMessageId,
        }))
      }),
    })
    const pending = store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '你好' })
    const clientMessageId = store.messages.a?.[0]?.clientMessageId ?? ''

    store.applyIncomingMessage({
      message: message({ messageId: '900', senderUserId: 'me', clientMessageId }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })
    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['900'])
    expect(store.messages.a?.[0]?.pending).toBeUndefined()

    resolveSend(message({ messageId: '900' }))
    await pending

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['900'])
  })

  it('回执与推送带的是同一条正式消息时，先到的推送不制造第二条重影', async () => {
    const store = setup({
      sendMessage: (input: { clientMessageId?: null | string }) => Promise.resolve(message({
        messageId: '900',
        senderUserId: 'me',
        clientMessageId: input.clientMessageId,
      })),
    })

    await store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '你好' })
    const clientMessageId = store.messages.a?.[0]?.clientMessageId ?? ''
    store.applyIncomingMessage({
      message: message({ messageId: '900', senderUserId: 'me', clientMessageId }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['900'])
  })

  it('发送失败的条目转为 failed 且异常继续上抛给调用方', async () => {
    const store = setup({ sendMessage: () => Promise.reject(new Error('禁言中')) })

    await expect(
      store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '试试' }),
    ).rejects.toThrow('禁言中')

    expect(store.messages.a?.[0]?.failed).toBe(true)
    expect(store.messages.a?.[0]?.pending).toBe(false)
  })

  it('重发只作用于失败条目，成功后收敛成正式消息', async () => {
    let attempt = 0
    const store = setup({
      sendMessage: (input: { clientMessageId?: null | string }) => {
        attempt += 1
        return attempt === 1
          ? Promise.reject(new Error('超时'))
          : Promise.resolve(message({ messageId: '901', senderUserId: 'me', clientMessageId: input.clientMessageId }))
      },
    })
    await expect(
      store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '重发我' }),
    ).rejects.toThrow('超时')
    const clientMessageId = store.messages.a?.[0]?.clientMessageId ?? ''

    await store.retryMessage('a', clientMessageId)

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['901'])
    expect(attempt).toBe(2)
  })

  it('重发一条并不存在或未失败的消息是空转，不打接口', async () => {
    let calls = 0
    const store = setup({
      sendMessage: () => {
        calls += 1
        return new Promise<ChatMessageItem>(() => {})
      },
    })
    void store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '发送中' })
    const clientMessageId = store.messages.a?.[0]?.clientMessageId ?? ''
    calls = 0

    await store.retryMessage('a', clientMessageId)
    await store.retryMessage('a', '根本不存在')

    expect(calls).toBe(0)
  })

  it('重发再次失败时条目回到 failed，仍可继续重试', async () => {
    const store = setup({ sendMessage: () => Promise.reject(new Error('还是不行')) })
    await expect(
      store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: 'x' }),
    ).rejects.toThrow('还是不行')
    const clientMessageId = store.messages.a?.[0]?.clientMessageId ?? ''

    await expect(store.retryMessage('a', clientMessageId)).rejects.toThrow('还是不行')

    expect(store.messages.a?.[0]?.failed).toBe(true)
  })

  it('移除本地消息只清理 pending/failed 条目，已落库消息不受影响', async () => {
    const store = setup({ sendMessage: () => Promise.reject(new Error('失败')) })
    store.messages.a = [message({ messageId: '800', clientMessageId: 'shared' })]
    await expect(
      store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: 'x' }),
    ).rejects.toThrow('失败')
    const clientMessageId = store.messages.a?.[1]?.clientMessageId ?? ''

    store.removeLocalMessage('a', clientMessageId)
    store.removeLocalMessage('a', 'shared')

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['800'])
  })

  it('发送成功后刷新会话预览，超长正文截断到 60 字并补省略号', async () => {
    const long = '字'.repeat(80)
    const store = setup({
      myConversations: () => Promise.resolve([conversation({ conversationId: 'a' })]),
      sendMessage: () => Promise.resolve(message({
        messageId: '902',
        senderUserId: 'me',
        content: long,
        createdTime: '2026-08-27T12:00:00Z',
      })),
    })
    await store.loadConversations()

    await store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: long })

    expect(store.conversations[0]?.lastMessagePreview).toBe(`${'字'.repeat(60)}…`)
    expect(store.conversations[0]?.lastMessageTime).toBe('2026-08-27T12:00:00Z')
  })

  it('恰好 60 字的正文不加省略号', async () => {
    const exact = '字'.repeat(60)
    const store = setup({
      myConversations: () => Promise.resolve([conversation({ conversationId: 'a' })]),
      sendMessage: () => Promise.resolve(message({ messageId: '903', senderUserId: 'me', content: exact })),
    })
    await store.loadConversations()

    await store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: exact })

    expect(store.conversations[0]?.lastMessagePreview).toBe(exact)
  })

  it('无正文的图片消息按张数生成预览', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([conversation({ conversationId: 'a' })]),
      sendMessage: () => Promise.resolve(message({
        messageId: '904',
        senderUserId: 'me',
        content: null,
        messageType: ChatMessageType.Image,
        attachments: [
          { fileId: '1', fileName: 'a.png' },
          { fileId: '2', fileName: 'b.png' },
        ],
      })),
    })
    await store.loadConversations()

    await store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Image, content: null })

    expect(store.conversations[0]?.lastMessagePreview).toBe('[图片] 2张')
  })
})

describe('推送回灌', () => {
  it('他人消息在非活跃会话累计未读，并在被 @ 时置起提示', () => {
    const store = setup()
    store.conversations = [conversation({ conversationId: 'a', unreadCount: 1 })]

    store.applyIncomingMessage({
      message: message({ messageId: '1', senderUserId: 'other', mentionedUserIds: ['me'] }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.conversations[0]?.unreadCount).toBe(2)
    expect(store.mentionsPending.a).toBe(true)
  })

  it('自己发的消息回声不累计未读', () => {
    const store = setup()
    store.conversations = [conversation({ conversationId: 'a', unreadCount: 0 })]

    store.applyIncomingMessage({
      message: message({ messageId: '1', senderUserId: 'me' }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.conversations[0]?.unreadCount).toBe(0)
  })

  it('系统提示消息只刷时间线与预览，不计未读（与后端口径一致）', () => {
    const store = setup()
    store.conversations = [conversation({ conversationId: 'a', unreadCount: 0 })]

    store.applyIncomingMessage({
      message: message({
        messageId: '1',
        senderUserId: '0',
        messageType: ChatMessageType.System,
        content: '张三加入了群聊',
      }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.conversations[0]?.unreadCount).toBe(0)
    expect(store.conversations[0]?.lastMessagePreview).toBe('张三加入了群聊')
  })

  it('正开着该会话且窗口在前台时自动已读而不是累加未读', async () => {
    vi.useFakeTimers()
    const marked: string[] = []
    const store = setup({
      markRead: (conversationId: string) => {
        marked.push(conversationId)
        return Promise.resolve()
      },
    })
    store.conversations = [conversation({ conversationId: 'a', unreadCount: 3 })]
    store.activeConversationId = 'a'
    vi.spyOn(document, 'hasFocus').mockReturnValue(true)

    store.applyIncomingMessage({
      message: message({ messageId: '1', senderUserId: 'other' }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.conversations[0]?.unreadCount).toBe(0)
    await vi.advanceTimersByTimeAsync(1000)
    expect(marked).toStrictEqual(['a'])
  })

  it('会话虽活跃但窗口失焦时仍累计未读', () => {
    const store = setup()
    store.conversations = [conversation({ conversationId: 'a', unreadCount: 0 })]
    store.activeConversationId = 'a'
    vi.spyOn(document, 'hasFocus').mockReturnValue(false)

    store.applyIncomingMessage({
      message: message({ messageId: '1', senderUserId: 'other' }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.conversations[0]?.unreadCount).toBe(1)
  })

  it('陌生会话的第一条消息触发整表刷新，用完整列表项补齐', async () => {
    let calls = 0
    const store = setup({
      myConversations: () => {
        calls += 1
        return Promise.resolve([conversation({ conversationId: 'brand-new' })])
      },
    })

    store.applyIncomingMessage({
      message: message({ messageId: '1', conversationId: 'brand-new', senderUserId: 'other' }),
      conversation: { conversationId: 'brand-new', conversationType: ChatConversationType.Group },
    })
    await vi.waitFor(() => expect(calls).toBe(1))

    expect(store.conversations.map(item => item.conversationId)).toStrictEqual(['brand-new'])
  })

  it('对方发来新消息即清掉他自己的「正在输入」提示', () => {
    const store = setup()
    store.conversations = [conversation({ conversationId: 'a' })]
    store.typingIndicators.a = { conversationId: 'a', userId: 'other' }

    store.applyIncomingMessage({
      message: message({ messageId: '1', senderUserId: 'other' }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.typingIndicators.a).toBeUndefined()
  })

  it('撤回回灌把正文清空并置撤回标记', () => {
    const store = setup()
    store.messages.a = [message({ messageId: '1', content: '说错话了' })]

    store.applyMessageRecalled({ conversationId: 'a', messageId: '1' })

    expect(store.messages.a?.[0]?.isRecalled).toBe(true)
    expect(store.messages.a?.[0]?.content).toBeNull()
  })

  it('编辑回灌同时更新正文与编辑时间，缓存里没有该消息时静默忽略', () => {
    const store = setup()
    store.messages.a = [message({ messageId: '1', content: '原文' })]

    store.applyMessageEdited({
      conversationId: 'a',
      messageId: '1',
      content: '改过',
      editedTime: '2026-08-27T01:00:00Z',
    })
    store.applyMessageEdited({ conversationId: 'a', messageId: '不存在', content: 'x' })

    expect(store.messages.a?.[0]?.content).toBe('改过')
    expect(store.messages.a?.[0]?.editedTime).toBe('2026-08-27T01:00:00Z')
    expect(store.messages.a).toHaveLength(1)
  })

  it('表情回应增删幂等：重复 added 不叠加，重复移除不报错', () => {
    const store = setup()
    store.messages.a = [message({ messageId: '1' })]
    const payload = { conversationId: 'a', messageId: '1', emoji: '👍', userId: 'u2', userName: '李四' }

    store.applyReactionChanged({ ...payload, added: true })
    store.applyReactionChanged({ ...payload, added: true })
    expect(store.messages.a?.[0]?.reactions).toHaveLength(1)

    store.applyReactionChanged({ ...payload, added: false })
    store.applyReactionChanged({ ...payload, added: false })
    expect(store.messages.a?.[0]?.reactions).toStrictEqual([])
  })

  it('同一条消息上不同人的同一表情各占一项', () => {
    const store = setup()
    store.messages.a = [message({ messageId: '1' })]

    store.applyReactionChanged({ conversationId: 'a', messageId: '1', emoji: '👍', userId: 'u2', added: true })
    store.applyReactionChanged({ conversationId: 'a', messageId: '1', emoji: '👍', userId: 'u3', added: true })

    expect(store.messages.a?.[0]?.reactions.map(item => item.userId)).toStrictEqual(['u2', 'u3'])
  })

  it('已读位回灌只写进已加载过的会话，未加载会话不凭空建表', () => {
    const store = setup()
    store.readPositions.a = { u2: null }

    store.applyReadPositionChanged({ conversationId: 'a', userId: 'u2', lastReadMessageId: '500' })
    store.applyReadPositionChanged({ conversationId: 'never-loaded', userId: 'u2', lastReadMessageId: '500' })

    expect(store.readPositions.a).toStrictEqual({ u2: '500' })
    expect(store.readPositions['never-loaded']).toBeUndefined()
  })
})

describe('表情回应与 Pin 的乐观写', () => {
  it('点表情先本地生效，接口失败后回滚到点之前的状态', async () => {
    const store = setup({ toggleReaction: () => Promise.reject(new Error('网络断了')) })
    store.messages.a = [message({ messageId: '1' })]

    await expect(store.toggleReaction('a', '1', '👍')).rejects.toThrow('网络断了')

    expect(store.messages.a?.[0]?.reactions).toStrictEqual([])
  })

  it('取消已有表情失败时把自己的那一项加回来', async () => {
    const store = setup({ toggleReaction: () => Promise.reject(new Error('网络断了')) })
    store.messages.a = [message({
      messageId: '1',
      reactions: [{ emoji: '👍', userId: 'me', userName: '我' }],
    })]

    await expect(store.toggleReaction('a', '1', '👍')).rejects.toThrow('网络断了')

    expect(store.messages.a?.[0]?.reactions.map(item => item.userId)).toStrictEqual(['me'])
  })

  it('置顶消息成功后刷新 Pin 列表并把消息标记为已置顶', async () => {
    let pinnedCalls = 0
    const store = setup({
      pinMessage: () => Promise.resolve(),
      pinnedMessages: () => {
        pinnedCalls += 1
        return Promise.resolve([message({ messageId: '1', isPinned: true })])
      },
    })
    store.messages.a = [message({ messageId: '1' })]

    await store.pinMessage('a', '1')

    expect(store.messages.a?.[0]?.isPinned).toBe(true)
    expect(store.pinnedMessages.a?.map(item => item.messageId)).toStrictEqual(['1'])
    expect(pinnedCalls).toBe(1)
  })

  it('取消 Pin 后消息回到未置顶', async () => {
    const store = setup({
      unpinMessage: () => Promise.resolve(),
      pinnedMessages: () => Promise.resolve([]),
    })
    store.messages.a = [message({ messageId: '1', isPinned: true })]

    await store.unpinMessage('a', '1')

    expect(store.messages.a?.[0]?.isPinned).toBe(false)
    expect(store.pinnedMessages.a).toStrictEqual([])
  })

  it('撤回接口成功后本地立即落撤回态', async () => {
    const store = setup({ recallMessage: () => Promise.resolve() })
    store.messages.a = [message({ messageId: '1', content: '撤回我' })]

    await store.recallMessage('a', '1')

    expect(store.messages.a?.[0]?.isRecalled).toBe(true)
  })

  it('编辑成功后退出编辑态，并以服务端返回的正文为准', async () => {
    const store = setup({
      editMessage: () => Promise.resolve(message({
        messageId: '1',
        content: '服务端规范化后的正文',
        editedTime: '2026-08-27T02:00:00Z',
      })),
    })
    store.messages.a = [message({ messageId: '1', content: '原文' })]
    store.editTarget = store.messages.a[0] ?? null

    await store.editMessage('a', '1', '我输入的正文')

    expect(store.messages.a?.[0]?.content).toBe('服务端规范化后的正文')
    expect(store.editTarget).toBeNull()
  })
})

describe('搜索定位与视口分离态', () => {
  it('跳转到命中消息会重建上下文、进入分离态并在 3 秒后自动取消高亮', async () => {
    vi.useFakeTimers()
    const store = setup({
      messageHistory: () => Promise.resolve({
        items: [message({ messageId: '100' }), message({ messageId: '200' })],
        hasMore: true,
      }),
    })

    await store.jumpToMessage('a', '200')

    expect(store.detachedConversations.a).toBe(true)
    expect(store.highlightMessageId).toBe('200')
    expect(store.hasMoreOlder.a).toBe(true)

    await vi.advanceTimersByTimeAsync(3000)
    expect(store.highlightMessageId).toBeNull()
  })

  it('分离态下新到达的消息不追加进当前 bucket，避免历史与最新之间出现空洞', async () => {
    const store = setup({
      messageHistory: () => Promise.resolve({ items: [message({ messageId: '100' })], hasMore: true }),
    })
    store.conversations = [conversation({ conversationId: 'a' })]
    await store.jumpToMessage('a', '100')

    store.applyIncomingMessage({
      message: message({ messageId: '999', senderUserId: 'other' }),
      conversation: { conversationId: 'a', conversationType: ChatConversationType.Single },
    })

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['100'])
    expect(store.conversations[0]?.unreadCount).toBe(1)
  })

  it('回到最新会清空分离态与旧 bucket 并重载最新一页', async () => {
    let call = 0
    const store = setup({
      messageHistory: () => {
        call += 1
        return Promise.resolve({
          items: call === 1 ? [message({ messageId: '100' })] : [message({ messageId: '900' })],
          hasMore: call === 1,
        })
      },
    })
    await store.jumpToMessage('a', '100')

    await store.reloadLatest('a')

    expect(store.detachedConversations.a).toBeUndefined()
    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['900'])
    expect(store.hasMoreOlder.a).toBe(false)
  })

  it('分离态下发消息先回到最新，乐观条目落在最新一页之后', async () => {
    let call = 0
    const store = setup({
      messageHistory: () => {
        call += 1
        return Promise.resolve({
          items: call === 1 ? [message({ messageId: '100' })] : [message({ messageId: '900' })],
          hasMore: false,
        })
      },
      sendMessage: (input: { clientMessageId?: null | string }) => Promise.resolve(message({
        messageId: '901',
        senderUserId: 'me',
        clientMessageId: input.clientMessageId,
      })),
    })
    await store.jumpToMessage('a', '100')

    await store.sendMessage({ conversationId: 'a', messageType: ChatMessageType.Text, content: '发一条' })

    expect(store.messages.a?.map(item => item.messageId)).toStrictEqual(['900', '901'])
    expect(store.detachedConversations.a).toBeUndefined()
  })
})

describe('语音已听本地记录', () => {
  it('已听记录按「用户ID:消息ID」存，换账号后同一条语音仍算未听', () => {
    const store = setup()

    store.markVoicePlayed('m1')
    expect(store.isVoicePlayed('m1')).toBe(true)

    useUserStore().userInfo = { basicId: 'other', userName: 'other', roles: [], permissions: [] }
    expect(store.isVoicePlayed('m1')).toBe(false)
  })

  it('重复标记同一条不会在存储里堆重复项', () => {
    const store = setup()

    store.markVoicePlayed('m1')
    store.markVoicePlayed('m1')

    expect(JSON.parse(localStorage.getItem(CHAT_VOICE_PLAYED_STORAGE_KEY) ?? '[]')).toStrictEqual(['me:m1'])
  })

  it('超出上限时丢掉最旧的记录并保留最新一条', () => {
    const seeded = Array.from({ length: CHAT_VOICE_PLAYED_CAP }, (_, index) => `me:old-${index}`)
    localStorage.setItem(CHAT_VOICE_PLAYED_STORAGE_KEY, JSON.stringify(seeded))
    const store = setup()

    store.markVoicePlayed('newest')

    const stored = JSON.parse(localStorage.getItem(CHAT_VOICE_PLAYED_STORAGE_KEY) ?? '[]') as string[]
    expect(stored).toHaveLength(CHAT_VOICE_PLAYED_CAP)
    expect(stored[0]).toBe('me:newest')
    expect(stored.includes(`me:old-${CHAT_VOICE_PLAYED_CAP - 1}`)).toBe(false)
    expect(store.isVoicePlayed('newest')).toBe(true)
  })

  it('从未标记过的语音是未听状态', () => {
    const store = setup()

    expect(store.isVoicePlayed('never')).toBe(false)
  })
})

describe('群已读人数', () => {
  it('按雪花 ID 单调递增比较：先比长度再比字典序', async () => {
    const store = setup({
      readPositions: () => Promise.resolve([
        { userId: 'u2', lastReadMessageId: '1000000000000000005' },
        { userId: 'u3', lastReadMessageId: '999999999999999999' },
        { userId: 'u4', lastReadMessageId: '1000000000000000009' },
      ]),
    })
    await store.loadReadPositions('a')

    expect(store.readCountFor('a', '1000000000000000005')).toBe(2)
  })

  it('自己与从未读过的成员都不计入已读人数', async () => {
    const store = setup({
      readPositions: () => Promise.resolve([
        { userId: 'me', lastReadMessageId: '9999' },
        { userId: 'u2', lastReadMessageId: null },
        { userId: 'u3', lastReadMessageId: '9999' },
      ]),
    })
    await store.loadReadPositions('a')

    expect(store.readCountFor('a', '9999')).toBe(1)
  })

  it('会话尚未加载过已读位时返回 0', () => {
    const store = setup()

    expect(store.readCountFor('never-loaded', '1')).toBe(0)
  })
})
