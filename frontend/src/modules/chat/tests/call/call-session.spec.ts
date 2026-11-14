/**
 * call-session.ts：与一个对端之间的媒体连接
 *
 * 职责边界：出 offer 的一方按固定次序留好麦克风、摄像头、屏幕三个轨道位，另一方在对端的 offer 带出的位上挂自己的轨道；
 * 对端的轨道凭位置认出是哪一路；往位上换轨道不重新协商；对端的候选比会话描述先到时先存着，一条候选加不上不算失败；
 * 信令一条一条顺序处理、一条失败不拦后面的；关了之后什么都不再做。连接本身用替身，不碰真的 WebRTC
 */
import type { ChatCallSessionOptions, ChatCallSlot } from '@/modules/chat/call/call-session'
import type { ChatCallSignal } from '@/modules/chat/call/call-signal'
import { describe, expect, it, vi } from 'vitest'
import { CHAT_CALL_SLOTS, ChatCallSession } from '@/modules/chat/call/call-session'

interface FakeTransceiver {
  kind: string
  direction: string
  sender: { track: MediaStreamTrack | null, replaceTrack: (track: MediaStreamTrack | null) => Promise<void> }
}

/** RTCPeerConnection 的替身：把被调的顺序记下来 */
class FakePeer {
  connectionState: RTCPeerConnectionState = 'new'
  onicecandidate: ((event: { candidate: unknown }) => void) | null = null
  ontrack: ((event: { transceiver: unknown, track: unknown }) => void) | null = null
  onconnectionstatechange: (() => void) | null = null
  readonly log: string[] = []
  readonly transceivers: FakeTransceiver[] = []
  closed = false
  /** 下一次设远端描述时抛出 */
  failRemote = false
  /** 加这条候选时抛出 */
  badCandidate: null | string = null

  constructor(readonly configuration: RTCConfiguration) {}

  private transceiver(kind: string, track: MediaStreamTrack | null, direction: string): FakeTransceiver {
    const created: FakeTransceiver = {
      kind,
      direction,
      sender: {
        track,
        replaceTrack: async (next) => {
          created.sender.track = next
        },
      },
    }
    this.transceivers.push(created)
    return created
  }

  addTransceiver(trackOrKind: MediaStreamTrack | string, init: { direction: string }): FakeTransceiver {
    return typeof trackOrKind === 'string'
      ? this.transceiver(trackOrKind, null, init.direction)
      : this.transceiver(trackOrKind.kind, trackOrKind, init.direction)
  }

  getTransceivers(): FakeTransceiver[] {
    return this.transceivers
  }

  async createOffer(): Promise<RTCSessionDescriptionInit> {
    return { type: 'offer', sdp: 'offer-sdp' }
  }

  async createAnswer(): Promise<RTCSessionDescriptionInit> {
    return { type: 'answer', sdp: 'answer-sdp' }
  }

  async setLocalDescription(description: RTCSessionDescriptionInit): Promise<void> {
    this.log.push(`local:${description.type}`)
  }

  async setRemoteDescription(description: RTCSessionDescriptionInit): Promise<void> {
    if (this.failRemote) {
      this.failRemote = false
      throw new Error('bad sdp')
    }
    this.log.push(`remote:${description.type}`)
    // 对端的 offer 带出三个只收的位（浏览器按 offer 里的次序建）
    if (description.type === 'offer' && this.transceivers.length === 0) {
      for (const kind of ['audio', 'video', 'video']) {
        this.transceiver(kind, null, 'recvonly')
      }
    }
  }

  async addIceCandidate(candidate: RTCIceCandidateInit): Promise<void> {
    if (candidate.candidate === this.badCandidate) {
      throw new Error('unknown ufrag')
    }
    this.log.push(`candidate:${candidate.candidate}`)
  }

  close(): void {
    this.closed = true
  }
}

function track(kind: 'audio' | 'video', id: string): MediaStreamTrack {
  return { kind, id } as MediaStreamTrack
}

function create(patch: Partial<ChatCallSessionOptions> = {}) {
  const sent: ChatCallSignal[] = []
  const remote: [ChatCallSlot, string][] = []
  const states: RTCPeerConnectionState[] = []
  let peer!: FakePeer
  const session = new ChatCallSession({
    offerer: true,
    iceServers: [
      {
        urls: ['stun:turn.example.com:3478'],
      },
    ],
    tracks: {
      mic: track('audio', 'mic'),
      camera: track('video', 'cam'),
    },
    send: signal => sent.push(signal),
    onRemoteTrack: (slot, arrived) => remote.push([slot, arrived.id]),
    onStateChange: state => states.push(state),
    createPeer: (configuration) => {
      peer = new FakePeer(configuration)
      return peer as unknown as RTCPeerConnection
    },
    ...patch,
  })
  return {
    session,
    sent,
    remote,
    states,
    peer,
  }
}

