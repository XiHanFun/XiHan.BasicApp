/**
 * packages/chat/composables/useVoiceRecorder.ts 的录音状态机。
 *
 * 职责边界：容器格式选择、计时与自动停止、结果产出的三条淘汰规则（太短 / 无数据 / 被取消）、
 * 以及**组件卸载后必须停掉麦克风轨道与全部定时器**。
 * MediaRecorder / navigator.mediaDevices 在 jsdom 中不存在，这里用最小替身注入，并在 afterEach 全部还原。
 */
import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { MAX_VOICE_SECONDS, useVoiceRecorder } from './composables/useVoiceRecorder'

interface FakeTrack {
  stop: () => void
  stopped: boolean
}

const tracks: FakeTrack[] = []
const recorders: FakeMediaRecorder[] = []
let supportedMimeTypes: string[] = ['audio/webm;codecs=opus', 'audio/webm']
let getUserMediaCalls = 0
let getUserMediaError: Error | null = null

class FakeMediaRecorder {
  static isTypeSupported(type: string): boolean {
    return supportedMimeTypes.includes(type)
  }

  state: 'inactive' | 'recording' = 'inactive'
  mimeType: string
  /** 停止时吐出的音频分片；null 表示浏览器一个字节都没给 */
  nextChunk: Blob | null = new Blob(['0123456789'], { type: 'audio/webm' })
  ondataavailable: ((event: { data: Blob }) => void) | null = null
  onstop: (() => void) | null = null

  constructor(_stream: unknown, options?: { mimeType?: string }) {
    this.mimeType = options?.mimeType ?? ''
    recorders.push(this)
  }

  start(): void {
    this.state = 'recording'
  }

  stop(): void {
    this.state = 'inactive'
    if (this.nextChunk) {
      this.ondataavailable?.({ data: this.nextChunk })
    }
    this.onstop?.()
  }
}

function installMediaStack(): void {
  vi.stubGlobal('MediaRecorder', FakeMediaRecorder)
  Object.defineProperty(navigator, 'mediaDevices', {
    configurable: true,
    writable: true,
    value: {
      getUserMedia: () => {
        getUserMediaCalls += 1
        if (getUserMediaError) {
          return Promise.reject(getUserMediaError)
        }
        const track: FakeTrack = {
          stopped: false,
          stop() {
            this.stopped = true
          },
        }
        tracks.push(track)
        return Promise.resolve({ getTracks: () => [track] })
      },
    },
  })
}

type Recorder = ReturnType<typeof useVoiceRecorder>

function mountRecorder(): { recorder: Recorder, unmount: () => void } {
  let captured: null | Recorder = null
  const wrapper = mount(defineComponent({
    setup() {
      captured = useVoiceRecorder()
      return () => h('div')
    },
  }))
  return { recorder: captured as unknown as Recorder, unmount: () => wrapper.unmount() }
}

beforeEach(() => {
  tracks.length = 0
  recorders.length = 0
  supportedMimeTypes = ['audio/webm;codecs=opus', 'audio/webm']
  getUserMediaCalls = 0
  getUserMediaError = null
  vi.useFakeTimers()
  vi.setSystemTime(new Date('2026-08-27T00:00:00Z'))
  installMediaStack()
})

afterEach(() => {
  vi.useRealTimers()
  Reflect.deleteProperty(navigator, 'mediaDevices')
})

describe('录音能力探测', () => {
  it('浏览器没有 MediaRecorder 时不支持录音，start 也不去请求麦克风', async () => {
    vi.stubGlobal('MediaRecorder', undefined)
    const { recorder, unmount } = mountRecorder()

    expect(recorder.supported.value).toBe(false)
    await recorder.start()

    expect(getUserMediaCalls).toBe(0)
    expect(recorder.recording.value).toBe(false)
    unmount()
  })

  it('有 MediaRecorder 但拿不到 getUserMedia 时同样判定为不支持', async () => {
    Object.defineProperty(navigator, 'mediaDevices', { configurable: true, writable: true, value: undefined })
    const { recorder, unmount } = mountRecorder()

    expect(recorder.supported.value).toBe(false)
    await recorder.start()

    expect(recorder.recording.value).toBe(false)
    unmount()
  })

  it('两个条件都满足时支持录音', () => {
    const { recorder, unmount } = mountRecorder()

    expect(recorder.supported.value).toBe(true)
    unmount()
  })

  it('麦克风被拒绝时异常上抛给调用方提示', async () => {
    getUserMediaError = new Error('NotAllowedError')
    const { recorder, unmount } = mountRecorder()

    await expect(recorder.start()).rejects.toThrow('NotAllowedError')

    expect(recorder.recording.value).toBe(false)
    unmount()
  })
})

