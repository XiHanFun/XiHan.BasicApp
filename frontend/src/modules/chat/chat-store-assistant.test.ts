/**
 * store.ts 的助手回复流、发起会话与 $reset 清理
 *
 * 职责边界：AI 助手是可选扩展——未注册提供方时聊天必须照常工作；已注册时增量气泡的
 * replyId 配对、失败展示与落库后丢弃是本文件的重点。另外覆盖发起各类会话的「刷新列表再进入」
 * 顺序，以及 $reset 是否真的把定时器一起清掉（不清就会在登出后继续打接口）
 */
import type { ChatApiContract, ChatConversationListItem, ChatConversationOpenResult, ChatMessageItem } from './types'
import type { ChatAssistantProvider, Session } from '~/stores'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'
import { setChatApi } from './api-contract'
import { useChatStore } from './store'

const hub = vi.hoisted(() => ({ calls: [] as string[] }))

// 助手提供方注册表本身是模块级单例，真实模块没有反注册入口
// 这里整体替身掉并在每个用例前清空，保证「未注册」的降级用例不受其它用例注册残留影响，
// 用例因此可任意顺序 / 并行执行。注册表自身的语义在 packages/stores/chat-assistant.test.ts 里测
const assistant = vi.hoisted(() => ({ provider: null as unknown }))

vi.mock('~/stores/chat-assistant', () => ({
  getChatAssistantProvider: () => assistant.provider,
  hasChatAssistantProvider: () => assistant.provider !== null,
  registerChatAssistantProvider: (impl: unknown) => {
    assistant.provider = impl
  },
}))

vi.mock('~/composables/useSignalR', () => ({
  useSignalR: () => ({
    invoke: (method: string) => {
      hub.calls.push(method)
      return Promise.resolve()
    },
  }),
}))

function conversation(patch: Partial<ChatConversationListItem> & { conversationId: string }): ChatConversationListItem {
  return {
    lastSeq: 0,
    conversationType: 'Single',
    displayName: '会话',
    memberCount: 2,
    memberRole: 'Member',
    unreadCount: 0,
    isMuted: false,
    isPinned: false,
    isSilenced: false,
    ...patch,
  }
}

