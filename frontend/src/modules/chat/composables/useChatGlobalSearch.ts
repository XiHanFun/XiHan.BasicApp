/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatMessageItem } from '../types'
import { ref, shallowRef } from 'vue'
import { getChatApi } from '../api-contract'

/** 跨会话搜索每页条数 */
export const CHAT_GLOBAL_SEARCH_PAGE_SIZE = 20

/**
 * 跨会话搜索的取数：换关键字从头取，往下按已取的条数续取
 * 只认最后一次请求：慢响应不覆盖新关键字的结果，过期的请求静默结束、失败也不抛
 */
export function useChatGlobalSearch() {
  const hits = shallowRef<ChatMessageItem[]>([])
  const hasMore = ref(false)
  const loading = ref(false)
  /** 第一页已经有了结论（取到或失败）的关键字；与要找的词不一样，就是结果还没跟上 */
  const searched = ref('')
  let seq = 0
  let keyword = ''

  async function fetchPage(append: boolean): Promise<void> {
    const current = ++seq
    loading.value = true
    try {
      const result = await getChatApi().searchAllMessages({
        keyword,
        skip: append
          ? hits.value.length
          : 0,
        take: CHAT_GLOBAL_SEARCH_PAGE_SIZE,
      })
      if (current !== seq) {
        return
      }
      if (append) {
        // 翻页之间来了新消息会错开：同一条只留先到的
        const seen = new Set(hits.value.map(hit => hit.messageId))
        hits.value = [
          ...hits.value,
          ...result.items.filter(hit => !seen.has(hit.messageId)),
        ]
      }
      else {
        hits.value = result.items
        searched.value = keyword
      }
      hasMore.value = result.hasMore
    }
    catch (error) {
      if (current !== seq) {
        return
      }
      if (!append) {
        hits.value = []
        hasMore.value = false
        searched.value = keyword
      }
      throw error
    }
    finally {
      if (current === seq) {
        loading.value = false
      }
    }
  }

  /**
   * 按新关键字从第一页取；关键字为空即清空，不发请求
   * @param nextKeyword 关键字（匹配正文与附件名）
   */
  function search(nextKeyword: string): Promise<void> {
    keyword = nextKeyword.trim()
    if (!keyword) {
      seq += 1
      hits.value = []
      hasMore.value = false
      loading.value = false
      searched.value = ''
      return Promise.resolve()
    }
    return fetchPage(false)
  }

  /** 往下取一页；没有下一页或正在取时不发请求 */
  function loadMore(): Promise<void> {
    return hasMore.value && !loading.value
      ? fetchPage(true)
      : Promise.resolve()
  }

  return {
    hits,
    hasMore,
    loading,
    searched,
    search,
    loadMore,
  }
}
