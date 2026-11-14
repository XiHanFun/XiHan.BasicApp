/**
 * 聊天模块的登记（setup）单元测试
 * 职责边界：新版欢迎页里介绍聊天的那一屏——挂在哪个页面上、「去看看」去的页面在本模块里有视图、
 * 文案键在十种区域里都有译文
 */
import type { WelcomeHighlight } from '~/stores'
import { describe, expect, it } from 'vitest'
import setup from '@/modules/chat/setup'
import { i18n, LOCALES } from '~/locales'
import { useWelcomeHighlights } from '~/stores'

const views = import.meta.glob('../views/**/index.vue')

/** 登记表按版本线与标识去重，各用例重复登记得到的是同一屏 */
function registered(): WelcomeHighlight {
  setup()
  const found = useWelcomeHighlights().find(highlight => highlight.key === 'chat')
  if (!found)
    throw new Error('没有登记 chat')
  return found
}

describe('聊天模块在欢迎页里的一屏', () => {
  it('挂在本模块的页面上：后端不下发这个页面，这一屏就不出现', () => {
    const highlight = registered()

    expect(highlight.page).toBe('chat.conversations')
    expect(highlight.action?.to).toBe('/chat/conversations')
    expect(Object.keys(views)).toContain('../views/conversations/index.vue')
  })

  it('用到的文案键在十种区域里都有译文', () => {
    const highlight = registered()
    const keys = [
      highlight.categoryKey,
      highlight.titleKey,
      highlight.descriptionKey,
      ...highlight.pointKeys,
      ...(highlight.action
        ? [highlight.action.labelKey]
        : []),
    ]

    for (const locale of LOCALES) {
      const missing = keys.filter(key => !i18n.global.te(key, locale))
      expect(missing, `${locale} 缺译文`).toEqual([])
    }
  })
})
