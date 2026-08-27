/**
 * packages/utils/device-fingerprint.ts 单元测试。
 *
 * 职责边界：只验证指纹生成的**契约**——格式、同环境稳定、不同环境可分辨、各特征采集器的
 * 降级不抛错。不验证熵值高低（那取决于真实浏览器）。
 * 其中一条是回归锚点：源码注释写明音频指纹必须走 OfflineAudioContext，
 * 用实时 AudioContext 会因自动播放策略在登录页拿到空串、退出后又拿到真实值，
 * 同一台设备算出两种指纹而被误判成新设备。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { generateDeviceFingerprint } from './device-fingerprint'

const FINGERPRINT_PATTERN = /^dfp_[0-9a-f]{1,8}_[0-9a-f]{1,8}$/

const restorers: Array<() => void> = []

function defineTemporary(target: object, key: string, value: unknown): void {
  const original = Object.getOwnPropertyDescriptor(target, key)
  Object.defineProperty(target, key, { value, configurable: true, writable: true })
  restorers.push(() => {
    if (original) {
      Object.defineProperty(target, key, original)
    }
    else {
      Reflect.deleteProperty(target, key)
    }
  })
}

/** 画布替身：2d 上下文按 font 串给出可控的文本宽度，webgl 一律不可用。 */
function stubCanvas(options: { dataUrl: string, wideFontMarker?: string }): void {
  const marker = options.wideFontMarker ?? 'Arial'
  const context = {
    font: '',
    textBaseline: '',
    fillStyle: '',
    fillRect: () => {},
    fillText: () => {},
    measureText(): { width: number } {
      return { width: context.font.includes(marker) ? 111 : 100 }
    },
  }

  defineTemporary(HTMLCanvasElement.prototype, 'getContext', (kind: string) =>
    (kind === '2d' ? context : null))
  defineTemporary(HTMLCanvasElement.prototype, 'toDataURL', () => options.dataUrl)
}

function stubCanvasUnavailable(): void {
  defineTemporary(HTMLCanvasElement.prototype, 'getContext', () => null)
}

function createAudioParam(): { setValueAtTime: () => void } {
  return { setValueAtTime: () => {} }
}

/** 离线音频上下文替身：渲染结果由 sampleValue 决定，便于制造不同音频特征。 */
function createOfflineAudioContextStub(sampleValue: number, shouldReject = false) {
  return class FakeOfflineAudioContext {
    currentTime = 0
    destination = {}

    createOscillator() {
      return {
        type: '',
        frequency: createAudioParam(),
        connect: () => {},
        start: () => {},
      }
    }

    createDynamicsCompressor() {
      return {
        threshold: createAudioParam(),
        knee: createAudioParam(),
        ratio: createAudioParam(),
        attack: createAudioParam(),
        release: createAudioParam(),
        connect: () => {},
      }
    }

    startRendering() {
      if (shouldReject) {
        return Promise.reject(new Error('渲染失败'))
      }
      const channel = new Float32Array(44100).fill(sampleValue)
      return Promise.resolve({ getChannelData: () => channel })
    }
  }
}

function useOfflineAudio(sampleValue: number, shouldReject = false): void {
  vi.stubGlobal('OfflineAudioContext', createOfflineAudioContextStub(sampleValue, shouldReject))
}

beforeEach(() => {
  // 固定一套可控的设备环境，保证用例之间互不影响、与本机真实硬件无关
  stubCanvas({ dataUrl: 'data:image/png;base64,BASE' })
  useOfflineAudio(0.5)
  defineTemporary(window.navigator, 'language', 'zh-CN')
  defineTemporary(window.navigator, 'languages', ['zh-CN', 'en'])
  defineTemporary(window.navigator, 'hardwareConcurrency', 8)
  defineTemporary(window.navigator, 'maxTouchPoints', 0)
  defineTemporary(window.screen, 'width', 1920)
  defineTemporary(window.screen, 'height', 1080)
  defineTemporary(window.screen, 'colorDepth', 24)
  defineTemporary(Date.prototype, 'getTimezoneOffset', () => -480)
})