function message(patch: Partial<ChatMessageItem> & { messageId: string }): ChatMessageItem {
  return {
    // 数字形态的消息号即会话内序号，便于断言按 seq 落位
    seq: Number(patch.messageId) || 1,
    attachments: [],
    conversationId: 'a',
    senderUserId: 'me',
    messageType: 'Text',
    content: '问一句',
    isRecalled: false,
    createdAt: '2026-08-27T00:00:00Z',
    mentionedUserIds: [],
    isPinned: false,
    reactions: [],
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

/** 登录为指定用户：userInfo 是 auth 会话的投影，测试直接写会话（不落本地存储） */
function signIn(userId: string, displayName = userId) {
  useAuthStore().session = {
    accessToken: 'a-1',
    refreshToken: 'r-1',
    sessionId: 's-1',
    tenantId: null,
    tenantName: null,
    impersonatorUserName: null,
    mustChangePassword: false,
    user: { id: userId, userName: userId, displayName },
  } as unknown as Session
}

function setup(overrides: Partial<ChatApiContract> = {}) {
  setActivePinia(createPinia())
  signIn('me', '我')
  setChatApi(createApi(overrides))
  return useChatStore()
}

function fakeProvider(overrides: Partial<ChatAssistantProvider> = {}): ChatAssistantProvider {
  return {
    availableAssistants: () => Promise.resolve([]),
    openConversation: (assistantId: string) => Promise.resolve({
      conversationId: `conv-${assistantId}`,
      assistantId,
      assistantName: '小助手',
      avatar: null,
      created: true,
    }),
    reply: () => Promise.resolve({ messageId: 'reply-1' }),
    ...overrides,
  }
}

/** 注册助手提供方（写进被替身的注册表） */
function registerChatAssistantProvider(impl: ChatAssistantProvider): void {
  assistant.provider = impl
}

beforeEach(() => {
  hub.calls = []
  assistant.provider = null
})

afterEach(() => {
  vi.useRealTimers()
})

describe('助手入口的探测（后端关掉 AI、前端 AI 模块还在）', () => {
  it('收到「助手不可用」即隐藏入口，只问一次', async () => {
    let calls = 0
    registerChatAssistantProvider(fakeProvider({
      availableAssistants: () => {
        calls += 1
        return Promise.reject(new ApiError(503, { code: 'CHAT_UNAVAILABLE_001', status: 503, detail: '助手不可用' }))
      },
    }))
    const store = setup()

    await store.probeAssistants()
    await store.probeAssistants()

    expect(store.assistantsUnavailable).toBe(true)
    expect(calls).toBe(1)
  })

  it('别的错误抛给调用方、不隐藏入口，下次展开再问', async () => {
    let calls = 0
    registerChatAssistantProvider(fakeProvider({
      availableAssistants: () => {
        calls += 1
        return calls === 1
          ? Promise.reject(new ApiError(500, { code: 'KERNEL_UNEXPECTED_000', status: 500, detail: '出错了' }))
          : Promise.resolve([])
      },
    }))
    const store = setup()

    await expect(store.probeAssistants()).rejects.toBeInstanceOf(ApiError)
    expect(store.assistantsUnavailable).toBe(false)

    await store.probeAssistants()
    await store.probeAssistants()

    expect(store.assistantsUnavailable).toBe(false)
    expect(calls).toBe(2)
  })

  it('没有登记提供方时不发请求', async () => {
    const store = setup()

    await expect(store.probeAssistants()).resolves.toBeUndefined()
    expect(store.assistantsUnavailable).toBe(false)
  })
})

describe('未注册助手提供方时的降级', () => {
  it('请求助手回复是空转，不建临时气泡', async () => {
    const store = setup()

    await store.requestAssistantReply('a')

    expect(store.assistantStreams.a).toBeUndefined()
  })

  it('发起助手会话直接抛出「AI 模块未安装」，而不是静默失败', async () => {
    const store = setup()

    await expect(store.startAssistantConversation('bot-1')).rejects.toThrow(/未注册/u)
  })
})

describe('助手增量气泡', () => {
  it('发起回复即建立生成中的空气泡，replyId 与后续增量配对', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()

    void store.requestAssistantReply('a')

    expect(store.assistantStreams.a?.streaming).toBe(true)
    expect(store.assistantStreams.a?.text).toBe('')
  })

  it('增量按到达顺序拼接成完整文本', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()
    void store.requestAssistantReply('a')
    const replyId = store.assistantStreams.a?.replyId ?? ''

    store.applyAssistantDelta({ conversationId: 'a', replyId, delta: '你' })
    store.applyAssistantDelta({ conversationId: 'a', replyId, delta: '好' })

    expect(store.assistantStreams.a?.text).toBe('你好')
  })

  it('replyId 不匹配的增量被丢弃，上一轮的迟到分片不会污染本轮气泡', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()
    void store.requestAssistantReply('a')
    const replyId = store.assistantStreams.a?.replyId ?? ''

    store.applyAssistantDelta({ conversationId: 'a', replyId, delta: '本轮' })
    store.applyAssistantDelta({ conversationId: 'a', replyId: '上一轮', delta: '迟到' })

    expect(store.assistantStreams.a?.text).toBe('本轮')
  })

  it('没有进行中气泡时收到增量是安全空转', () => {
    const store = setup()

    store.applyAssistantDelta({ conversationId: 'a', replyId: 'r1', delta: '孤儿分片' })

    expect(store.assistantStreams.a).toBeUndefined()
  })

  it('完成推送成功时直接丢弃临时气泡，让正式落库消息接管，避免重影', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()
    void store.requestAssistantReply('a')
    const replyId = store.assistantStreams.a?.replyId ?? ''
    store.applyAssistantDelta({ conversationId: 'a', replyId, delta: '答案' })

    store.applyAssistantCompleted({ conversationId: 'a', replyId, messageId: '900' })

    expect(store.assistantStreams.a).toBeUndefined()
  })

  it('完成推送带错误时保留气泡并转为错误态供用户看见原因', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()
    void store.requestAssistantReply('a')
    const replyId = store.assistantStreams.a?.replyId ?? ''

    store.applyAssistantCompleted({ conversationId: 'a', replyId, error: '模型额度不足' })

    expect(store.assistantStreams.a?.streaming).toBe(false)
    expect(store.assistantStreams.a?.error).toBe('模型额度不足')
  })

  it('replyId 不匹配的完成推送不会误关掉当前这一轮气泡', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()
    void store.requestAssistantReply('a')

    store.applyAssistantCompleted({ conversationId: 'a', replyId: '别的轮次', messageId: '900' })

    expect(store.assistantStreams.a?.streaming).toBe(true)
  })

  it('后端的失败取本地化正文，不带错误码前缀', async () => {
    const problem = new ApiError(503, { code: 'CHAT_UNAVAILABLE_002', status: 503, detail: '助手回复生成失败' })
    registerChatAssistantProvider(fakeProvider({
      reply: () => Promise.reject(problem),
    }))
    const store = setup()

    await store.requestAssistantReply('a')

    expect(store.assistantStreams.a?.streaming).toBe(false)
    expect(store.assistantStreams.a?.error).toBe('助手回复生成失败')
  })

  it('提供方抛异常时取 message 作为错误文案', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => Promise.reject(new Error('请求超时')),
    }))
    const store = setup()

    await store.requestAssistantReply('a')

    expect(store.assistantStreams.a?.error).toBe('请求超时')
  })

  it('提供方抛出非 Error 值时按字符串化处理，不至于显示 undefined', async () => {
    registerChatAssistantProvider(fakeProvider({
      // 刻意抛非 Error：被测分支正是 error instanceof Error 为假时的字符串化兜底
      // eslint-disable-next-line prefer-promise-reject-errors
      reply: () => Promise.reject('纯字符串失败'),
    }))
    const store = setup()

    await store.requestAssistantReply('a')

    expect(store.assistantStreams.a?.error).toBe('纯字符串失败')
  })

  it('手动关闭错误气泡后该会话不再有临时气泡', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => Promise.reject(new Error('失败')),
    }))
    const store = setup()
    await store.requestAssistantReply('a')

    store.dismissAssistantStream('a')

    expect(store.assistantStreams.a).toBeUndefined()
  })

  it('activeAssistantStream 只跟随当前活跃会话，切走后取不到别的会话的气泡', async () => {
    registerChatAssistantProvider(fakeProvider({
      reply: () => new Promise(() => {}),
    }))
    const store = setup()
    void store.requestAssistantReply('a')

    store.activeConversationId = 'a'
    expect(store.activeAssistantStream?.streaming).toBe(true)

    store.activeConversationId = 'b'
    expect(store.activeAssistantStream).toBeNull()
  })

  it('往助手会话发消息，落库后自动触发一轮助手回复', async () => {
    let replyCalls = 0
    registerChatAssistantProvider(fakeProvider({
      reply: () => {
        replyCalls += 1
        return new Promise(() => {})
      },
    }))
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'a', conversationType: 'Assistant' }),
      ]),
      sendMessage: (input: { clientMessageId?: null | string }) => Promise.resolve(
        message({ messageId: '900', clientMessageId: input.clientMessageId }),
      ),
    })
    await store.loadConversations()

    await store.sendMessage({ conversationId: 'a', messageType: 'Text', content: '问一句' })
    await vi.waitFor(() => expect(replyCalls).toBe(1))

    expect(store.assistantStreams.a?.streaming).toBe(true)
  })

  it('往普通单聊发消息不会触发助手回复', async () => {
    let replyCalls = 0
    registerChatAssistantProvider(fakeProvider({
      reply: () => {
        replyCalls += 1
        return new Promise(() => {})
      },
    }))
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'a' }),
      ]),
      sendMessage: (input: { clientMessageId?: null | string }) => Promise.resolve(
        message({ messageId: '900', clientMessageId: input.clientMessageId }),
      ),
    })
    await store.loadConversations()

    await store.sendMessage({ conversationId: 'a', messageType: 'Text', content: '你好' })

    expect(replyCalls).toBe(0)
    expect(store.assistantStreams.a).toBeUndefined()
  })
})

