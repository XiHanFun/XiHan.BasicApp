/**
 * packages/chat 两个注入缝的注册表语义：api-contract 与 assistant-provider。
 *
 * 职责边界：只验证「未注册时的行为」「注册后的可见性」「后注册覆盖先注册」这三件事。
 * 两个模块都持有模块级单例，因此每个用例都 vi.resetModules() 后重新动态导入，互不串状态。
 */
import type { ChatAssistantProvider } from './assistant-provider'
import type { ChatApiContract } from './types'
import { afterEach, describe, expect, it, vi } from 'vitest'

async function loadApiContract(): Promise<typeof import('./api-contract')> {
  vi.resetModules()
  return import('./api-contract')
}

async function loadAssistantProvider(): Promise<typeof import('./assistant-provider')> {
  vi.resetModules()
  return import('./assistant-provider')
}

/** 只填被测路径会碰到的成员，其余按契约形状补空实现 */
function createFakeApi(tag: string): ChatApiContract {
  const notUsed = () => Promise.reject(new Error(`未预期的调用：${tag}`))
  return {
    openSingleConversation: notUsed,
    createGroupConversation: notUsed,
    openDepartmentConversation: notUsed,
    addMembers: notUsed,
    removeMember: notUsed,
    leaveConversation: notUsed,
    sendMessage: notUsed,
    recallMessage: notUsed,
    markRead: notUsed,
    editMessage: notUsed,
    toggleReaction: notUsed,
    pinMessage: notUsed,
    unpinMessage: notUsed,
    togglePinConversation: notUsed,
    toggleMuteConversation: notUsed,
    updateConversationInfo: notUsed,
    transferOwner: notUsed,
    setMemberSilence: notUsed,
    setMemberRole: notUsed,
    myConversations: () => Promise.resolve([]),
    messageHistory: notUsed,
    searchMessages: notUsed,
    members: notUsed,
    readPositions: notUsed,
    pinnedMessages: notUsed,
    selectUsers: notUsed,
    departmentTree: notUsed,
    uploadAttachment: notUsed,
    getFileUrl: () => Promise.resolve(tag),
  } as unknown as ChatApiContract
}

function createFakeProvider(tag: string): ChatAssistantProvider {
  return {
    availableAssistants: () => Promise.resolve([]),
    openConversation: () => Promise.resolve({
      conversationId: tag,
      assistantId: tag,
      assistantName: tag,
      created: true,
    }),
    reply: () => Promise.resolve({ messageId: tag }),
  }
}

afterEach(() => {
  vi.resetModules()
})

describe('聊天 API 注入缝', () => {
  it('未注册时 getChatApi 抛出提示 setup 未执行的错误，而不是返回 undefined 让调用点稍后崩', async () => {
    const { getChatApi } = await loadApiContract()

    expect(() => getChatApi()).toThrow(/尚未注册/u)
  })

  it('注册后 getChatApi 返回的正是注册进去的那个实现实例', async () => {
    const { getChatApi, setChatApi } = await loadApiContract()
    const api = createFakeApi('first')

    setChatApi(api)

    expect(getChatApi()).toBe(api)
  })

  it('重复注册以最后一次为准，热更新重装模块不会残留旧实现', async () => {
    const { getChatApi, setChatApi } = await loadApiContract()
    const second = createFakeApi('second')

    setChatApi(createFakeApi('first'))
    setChatApi(second)

    expect(getChatApi()).toBe(second)
    await expect(getChatApi().getFileUrl('x')).resolves.toBe('second')
  })
})

describe('助手提供方扩展点（AI 模块可选）', () => {
  it('未注册时 hasChatAssistantProvider 为假、getChatAssistantProvider 返回 null（助手入口据此隐藏）', async () => {
    const { getChatAssistantProvider, hasChatAssistantProvider } = await loadAssistantProvider()

    expect(hasChatAssistantProvider()).toBe(false)
    expect(getChatAssistantProvider()).toBeNull()
  })

  it('注册后 has 为真且拿到同一实例，删除 AI 模块即恢复未注册态', async () => {
    const { getChatAssistantProvider, hasChatAssistantProvider, registerChatAssistantProvider }
      = await loadAssistantProvider()
    const provider = createFakeProvider('ai')

    registerChatAssistantProvider(provider)

    expect(hasChatAssistantProvider()).toBe(true)
    expect(getChatAssistantProvider()).toBe(provider)

    const reloaded = await loadAssistantProvider()
    expect(reloaded.hasChatAssistantProvider()).toBe(false)
  })

  it('重复注册以最后一次为准', async () => {
    const { getChatAssistantProvider, registerChatAssistantProvider } = await loadAssistantProvider()
    const latest = createFakeProvider('latest')

    registerChatAssistantProvider(createFakeProvider('stale'))
    registerChatAssistantProvider(latest)

    expect(getChatAssistantProvider()).toBe(latest)
    await expect(latest.openConversation('a')).resolves.toMatchObject({ conversationId: 'latest' })
  })

  it('未注册助手时不抛错——助手能力是可选扩展，不能把聊天壳层拖崩', async () => {
    const { getChatAssistantProvider } = await loadAssistantProvider()

    expect(() => getChatAssistantProvider()).not.toThrow()
  })
})