afterEach(() => {
  while (restorers.length > 0) {
    restorers.pop()?.()
  }
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('输出格式', () => {
  it('返回 dfp_ 前缀加两段十六进制哈希', async () => {
    await expect(generateDeviceFingerprint()).resolves.toMatch(FINGERPRINT_PATTERN)
  })

  it('两段哈希各自不超过 8 位十六进制（32 位无符号整数）', async () => {
    const [, first, second] = (await generateDeviceFingerprint()).split('_')
    expect(first?.length).toBeLessThanOrEqual(8)
    expect(second?.length).toBeLessThanOrEqual(8)
  })
})

describe('同一环境下的稳定性', () => {
  it('同一环境连续两次调用结果完全一致', async () => {
    const first = await generateDeviceFingerprint()
    const second = await generateDeviceFingerprint()

    expect(second).toBe(first)
  })

  it('并发调用不会相互干扰，结果同样一致', async () => {
    const results = await Promise.all([
      generateDeviceFingerprint(),
      generateDeviceFingerprint(),
      generateDeviceFingerprint(),
    ])

    expect(new Set(results).size).toBe(1)
  })

  it('系统时间推进不影响指纹，指纹与时刻无关', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-01T00:00:00.000Z'))
    const first = await generateDeviceFingerprint()

    vi.setSystemTime(new Date('2026-12-31T23:59:59.000Z'))
    const second = await generateDeviceFingerprint()
    vi.useRealTimers()

    expect(second).toBe(first)
  })
})

describe('不同环境的可分辨性', () => {
  it('屏幕分辨率不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    defineTemporary(window.screen, 'width', 1280)

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('浏览器语言不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    defineTemporary(window.navigator, 'language', 'en-US')

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('逻辑核心数不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    defineTemporary(window.navigator, 'hardwareConcurrency', 16)

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('时区偏移不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    defineTemporary(Date.prototype, 'getTimezoneOffset', () => 0)

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('画布渲染结果不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    stubCanvas({ dataUrl: 'data:image/png;base64,OTHER' })

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('可用字体集合不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    stubCanvas({ dataUrl: 'data:image/png;base64,BASE', wideFontMarker: 'Consolas' })

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('音频渲染样本不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    useOfflineAudio(0.25)

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('插件列表不同则指纹不同', async () => {
    const before = await generateDeviceFingerprint()
    defineTemporary(window.navigator, 'plugins', { length: 1, 0: { name: 'PDF Viewer' } })

    expect(await generateDeviceFingerprint()).not.toBe(before)
  })

  it('插件只采样前 20 个，第 21 个之后的变化不影响指纹', async () => {
    const makePlugins = (lastName: string): Record<number | string, unknown> => {
      const plugins: Record<number | string, unknown> = { length: 25 }
      for (let i = 0; i < 25; i++) {
        plugins[i] = { name: i === 24 ? lastName : `p${String(i).padStart(2, '0')}` }
      }
      return plugins
    }

    defineTemporary(window.navigator, 'plugins', makePlugins('p24'))
    const before = await generateDeviceFingerprint()

    defineTemporary(window.navigator, 'plugins', makePlugins('zzz-different'))
    expect(await generateDeviceFingerprint()).toBe(before)
  })
})

describe('特征采集降级', () => {
  it('画布完全不可用时仍产出合法指纹，不抛错', async () => {
    stubCanvasUnavailable()

    await expect(generateDeviceFingerprint()).resolves.toMatch(FINGERPRINT_PATTERN)
  })

  it('音频指纹必须走 OfflineAudioContext，实时 AudioContext 不被构造', async () => {
    const RealtimeCtor = vi.fn()
    vi.stubGlobal('AudioContext', RealtimeCtor)
    vi.stubGlobal('webkitAudioContext', RealtimeCtor)

    await generateDeviceFingerprint()

    expect(RealtimeCtor).not.toHaveBeenCalled()
  })

  it('浏览器只提供 webkit 前缀的离线音频构造器时同样可用，结果与标准前缀一致', async () => {
    const standard = await generateDeviceFingerprint()

    vi.stubGlobal('OfflineAudioContext', undefined)
    vi.stubGlobal('webkitOfflineAudioContext', createOfflineAudioContextStub(0.5))

    expect(await generateDeviceFingerprint()).toBe(standard)
  })

  it('离线音频渲染失败与浏览器不支持离线音频降级到同一空特征', async () => {
    useOfflineAudio(0, true)
    const rejected = await generateDeviceFingerprint()

    vi.stubGlobal('OfflineAudioContext', undefined)
    vi.stubGlobal('webkitOfflineAudioContext', undefined)

    expect(await generateDeviceFingerprint()).toBe(rejected)
  })

  it('插件读取抛异常时降级为空特征而不是让整个指纹失败', async () => {
    defineTemporary(window.navigator, 'plugins', {
      get length(): number {
        throw new Error('被隐私插件拦截')
      },
    })

    await expect(generateDeviceFingerprint()).resolves.toMatch(FINGERPRINT_PATTERN)
  })
})
