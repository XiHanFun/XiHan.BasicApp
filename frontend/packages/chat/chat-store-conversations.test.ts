/**
 * packages/chat/store.ts 的会话维度行为。
 *
 * 职责边界：会话列表拉取与并发去重、排序（置顶优先 + 最后消息时间倒序）、未读合计与免打扰、
 * 打开/关闭会话时的 Hub 进出组、已读防抖上报、typing 展示与节流、草稿持久化、置顶/免打扰开关。
 * 消息流与助手流分别在 chat-store-messages / chat-store-assistant 两个文件里。
 *
 * SignalR 被整体替身掉（不建立真实连接），并记录每次 invoke 以便断言进出组顺序。
 */
import type { ChatApiContract, ChatConversationListItem } from './types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useUserStore } from '~/stores'
import { setChatApi } from './api-contract'
import { CHAT_DRAFTS_STORAGE_KEY, CHAT_HUB_METHODS } from './constants'
import { ChatConversationType, ChatMemberRole } from './enums'
import { useChatStore } from './store'

const hub = vi.hoisted(() => ({
  calls: [] as { conversationId: string, method: string }[],
  shouldFail: false,
}))

vi.mock('~/composables/useSignalR', () => ({
  useSignalR: () => ({
    invoke: (method: string, conversationId: string) => {
      hub.calls.push({ method, conversationId })
      return hub.shouldFail ? Promise.reject(new Error('未连接')) : Promise.resolve()
    },
  }),
}))

function conversation(patch: Partial<ChatConversationListItem> & { conversationId: string }): ChatConversationListItem {
  return {
    conversationType: ChatConversationType.Single,
    displayName: `会话 ${patch.conversationId}`,
    memberCount: 2,
    memberRole: ChatMemberRole.Member,
    unreadCount: 0,
    isMuted: false,
    isPinned: false,
    isSilenced: false,
    ...patch,
  }
}

/** 只实现被测路径会走到的成员，其余调用直接失败以暴露非预期依赖 */
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
  userStore.userInfo = {
    basicId: 'me',
    userName: 'me',
    nickName: '我',
    roles: [],
    permissions: [],
  }
  setChatApi(createApi(overrides))
  return useChatStore()
}

beforeEach(() => {
  hub.calls = []
  hub.shouldFail = false
})

afterEach(() => {
  vi.useRealTimers()
})

describe('会话列表拉取', () => {
  it('拉取中重复调用直接返回，同一批只打一次接口', async () => {
    let calls = 0
    let release: (value: ChatConversationListItem[]) => void = () => undefined
    const store = setup({
      myConversations: () => {
        calls += 1
        return new Promise<ChatConversationListItem[]>((resolve) => {
          release = resolve
        })
      },
    })

    const first = store.loadConversations()
    await store.loadConversations()
    expect(calls).toBe(1)

    release([conversation({ conversationId: 'a' })])
    await first

    expect(calls).toBe(1)
    expect(store.conversations).toHaveLength(1)
  })

  it('接口抛错时 loading 仍被复位，下一次仍可重试', async () => {
    let calls = 0
    const store = setup({
      myConversations: () => {
        calls += 1
        return calls === 1 ? Promise.reject(new Error('网络错误')) : Promise.resolve([conversation({ conversationId: 'a' })])
      },
    })

    await expect(store.loadConversations()).rejects.toThrow('网络错误')
    expect(store.conversationsLoading).toBe(false)
    expect(store.conversationsLoaded).toBe(false)

    await store.loadConversations()
    expect(store.conversationsLoaded).toBe(true)
  })

  it('ensureConversations 只在未加载过时拉取，已加载则不再打接口', async () => {
    let calls = 0
    const store = setup({
      myConversations: () => {
        calls += 1
        return Promise.resolve([])
      },
    })

    await store.ensureConversations()
    await store.ensureConversations()

    expect(calls).toBe(1)
  })

  it('活跃会话在新一轮列表中消失时收敛回列表态，避免停留在已被移出的会话', async () => {
    const store = setup({ myConversations: () => Promise.resolve([conversation({ conversationId: 'gone' })]) })
    await store.loadConversations()
    await store.openConversation('gone')
    expect(store.activeConversationId).toBe('gone')

    setChatApi(createApi({ myConversations: () => Promise.resolve([conversation({ conversationId: 'other' })]) }))
    await store.loadConversations()

    expect(store.activeConversationId).toBeNull()
  })
})

