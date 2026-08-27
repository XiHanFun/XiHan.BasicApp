/**
 * useNotificationSound 提示音合成单元测试。
 * 职责：用假 AudioContext 注入（jsdom 无 Web Audio），锁定
 * 「内置 notice 音色为下行小六度 E5→B4」「偏好关闭 / 音色未注册 / 浏览器不支持时静默跳过」
 * 「密集到达按 0.2s 依次排开、排到 1.4s 之后的直接丢」「suspended 时尝试 resume 且吞掉拒绝」。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'

interface ScheduledTone {
  frequency: number
  start: number
  stop: number
  ramps: Array<{ value: number, at: number }>
}

interface FakeAudio {
  currentTime: number
  state: string
  resumeCalls: number
  resumeRejects: boolean
  tones: ScheduledTone[]
}

const originalAudioContext = Object.getOwnPropertyDescriptor(window, 'AudioContext')

let audio: FakeAudio | null = null
let constructThrows = false
let constructCount = 0

function installAudioContext(available: boolean): void {
  constructCount = 0
  const Ctor = function FakeAudioContext(this: Record<string, unknown>) {
    constructCount += 1
    if (constructThrows) {
      throw new Error('无音频设备')
    }
    const state: FakeAudio = {
      currentTime: 0,
      state: 'running',
      resumeCalls: 0,
      resumeRejects: false,
      tones: [],
    }
    audio = state
    const destination = {}
    Object.defineProperty(this, 'currentTime', { get: () => state.currentTime })
    Object.defineProperty(this, 'state', { get: () => state.state })
    Object.assign(this, {
      destination,
      resume: () => {
        state.resumeCalls += 1
        return state.resumeRejects ? Promise.reject(new Error('拒绝自动播放')) : Promise.resolve()
      },
      createGain: () => {
        const tone: ScheduledTone = { frequency: 0, start: 0, stop: 0, ramps: [] }
        return {
          __tone: tone,
          gain: {
            setValueAtTime: () => {},
            exponentialRampToValueAtTime: (value: number, at: number) => {
              tone.ramps.push({ value, at })
            },
          },
          connect: () => destination,
        }
      },
      createOscillator: () => {
        const node = {
          type: 'sine',
          frequency: { value: 0 },
          gainRef: null as null | { __tone: ScheduledTone },
          connect(gain: { __tone: ScheduledTone }) {
            node.gainRef = gain
            return { connect: () => {} }
          },
          start(at: number) {
            if (node.gainRef) {
              node.gainRef.__tone.frequency = node.frequency.value
              node.gainRef.__tone.start = at
              state.tones.push(node.gainRef.__tone)
            }
          },
          stop(at: number) {
            if (node.gainRef) {
              node.gainRef.__tone.stop = at
            }
          },
        }
        return node
      },
    })
  } as unknown as typeof AudioContext

  Object.defineProperty(window, 'AudioContext', {
    writable: true,
    configurable: true,
    value: available ? Ctor : undefined,
  })
}

/** 每个用例一份全新模块状态：AudioContext 与排队时钟都是模块级的 */
async function loadModule(options: { available?: boolean, throws?: boolean } = {}) {
  audio = null
  constructThrows = options.throws ?? false
  installAudioContext(options.available ?? true)
  vi.resetModules()
  return import('./useNotificationSound')
}

// 覆盖了全局 AudioContext，必须还原
afterEach(() => {
  if (originalAudioContext) {
    Object.defineProperty(window, 'AudioContext', originalAudioContext)
  }
  else {
    Reflect.deleteProperty(window as unknown as Record<string, unknown>, 'AudioContext')
  }
  audio = null
  constructThrows = false
})

describe('内置 notice 音色', () => {
  it('两声下行小六度：E5(659.25Hz) → B4(493.88Hz)', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('notice')

    expect(audio?.tones.map(tone => tone.frequency)).toEqual([659.25, 493.88])
  })

  it('第二声相对起点延迟 0.13s，且时长更长（偏公告口吻）', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('notice')

    const [first, second] = audio?.tones ?? []
    expect(second!.start - first!.start).toBeCloseTo(0.13, 6)
    expect(second!.stop - second!.start).toBeCloseTo(0.34, 6)
    expect(first!.stop - first!.start).toBeCloseTo(0.2, 6)
  })

  it('主音量压在 0.09 以下，第二声再低一档', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('notice')

    const peaks = audio?.tones.map(tone => Math.max(...tone.ramps.map(ramp => ramp.value))) ?? []
    expect(peaks[0]).toBeCloseTo(0.09, 6)
    expect(peaks[1]).toBeCloseTo(0.09 * 0.85, 6)
  })
})

