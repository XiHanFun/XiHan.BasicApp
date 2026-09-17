/**
 * xhTranslationsOfCurrentLocale 文案回退单元测试。
 * 职责边界：已登记语言直接取自己的一份；未登记语言按主语言回退——
 * zh 系（如 zh-TW）回退 zh-CN，其余（如 ja-JP）回退 en-US，不冒出中文。
 * 浏览器语言探测让 ja-JP / ko-KR / hi-IN 等未登记语言默认可达，这条回退规则是防线。
 */
import { afterEach, describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import { xhTranslations } from '~/locales/xihan-ui'
import { xhTranslationsOfCurrentLocale } from './xh-config'

const originalLocale = i18n.global.locale.value

afterEach(() => {
  i18n.global.locale.value = originalLocale
})

describe('xhTranslationsOfCurrentLocale', () => {
  it('未登记的 ja-JP 回退英文覆盖，不冒出中文', () => {
    i18n.global.locale.value = 'ja-JP'

    expect(xhTranslationsOfCurrentLocale()).toBe(xhTranslations['en-US'])
  })

  it('未登记的 zh-TW 回退简体中文覆盖', () => {
    i18n.global.locale.value = 'zh-TW'

    expect(xhTranslationsOfCurrentLocale()).toBe(xhTranslations['zh-CN'])
  })

  it('已登记的 de-DE 直接取自己的一份', () => {
    i18n.global.locale.value = 'de-DE'

    expect(xhTranslationsOfCurrentLocale()).toBe(xhTranslations['de-DE'])
  })
})
