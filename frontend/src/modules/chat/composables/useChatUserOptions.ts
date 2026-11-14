/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { ChatUserOption } from '../types'
import { ref, shallowRef } from 'vue'
import { getChatApi } from '../api-contract'

/** 选人候选每页条数 */
export const CHAT_USER_OPTIONS_PAGE_SIZE = 30

/**
 * 选人候选的取数：换条件从头取，往下逐页追加
 * 只认最后一次请求：慢响应不覆盖新条件的结果，过期的请求静默结束、失败也不抛
 */
export function useChatUserOptions() {
  const users = shallowRef<ChatUserOption[]>([])
  const hasMore = ref(false)
  const loading = ref(false)
  let seq = 0
  let keyword = ''
  let departmentId: null | string = null

  async function fetchPage(append: boolean): Promise<void> {
    const current = ++seq
    loading.value = true
    try {
      const result = await getChatApi().userOptions({
        keyword,
        departmentId,
        skip: append
          ? users.value.length
          : 0,
        limit: CHAT_USER_OPTIONS_PAGE_SIZE,
      })
      if (current !== seq) {
        return
      }
      if (append) {
        // 翻页之间目录可能变了：同一个人只留先到的那条
        const seen = new Set(users.value.map(user => user.userId))
        users.value = [
          ...users.value,
          ...result.items.filter(user => !seen.has(user.userId)),
        ]
      }
      else {
        users.value = result.items
      }
      hasMore.value = result.hasMore
    }
    catch (error) {
      if (current !== seq) {
        return
      }
      if (!append) {
        users.value = []
        hasMore.value = false
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
   * 按新条件从第一页取
   * @param nextKeyword 关键字（匹配用户名与显示名）
   * @param nextDepartmentId 部门（含全部下级），不筛为 null
   */
  function search(nextKeyword: string, nextDepartmentId: null | string): Promise<void> {
    keyword = nextKeyword
    departmentId = nextDepartmentId
    return fetchPage(false)
  }

  /** 取下一页；没有下一页或正在取时不发请求 */
  function loadMore(): Promise<void> {
    return hasMore.value && !loading.value
      ? fetchPage(true)
      : Promise.resolve()
  }

  return {
    users,
    hasMore,
    loading,
    search,
    loadMore,
  }
}