/** 各轨道位上此刻在发的轨道 */
function sending(peer: FakePeer): (null | string)[] {
  return peer.transceivers.map(transceiver => transceiver.sender.track?.id ?? null)
}

describe('建立连接', () => {
  it('带上穿透服务器；出 offer 的一方按麦克风、摄像头、屏幕的次序留好三个双向的位', () => {
    const { peer } = create()

    expect(peer.configuration).toStrictEqual({
      iceServers: [
        {
          urls: ['stun:turn.example.com:3478'],
        },
      ],
    })
    expect(CHAT_CALL_SLOTS).toStrictEqual(['mic', 'camera', 'screen'])
    expect(peer.transceivers.map(transceiver => [transceiver.kind, transceiver.direction])).toStrictEqual([
      ['audio', 'sendrecv'],
      ['video', 'sendrecv'],
      ['video', 'sendrecv'],
    ])
    expect(sending(peer)).toStrictEqual(['mic', 'cam', null])
  })

  it('语音通话没有摄像头：位照留，空着', () => {
    const { peer } = create({
      tracks: {
        mic: track('audio', 'mic'),
      },
    })

    expect(peer.transceivers).toHaveLength(3)
    expect(sending(peer)).toStrictEqual(['mic', null, null])
  })

  it('出 offer：先设本端描述再交给对端', async () => {
    const { session, sent, peer } = create()

    await session.start()

    expect(peer.log).toStrictEqual(['local:offer'])
    expect(sent).toStrictEqual([
      { type: 'offer', sdp: 'offer-sdp' },
    ])
  })

  it('另一方自己不留位：收到 offer 后在它带出的位上改成双向、挂上自己的轨道，再回 answer', async () => {
    const { session, sent, peer } = create({ offerer: false })
    expect(peer.transceivers).toHaveLength(0)

    await session.handle({ type: 'offer', sdp: 'their-offer' })

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer'])
    expect(peer.transceivers.map(transceiver => transceiver.direction)).toStrictEqual(['sendrecv', 'sendrecv', 'sendrecv'])
    expect(sending(peer)).toStrictEqual(['mic', 'cam', null])
    expect(sent).toStrictEqual([
      { type: 'answer', sdp: 'answer-sdp' },
    ])
  })

  it('出 offer 的一方收到 answer：只设远端描述，不再回话', async () => {
    const { session, sent, peer } = create()
    await session.start()

    await session.handle({ type: 'answer', sdp: 'their-answer' })

    expect(peer.log).toStrictEqual(['local:offer', 'remote:answer'])
    expect(sent).toHaveLength(1)
  })
})

describe('iCE 候选', () => {
  it('比会话描述先到的候选先存着，远端描述设好后按到达的先后加上', async () => {
    const { session, peer } = create({ offerer: false })

    await session.handle({
      type: 'candidate',
      candidate: { candidate: 'a' },
    })
    await session.handle({
      type: 'candidate',
      candidate: { candidate: 'b' },
    })
    expect(peer.log).toStrictEqual([])

    await session.handle({ type: 'offer', sdp: 'their-offer' })
    await session.handle({
      type: 'candidate',
      candidate: { candidate: 'c' },
    })

    expect(peer.log).toStrictEqual([
      'remote:offer',
      'candidate:a',
      'candidate:b',
      'local:answer',
      'candidate:c',
    ])
  })

  it('一条候选加不上不算失败：别的照加，协商照常走完', async () => {
    const { session, sent, peer } = create({ offerer: false })
    peer.badCandidate = 'stale'

    await session.handle({
      type: 'candidate',
      candidate: { candidate: 'stale' },
    })
    await session.handle({ type: 'offer', sdp: 'their-offer' })
    await session.handle({
      type: 'candidate',
      candidate: { candidate: 'stale' },
    })
    await session.handle({
      type: 'candidate',
      candidate: { candidate: 'good' },
    })

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer', 'candidate:good'])
    expect(sent).toHaveLength(1)
  })

  it('本端找到的候选交给对端；收集完的那一下（空候选）不发', () => {
    const { sent, peer } = create()

    peer.onicecandidate?.({
      candidate: {
        toJSON: () => ({ candidate: 'mine', sdpMid: '0' }),
      },
    })
    peer.onicecandidate?.({ candidate: null })

    expect(sent).toStrictEqual([
      {
        type: 'candidate',
        candidate: { candidate: 'mine', sdpMid: '0' },
      },
    ])
  })
})

