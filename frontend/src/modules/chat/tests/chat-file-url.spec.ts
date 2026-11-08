/**
 * composables/useChatFileUrl.ts：聊天附件按会话换签名地址（附件以会话为访问范围）
 * 锁定：同一会话的同一文件并发只换一次、到期前复用缓存、快到期了重新换；不同会话分开换；换不到地址时为空串
 */
import type { ChatApiContract, ChatFileUrl } from '@/modules/chat/types'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick, ref } from 'vue'
import { setChatApi } from '@/modules/chat/api-contract'
import { clearChatFileUrlCache, resolveChatFileUrl, useChatFileUrl, useChatFileUrls } from '@/modules/chat/composables/useChatFileUrl'

let calls: { conversationId: string, fileId: string }[] = []
let respond: (conversationId: string, fileId: string) => Promise<ChatFileUrl>

function inMinutes(minutes: number): string {
  return new Date(Date.now() + minutes * 60_000).toISOString()
}

beforeEach(() => {
  clearChatFileUrlCache()
  calls = []
  respond = (conversationId, fileId) => Promise.resolve({
    url: `https://oss/${conversationId}/${fileId}`,
    expiresAt: inMinutes(5),
  })
  setChatApi({
    getFileUrl: (conversationId: string, fileId: string) => {
      calls.push({ conversationId, fileId })
      return respond(conversationId, fileId)
    },
  } as unknown as ChatApiContract)
})

afterEach(() => {
  vi.useRealTimers()
})

describe('换取与缓存', () => {
  it('同一会话的同一文件并发只换一次，换到后到期前复用缓存', async () => {
    const [a, b] = await Promise.all([
      resolveChatFileUrl({ conversationId: 'c-1', fileId: 'f-1' }),
      resolveChatFileUrl({ conversationId: 'c-1', fileId: 'f-1' }),
    ])
    const again = await resolveChatFileUrl({ conversationId: 'c-1', fileId: 'f-1' })

    expect([a, b, again]).toEqual(['https://oss/c-1/f-1', 'https://oss/c-1/f-1', 'https://oss/c-1/f-1'])
    expect(calls).toHaveLength(1)
  })

  it('不同会话里的同一文件按各自会话换（访问范围是会话）', async () => {
    await resolveChatFileUrl({ conversationId: 'c-1', fileId: 'f-1' })
    await resolveChatFileUrl({ conversationId: 'c-2', fileId: 'f-1' })

    expect(calls).toEqual([
      { conversationId: 'c-1', fileId: 'f-1' },
      { conversationId: 'c-2', fileId: 'f-1' },
    ])
  })

  it('快到期的地址不再复用，重新换一次', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-11-01T00:00:00Z'))
    await resolveChatFileUrl({ conversationId: 'c-1', fileId: 'f-1' })

    vi.setSystemTime(new Date('2026-11-01T00:04:45Z'))
    await resolveChatFileUrl({ conversationId: 'c-1', fileId: 'f-1' })

    expect(calls).toHaveLength(2)
  })
})

describe('响应式地址', () => {
  it('一组附件逐项对应；换不到的那一项为空串，其余照常', async () => {
    respond = (conversationId, fileId) => (fileId === 'bad'
      ? Promise.reject(new Error('denied'))
      : Promise.resolve({
          url: `https://oss/${conversationId}/${fileId}`,
          expiresAt: inMinutes(5),
        }))
    const urls = useChatFileUrls(() => [
      { conversationId: 'c-1', fileId: 'f-1' },
      { conversationId: 'c-1', fileId: 'bad' },
    ])

    await vi.waitFor(() => expect(urls.value[0]).toBe('https://oss/c-1/f-1'))
    expect(urls.value[1]).toBe('')
  })

  it('单个附件：入参换成别的文件时只认新的那个，没有附件时为空串', async () => {
    const source = ref<null | { conversationId: string, fileId: string }>({ conversationId: 'c-1', fileId: 'v-1' })
    const url = useChatFileUrl(source)
    await vi.waitFor(() => expect(url.value).toBe('https://oss/c-1/v-1'))

    source.value = null
    await nextTick()
    expect(url.value).toBe('')
  })
})
