/**
 * 测试环境统一初始化。
 *
 * jsdom 只实现了 DOM 规范，浏览器里存在的这几个观察者 / 媒体查询 API 它一概没有，
 * 而布局、主题、响应式断点、虚拟滚动这些代码在导入期就会用到它们。
 * 这里补的是**最小可用替身**：只保证被测代码不因缺 API 崩溃，
 * 不模拟任何行为——需要断言行为的用例请在用例内自行 mock，避免全局替身悄悄决定断言结果。
 */
import { afterEach, beforeEach, vi } from 'vitest'

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
