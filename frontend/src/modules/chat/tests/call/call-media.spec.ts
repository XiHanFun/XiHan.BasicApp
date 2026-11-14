/**
 * call-media.ts：通话设备的获取、停用与列举
 *
 * 职责边界：语音只要麦克风、视频另要摄像头，取不全即失败（不悄悄降成语音）；
 * 浏览器的各种拒绝归成四种原因，界面据此说人话；换设备按设备号精确取一条轨道
 * navigator.mediaDevices 在 jsdom 里没有，每个用例自己放替身
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { acquireCallMedia, acquireCallTrack, ChatCallMediaError, listCallDevices, stopStream } from '@/modules/chat/call/call-media'

function installMediaDevices(devices: Partial<MediaDevices> | undefined): void {
  Object.defineProperty(navigator, 'mediaDevices', { configurable: true, value: devices })
}

function namedError(name: string): Error {
  const error = new Error(name)
  error.name = name
  return error
}

function fakeTrack(kind = 'audio') {
  return {
    kind,
    stop: vi.fn(),
  }
}

afterEach(() => {
  installMediaDevices(undefined)
})

describe('取通话媒体', () => {
  it('语音通话只要麦克风，视频通话另要摄像头', async () => {
    const getUserMedia = vi.fn(async () => ({ id: 'stream' }))
    installMediaDevices({ getUserMedia } as unknown as MediaDevices)

    await acquireCallMedia('Voice')
    await acquireCallMedia('Video')

    expect(getUserMedia.mock.calls).toStrictEqual([
      [
        { audio: true, video: false },
      ],
      [
        { audio: true, video: true },
      ],
    ])
  })

  it('浏览器不给设备接口（不支持，或页面不在安全上下文里）：按不支持说', async () => {
    installMediaDevices(undefined)

    await expect(acquireCallMedia('Voice')).rejects.toMatchObject({ failure: 'unsupported', media: 'Voice' })
  })

  it.each([
    ['NotAllowedError', 'denied'],
    ['SecurityError', 'denied'],
    ['NotFoundError', 'missing'],
    ['OverconstrainedError', 'missing'],
    ['NotReadableError', 'busy'],
    ['AbortError', 'busy'],
    ['TypeError', 'denied'],
  ])('浏览器报 %s：归成 %s，原错误留在 cause 里', async (name, failure) => {
    const cause = namedError(name)
    installMediaDevices({
      getUserMedia: () => Promise.reject(cause),
    } as unknown as MediaDevices)

    const error = await acquireCallMedia('Video').catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ChatCallMediaError)
    expect(error).toMatchObject({ failure, media: 'Video', cause })
  })
})

describe('换设备', () => {
  it('按设备号精确取：麦克风只要音频，摄像头只要视频', async () => {
    const getUserMedia = vi.fn(async (constraints: MediaStreamConstraints) => ({
      getTracks: () => [
        fakeTrack(constraints.video ? 'video' : 'audio'),
      ],
    }))
    installMediaDevices({ getUserMedia } as unknown as MediaDevices)

    const mic = await acquireCallTrack('audioinput', 'mic-2')
    const cam = await acquireCallTrack('videoinput', 'cam-2')

    expect([mic.kind, cam.kind]).toStrictEqual(['audio', 'video'])
    expect(getUserMedia.mock.calls).toStrictEqual([
      [
        {
          audio: {
            deviceId: { exact: 'mic-2' },
          },
        },
      ],
      [
        {
          video: {
            deviceId: { exact: 'cam-2' },
          },
        },
      ],
    ])
  })

  it('取回来没有轨道、或浏览器拒绝：都带原因失败', async () => {
    installMediaDevices({
      getUserMedia: async () => ({
        getTracks: () => [],
      }),
    } as unknown as MediaDevices)
    await expect(acquireCallTrack('audioinput', 'gone')).rejects.toMatchObject({ failure: 'missing', media: 'Voice' })

    installMediaDevices({
      getUserMedia: () => Promise.reject(namedError('NotReadableError')),
    } as unknown as MediaDevices)
    await expect(acquireCallTrack('videoinput', 'cam')).rejects.toMatchObject({ failure: 'busy', media: 'Video' })
  })
})

describe('停用与列举', () => {
  it('停掉一路媒体的全部轨道；没有媒体时什么都不做', () => {
    const tracks = [
      fakeTrack(),
      fakeTrack('video'),
    ]

    stopStream({
      getTracks: () => tracks,
    } as unknown as MediaStream)
    stopStream(null)

    expect(tracks.map(track => track.stop.mock.calls.length)).toStrictEqual([1, 1])
  })

  it('设备按种类分开；浏览器不支持选输出设备时不列扬声器', async () => {
    const all = [
      { kind: 'audioinput', deviceId: 'mic', label: '麦克风' },
      { kind: 'videoinput', deviceId: 'cam', label: '摄像头' },
      { kind: 'audiooutput', deviceId: 'spk', label: '扬声器' },
    ]
    installMediaDevices({
      enumerateDevices: async () => all,
    } as unknown as MediaDevices)

    const without = await listCallDevices()
    expect(without.microphones.map(device => device.deviceId)).toStrictEqual(['mic'])
    expect(without.cameras.map(device => device.deviceId)).toStrictEqual(['cam'])
    expect(without.speakers).toStrictEqual([])

    Object.defineProperty(HTMLMediaElement.prototype, 'setSinkId', {
      configurable: true,
      value: () => Promise.resolve(),
    })
    try {
      expect((await listCallDevices()).speakers.map(device => device.deviceId)).toStrictEqual(['spk'])
    }
    finally {
      Reflect.deleteProperty(HTMLMediaElement.prototype, 'setSinkId')
    }
  })

  it('浏览器没有设备接口时三类都为空', async () => {
    installMediaDevices(undefined)

    expect(await listCallDevices()).toStrictEqual({ microphones: [], cameras: [], speakers: [] })
  })
})
