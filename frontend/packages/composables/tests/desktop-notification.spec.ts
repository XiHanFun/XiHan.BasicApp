/**
 * composables/desktop-notification.ts：桌面通知
 * 锁定：环境没有通知能力即 unsupported、不发也不问；已有结论时不再询问；
 * 没授权不发、页面看得到不发（窗口没有焦点也不发）；发出去带标题与正文、系统不另响，带标签的顶替时照样弹出，来电挂着不消失；
 * 点了先把窗口拉回前台、关掉通知再交给回调，发出去的能收起；构造抛出当作发不了；
 * 还没问过授权时借第一下点击或按键问一次，偏好关着的那几下不问；授权状态变了告诉订阅的人
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  attachDesktopNotificationPrompt,
  desktopNotificationPermission,
  onDesktopNotificationPermissionChange,
  requestDesktopNotificationPermission,
  showDesktopNotification,
} from '~/composables/desktop-notification'

interface FakeNotification {
  title: string
  options: NotificationOptions & { renotify?: boolean }
  onclick: (() => void) | null
  close: ReturnType<typeof vi.fn>
}

let created: FakeNotification[] = []
let permission: NotificationPermission = 'default'
let answer: NotificationPermission = 'granted'
let asked = 0
let throwOnCreate = false

function installNotification() {
  class Fake {
    static get permission() {
      return permission
    }

    static requestPermission() {
      asked += 1
      permission = answer
      return Promise.resolve(answer)
    }

    onclick: (() => void) | null = null
    close = vi.fn()

    constructor(public title: string, public options: NotificationOptions) {
      if (throwOnCreate)
        throw new TypeError('Illegal constructor')
      created.push(this)
    }
  }
  vi.stubGlobal('Notification', Fake)
}

/** 页面此刻在不在前台 */
function foreground(visible: boolean, focused: boolean) {
  vi.spyOn(document, 'visibilityState', 'get').mockReturnValue(visible
    ? 'visible'
    : 'hidden')
  vi.spyOn(document, 'hasFocus').mockReturnValue(focused)
}

