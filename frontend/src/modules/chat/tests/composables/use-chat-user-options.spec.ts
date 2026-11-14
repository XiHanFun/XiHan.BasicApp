/**
 * composables/useChatUserOptions.ts：选人候选的取数
 * 锁定：换条件从第一页取、往下按已取的条数续取并去重；只认最后一次请求，慢响应不覆盖新结果；
 * 第一页失败清空并抛出，续取失败保留已有的；没有下一页或正在取时不发请求
 */
import type { ChatApiContract, ChatUserOption, ChatUserOptionsQuery, ChatUserOptionsResult } from '@/modules/chat/types'
import { beforeEach, describe, expect, it } from 'vitest'
import { setChatApi } from '@/modules/chat/api-contract'
import { CHAT_USER_OPTIONS_PAGE_SIZE, useChatUserOptions } from '@/modules/chat/composables/useChatUserOptions'

let queries: ChatUserOptionsQuery[] = []
let respond: (query: ChatUserOptionsQuery) => Promise<ChatUserOptionsResult>

function user(userId: string): ChatUserOption {
  return {
    userId,
    userName: userId,
    displayName: userId.toUpperCase(),
    avatarFileId: null,
    departmentName: null,
  }
}

function page(userIds: string[], hasMore = false): ChatUserOptionsResult {
  return {
    items: userIds.map(user),
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
    userOptions: (query: ChatUserOptionsQuery) => {
      queries.push(query)
      return respond(query)
    },
  } as unknown as ChatApiContract)
})

describe('useChatUserOptions', () => {
  it('换条件从第一页取，带上关键字、部门与每页条数', async () => {
    respond = () => Promise.resolve(page(['a', 'b'], true))
    const directory = useChatUserOptions()

    await directory.search('ali', 'dept-1')

    expect(queries).toEqual([
      {
        keyword: 'ali',
        departmentId: 'dept-1',
        skip: 0,
        limit: CHAT_USER_OPTIONS_PAGE_SIZE,
      },
    ])
    expect(directory.users.value.map(item => item.userId)).toEqual(['a', 'b'])
    expect(directory.hasMore.value).toBe(true)
    expect(directory.loading.value).toBe(false)
  })

  it('续取按已取的条数跳过、沿用上一次的条件，重复的人只留先到的', async () => {
    respond = query => Promise.resolve(query.skip
      ? page(['b', 'c'])
      : page(['a', 'b'], true))
    const directory = useChatUserOptions()

    await directory.search('x', null)
    await directory.loadMore()

    expect(queries[1]).toEqual({
      keyword: 'x',
      departmentId: null,
      skip: 2,
      limit: CHAT_USER_OPTIONS_PAGE_SIZE,
    })
    expect(directory.users.value.map(item => item.userId)).toEqual(['a', 'b', 'c'])
    expect(directory.hasMore.value).toBe(false)
  })

  it('没有下一页时续取不发请求', async () => {
    respond = () => Promise.resolve(page(['a']))
    const directory = useChatUserOptions()

    await directory.search('', null)
    await directory.loadMore()

    expect(queries).toHaveLength(1)
  })

  it('只认最后一次请求：先发的慢响应回来不覆盖新条件的结果，它失败也不抛', async () => {
    const slow = deferred<ChatUserOptionsResult>()
    respond = query => query.keyword === 'old'
      ? slow.promise
      : Promise.resolve(page(['new']))
    const directory = useChatUserOptions()

    const stale = directory.search('old', null)
    await directory.search('new', null)
    slow.reject(new Error('timeout'))

    await expect(stale).resolves.toBeUndefined()
    expect(directory.users.value.map(item => item.userId)).toEqual(['new'])
    expect(directory.loading.value).toBe(false)
  })

  it('第一页失败：清空并抛出', async () => {
    respond = () => Promise.resolve(page(['a'], true))
    const directory = useChatUserOptions()
    await directory.search('', null)
    respond = () => Promise.reject(new Error('boom'))

    await expect(directory.search('x', null)).rejects.toThrow('boom')

    expect(directory.users.value).toEqual([])
    expect(directory.hasMore.value).toBe(false)
    expect(directory.loading.value).toBe(false)
  })

  it('续取失败：已有的留着、还能再试', async () => {
    respond = () => Promise.resolve(page(['a'], true))
    const directory = useChatUserOptions()
    await directory.search('', null)
    respond = () => Promise.reject(new Error('boom'))

    await expect(directory.loadMore()).rejects.toThrow('boom')

    expect(directory.users.value.map(item => item.userId)).toEqual(['a'])
    expect(directory.hasMore.value).toBe(true)
  })
})