describe('容器格式选择', () => {
  it('按优先级挑第一个被支持的容器，opus webm 优先', async () => {
    const { recorder, unmount } = mountRecorder()

    await recorder.start()

    expect(recorders[0]?.mimeType).toBe('audio/webm;codecs=opus')
    unmount()
  })

  it('只支持 mp4 时退到 mp4，文件扩展名为 m4a', async () => {
    supportedMimeTypes = ['audio/mp4']
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    recorders[0]!.nextChunk = new Blob(['data'], { type: 'audio/mp4' })

    vi.advanceTimersByTime(2000)
    const result = await recorder.stop()

    expect(recorders[0]?.mimeType).toBe('audio/mp4')
    expect(result?.file.name.endsWith('.m4a')).toBe(true)
    unmount()
  })

  it('只支持 ogg 时文件扩展名为 ogg', async () => {
    supportedMimeTypes = ['audio/ogg;codecs=opus']
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    vi.advanceTimersByTime(2000)
    const result = await recorder.stop()

    expect(result?.file.name.endsWith('.ogg')).toBe(true)
    unmount()
  })

  it('一个容器都不支持时不传 mimeType，产物按 webm 兜底命名', async () => {
    supportedMimeTypes = []
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    vi.advanceTimersByTime(2000)
    const result = await recorder.stop()

    expect(recorders[0]?.mimeType).toBe('')
    expect(result?.file.name.endsWith('.webm')).toBe(true)
    expect(result?.file.type).toBe('audio/webm')
    unmount()
  })
})

describe('计时与自动停止', () => {
  it('录音期间 elapsed 每 200ms 刷新一次，按整秒向下取整', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    expect(recorder.elapsed.value).toBe(0)
    vi.advanceTimersByTime(1000)
    expect(recorder.elapsed.value).toBe(1)
    vi.advanceTimersByTime(1400)
    expect(recorder.elapsed.value).toBe(2)

    unmount()
  })

  it('正在录音时重复 start 不会再要一次麦克风权限', async () => {
    const { recorder, unmount } = mountRecorder()

    await recorder.start()
    await recorder.start()

    expect(getUserMediaCalls).toBe(1)
    expect(recorders).toHaveLength(1)
    unmount()
  })

  it('录满 60 秒自动停止，结果被暂存到调用方松手时取走', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    vi.advanceTimersByTime(MAX_VOICE_SECONDS * 1000)
    expect(recorder.recording.value).toBe(false)
    expect(recorder.elapsed.value).toBe(0)

    const result = await recorder.stop()

    expect(result?.durationSeconds).toBe(MAX_VOICE_SECONDS)
    unmount()
  })

  it('暂存结果只能取走一次，再次 stop 返回 null', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    vi.advanceTimersByTime(MAX_VOICE_SECONDS * 1000)

    await recorder.stop()
    const second = await recorder.stop()

    expect(second).toBeNull()
    unmount()
  })

  it('从未开始过就 stop 返回 null', async () => {
    const { recorder, unmount } = mountRecorder()

    await expect(recorder.stop()).resolves.toBeNull()
    unmount()
  })
})

describe('结果产出规则', () => {
  it('正常停止产出 File 与向上取整的秒数，1.2 秒记 2 秒', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    vi.advanceTimersByTime(1200)
    const result = await recorder.stop()

    expect(result?.durationSeconds).toBe(2)
    expect(result?.file).toBeInstanceOf(File)
    expect(result?.file.size).toBeGreaterThan(0)
    expect(recorder.recording.value).toBe(false)
    unmount()
  })

  it('时长不足 1 秒按误触丢弃', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    const result = await recorder.stop()

    expect(result).toBeNull()
    unmount()
  })

  it('浏览器一个字节都没吐出时按失败处理，不产出空文件', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    recorders[0]!.nextChunk = null

    vi.advanceTimersByTime(3000)
    const result = await recorder.stop()

    expect(result).toBeNull()
    unmount()
  })

  it('取消录音让挂起的 stop 立刻以 null 收场', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    vi.advanceTimersByTime(3000)
    recorders[0]!.nextChunk = null
    recorders[0]!.stop = function stop(this: FakeMediaRecorder) {
      this.state = 'inactive'
    }

    const pending = recorder.stop()
    recorder.cancel()

    await expect(pending).resolves.toBeNull()
    unmount()
  })

  it('取消后麦克风轨道被停掉、计时归零', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    vi.advanceTimersByTime(3000)

    recorder.cancel()

    expect(tracks[0]?.stopped).toBe(true)
    expect(recorder.recording.value).toBe(false)
    expect(recorder.elapsed.value).toBe(0)
    unmount()
  })

  it('取消会丢掉自动停止暂存的结果，松手后拿不到这条录音', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    vi.advanceTimersByTime(MAX_VOICE_SECONDS * 1000)

    recorder.cancel()

    await expect(recorder.stop()).resolves.toBeNull()
    unmount()
  })
})

describe('组件卸载清理', () => {
  it('卸载时自动取消：麦克风轨道被停掉', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    unmount()

    expect(tracks[0]?.stopped).toBe(true)
    expect(recorder.recording.value).toBe(false)
  })

  it('卸载后计时定时器不再推进 elapsed', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()
    vi.advanceTimersByTime(1000)
    expect(recorder.elapsed.value).toBe(1)

    unmount()
    vi.advanceTimersByTime(5000)

    expect(recorder.elapsed.value).toBe(0)
  })

  it('卸载后自动停止定时器不再触发，60 秒后也不会凭空产出录音', async () => {
    const { recorder, unmount } = mountRecorder()
    await recorder.start()

    unmount()
    vi.advanceTimersByTime(MAX_VOICE_SECONDS * 2 * 1000)

    await expect(recorder.stop()).resolves.toBeNull()
    expect(vi.getTimerCount()).toBe(0)
  })

  it('未开始录音就卸载是安全空转', () => {
    const { unmount } = mountRecorder()

    expect(() => unmount()).not.toThrow()
    expect(tracks).toHaveLength(0)
  })
})
