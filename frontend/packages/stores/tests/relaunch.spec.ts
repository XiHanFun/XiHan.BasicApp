import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { destroyAllSignalRConnections } from '~/composables/useSignalR'
import { LOCK_STATE_KEY, TABS_LIST_KEY } from '~/constants'
import { relaunchApp } from '~/stores/relaunch'

vi.mock('~/composables/useSignalR', () => ({
  destroyAllSignalRConnections: vi.fn(async () => {}),
}))

describe('relaunchApp', () => {
  let descriptor: PropertyDescriptor | undefined
  let assign: ReturnType<typeof vi.fn>

  beforeEach(() => {
    localStorage.clear()
    sessionStorage.clear()
    vi.mocked(destroyAllSignalRConnections).mockClear()
    descriptor = Object.getOwnPropertyDescriptor(window, 'location')
    assign = vi.fn()
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { assign },
    })
  })

  afterEach(() => {
    if (descriptor)
      Object.defineProperty(window, 'location', descriptor)
  })

  it('清掉上一上下文的标签、锁屏标记与实时连接，再整页回到入口', () => {
    sessionStorage.setItem(TABS_LIST_KEY, '[]')
    localStorage.setItem(TABS_LIST_KEY, '[]')
    localStorage.setItem(LOCK_STATE_KEY, '1')

    relaunchApp()

    expect(sessionStorage.getItem(TABS_LIST_KEY)).toBeNull()
    expect(localStorage.getItem(TABS_LIST_KEY)).toBeNull()
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(destroyAllSignalRConnections).toHaveBeenCalledOnce()
    expect(assign).toHaveBeenCalledOnce()
  })

  it('不碰身份切换标记：它等重建后的守卫取走', () => {
    sessionStorage.setItem('xihan.next.identity-switched', '1')

    relaunchApp()

    expect(sessionStorage.getItem('xihan.next.identity-switched')).toBe('1')
  })
})
