/**
 * composables/useChatGlobalSearch.ts：跨会话搜索的取数
 * 锁定：换关键字从第一页取、往下按已取的条数续取并去重；关键字为空清空且不发请求；
 * 只认最后一次请求，慢响应不覆盖新结果；第一页失败清空并抛出，续取失败保留已有的；没有下一页或正在取时不发请求；
 * searched 是第一页已有结论的关键字，页面拿它判断结果跟没跟上输入
 */
import type { ChatApiContract, ChatMessageHistoryResult, ChatMessageItem, ChatMessageSearchAllQuery } from '@/modules/chat/types'
import { beforeEach, describe, expect, it } from 'vitest'
import { setChatApi } from '@/modules/chat/api-contract'
import { CHAT_GLOBAL_SEARCH_PAGE_SIZE, useChatGlobalSearch } from '@/modules/chat/composables/useChatGlobalSearch'

let queries: ChatMessageSearchAllQuery[] = []
let respond: (query: ChatMessageSearchAllQuery) => Promise<ChatMessageHistoryResult>

function hit(messageId: string): ChatMessageItem {
  return {
    messageId,
    conversationId: 'c-1',
    seq: 1,
    senderUserId: 'u-1',
    senderUserName: 'alice',
    messageType: 'Text',
    content: `hit ${messageId}`,
    attachments: [],
    isRecalled: false,
    createdAt: '2026-11-14T08:00:00Z',
    mentionedUserIds: [],
    mentionAll: false,
    isPinned: false,
    reactions: [],
  }
}

function page(messageIds: string[], hasMore = false): ChatMessageHistoryResult {
  return {
    items: messageIds.map(hit),
    hasMore,
  }
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

beforeEach(() => {
  queries = []
  respond = () => Promise.resolve(page([]))
  setChatApi({
    searchAllMessages: (query: ChatMessageSearchAllQuery) => {
      queries.push(query)
      return respond(query)
    },
  } as unknown as ChatApiContract)
})

describe('useChatGlobalSearch', () => {
  it('换关键字从第一页取，带上去掉首尾空白的关键字与每页条数', async () => {
    respond = () => Promise.resolve(page(['a', 'b'], true))
    const search = useChatGlobalSearch()

    await search.search('  report ')

    expect(queries).toEqual([
      { keyword: 'report', skip: 0, take: CHAT_GLOBAL_SEARCH_PAGE_SIZE },
    ])
    expect(search.hits.value.map(item => item.messageId)).toEqual(['a', 'b'])
    expect([search.hasMore.value, search.loading.value]).toEqual([true, false])
  })

  it('关键字为空：清空结果，不发请求', async () => {
    respond = () => Promise.resolve(page(['a'], true))
    const search = useChatGlobalSearch()
    await search.search('report')

    await search.search('   ')

    expect(queries).toHaveLength(1)
    expect([search.hits.value, search.hasMore.value]).toEqual([[], false])
  })

  it('往下按已取的条数续取，同一条只留先到的', async () => {
    const search = useChatGlobalSearch()
    respond = () => Promise.resolve(page(['a', 'b'], true))
    await search.search('report')

    respond = () => Promise.resolve(page(['b', 'c']))
    await search.loadMore()

    expect(queries[1]).toEqual({ keyword: 'report', skip: 2, take: CHAT_GLOBAL_SEARCH_PAGE_SIZE })
    expect(search.hits.value.map(item => item.messageId)).toEqual(['a', 'b', 'c'])
    expect(search.hasMore.value).toBe(false)
  })

  it('没有下一页或正在取时不发请求', async () => {
    const search = useChatGlobalSearch()
    await search.search('report')
    await search.loadMore()
    expect(queries).toHaveLength(1)

    const pending = deferred<ChatMessageHistoryResult>()
    respond = () => pending.promise
    const searching = search.search('other')
    await search.loadMore()
    expect(queries).toHaveLength(2)
    pending.resolve(page(['x']))
    await searching
  })

  it('只认最后一次请求：慢响应不覆盖新关键字的结果，过期的失败也不抛', async () => {
    const search = useChatGlobalSearch()
    const slow = deferred<ChatMessageHistoryResult>()
    const failing = deferred<ChatMessageHistoryResult>()
    respond = query => (query.keyword === 'slow'
      ? slow.promise
      : query.keyword === 'failing'
        ? failing.promise
        : Promise.resolve(page(['fresh'])))

    const first = search.search('slow')
    const second = search.search('failing')
    await search.search('fresh')
    slow.resolve(page(['stale']))
    failing.reject(new Error('gone'))
    await Promise.all([first, second])

    expect(search.hits.value.map(item => item.messageId)).toEqual(['fresh'])
    expect(search.loading.value).toBe(false)
  })

  it('搜到一半把关键字清空：迟到的结果不再落地', async () => {
    const search = useChatGlobalSearch()
    const slow = deferred<ChatMessageHistoryResult>()
    respond = () => slow.promise

    const pending = search.search('slow')
    await search.search('')
    slow.resolve(page(['stale']))
    await pending

    expect(search.hits.value).toEqual([])
  })

  it('searched 跟着第一页的结论走：请求回来之前还是上一个词，失败也算有了结论，清空关键字即清空', async () => {
    const search = useChatGlobalSearch()
    expect(search.searched.value).toBe('')

    const slow = deferred<ChatMessageHistoryResult>()
    respond = () => slow.promise
    const pending = search.search(' report ')
    expect(search.searched.value).toBe('')
    slow.resolve(page(['a'], true))
    await pending
    expect(search.searched.value).toBe('report')

    // 续取不动它
    respond = () => Promise.resolve(page(['b']))
    await search.loadMore()
    expect(search.searched.value).toBe('report')

    respond = () => Promise.reject(new Error('offline'))
    await expect(search.search('again')).rejects.toThrow('offline')
    expect(search.searched.value).toBe('again')

    await search.search('')
    expect(search.searched.value).toBe('')
  })

  it('第一页失败清空并抛出；续取失败保留已有的', async () => {
    const search = useChatGlobalSearch()
    respond = () => Promise.resolve(page(['a'], true))
    await search.search('report')

    respond = () => Promise.reject(new Error('offline'))
    await expect(search.loadMore()).rejects.toThrow('offline')
    expect(search.hits.value.map(item => item.messageId)).toEqual(['a'])

    await expect(search.search('again')).rejects.toThrow('offline')
    expect([search.hits.value, search.hasMore.value, search.loading.value]).toEqual([[], false, false])
  })
})
