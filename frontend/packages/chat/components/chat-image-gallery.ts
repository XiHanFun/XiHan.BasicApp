/**
 * 会话图片组：同一会话里的图片按消息先后、附件先后串成一组，看片浮层里左右翻得到。
 *
 * 浮层只放一个，由消息流（ChatMessageThread）提供；图片消息（ChatMessageItemView）点缩略图时经 open
 * 从这一张打开。此前每条消息各带一个浮层，只能看点开的那一条里的图。
 */
import type { ComputedRef, InjectionKey, MaybeRefOrGetter, Ref, WritableComputedRef } from 'vue'
import type { ChatMessageItem } from '../types'
import { computed, inject, provide, ref, toValue, watch } from 'vue'
import { useAvatarUrls } from '~/composables'
import { ChatMessageType } from '../enums'

/** 图片组里的一张：哪条消息的哪个附件。同一张图发了两次也是两张 */
export interface ChatGalleryEntry {
  key: string
  fileId: string
  alt?: string
}

/** 定位键：消息与附件拼起来 */
export function chatGalleryKey(messageId: string, fileId: string): string {
  return `${messageId}:${fileId}`
}

/** 会话里的图片按消息先后、附件先后串成一组；撤回的消息不算 */
export function chatGalleryEntries(messages: readonly ChatMessageItem[]): ChatGalleryEntry[] {
  return messages.flatMap(message => message.messageType === ChatMessageType.Image && !message.isRecalled
    ? (message.attachments ?? []).map(att => ({
        key: chatGalleryKey(message.messageId, att.fileId),
        fileId: att.fileId,
        alt: att.fileName || undefined,
      }))
    : [])
}

export interface ChatImageGallery {
  /** 从这一张打开看片浮层，左右翻得到同会话的其他图片 */
  open: (messageId: string, fileId: string) => void
}

export const CHAT_IMAGE_GALLERY_KEY: InjectionKey<ChatImageGallery> = Symbol('ChatImageGallery')

/** 取消息流提供的会话图片组；图片消息只在消息流里渲染 */
export function useChatImageGallery(): ChatImageGallery {
  const gallery = inject(CHAT_IMAGE_GALLERY_KEY, null)
  if (!gallery)
    throw new Error('[chat] 图片消息须放在 ChatMessageThread 里：会话图片组由消息流提供')
  return gallery
}

/** 看片浮层要的一张：地址已换到 */
export interface ChatGalleryItem {
  key: string
  src: string
  alt?: string
}

/**
 * 在消息流里提供会话图片组：整个会话一个看片浮层，按消息先后串起所有图片。
 * 返回浮层要绑的列表、开合与下标；往上翻出更早的历史、新图片进来时列表会变，按正在看的那张把下标对回去。
 */
export function provideChatImageGallery(messages: MaybeRefOrGetter<readonly ChatMessageItem[]>): {
  items: ComputedRef<ChatGalleryItem[]>
  open: Ref<boolean>
  index: WritableComputedRef<number>
} {
  const entries = computed(() => chatGalleryEntries(toValue(messages)))
  const urls = useAvatarUrls(() => entries.value.map(entry => entry.fileId))
  // 地址还没换到的那张先不进（它的缩略图也还没出来，点不到）
  const items = computed<ChatGalleryItem[]>(() => entries.value.flatMap((entry, index) => {
    const src = urls.value[index]
    return src ? [{ key: entry.key, src, alt: entry.alt }] : []
  }))
  const open = ref(false)
  const position = ref(0)
  /** 正在看的那张。翻页写下标时当场记下：事后再按下标去新列表里取，取到的已经是别的图 */
  let current: null | string = null
  const index = computed({
    get: () => position.value,
    set: (value: number) => {
      position.value = value
      current = items.value[value]?.key ?? current
    },
  })
  watch(items, (list) => {
    const found = current ? list.findIndex(item => item.key === current) : -1
    if (found >= 0)
      position.value = found
  })
  provide(CHAT_IMAGE_GALLERY_KEY, {
    open(messageId, fileId) {
      const key = chatGalleryKey(messageId, fileId)
      const found = items.value.findIndex(item => item.key === key)
      if (found < 0)
        return
      current = key
      position.value = found
      open.value = true
    },
  })
  return { items, open, index }
}