describe('发起会话', () => {
  const opened: ChatConversationOpenResult = {
    conversationId: 'new-1',
    conversationType: 'Group',
    created: true,
  }

  it('单聊建立后先刷新列表再进入，保证进入时列表项已存在', async () => {
    const order: string[] = []
    const store = setup({
      openSingleConversation: () => {
        order.push('open-api')
        return Promise.resolve({ ...opened, conversationType: 'Single' })
      },
      myConversations: () => {
        order.push('list')
        return Promise.resolve([
          conversation({ conversationId: 'new-1' }),
        ])
      },
    })

    const result = await store.startSingleConversation('peer-1')

    expect(order).toStrictEqual(['open-api', 'list'])
    expect(store.activeConversationId).toBe('new-1')
    expect(result.conversationId).toBe('new-1')
  })

  it('建群后进入新会话', async () => {
    const store = setup({
      createGroupConversation: () => Promise.resolve(opened),
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'new-1' }),
      ]),
    })

    await store.startGroupConversation('项目组', ['u2', 'u3'])

    expect(store.activeConversationId).toBe('new-1')
  })

  it('部门群打开后进入新会话', async () => {
    const store = setup({
      openDepartmentConversation: () => Promise.resolve({ ...opened, conversationType: 'Department' }),
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'new-1' }),
      ]),
    })

    await store.startDepartmentConversation('dept-9')

    expect(store.activeConversationId).toBe('new-1')
  })

  it('已注册助手提供方时，助手会话经提供方开启并进入', async () => {
    registerChatAssistantProvider(fakeProvider())
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'conv-bot-1', conversationType: 'Assistant' }),
      ]),
    })

    const result = await store.startAssistantConversation('bot-1')

    expect(result.conversationId).toBe('conv-bot-1')
    expect(store.activeConversationId).toBe('conv-bot-1')
  })
})