describe('会话排序', () => {
  it('置顶会话恒排在未置顶之前，与最后消息时间无关', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'new', lastMessageTime: '2026-08-27T10:00:00Z' }),
        conversation({ conversationId: 'pinned-old', isPinned: true, lastMessageTime: '2020-01-01T00:00:00Z' }),
      ]),
    })

    await store.loadConversations()

    expect(store.conversations.map(item => item.conversationId)).toStrictEqual(['pinned-old', 'new'])
  })

  it('同为未置顶时按最后消息时间倒序，缺时间的排在最后', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'no-time', lastMessageTime: null }),
        conversation({ conversationId: 'older', lastMessageTime: '2026-08-01T00:00:00Z' }),
        conversation({ conversationId: 'newer', lastMessageTime: '2026-08-27T00:00:00Z' }),
      ]),
    })

    await store.loadConversations()

    expect(store.conversations.map(item => item.conversationId)).toStrictEqual(['newer', 'older', 'no-time'])
  })

  it('置顶开关切换后立即重排，取消置顶的会话按时间落回原位', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'a', isPinned: true, lastMessageTime: '2020-01-01T00:00:00Z' }),
        conversation({ conversationId: 'b', lastMessageTime: '2026-08-27T00:00:00Z' }),
      ]),
      togglePinConversation: () => Promise.resolve({ isOn: false }),
    })
    await store.loadConversations()
    expect(store.conversations[0]?.conversationId).toBe('a')

    await store.togglePinConversation('a')

    expect(store.conversations.map(item => item.conversationId)).toStrictEqual(['b', 'a'])
    expect(store.conversations.find(item => item.conversationId === 'a')?.isPinned).toBe(false)
  })

  it('免打扰开关只改标志位，不参与排序', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'a', lastMessageTime: '2026-08-27T00:00:00Z' }),
        conversation({ conversationId: 'b', lastMessageTime: '2026-08-01T00:00:00Z' }),
      ]),
      toggleMuteConversation: () => Promise.resolve({ isOn: true }),
    })
    await store.loadConversations()

    await store.toggleMuteConversation('a')

    expect(store.conversations.map(item => item.conversationId)).toStrictEqual(['a', 'b'])
    expect(store.conversations[0]?.isMuted).toBe(true)
  })

  it('开关目标不在列表里时静默忽略，不新增幽灵条目', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([conversation({ conversationId: 'a' })]),
      togglePinConversation: () => Promise.resolve({ isOn: true }),
    })
    await store.loadConversations()

    await store.togglePinConversation('missing')

    expect(store.conversations).toHaveLength(1)
    expect(store.conversations[0]?.isPinned).toBe(false)
  })
})

describe('未读合计', () => {
  it('免打扰会话的未读不计入顶栏角标', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'a', unreadCount: 3 }),
        conversation({ conversationId: 'muted', unreadCount: 99, isMuted: true }),
        conversation({ conversationId: 'b', unreadCount: 4 }),
      ]),
    })

    await store.loadConversations()

    expect(store.totalUnread).toBe(7)
  })

  it('列表为空时未读合计是 0 而非 NaN', () => {
    const store = setup()

    expect(store.totalUnread).toBe(0)
  })
})