beforeEach(() => {
  created = []
  permission = 'default'
  answer = 'granted'
  asked = 0
  throwOnCreate = false
  installNotification()
  foreground(false, false)
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('授权', () => {
  it('环境没有通知能力：unsupported，不问也不发', async () => {
    vi.stubGlobal('Notification', undefined)

    expect(desktopNotificationPermission()).toBe('unsupported')
    await expect(requestDesktopNotificationPermission()).resolves.toBe('unsupported')
    expect(showDesktopNotification({ title: 'hi' })).toBeNull()
  })

  it('还没有结论时才询问，问到什么是什么', async () => {
    answer = 'denied'
    await expect(requestDesktopNotificationPermission()).resolves.toBe('denied')
    expect(asked).toBe(1)

    // 已经有结论：不再询问
    await expect(requestDesktopNotificationPermission()).resolves.toBe('denied')
    permission = 'granted'
    await expect(requestDesktopNotificationPermission()).resolves.toBe('granted')
    expect(asked).toBe(1)
  })
})

describe('发通知', () => {
  it('没授权不发', () => {
    expect(showDesktopNotification({ title: 'hi' })).toBeNull()
    permission = 'denied'
    expect(showDesktopNotification({ title: 'hi' })).toBeNull()
    expect(created).toHaveLength(0)
  })

  it('页面看得到就不发，窗口没有焦点（在别的程序里操作）也不发；最小化、切到别的标签页才发', () => {
    permission = 'granted'
    foreground(true, true)
    expect(showDesktopNotification({ title: 'focused' })).toBeNull()
    foreground(true, false)
    expect(showDesktopNotification({ title: 'unfocused' })).toBeNull()

    foreground(false, false)
    expect(showDesktopNotification({ title: 'hidden' })).not.toBeNull()
    expect(created.map(item => item.title)).toEqual(['hidden'])
  })

  it('带标题与正文，系统不另响一声：提示音由页面自己放', () => {
    permission = 'granted'

    showDesktopNotification({ title: 'notice', body: 'hello' })

    expect(created[0]?.options).toEqual({ body: 'hello', silent: true })
  })

  it('带标签的同标签顶替时照样弹出；要求挂着的不自己消失', () => {
    permission = 'granted'

    showDesktopNotification({ title: 'alice', body: 'hello', tag: 'chat:c-1' })
    showDesktopNotification({
      title: 'bob',
      body: 'calling',
      tag: 'chat-call:call-1',
      requireInteraction: true,
    })

    expect(created.map(item => item.options)).toEqual([
      {
        body: 'hello',
        silent: true,
        tag: 'chat:c-1',
        renotify: true,
      },
      {
        body: 'calling',
        silent: true,
        tag: 'chat-call:call-1',
        renotify: true,
        requireInteraction: true,
      },
    ])
  })

  it('点了先把窗口拉回前台、关掉通知，再交给回调', () => {
    permission = 'granted'
    const focus = vi.spyOn(window, 'focus').mockImplementation(() => {})
    const onClick = vi.fn(() => {
      expect(focus).toHaveBeenCalledTimes(1)
    })

    showDesktopNotification({ title: 'alice', onClick })
    const [shown] = created
    shown?.onclick?.()

    expect(shown?.close).toHaveBeenCalledTimes(1)
    expect(onClick).toHaveBeenCalledTimes(1)
  })

  it('发出去的能收起：来电有了结果就关掉那一条', () => {
    permission = 'granted'

    const handle = showDesktopNotification({ title: 'bob' })
    handle?.close()

    expect(created[0]?.close).toHaveBeenCalledTimes(1)
  })

  it('构造抛出（只许 Service Worker 发通知的环境）：当作发不了', () => {
    permission = 'granted'
    throwOnCreate = true

    expect(showDesktopNotification({ title: 'hi' })).toBeNull()
  })
})

describe('第一下操作时申请授权', () => {
  function press(key: string) {
    document.dispatchEvent(new KeyboardEvent('keydown', { key }))
  }

  it('还没问过：第一下点击时问一次，之后不再问', () => {
    const detach = attachDesktopNotificationPrompt(() => true)

    document.dispatchEvent(new MouseEvent('click'))
    permission = 'default'
    document.dispatchEvent(new MouseEvent('click'))

    expect(asked).toBe(1)
    detach()
  })

  it('打字也算一下操作；Esc、Tab 这类不算', () => {
    const detach = attachDesktopNotificationPrompt(() => true)

    press('Escape')
    press('Tab')
    expect(asked).toBe(0)
    press('a')
    expect(asked).toBe(1)
    detach()
  })

  it('偏好关着的那几下不问，等它开着了再问', () => {
    let wanted = false
    const detach = attachDesktopNotificationPrompt(() => wanted)

    document.dispatchEvent(new MouseEvent('click'))
    expect(asked).toBe(0)
    wanted = true
    document.dispatchEvent(new MouseEvent('click'))
    expect(asked).toBe(1)
    detach()
  })

  it('已经有结论或撤掉了监听：不问', () => {
    permission = 'denied'
    attachDesktopNotificationPrompt(() => true)
    document.dispatchEvent(new MouseEvent('click'))

    permission = 'default'
    attachDesktopNotificationPrompt(() => true)()
    document.dispatchEvent(new MouseEvent('click'))

    expect(asked).toBe(0)
  })
})

describe('授权状态变化', () => {
  it('浏览器报授权变了就告诉订阅的人；取消订阅后不再告诉', async () => {
    const status = new EventTarget()
    vi.stubGlobal('navigator', {
      permissions: {
        query: vi.fn(async () => status),
      },
    })
    const listener = vi.fn()

    const dispose = onDesktopNotificationPermissionChange(listener)
    await Promise.resolve()
    status.dispatchEvent(new Event('change'))
    dispose()
    status.dispatchEvent(new Event('change'))

    expect(listener).toHaveBeenCalledTimes(1)
  })

  it('浏览器没有权限查询接口：什么都不做', () => {
    vi.stubGlobal('navigator', {})

    expect(() => onDesktopNotificationPermissionChange(() => {})()).not.toThrow()
  })
})