describe('会话变更推送', () => {
  it('会话内 Pin 列表变更只刷新已加载过的 Pin 缓存，不触发整表刷新', async () => {
    let listCalls = 0
    let pinnedCalls = 0
    const store = setup({
      myConversations: () => {
        listCalls += 1
        return Promise.resolve([])
      },
      pinnedMessages: () => {
        pinnedCalls += 1
        return Promise.resolve([])
      },
    })
    store.pinnedMessages.a = []

    store.applyConversationChanged({ conversationId: 'a', changeType: 'pinned-changed' })
    await vi.waitFor(() => expect(pinnedCalls).toBe(1))

    expect(listCalls).toBe(0)
  })

  it('未加载过 Pin 缓存的会话收到 Pin 变更时什么都不做', async () => {
    let pinnedCalls = 0
    const store = setup({
      pinnedMessages: () => {
        pinnedCalls += 1
        return Promise.resolve([])
      },
    })

    store.applyConversationChanged({ conversationId: 'never-loaded', changeType: 'pinned-changed' })
    await Promise.resolve()

    expect(pinnedCalls).toBe(0)
  })

  it('成员增删等其它变更类型一律整表刷新', async () => {
    let listCalls = 0
    const store = setup({
      myConversations: () => {
        listCalls += 1
        return Promise.resolve([])
      },
    })

    store.applyConversationChanged({ conversationId: 'a', changeType: 'member-removed' })
    await vi.waitFor(() => expect(listCalls).toBe(1))
  })
})

describe('$reset 清理', () => {
  it('清空全部会话与消息态', async () => {
    const store = setup({
      myConversations: () => Promise.resolve([
        conversation({ conversationId: 'a', unreadCount: 3 }),
      ]),
    })
    await store.loadConversations()
    await store.openConversation('a')
    store.messages.a = [
      message({ messageId: '1' }),
    ]
    store.mentionsPending.a = true
    store.highlightMessageId = '1'
    store.detachedConversations.a = true

    store.$reset()

    expect(store.conversations).toStrictEqual([])
    expect(store.conversationsLoaded).toBe(false)
    expect(store.activeConversationId).toBeNull()
    expect(store.messages).toStrictEqual({})
    expect(store.mentionsPending).toStrictEqual({})
    expect(store.highlightMessageId).toBeNull()
    expect(store.detachedConversations).toStrictEqual({})
    expect(store.totalUnread).toBe(0)
  })

  it('登出触发 $reset 后待发的已读上报被取消，不会在无权限时继续打接口', async () => {
    vi.useFakeTimers()
    const marked: string[] = []
    const store = setup({
      markRead: (conversationId: string) => {
        marked.push(conversationId)
        return Promise.resolve()
      },
    })
    store.markConversationRead('a')

    store.$reset()
    await vi.advanceTimersByTimeAsync(2000)

    expect(marked).toStrictEqual([])
  })

  it('$reset 取消高亮定时器后，旧定时器不会把新一轮高亮清掉', async () => {
    vi.useFakeTimers()
    const store = setup({
      messageHistory: () => Promise.resolve({
        items: [
          message({ messageId: '100' }),
        ],
        hasMore: false,
      }),
    })
    await store.jumpToMessage('a', '100')

    store.$reset()
    store.highlightMessageId = '新一轮'
    await vi.advanceTimersByTimeAsync(5000)

    expect(store.highlightMessageId).toBe('新一轮')
  })

  it('$reset 清掉 typing 定时器后不会再把新的输入中提示抹掉', async () => {
    vi.useFakeTimers()
    const store = setup()
    store.applyTyping({ conversationId: 'a', userId: 'other' })

    store.$reset()
    store.typingIndicators.a = { conversationId: 'a', userId: 'other' }
    await vi.advanceTimersByTimeAsync(5000)

    expect(store.typingIndicators.a).toBeDefined()
  })

  it('$reset 清掉 typing 节流记录，登出再登录后第一次输入立即上报', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00Z'))
    const store = setup()
    store.sendTyping('a')
    expect(hub.calls.filter(item => item === 'Typing')).toHaveLength(1)

    store.$reset()
    store.sendTyping('a')

    expect(hub.calls.filter(item => item === 'Typing')).toHaveLength(2)
  })
})
