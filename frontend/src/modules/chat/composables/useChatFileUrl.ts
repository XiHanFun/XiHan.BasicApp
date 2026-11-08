/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { MaybeRefOrGetter, Ref } from 'vue'
import { ref, toValue, watchEffect } from 'vue'
import { getChatApi } from '../api-contract'

/**
 * 聊天附件的可显示地址（图片内联、语音播放）
 *
 * 附件以会话为访问范围（方案 H2）：会话成员凭会话换签名地址（chat-messages.presign），
 * 不走通用文件的签名（那要通用文件权限）
 * 地址会过期：按「会话 + 文件」缓存到到期前一刻，同一附件并发只换一次；换取失败为空串（缩略图显示失败态）
 */

/** 一个附件引用：哪个会话里的哪个文件 */
export interface ChatFileRef {
  conversationId: string
  fileId: string
}

/** 到期前留出的余量：快到期的地址不再发给 <img> / <audio>，免得加载到一半失效 */
const EXPIRY_MARGIN_MS = 30_000

const cache = new Map<string, { url: string, expiresAt: number }>()
const inflight = new Map<string, Promise<string>>()

function keyOf(file: ChatFileRef): string {
  return `${file.conversationId}:${file.fileId}`
}

/** 能同步给出的地址：缓存里还没到期的；否则 null（要异步换取） */
function peekChatFileUrl(file: ChatFileRef): null | string {
  const hit = cache.get(keyOf(file))
  if (hit && hit.expiresAt - EXPIRY_MARGIN_MS > Date.now())
    return hit.url
  return null
}

/**
 * 换一个附件的签名地址（带缓存与并发去重）；失败抛出，由调用方决定怎么展示
 * @param file 附件引用
 * @returns 可直接用于 src 的地址
 */
export function resolveChatFileUrl(file: ChatFileRef): Promise<string> {
  const known = peekChatFileUrl(file)
  if (known)
    return Promise.resolve(known)
  const key = keyOf(file)
  let pending = inflight.get(key)
  if (!pending) {
    pending = getChatApi()
      .getFileUrl(file.conversationId, file.fileId)
      .then((signed) => {
        const expiresAt = Date.parse(signed.expiresAt)
        cache.set(key, {
          url: signed.url,
          expiresAt: Number.isNaN(expiresAt)
            ? 0
            : expiresAt,
        })
        return signed.url
      })
      .finally(() => {
        inflight.delete(key)
      })
    inflight.set(key, pending)
  }
  return pending
}

/** 清空地址缓存（换会话身份、测试之间） */
export function clearChatFileUrlCache(): void {
  cache.clear()
  inflight.clear()
}

/**
 * 一组附件的响应式可显示地址，与入参逐项对应；某项换取中或失败时为 ''
 * 入参变了（往上翻出历史、换会话）只换新出现的那几项，已缓存的同步给出
 */
export function useChatFileUrls(source: MaybeRefOrGetter<readonly ChatFileRef[]>): Ref<string[]> {
  const urls = ref<string[]>([])

  watchEffect(() => {
    const files = toValue(source)
    const key = files.map(keyOf).join('\n')
    const known = files.map(file => peekChatFileUrl(file))
    urls.value = known.map(url => url ?? '')
    files.forEach((file, index) => {
      // 这里只看本地的 known，不读 urls：读了会被收集成依赖，异步回填又把本段重跑一遍
      if (known[index] !== null)
        return
      resolveChatFileUrl(file).then((resolved) => {
        // 换取期间入参可能已变，仍是同一组时才回填，免得把旧图的地址填进新相册
        if (toValue(source).map(keyOf).join('\n') === key)
          urls.value[index] = resolved
      }, () => {
        // 换不到地址：这一项保持空串，缩略图与语音条显示失败 / 加载态
      })
    })
  })

  return urls
}

/** 单个附件的响应式可显示地址；没有附件或换取中、失败时为 '' */
export function useChatFileUrl(source: MaybeRefOrGetter<ChatFileRef | null>): Ref<string> {
  const url = ref('')

  watchEffect(() => {
    const file = toValue(source)
    if (!file) {
      url.value = ''
      return
    }
    const known = peekChatFileUrl(file)
    if (known !== null) {
      url.value = known
      return
    }
    url.value = ''
    const key = keyOf(file)
    resolveChatFileUrl(file).then((resolved) => {
      const current = toValue(source)
      if (current && keyOf(current) === key)
        url.value = resolved
    }, () => {
      // 换不到地址：保持空串
    })
  })

  return url
}