describe('静默跳过的分支', () => {
  it('未注册的音色名不发声，也不构造音频上下文', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('not-registered')

    expect(constructCount).toBe(0)
    expect(audio).toBeNull()
  })

  it('偏好关闭时不发声', async () => {
    const { configureNotificationSound, playNotificationSound } = await loadModule()
    configureNotificationSound(() => false)

    playNotificationSound('notice')

    expect(constructCount).toBe(0)
  })

  it('偏好重新打开后恢复发声', async () => {
    const { configureNotificationSound, playNotificationSound } = await loadModule()
    let enabled = false
    configureNotificationSound(() => enabled)

    playNotificationSound('notice')
    enabled = true
    playNotificationSound('notice')

    expect(audio?.tones).toHaveLength(2)
  })

  it('未注入偏好判定时默认发声', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('notice')

    expect(audio?.tones).toHaveLength(2)
  })

  it('浏览器不支持 Web Audio 时静默降级', async () => {
    const { playNotificationSound } = await loadModule({ available: false })

    expect(() => playNotificationSound('notice')).not.toThrow()
    expect(audio).toBeNull()
  })

  it('构造 AudioContext 即抛（无音频设备 / 隐私模式）时静默降级，且不反复重试构造', async () => {
    const { playNotificationSound } = await loadModule({ throws: true })

    playNotificationSound('notice')
    playNotificationSound('notice')

    expect(audio).toBeNull()
    expect(constructCount).toBe(2)
  })
})

describe('自动播放策略', () => {
  it('上下文处于 suspended 时尝试 resume', async () => {
    const { playNotificationSound } = await loadModule()
    playNotificationSound('notice')
    audio!.state = 'suspended'

    playNotificationSound('notice')

    expect(audio?.resumeCalls).toBe(1)
  })

  it('resume 被拒绝时不抛出未处理错误，仍照常排音', async () => {
    const { playNotificationSound } = await loadModule()
    playNotificationSound('notice')
    audio!.state = 'suspended'
    audio!.resumeRejects = true

    expect(() => playNotificationSound('notice')).not.toThrow()
    expect(audio!.tones.length).toBeGreaterThan(2)
  })
})

describe('密集到达的排队与丢弃', () => {
  it('相邻两次发声至少岔开 0.2s，不叠成一坨噪音', async () => {
    const { playNotificationSound } = await loadModule()

    playNotificationSound('notice')
    const firstStart = audio!.tones[0]!.start
    playNotificationSound('notice')
    const secondStart = audio!.tones[2]!.start

    expect(secondStart - firstStart).toBeCloseTo(0.2, 6)
  })

  it('排到 1.4s 之后的直接丢弃，避免响个没完', async () => {
    const { playNotificationSound } = await loadModule()

    // 起点依次为 0.01 + 0.2k，第 8 次已超过 currentTime + 1.4s，从此全部丢弃
    for (let i = 0; i < 20; i++) {
      playNotificationSound('notice')
    }

    expect(audio!.tones.length / 2).toBe(7)
  })

  it('时钟推进后队列腾空，可以继续发声', async () => {
    const { playNotificationSound } = await loadModule()
    for (let i = 0; i < 20; i++) {
      playNotificationSound('notice')
    }
    const before = audio!.tones.length

    audio!.currentTime = 10
    playNotificationSound('notice')

    expect(audio!.tones.length).toBe(before + 2)
  })

  it('起点不早于当前时刻加 0.01s，避免排在已过去的时间点', async () => {
    const { playNotificationSound } = await loadModule()
    audio = null
    playNotificationSound('notice')
    audio!.currentTime = 5

    playNotificationSound('notice')

    expect(audio!.tones[2]!.start).toBeCloseTo(5.01, 6)
  })
})

describe('registerNotificationSound 自定义音色', () => {
  it('注册后即可按名发声，参数逐条生效', async () => {
    const { registerNotificationSound, playNotificationSound } = await loadModule()

    registerNotificationSound('chat', [
      { frequency: 880, offset: 0, duration: 0.1, gain: 1 },
      { frequency: 440, offset: 0.05, duration: 0.2, gain: 0.5 },
    ])
    playNotificationSound('chat')

    expect(audio?.tones.map(tone => tone.frequency)).toEqual([880, 440])
  })

  it('同名注册覆盖旧音色', async () => {
    const { registerNotificationSound, playNotificationSound } = await loadModule()

    registerNotificationSound('chat', [{ frequency: 880, offset: 0, duration: 0.1, gain: 1 }])
    registerNotificationSound('chat', [{ frequency: 220, offset: 0, duration: 0.1, gain: 1 }])
    playNotificationSound('chat')

    expect(audio?.tones.map(tone => tone.frequency)).toEqual([220])
  })

  it('注册空音序列时被视为已注册：照常拿上下文但一个音都不发', async () => {
    const { registerNotificationSound, playNotificationSound } = await loadModule()

    registerNotificationSound('silent', [])
    playNotificationSound('silent')

    expect(constructCount).toBe(1)
    expect(audio?.tones).toEqual([])
  })

  it('自定义音色不影响内置 notice', async () => {
    const { registerNotificationSound, playNotificationSound } = await loadModule()

    registerNotificationSound('chat', [{ frequency: 880, offset: 0, duration: 0.1, gain: 1 }])
    playNotificationSound('notice')

    expect(audio?.tones.map(tone => tone.frequency)).toEqual([659.25, 493.88])
  })
})
