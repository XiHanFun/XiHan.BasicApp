/**
 * 聊天模块注入缝的注册表语义：api-contract
 *
 * 职责边界：只验证「未注册时的行为」「注册后的可见性」「后注册覆盖先注册」这三件事
 * 模块持有模块级单例，因此每个用例都 vi.resetModules() 后重新动态导入，互不串状态
 */
import type { ChatApiContract } from './types'
import { afterEach, describe, expect, it, vi } from 'vitest'

async function loadApiContract(): Promise<typeof import('./api-contract')> {
  vi.resetModules()
  return import('./api-contract')
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
    forwardMessage: notUsed,
    pinMessage: notUsed,
    unpinMessage: notUsed,
    setConversationPinned: notUsed,
    setConversationMuted: notUsed,
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
    getFileUrl: () => Promise.resolve({ url: tag, expiresAt: '2026-11-01T00:00:00Z' }),
  } satisfies ChatApiContract
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
    await expect(getChatApi().getFileUrl('c', 'x')).resolves.toMatchObject({ url: 'second' })
  })
})
