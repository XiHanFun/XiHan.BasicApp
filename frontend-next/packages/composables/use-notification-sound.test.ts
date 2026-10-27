/**
 * useNotificationSound 提示音单元测试。
 * 职责：声音交给组件库的共享播放器（@xihan-ui/vue/sound），这里锁定应用自己那一层：
 * 「按语义名交给共享播放器」「偏好关闭时不响，排队期间关掉的那一声也不响」
 * 「密集到达按 200ms 依次排开、排到 1.4s 之后的直接丢」「首次手势解锁只挂一次，偏好关着时不解锁」。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const player = vi.hoisted(() => ({ played: [] as Array<{ kind: string, at: number }>, unlocks: 0 }))
const gesture = vi.hoisted(() => ({ attached: 0, unlock: null as null | (() => void) }))

vi.mock('@xihan-ui/vue/sound', () => ({
  getSoundPlayer: () => ({
    play: (kind: string) => {
      player.played.push({ kind, at: performance.now() })
    },
    unlock: () => {
      player.unlocks += 1
    },
  }),
}))

vi.mock('@xihan-ui/sound', () => ({
  attachDocumentSoundUnlock: (unlock: () => void) => {
    gesture.attached += 1
    gesture.unlock = unlock
    return () => {}
  },
}))

/** 每个用例一份全新模块状态：排队时钟、偏好判定与解锁挂接都是模块级的 */
async function loadModule() {
  vi.resetModules()
  return import('./useNotificationSound')
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'performance'] })
  player.played = []
  player.unlocks = 0
  gesture.attached = 0
  gesture.unlock = null
})

afterEach(() => {
  vi.useRealTimers()
})

describe('交给组件库的共享播放器', () => {
  it('按语义名播放：站内通知、收到消息、发送成功各是库里的一个声音', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('notification')
    vi.advanceTimersByTime(1000)
    playNotificationSound('receive')
    vi.advanceTimersByTime(1000)
    playNotificationSound('send')

    expect(player.played.map(item => item.kind)).toStrictEqual(['notification', 'receive', 'send'])
  })

  it('没接偏好时照常发声', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('send')

    expect(player.played).toHaveLength(1)
  })
})

describe('偏好开关', () => {
  it('偏好关闭时不响', async () => {
    const { configureNotificationSound, playNotificationSound } = await loadModule()
    configureNotificationSound(() => false)

    playNotificationSound('notification')
    vi.advanceTimersByTime(2000)

    expect(player.played).toHaveLength(0)
  })

  it('排队期间偏好关掉，排着的那一声不再响', async () => {
    const { configureNotificationSound, playNotificationSound } = await loadModule()
    let enabled = true
    configureNotificationSound(() => enabled)

    playNotificationSound('receive')
    playNotificationSound('receive')
    enabled = false
    vi.advanceTimersByTime(1000)

    expect(player.played).toHaveLength(1)
  })
})

describe('密集到达', () => {
  it('每条都响，按 200ms 依次排开', async () => {
    const { playNotificationSound } = await loadModule()
    const start = performance.now()

    playNotificationSound('receive')
    playNotificationSound('receive')
    playNotificationSound('receive')
    expect(player.played).toHaveLength(1)

    vi.advanceTimersByTime(199)
    expect(player.played).toHaveLength(1)
    vi.advanceTimersByTime(1)
    expect(player.played).toHaveLength(2)
    vi.advanceTimersByTime(200)

    expect(player.played.map(item => item.at - start)).toStrictEqual([0, 200, 400])
  })

  it('排到 1.4s 之后的直接丢，刷屏时不响个没完', async () => {
    const { playNotificationSound } = await loadModule()

    for (let i = 0; i < 12; i++) {
      playNotificationSound('receive')
    }
    vi.advanceTimersByTime(3000)

    // 0、200 … 1400 共 8 声
    expect(player.played).toHaveLength(8)
  })

  it('排开之后隔了一阵再来的消息立刻响，不沿着旧队列往后排', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('receive')
    playNotificationSound('receive')
    vi.advanceTimersByTime(5000)
    const before = performance.now()
    playNotificationSound('send')

    expect(player.played.at(-1)).toStrictEqual({ kind: 'send', at: before })
  })
})

describe('首次手势解锁', () => {
  it('只挂一次：偏好再接一遍不重复挂', async () => {
    const { configureNotificationSound } = await loadModule()

    configureNotificationSound(() => true)
    configureNotificationSound(() => true)

    expect(gesture.attached).toBe(1)
  })

  it('用户第一下操作时解锁播放器；偏好关着就不解锁', async () => {
    const { configureNotificationSound } = await loadModule()
    let enabled = false
    configureNotificationSound(() => enabled)

    gesture.unlock?.()
    expect(player.unlocks).toBe(0)

    enabled = true
    gesture.unlock?.()
    expect(player.unlocks).toBe(1)
  })
})