describe('媒体与状态', () => {
  it('对端的轨道凭位置认出是麦克风、摄像头还是屏幕；不在三个位上的不理', () => {
    const { remote, peer } = create()

    peer.ontrack?.({
      transceiver: peer.transceivers[2],
      track: track('video', 'their-screen'),
    })
    peer.ontrack?.({
      transceiver: peer.transceivers[0],
      track: track('audio', 'their-mic'),
    })
    peer.ontrack?.({
      transceiver: peer.transceivers[1],
      track: track('video', 'their-cam'),
    })
    peer.ontrack?.({
      transceiver: {},
      track: track('video', 'stray'),
    })

    expect(remote).toStrictEqual([
      ['screen', 'their-screen'],
      ['mic', 'their-mic'],
      ['camera', 'their-cam'],
    ])
  })

  it('连接状态变了告诉调用方', () => {
    const { states, peer } = create()

    peer.connectionState = 'connected'
    peer.onconnectionstatechange?.()

    expect(states).toStrictEqual(['connected'])
  })

  it('往位上换轨道：换设备、开始共享给新轨道，停止共享给空；都不重新协商', async () => {
    const { session, sent, peer } = create()

    await session.setTrack('mic', track('audio', 'mic-2'))
    await session.setTrack('screen', track('video', 'screen'))
    expect(sending(peer)).toStrictEqual(['mic-2', 'cam', 'screen'])

    await session.setTrack('screen', null)
    expect(sending(peer)).toStrictEqual(['mic-2', 'cam', null])
    expect(peer.log).toStrictEqual([])
    expect(sent).toStrictEqual([])
  })

  it('另一方在 offer 到之前还没有位：先记着，应答时一并挂上', async () => {
    const { session, peer } = create({ offerer: false })

    await session.setTrack('screen', track('video', 'screen'))
    await session.setTrack('camera', null)
    expect(peer.transceivers).toHaveLength(0)

    await session.handle({ type: 'offer', sdp: 'their-offer' })

    expect(sending(peer)).toStrictEqual(['mic', null, 'screen'])
  })
})

describe('顺序与收场', () => {
  it('信令按到达的先后处理：不等前一条做完就来的也排在后面', async () => {
    const { session, peer } = create({ offerer: false })

    const offered = session.handle({ type: 'offer', sdp: 'their-offer' })
    const added = session.handle({
      type: 'candidate',
      candidate: { candidate: 'late' },
    })
    await Promise.all([offered, added])

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer', 'candidate:late'])
  })

  it('一条失败原样交给调用方，后面的照常处理', async () => {
    const { session, peer } = create({ offerer: false })
    peer.failRemote = true

    await expect(session.handle({ type: 'offer', sdp: 'broken' })).rejects.toThrow('bad sdp')
    await session.handle({ type: 'offer', sdp: 'their-offer' })

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer'])
  })

  it('关了之后：连接关掉，之后到的信令、事件与换轨道都不再处理', async () => {
    const { session, sent, remote, states, peer } = create()
    const candidate = peer.onicecandidate
    const tracked = peer.ontrack
    const changed = peer.onconnectionstatechange

    session.close()
    session.close()
    await session.handle({ type: 'answer', sdp: 'their-answer' })
    await session.setTrack('mic', track('audio', 'mic-2'))
    candidate?.({
      candidate: {
        toJSON: () => ({ candidate: 'mine' }),
      },
    })
    tracked?.({
      transceiver: peer.transceivers[0],
      track: track('audio', 'theirs'),
    })
    changed?.()

    expect(peer.closed).toBe(true)
    expect(peer.log).toStrictEqual([])
    expect([sent, remote, states]).toStrictEqual([[], [], []])
    expect(sending(peer)).toStrictEqual(['mic', 'cam', null])
  })

  it('开关状态的信令不归连接管：收到不动连接', async () => {
    const handle = vi.fn()
    const { session, peer } = create({ offerer: false, send: handle })

    await session.handle({
      type: 'state',
      micOn: false,
      cameraOn: false,
      screenOn: true,
    })

    expect(peer.log).toStrictEqual([])
    expect(handle).not.toHaveBeenCalled()
  })
})
