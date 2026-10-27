/**
 * usePlatform 平台检测与快捷键标签单元测试。
 * 职责：锁定 isMac 在导入期由 userAgent 一次性判定（会话内恒定）、
 * Mac 下修饰键换符号且去掉 + 分隔、非 Mac 平台原样返回这几条约定。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'

const MAC_UA = 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36'
const WIN_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'

const originalUserAgent = Object.getOwnPropertyDescriptor(
  window.navigator.constructor.prototype,
  'userAgent',
)

/** 每个用例一份全新模块状态：isMac 是导入期计算的模块常量 */
async function loadModule(userAgent: string) {
  Object.defineProperty(window.navigator, 'userAgent', {
    configurable: true,
    get: () => userAgent,
  })
  vi.resetModules()
  const mod = await import('./usePlatform')
  return mod.usePlatform()
}

// 覆盖了全局 navigator.userAgent，必须还原
afterEach(() => {
  Reflect.deleteProperty(window.navigator, 'userAgent')
  if (originalUserAgent) {
    Object.defineProperty(window.navigator.constructor.prototype, 'userAgent', originalUserAgent)
  }
})

describe('usePlatform.isMac', () => {
  it('带 Macintosh 的 UA 判定为 Mac', async () => {
    const { isMac } = await loadModule(MAC_UA)

    expect(isMac).toBe(true)
  })

  it('带 Windows 的 UA 判定为非 Mac', async () => {
    const { isMac } = await loadModule(WIN_UA)

    expect(isMac).toBe(false)
  })

  it('大小写混写的 mac 同样命中（正则忽略大小写）', async () => {
    const { isMac } = await loadModule('SomeBrowser/1.0 (MACINTOSH)')

    expect(isMac).toBe(true)
  })

  it('空 UA 判定为非 Mac', async () => {
    const { isMac } = await loadModule('')

    expect(isMac).toBe(false)
  })
})

describe('formatShortcut 非 Mac 平台', () => {
  it('组合键原样返回，保留 + 分隔与大小写', async () => {
    const { formatShortcut } = await loadModule(WIN_UA)

    expect(formatShortcut('Ctrl+K')).toBe('Ctrl+K')
    expect(formatShortcut('Alt+Shift+L')).toBe('Alt+Shift+L')
  })

  it('空串原样返回', async () => {
    const { formatShortcut } = await loadModule(WIN_UA)

    expect(formatShortcut('')).toBe('')
  })

  it('不做去空白处理，输入什么就是什么', async () => {
    const { formatShortcut } = await loadModule(WIN_UA)

    expect(formatShortcut(' Ctrl + K ')).toBe(' Ctrl + K ')
  })
})

describe('formatShortcut Mac 平台', () => {
  it('把 Ctrl 映射为 ⌘ 并去掉 + 分隔', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('Ctrl+K')).toBe('⌘K')
  })

  it('把 Control / Cmd / Command / Meta 都映射为 ⌘', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('Control+K')).toBe('⌘K')
    expect(formatShortcut('Cmd+K')).toBe('⌘K')
    expect(formatShortcut('Command+K')).toBe('⌘K')
    expect(formatShortcut('Meta+K')).toBe('⌘K')
  })

  it('把 Alt / Option 映射为 ⌥，Shift 映射为 ⇧', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('Alt+L')).toBe('⌥L')
    expect(formatShortcut('Option+L')).toBe('⌥L')
    expect(formatShortcut('Shift+L')).toBe('⇧L')
  })

  it('多修饰键按原顺序拼接', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('Ctrl+Shift+P')).toBe('⌘⇧P')
  })

  it('映射不区分大小写与前后空白', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut(' ctrl + SHIFT + p ')).toBe('⌘⇧p')
  })

  it('未登记的按键去空白后原样保留', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('Ctrl+ Enter ')).toBe('⌘Enter')
    expect(formatShortcut('F5')).toBe('F5')
  })

  it('空串返回空串，不产生多余符号', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('')).toBe('')
  })

  it('单个修饰键也被替换为符号', async () => {
    const { formatShortcut } = await loadModule(MAC_UA)

    expect(formatShortcut('Shift')).toBe('⇧')
  })
})
