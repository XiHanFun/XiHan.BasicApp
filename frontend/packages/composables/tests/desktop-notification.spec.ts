/**
 * composables/desktop-notification.ts：桌面通知
 * 锁定：环境没有通知能力即 unsupported、不发也不问；已有结论时不再询问；
 * 没授权不发、页面在前台不发；发出去带标题、正文与标签，点了先把窗口拉回前台、关掉通知再交给回调；构造抛出当作发不了
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { desktopNotificationPermission, requestDesktopNotificationPermission, showDesktopNotification } from '~/composables/desktop-notification'

interface FakeNotification {
  title: string
  options: NotificationOptions
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
    expect(showDesktopNotification({ title: 'hi' })).toBe(false)
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
    expect(showDesktopNotification({ title: 'hi' })).toBe(false)
    permission = 'denied'
    expect(showDesktopNotification({ title: 'hi' })).toBe(false)
    expect(created).toHaveLength(0)
  })

  it('页面在前台不发；标签页被盖住或窗口没有焦点才发', () => {
    permission = 'granted'
    foreground(true, true)
    expect(showDesktopNotification({ title: 'hi' })).toBe(false)

    foreground(true, false)
    expect(showDesktopNotification({ title: 'unfocused' })).toBe(true)
    foreground(false, true)
    expect(showDesktopNotification({ title: 'hidden' })).toBe(true)
    expect(created.map(item => item.title)).toEqual(['unfocused', 'hidden'])
  })

  it('带标题、正文与标签；点了先把窗口拉回前台、关掉通知，再交给回调', () => {
    permission = 'granted'
    const focus = vi.spyOn(window, 'focus').mockImplementation(() => {})
    const onClick = vi.fn(() => {
      expect(focus).toHaveBeenCalledTimes(1)
    })

    expect(showDesktopNotification({
      title: 'alice',
      body: 'hello',
      tag: 'chat:c-1',
      onClick,
    })).toBe(true)
    const [shown] = created
    expect([shown?.title, shown?.options]).toEqual([
      'alice',
      { body: 'hello', tag: 'chat:c-1' },
    ])

    shown?.onclick?.()
    expect(shown?.close).toHaveBeenCalledTimes(1)
    expect(onClick).toHaveBeenCalledTimes(1)
  })

  it('构造抛出（只许 Service Worker 发通知的环境）：当作发不了', () => {
    permission = 'granted'
    throwOnCreate = true

    expect(showDesktopNotification({ title: 'hi' })).toBe(false)
  })
})
