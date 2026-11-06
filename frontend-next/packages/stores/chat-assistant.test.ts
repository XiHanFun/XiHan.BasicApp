/**
 * 聊天助手登记点的语义：没有登记时入口隐藏、登记后可见、后登记覆盖先登记
 * 登记点是模块级单例，每个用例都 vi.resetModules() 后重新动态导入，互不串状态
 */
import type { ChatAssistantProvider } from './chat-assistant'
import { afterEach, describe, expect, it, vi } from 'vitest'

async function load(): Promise<typeof import('./chat-assistant')> {
  vi.resetModules()
  return import('./chat-assistant')
}

function createFakeProvider(tag: string): ChatAssistantProvider {
  return {
    availableAssistants: () => Promise.resolve([
      {
        id: tag,
        code: tag,
        name: tag,
        avatar: null,
        description: null,
        isDefault: false,
      },
    ]),
    openConversation: assistantId => Promise.resolve({
      conversationId: tag,
      assistantId,
      assistantName: tag,
      avatar: null,
      created: true,
    }),
    reply: () => Promise.resolve({ messageId: tag }),
  }
}

afterEach(() => {
  vi.resetModules()
})

describe('聊天助手登记点', () => {
  it('没有登记时 hasChatAssistantProvider 为假、getChatAssistantProvider 返回 null（助手入口据此隐藏）', async () => {
    const { getChatAssistantProvider, hasChatAssistantProvider } = await load()

    expect(hasChatAssistantProvider()).toBe(false)
    expect(getChatAssistantProvider()).toBeNull()
  })

  it('登记后可见；重新加载模块回到没有登记', async () => {
    const { getChatAssistantProvider, hasChatAssistantProvider, registerChatAssistantProvider } = await load()
    const provider = createFakeProvider('only')

    registerChatAssistantProvider(provider)

    expect(hasChatAssistantProvider()).toBe(true)
    expect(getChatAssistantProvider()).toBe(provider)

    const reloaded = await load()
    expect(reloaded.hasChatAssistantProvider()).toBe(false)
  })

  it('重复登记以最后一次为准', async () => {
    const { getChatAssistantProvider, registerChatAssistantProvider } = await load()
    const latest = createFakeProvider('latest')

    registerChatAssistantProvider(createFakeProvider('stale'))
    registerChatAssistantProvider(latest)

    expect(getChatAssistantProvider()).toBe(latest)
  })
})
