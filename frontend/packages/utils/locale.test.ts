/**
 * 浏览器语言偵测单元测试。
 *
 * 职责边界：resolveBrowserLocale 的比对规则（完全相符 / 中文繁简 / 主语言 / 退回），
 * 以及 resolveInitialLocale 的优先序（本地存储 → 浏览器 → 默认语言）。
 */
import { afterEach, describe, expect, it } from 'vitest'
import { DEFAULT_LOCALE, FALLBACK_LOCALE, LOCALE_KEY } from '~/constants'
import { resolveBrowserLocale, resolveInitialLocale } from './locale'

function stubBrowserLanguages(languages: string[] | undefined): void {
  Object.defineProperty(window.navigator, 'languages', { configurable: true, get: () => languages })
}

afterEach(() => {
  stubBrowserLanguages(['zh-CN'])
})

describe('resolveBrowserLocale', () => {
  it('完全相符的语言原样采用', () => {
    expect(resolveBrowserLocale(['de-DE'])).toBe('de-DE')
    expect(resolveBrowserLocale(['ko-KR'])).toBe('ko-KR')
  })

  it('比对不分大小写，并回传标准写法', () => {
    expect(resolveBrowserLocale(['de-de'])).toBe('de-DE')
    expect(resolveBrowserLocale(['ZH-tw'])).toBe('zh-TW')
  })

  it('主语言相符时对到同主语言的已上架语言', () => {
    expect(resolveBrowserLocale(['de-AT'])).toBe('de-DE')
    expect(resolveBrowserLocale(['de'])).toBe('de-DE')
    expect(resolveBrowserLocale(['en-GB'])).toBe('en-US')
    expect(resolveBrowserLocale(['ja'])).toBe('ja-JP')
    expect(resolveBrowserLocale(['ko'])).toBe('ko-KR')
    expect(resolveBrowserLocale(['hi'])).toBe('hi-IN')
  })

  it('繁体中文地区与 zh-Hant 对到 zh-TW', () => {
    expect(resolveBrowserLocale(['zh-HK'])).toBe('zh-TW')
    expect(resolveBrowserLocale(['zh-MO'])).toBe('zh-TW')
    expect(resolveBrowserLocale(['zh-Hant'])).toBe('zh-TW')
    expect(resolveBrowserLocale(['zh-Hant-HK'])).toBe('zh-TW')
  })

  it('其余中文对到 zh-CN', () => {
    expect(resolveBrowserLocale(['zh'])).toBe('zh-CN')
    expect(resolveBrowserLocale(['zh-SG'])).toBe('zh-CN')
    expect(resolveBrowserLocale(['zh-Hans-CN'])).toBe('zh-CN')
  })

  it('按浏览器语言顺序取第一个能比对到的', () => {
    expect(resolveBrowserLocale(['fr-FR', 'ja', 'de-DE'])).toBe('ja-JP')
  })

  it('全部不受支持时退回 en-US', () => {
    expect(resolveBrowserLocale(['fr-FR', 'es'])).toBe(FALLBACK_LOCALE)
    expect(FALLBACK_LOCALE).toBe('en-US')
  })

  it('清单为空或忽略空白项后为空时回传 null', () => {
    expect(resolveBrowserLocale([])).toBeNull()
    expect(resolveBrowserLocale(['', '  '])).toBeNull()
  })

  it('不传参数时读取 navigator.languages', () => {
    stubBrowserLanguages(['de-CH', 'en'])
    expect(resolveBrowserLocale()).toBe('de-DE')
  })

  it('navigator.languages 不可用时回传 null', () => {
    stubBrowserLanguages(undefined)
    expect(resolveBrowserLocale()).toBeNull()
  })
})

describe('resolveInitialLocale', () => {
  it('本地存储有语言时优先采用，不看浏览器', () => {
    stubBrowserLanguages(['de-DE'])
    localStorage.setItem(LOCALE_KEY, JSON.stringify('ja-JP'))
    expect(resolveInitialLocale()).toBe('ja-JP')
  })

  it('本地存储没有语言时跟随浏览器', () => {
    stubBrowserLanguages(['de-DE'])
    expect(resolveInitialLocale()).toBe('de-DE')
  })

  it('浏览器语言不受支持时退回 en-US', () => {
    stubBrowserLanguages(['fr-FR'])
    expect(resolveInitialLocale()).toBe('en-US')
  })

  it('浏览器没有给语言时退回默认语言 zh-CN', () => {
    stubBrowserLanguages([])
    expect(resolveInitialLocale()).toBe(DEFAULT_LOCALE)
  })

  it('本地存储是坏 JSON 时视同未选择，改走浏览器语言', () => {
    stubBrowserLanguages(['ko-KR'])
    localStorage.setItem(LOCALE_KEY, '{ not json')
    expect(resolveInitialLocale()).toBe('ko-KR')
  })

  it('不把偵测结果写回本地存储', () => {
    stubBrowserLanguages(['de-DE'])
    resolveInitialLocale()
    expect(localStorage.getItem(LOCALE_KEY)).toBeNull()
  })
})
