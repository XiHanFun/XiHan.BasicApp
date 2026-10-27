/**
 * useIsMobile 全站小屏断点单元测试。
 * 职责：锁定唯一断点常量 768、实际下发给 matchMedia 的查询串为 (max-width: 767px)、
 * 模块级单例只建一次监听、以及 change 事件驱动的响应式更新与只读暴露。
 *
 * 断点判定必须用 matchMedia 替身验证：真实 jsdom 不实现媒体查询求值。
 */
import type { Ref } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'

type ChangeListener = (event: { matches: boolean }) => void

interface FakeMql {
  matches: boolean
  media: string
  listeners: Set<ChangeListener>
  addEventListenerCalls: number
  removeEventListenerCalls: number
  emit: (matches: boolean) => void
}

const originalMatchMedia = window.matchMedia
const created: FakeMql[] = []
let queries: string[] = []

function installMatchMedia(initialMatches: boolean): void {
  queries = []
  created.length = 0
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: (query: string) => {
      queries.push(query)
      const mql: FakeMql = {
        matches: initialMatches,
        media: query,
        listeners: new Set<ChangeListener>(),
        addEventListenerCalls: 0,
        removeEventListenerCalls: 0,
        emit(matches: boolean) {
          mql.matches = matches
          for (const listener of mql.listeners) {
            listener({ matches })
          }
        },
      }
      created.push(mql)
      return {
        get matches() {
          return mql.matches
        },
        media: query,
        onchange: null,
        addListener: () => {},
        removeListener: () => {},
        addEventListener: (_type: string, listener: ChangeListener) => {
          mql.addEventListenerCalls += 1
          mql.listeners.add(listener)
        },
        removeEventListener: (_type: string, listener: ChangeListener) => {
          mql.removeEventListenerCalls += 1
          mql.listeners.delete(listener)
        },
        dispatchEvent: () => false,
      } as unknown as MediaQueryList
    },
  })
}

/** 每个用例一份全新模块状态：单例监听在导入期就建好了 */
async function loadModule(initialMatches: boolean) {
  installMatchMedia(initialMatches)
  vi.resetModules()
  const mod = await import('./useIsMobile')
  return { ...mod, mql: created[0] }
}

// 覆盖了全局 matchMedia，必须还原，否则连锁影响同进程内的其它用例
afterEach(() => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: originalMatchMedia,
  })
  created.length = 0
  queries = []
})

describe('useIsMobile 断点常量', () => {
  it('全站唯一小屏断点为 768px', async () => {
    const { MOBILE_BREAKPOINT } = await loadModule(false)

    expect(MOBILE_BREAKPOINT).toBe(768)
  })

  it('查询串取断点减一，即 767px 及以下算小屏', async () => {
    await loadModule(false)

    expect(queries).toEqual(['(max-width: 767px)'])
  })

  it('宽度恰好 768px 不匹配查询，判定为非小屏', async () => {
    const { useIsMobile, mql } = await loadModule(false)

    // 768px 视口下媒体查询 (max-width: 767px) 不命中
    expect(mql?.matches).toBe(false)
    expect(useIsMobile().isMobile.value).toBe(false)
  })

  it('宽度 767px 命中查询，判定为小屏', async () => {
    const { useIsMobile } = await loadModule(true)

    expect(useIsMobile().isMobile.value).toBe(true)
  })
})

describe('useIsMobile 响应式更新', () => {
  it('媒体查询 change 事件把状态翻到小屏', async () => {
    const { useIsMobile, mql } = await loadModule(false)
    const { isMobile } = useIsMobile()

    expect(isMobile.value).toBe(false)

    mql?.emit(true)

    expect(isMobile.value).toBe(true)
  })

  it('从小屏回到大屏同样跟随，不是单向锁死', async () => {
    const { useIsMobile, mql } = await loadModule(true)
    const { isMobile } = useIsMobile()

    mql?.emit(false)

    expect(isMobile.value).toBe(false)
  })

  it('先取得的引用与后取得的引用是同一个状态源', async () => {
    const { useIsMobile, mql } = await loadModule(false)
    const first = useIsMobile()

    mql?.emit(true)
    const second = useIsMobile()

    expect(second.isMobile.value).toBe(true)
    expect(first.isMobile).toBe(second.isMobile)
  })
})

describe('useIsMobile 单例与只读', () => {
  it('导入期只建一个 MediaQueryList，多次调用不重复建监听', async () => {
    const { useIsMobile, mql } = await loadModule(false)

    useIsMobile()
    useIsMobile()
    useIsMobile()

    expect(created).toHaveLength(1)
    expect(mql?.addEventListenerCalls).toBe(1)
  })

  it('暴露的是只读引用，外部直接赋值不会改变状态', async () => {
    const { useIsMobile } = await loadModule(false)
    const { isMobile } = useIsMobile()

    // 只读 ref 写入被 Vue 拒绝（开发期告警），状态仍由 matchMedia 决定
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    ;(isMobile as unknown as Ref<boolean>).value = true
    warn.mockRestore()

    expect(isMobile.value).toBe(false)
  })

  it('不依赖组件生命周期：在无组件上下文中调用同样可用', async () => {
    const { useIsMobile, mql } = await loadModule(false)

    // 直接在模块作用域调用（如 Pinia store 内）
    const { isMobile } = useIsMobile()
    mql?.emit(true)

    expect(isMobile.value).toBe(true)
  })
})
