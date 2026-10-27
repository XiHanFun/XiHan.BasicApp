/**
 * 测试环境统一初始化。
 *
 * jsdom 只实现了 DOM 规范，浏览器里存在的这几个观察者 / 媒体查询 API 它一概没有，
 * 而布局、主题、响应式断点这些代码在导入期就会用到它们。
 * 这里补的是**最小可用替身**：只保证被测代码不因缺 API 崩溃，
 * 不模拟任何行为——需要断言行为的用例请在用例内自行 mock，避免全局替身悄悄决定断言结果。
 *
 * 文案按区域懒加载在测试里装不上：把壳层与各能力包的十种区域一次并进 i18n 实例，
 * 断言文案的用例（壳层组件、灵动岛、锁屏）才拿得到译文而不是裸 key。
 */
import { afterEach, beforeEach, vi } from 'vitest'
import { i18n } from '../packages/locales'

/** matchMedia：jsdom 未实现。默认一律不匹配，用例要测断点请自行覆写。 */
if (!window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: (query: string): MediaQueryList => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }),
  })
}

/** ResizeObserver / IntersectionObserver：jsdom 未实现，布局与懒加载代码在导入期即引用。 */
class NoopObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
  takeRecords(): [] {
    return []
  }
}
for (const name of ['ResizeObserver', 'IntersectionObserver', 'MutationObserver'] as const) {
  if (!(name in globalThis)) {
    Object.defineProperty(globalThis, name, {
      writable: true,
      configurable: true,
      value: NoopObserver,
    })
  }
}

/** scrollTo：jsdom 只抛 "not implemented"，噪声大且无信息量。 */
if (typeof window.scrollTo !== 'function' || window.scrollTo.toString().includes('not implemented')) {
  Object.defineProperty(window, 'scrollTo', { writable: true, configurable: true, value: () => {} })
}
Object.defineProperty(Element.prototype, 'scrollIntoView', {
  writable: true,
  configurable: true,
  value: () => {},
})

/**
 * navigator.languages：jsdom 默认 ['en-US', 'en']，初始语言会跟随浏览器，
 * 若不固定，整个测试套件的默认界面语言会悄悄变成英文。这里固定为 zh-CN，
 * 要测浏览器语言侦测的用例请自行覆写（用 Object.defineProperty，并在 afterEach 还原）。
 */
Object.defineProperty(window.navigator, 'languages', {
  configurable: true,
  get: () => ['zh-CN'],
})

type Messages = Record<string, unknown>
const shell = import.meta.glob<{ default: Messages }>('../packages/locales/langs/*.ts', { eager: true })
const modules = import.meta.glob<{ default: Messages }>('../src/modules/*/locales/*.ts', { eager: true })
for (const [path, mod] of [...Object.entries(shell), ...Object.entries(modules)]) {
  const locale = path.match(/([a-z]{2}-[A-Z]{2})\.ts$/)?.[1]
  if (locale)
    i18n.global.mergeLocaleMessage(locale, mod.default)
}

/**
 * 每个用例之间清干净跨用例可见的状态，保证测试可任意顺序、可并行执行。
 * localStorage / sessionStorage 由 jsdom 实现，但在同一文件的用例之间是共享的。
 */
beforeEach(() => {
  localStorage.clear()
  sessionStorage.clear()
  document.documentElement.className = ''
  document.documentElement.removeAttribute('style')
  document.documentElement.removeAttribute('data-theme')
  document.body.innerHTML = ''
})

afterEach(() => {
  vi.useRealTimers()
})