describe('打开与关闭会话', () => {
  it('切换会话时先离开旧组再加入新组，顺序不可颠倒', async () => {
    const store = setup({ myConversations: () => Promise.resolve([conversation({ conversationId: 'a' }), conversation({ conversationId: 'b' })]) })
    await store.loadConversations()

    await store.openConversation('a')
    hub.calls = []
    await store.openConversation('b')

    expect(hub.calls).toStrictEqual([
      { method: CHAT_HUB_METHODS.leaveConversation, conversationId: 'a' },
      { method: CHAT_HUB_METHODS.joinConversation, conversationId: 'b' },
    ])
  })

  it('连接未建立导致进组失败时不阻塞打开会话，消息仍照常加载', async () => {
    hub.shouldFail = true
    const store = setup({ myConversations: () => Promise.resolve([conversation({ conversationId: 'a' })]) })
    await store.loadConversations()

    await store.openConversation('a')

    expect(store.activeConversationId).toBe('a')
  })

  it('已有消息缓存时不重复拉历史', async () => {
    let historyCalls = 0
    const store = setup({
      messageHistory: () => {
        historyCalls += 1
        return Promise.resolve({ items: [], hasMore: false })
      },
    })

    await store.openConversation('a')
    await store.closeActiveConversation()
    await store.openConversation('a')

    expect(historyCalls).toBe(1)
  })

  it('历史加载失败不阻塞进入会话，进入后可下拉重试', async () => {
    const store = setup({ messageHistory: () => Promise.reject(new Error('历史服务不可用')) })

    await expect(store.openConversation('a')).resolves.toBeUndefined()

    expect(store.activeConversationId).toBe('a')
  })

  it('重复点击当前会话不重新进组，只消费未读', async () => {
    const store = setup({ myConversations: () => Promise.resolve([conversation({ conversationId: 'a', unreadCount: 5 })]) })
    await store.loadConversations()
    await store.openConversation('a')
    store.conversations[0]!.unreadCount = 5
    hub.calls = []

    await store.openConversation('a')

    expect(hub.calls).toStrictEqual([])
    expect(store.conversations[0]?.unreadCount).toBe(0)
  })

  it('打开会话时清空回复目标、编辑目标与「有人@我」提示', async () => {
    const store = setup()
    store.mentionsPending.a = true
    store.replyTarget = {
      messageId: 'm1',
      conversationId: 'a',
      senderUserId: 'other',
      messageType: 'Text' as never,
      isRecalled: false,
      createdTime: '2026-08-27T00:00:00Z',
      mentionedUserIds: [],
      isPinned: false,
      reactions: [],
    }

    await store.openConversation('a')

    expect(store.replyTarget).toBeNull()
    expect(store.editTarget).toBeNull()
    expect(store.mentionsPending.a).toBeUndefined()
  })

  it('关闭活跃会话会退出 Hub 组并清空活跃标识', async () => {
    const store = setup()
    await store.openConversation('a')
    hub.calls = []

    store.closeActiveConversation()

    expect(store.activeConversationId).toBeNull()
    expect(hub.calls).toStrictEqual([{ method: CHAT_HUB_METHODS.leaveConversation, conversationId: 'a' }])
  })

  it('没有活跃会话时关闭是安全空转，不发出退组调用', () => {
    const store = setup()

    store.closeActiveConversation()

    expect(hub.calls).toStrictEqual([])
  })
})

describe('已读上报防抖', () => {
  it('本地未读立即清零，接口调用推迟到 800ms 后且多次触发只报一次', async () => {
    vi.useFakeTimers()
    const marked: string[] = []
    const store = setup({
      myConversations: () => Promise.resolve([conversation({ conversationId: 'a', unreadCount: 9 })]),
      markRead: (conversationId: string) => {
        marked.push(conversationId)
        return Promise.resolve()
      },
    })
    await store.loadConversations()

    store.markConversationRead('a')
    store.markConversationRead('a')
    store.markConversationRead('a')

    expect(store.conversations[0]?.unreadCount).toBe(0)
    expect(marked).toStrictEqual([])

    await vi.advanceTimersByTimeAsync(799)
    expect(marked).toStrictEqual([])

    await vi.advanceTimersByTimeAsync(1)
    expect(marked).toStrictEqual(['a'])
  })

  it('上报接口失败被就地吞掉，且不影响下一轮重新上报', async () => {
    vi.useFakeTimers()
    const attempts: string[] = []
    const store = setup({
      markRead: (conversationId: string) => {
        attempts.push(conversationId)
        return Promise.reject(new Error('403'))
      },
    })

    store.markConversationRead('a')
    await vi.advanceTimersByTimeAsync(1000)
    expect(attempts).toStrictEqual(['a'])

    store.markConversationRead('a')
    await vi.advanceTimersByTimeAsync(1000)
    expect(attempts).toStrictEqual(['a', 'a'])
  })

  it('未读为 0 的活跃会话调用 consumeActiveUnread 不触发上报', async () => {
    vi.useFakeTimers()
    const marked: string[] = []
    const store = setup({
      myConversations: () => Promise.resolve([conversation({ conversationId: 'a', unreadCount: 0 })]),
      markRead: (conversationId: string) => {
        marked.push(conversationId)
        return Promise.resolve()
      },
    })
    await store.loadConversations()
    store.activeConversationId = 'a'

    store.consumeActiveUnread()
    await vi.advanceTimersByTimeAsync(1000)

    expect(marked).toStrictEqual([])
  })

  it('无活跃会话时 consumeActiveUnread 空转', async () => {
    vi.useFakeTimers()
    const marked: string[] = []
    const store = setup({
      markRead: (conversationId: string) => {
        marked.push(conversationId)
        return Promise.resolve()
      },
    })

    store.consumeActiveUnread()
    await vi.advanceTimersByTimeAsync(1000)

    expect(marked).toStrictEqual([])
  })
})

