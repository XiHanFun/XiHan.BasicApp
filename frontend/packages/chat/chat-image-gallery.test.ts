/**
 * 会话图片组：同一会话里的图片按消息先后、附件先后串成一组，看片浮层里左右翻得到。
 * 锁定：取法（跳过撤回与非图片消息、同一张图发两次算两张）；从某张打开时下标指到它在整个会话里的位置；
 * 往上翻出更早的历史后下标对回正在看的那张；图片消息不在消息流里时报错。
 */
import type { ChatMessageItem } from './types'
import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { computed, defineComponent, h, nextTick, ref, toValue } from 'vue'
import { chatGalleryEntries, provideChatImageGallery, useChatImageGallery } from './components/chat-image-gallery'
import { ChatMessageType } from './enums'

// 地址换取替身：文件 id 直接映成地址，不走预签名请求
vi.mock('~/composables', async importOriginal => ({
  ...await importOriginal<typeof import('~/composables')>(),
  useAvatarUrls: (source: () => readonly string[]) => computed(() => toValue(source).map(id => `blob:${id}`)),
}))

function message(messageId: string, patch: Partial<ChatMessageItem> = {}): ChatMessageItem {
  return {
    messageId,
    conversationId: 'c',
    senderUserId: 'u',
    messageType: ChatMessageType.Image,
    content: '',
    isRecalled: false,
    createdTime: '2026-10-13T00:00:00Z',
    mentionedUserIds: [],
    isPinned: false,
    reactions: [],
    attachments: [{ fileId: `f-${messageId}`, fileName: `${messageId}.png` }] as ChatMessageItem['attachments'],
    ...patch,
  }
}

describe('取法', () => {
  it('按消息先后、附件先后串起来；撤回的与非图片消息不算；同一张图发两次算两张', () => {
    const entries = chatGalleryEntries([
      message('m1', { attachments: [{ fileId: 'a' }, { fileId: 'b' }] as ChatMessageItem['attachments'] }),
      message('m2', { messageType: ChatMessageType.Text }),
      message('m3', { isRecalled: true }),
      message('m4', { attachments: [{ fileId: 'a' }] as ChatMessageItem['attachments'] }),
    ])

    expect(entries.map(entry => entry.key)).toStrictEqual(['m1:a', 'm1:b', 'm4:a'])
  })
})

function setup(initial: ChatMessageItem[]) {
  const messages = ref(initial)
  let gallery: ReturnType<typeof provideChatImageGallery> | null = null
  let open: ((messageId: string, fileId: string) => void) | null = null
  const Child = defineComponent({
    setup() {
      open = useChatImageGallery().open
      return () => null
    },
  })
  const Host = defineComponent({
    setup() {
      gallery = provideChatImageGallery(messages)
      return () => h(Child)
    },
  })
  const wrapper = mount(Host)
  return { wrapper, messages, gallery: gallery!, open: open! }
}

describe('从某张打开', () => {
  it('浮层列表是整个会话的图片；下标指到点的那张在会话里的位置', () => {
    const { gallery, open, wrapper } = setup([message('m1'), message('m2'), message('m3')])

    expect(gallery.items.value.map(item => item.key)).toStrictEqual(['m1:f-m1', 'm2:f-m2', 'm3:f-m3'])
    open('m2', 'f-m2')

    expect(gallery.open.value).toBe(true)
    expect(gallery.index.value).toBe(1)
    wrapper.unmount()
  })

  it('点的那张不在组里（地址还没换到等）：什么都不做', () => {
    const { gallery, open, wrapper } = setup([message('m1')])

    open('nope', 'nope')

    expect(gallery.open.value).toBe(false)
    wrapper.unmount()
  })

  it('往上翻出更早的历史：列表在前面多出几张，下标对回正在看的那张', async () => {
    const { gallery, open, messages, wrapper } = setup([message('m3'), message('m4')])
    open('m4', 'f-m4')
    expect(gallery.index.value).toBe(1)

    messages.value = [message('m1'), message('m2'), ...messages.value]
    await nextTick()

    expect(gallery.index.value).toBe(3)
    expect(gallery.items.value[gallery.index.value]?.key).toBe('m4:f-m4')
    wrapper.unmount()
  })
})

describe('在浮层里翻页', () => {
  it('翻到下一张后往上翻出更早的历史：下标仍对着翻到的那张', async () => {
    const { gallery, open, messages, wrapper } = setup([message('m3'), message('m4')])
    open('m3', 'f-m3')
    // 浮层翻页经 v-model:index 写回
    gallery.index.value = 1

    messages.value = [message('m1'), ...messages.value]
    await nextTick()

    expect(gallery.items.value[gallery.index.value]?.key).toBe('m4:f-m4')
    wrapper.unmount()
  })
})

describe('使用位置', () => {
  it('图片消息不在消息流里：报错指明', () => {
    const Orphan = defineComponent({
      setup() {
        useChatImageGallery()
        return () => null
      },
    })

    expect(() => mount(Orphan)).toThrow('ChatMessageThread')
  })
})
