/**
 * 组件库文案随应用语言取语言包的单元测试。
 * 职责边界：组件库有语言包的应用语言取自己的那一份；没有的（hi-IN）取英文、不冒出中文；
 * 应用新上架一种语言时，这里逼着先决定它对应哪一份语言包。
 */
import { deDE, enUS, jaJP, zhTW } from '@xihan-ui/vue/locale'
import { afterEach, describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import { xhLocales } from '~/locales/xihan-ui'
import { xhConfigValue, xhTranslationsOfCurrentLocale } from './xh-config'

const originalLocale = i18n.global.locale.value

afterEach(() => {
  i18n.global.locale.value = originalLocale
})

describe('xhTranslationsOfCurrentLocale', () => {
  it.each([
    ['zh-TW', zhTW],
    ['ja-JP', jaJP],
    ['de-DE', deDE],
  ] as const)('%s 取组件库同语言的语言包', (locale, pack) => {
    i18n.global.locale.value = locale

    expect(xhTranslationsOfCurrentLocale()).toBe(pack.translations)
  })

  it('组件库没有语言包的 hi-IN 取英文，不冒出中文', () => {
    i18n.global.locale.value = 'hi-IN'

    expect(xhTranslationsOfCurrentLocale()).toBe(enUS.translations)
  })

  it('应用上架的语言里只有 hi-IN 没有组件库语言包', () => {
    expect(i18n.global.availableLocales.filter(locale => !xhLocales[locale])).toEqual(['hi-IN'])
  })
})

describe('xhConfigValue', () => {
  it('语言标记跟应用语言走，不取回退语言包的 en-US', () => {
    i18n.global.locale.value = 'hi-IN'

    expect(xhConfigValue().locale).toBe('hi-IN')
  })
})