describe('输入中提示', () => {
  it('他人 typing 展示 4 秒后自动消失', async () => {
    vi.useFakeTimers()
    const store = setup()

    store.applyTyping({ conversationId: 'a', userId: 'other', userName: '张三' })
    expect(store.typingIndicators.a?.userId).toBe('other')

    await vi.advanceTimersByTimeAsync(3999)
    expect(store.typingIndicators.a).toBeDefined()

    await vi.advanceTimersByTimeAsync(1)
    expect(store.typingIndicators.a).toBeUndefined()
  })

  it('自己的 typing 回声被忽略，不给自己显示「对方正在输入」', () => {
    const store = setup()

    store.applyTyping({ conversationId: 'a', userId: 'me' })

    expect(store.typingIndicators.a).toBeUndefined()
  })

  it('同会话连续 typing 会重置 4 秒倒计时而不是叠加多个定时器', async () => {
    vi.useFakeTimers()
    const store = setup()

    store.applyTyping({ conversationId: 'a', userId: 'other' })
    await vi.advanceTimersByTimeAsync(3000)
    store.applyTyping({ conversationId: 'a', userId: 'other' })
    await vi.advanceTimersByTimeAsync(3000)

    expect(store.typingIndicators.a).toBeDefined()

    await vi.advanceTimersByTimeAsync(1000)
    expect(store.typingIndicators.a).toBeUndefined()
  })

  it('输入中上报按 3 秒节流，窗口内的多次输入只组播一次', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00Z'))
    const store = setup()

    store.sendTyping('a')
    store.sendTyping('a')
    expect(hub.calls.filter(call => call.method === CHAT_HUB_METHODS.typing)).toHaveLength(1)

    vi.setSystemTime(new Date('2026-08-27T00:00:02Z'))
    store.sendTyping('a')
    expect(hub.calls.filter(call => call.method === CHAT_HUB_METHODS.typing)).toHaveLength(1)

    vi.setSystemTime(new Date('2026-08-27T00:00:03Z'))
    store.sendTyping('a')
    expect(hub.calls.filter(call => call.method === CHAT_HUB_METHODS.typing)).toHaveLength(2)
  })

  it('不同会话的输入中上报各自计节流窗口', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00Z'))
    const store = setup()

    store.sendTyping('a')
    store.sendTyping('b')

    expect(hub.calls.filter(call => call.method === CHAT_HUB_METHODS.typing)).toHaveLength(2)
  })
})

describe('草稿持久化', () => {
  it('没有草稿的会话取到空串而非 undefined', () => {
    const store = setup()

    expect(store.getDraft('never-typed')).toBe('')
  })

  it('写入草稿同时落盘 localStorage，按会话隔离', () => {
    const store = setup()

    store.setDraft('a', '你好')
    store.setDraft('b', 'hello')

    expect(store.getDraft('a')).toBe('你好')
    expect(store.getDraft('b')).toBe('hello')
    expect(JSON.parse(localStorage.getItem(CHAT_DRAFTS_STORAGE_KEY) ?? '{}')).toStrictEqual({ a: '你好', b: 'hello' })
  })

  it('草稿被清空或只剩空白时删除该条，不在存储里留空串', () => {
    const store = setup()
    store.setDraft('a', '半句话')

    store.setDraft('a', '   ')

    expect(store.getDraft('a')).toBe('')
    expect(JSON.parse(localStorage.getItem(CHAT_DRAFTS_STORAGE_KEY) ?? '{}')).toStrictEqual({})
  })

  it('store 初始化时从 localStorage 恢复草稿', () => {
    localStorage.setItem(CHAT_DRAFTS_STORAGE_KEY, JSON.stringify({ a: '上次没发完' }))

    const store = setup()

    expect(store.getDraft('a')).toBe('上次没发完')
  })
})

describe('@提及请求与抽屉版本', () => {
  it('同一个人被连续「@TA」两次也能触发两轮插入，靠 seq 递增区分', () => {
    const store = setup()

    store.requestMention('a', 'u9', '李四')
    const first = store.mentionRequest?.seq ?? 0
    store.requestMention('a', 'u9', '李四')
    const second = store.mentionRequest?.seq ?? 0

    expect(second).toBe(first + 1)
    expect(store.mentionRequest).toMatchObject({ conversationId: 'a', userId: 'u9', userName: '李四' })
  })

  it('请求打开聊天抽屉时版本计数器单调递增', () => {
    const store = setup()
    const before = store.chatDrawerVersion

    store.requestOpenChatDrawer()
    store.requestOpenChatDrawer()

    expect(store.chatDrawerVersion).toBe(before + 2)
  })
})
