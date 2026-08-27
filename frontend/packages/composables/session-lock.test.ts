/**
 * session-lock 会话锁定状态读写单元测试。
 * 职责：锁定「只接管已知锁定原因」「同标签页必须靠自定义事件广播」这两条注释里写明的坑，
 * 以及标记读写、清除的完整性。这一层与 Vue 无关，仅操作 localStorage 与 window 事件。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LOCK_REASON_KEY, LOCK_STATE_KEY } from '~/constants'
import {
  clearLockState,
  isLockedState,
  lockedReason,
  markLockedFromServer,
  SESSION_LOCK_CHANGED_EVENT,
  SESSION_LOCK_REASON_PASSWORD_CHANGE,
  SESSION_LOCK_REASON_SCREEN,
} from './session-lock'

let listener: (() => void) | null = null

function listenLockChanged(): () => number {
  let count = 0
  listener = () => {
    count += 1
  }
  window.addEventListener(SESSION_LOCK_CHANGED_EVENT, listener)
  return () => count
}

beforeEach(() => {
  listener = null
})

afterEach(() => {
  if (listener) {
    window.removeEventListener(SESSION_LOCK_CHANGED_EVENT, listener)
    listener = null
  }
  vi.restoreAllMocks()
})

describe('session-lock 常量与后端对齐', () => {
  it('两个锁定原因取值与后端 SessionLockReasons 一致且互不相同', () => {
    expect(SESSION_LOCK_REASON_SCREEN).toBe('ScreenLock')
    expect(SESSION_LOCK_REASON_PASSWORD_CHANGE).toBe('PasswordChangeRequired')
    expect(SESSION_LOCK_REASON_SCREEN).not.toBe(SESSION_LOCK_REASON_PASSWORD_CHANGE)
  })

  it('本标签页广播事件名带 xihan 前缀，避免与宿主页面事件撞名', () => {
    expect(SESSION_LOCK_CHANGED_EVENT).toBe('xihan:session-lock-changed')
  })
})

describe('isLockedState 读取标记', () => {
  it('未写入任何标记时判定为未锁定', () => {
    expect(isLockedState()).toBe(false)
  })

  it('只认字符串 "1"，其它真值一律视为未锁定', () => {
    localStorage.setItem(LOCK_STATE_KEY, 'true')
    expect(isLockedState()).toBe(false)

    localStorage.setItem(LOCK_STATE_KEY, '0')
    expect(isLockedState()).toBe(false)

    localStorage.setItem(LOCK_STATE_KEY, '1')
    expect(isLockedState()).toBe(true)
  })
})

describe('lockedReason 读取原因', () => {
  it('未写入原因时返回 null 而不是空串', () => {
    expect(lockedReason()).toBeNull()
  })

  it('返回原样写入的原因串，不做归一化', () => {
    localStorage.setItem(LOCK_REASON_KEY, SESSION_LOCK_REASON_PASSWORD_CHANGE)

    expect(lockedReason()).toBe('PasswordChangeRequired')
  })
})

describe('markLockedFromServer 只接管已知原因', () => {
  it('锁屏原因写入标记并广播本标签页事件', () => {
    const count = listenLockChanged()

    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)

    expect(isLockedState()).toBe(true)
    expect(lockedReason()).toBe(SESSION_LOCK_REASON_SCREEN)
    expect(count()).toBe(1)
  })

  it('强制改密原因同样接管', () => {
    markLockedFromServer(SESSION_LOCK_REASON_PASSWORD_CHANGE)

    expect(isLockedState()).toBe(true)
    expect(lockedReason()).toBe(SESSION_LOCK_REASON_PASSWORD_CHANGE)
  })

  it('未知原因不接管：既不写标记也不广播，避免弹出打不开的口令框', () => {
    const count = listenLockChanged()

    markLockedFromServer('RiskSuspended')

    expect(isLockedState()).toBe(false)
    expect(lockedReason()).toBeNull()
    expect(count()).toBe(0)
  })

  it('原因缺省 / 为 null / 为空串时一律不接管', () => {
    markLockedFromServer()
    expect(isLockedState()).toBe(false)

    markLockedFromServer(null)
    expect(isLockedState()).toBe(false)

    markLockedFromServer('')
    expect(isLockedState()).toBe(false)
  })

  it('原因大小写敏感，小写变体不被接管', () => {
    markLockedFromServer('screenlock')

    expect(isLockedState()).toBe(false)
  })

  it('重复接管同一原因不会累积状态，只是重复广播', () => {
    const count = listenLockChanged()

    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)
    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)

    expect(localStorage.getItem(LOCK_STATE_KEY)).toBe('1')
    expect(count()).toBe(2)
  })

  it('已处于锁屏态时收到改密原因会覆盖原因，引导切换到改密表单', () => {
    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)
    markLockedFromServer(SESSION_LOCK_REASON_PASSWORD_CHANGE)

    expect(lockedReason()).toBe(SESSION_LOCK_REASON_PASSWORD_CHANGE)
  })
})

describe('clearLockState 清除标记', () => {
  it('同时清掉锁定位与原因', () => {
    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)

    clearLockState()

    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(localStorage.getItem(LOCK_REASON_KEY)).toBeNull()
    expect(isLockedState()).toBe(false)
    expect(lockedReason()).toBeNull()
  })

  it('必须广播本标签页事件——否则强制登出后遮罩刷新也去不掉', () => {
    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)
    const count = listenLockChanged()

    clearLockState()

    expect(count()).toBe(1)
  })

  it('未锁定时清除也照常广播，保证 UI 能收敛到未锁定态', () => {
    const count = listenLockChanged()

    clearLockState()

    expect(count()).toBe(1)
    expect(isLockedState()).toBe(false)
  })

  it('只动锁定相关的两个键，不误清其它 localStorage 数据', () => {
    localStorage.setItem('xihan_other', 'keep-me')
    markLockedFromServer(SESSION_LOCK_REASON_SCREEN)

    clearLockState()

    expect(localStorage.getItem('xihan_other')).toBe('keep-me')
  })
})

describe('session-lock 广播载荷', () => {
  it('广播的是 CustomEvent，监听方可直接读事件类型', () => {
    const received: Event[] = []
    listener = null
    const handler = (event: Event) => received.push(event)
    window.addEventListener(SESSION_LOCK_CHANGED_EVENT, handler)

    try {
      markLockedFromServer(SESSION_LOCK_REASON_SCREEN)
    }
    finally {
      window.removeEventListener(SESSION_LOCK_CHANGED_EVENT, handler)
    }

    expect(received).toHaveLength(1)
    expect(received[0]).toBeInstanceOf(CustomEvent)
    expect(received[0]?.type).toBe(SESSION_LOCK_CHANGED_EVENT)
  })
})
