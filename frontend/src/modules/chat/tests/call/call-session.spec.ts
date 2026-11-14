/**
 * call-session.ts：一场一对一通话的媒体连接
 *
 * 职责边界：发起方出 offer、接听方回 answer；对端的候选比会话描述先到时先存着；
 * 信令一条一条顺序处理、一条失败不拦后面的；关了之后什么都不再做。连接本身用替身，不碰真的 WebRTC
 */
import type { ChatCallSessionOptions } from '@/modules/chat/call/call-session'
import type { ChatCallSignal } from '@/modules/chat/call/call-signal'
import { describe, expect, it, vi } from 'vitest'
import { ChatCallSession } from '@/modules/chat/call/call-session'

interface FakeSender { track: MediaStreamTrack | null, replaceTrack: (track: MediaStreamTrack) => Promise<void> }

/** RTCPeerConnection 的替身：把被调的顺序记下来 */
class FakePeer {
  connectionState: RTCPeerConnectionState = 'new'
  onicecandidate: ((event: { candidate: unknown }) => void) | null = null
  ontrack: ((event: { streams: unknown[], track: unknown }) => void) | null = null
  onconnectionstatechange: (() => void) | null = null
  readonly log: string[] = []
  readonly senders: FakeSender[] = []
  closed = false
  /** 下一次设远端描述时抛出 */
  failRemote = false

  constructor(readonly configuration: RTCConfiguration) {}

  addTrack(track: MediaStreamTrack): void {
    const sender: FakeSender = {
      track,
      replaceTrack: async (next) => {
        sender.track = next
      },
    }
    this.senders.push(sender)
  }

  getSenders(): FakeSender[] {
    return this.senders
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
  }

  async addIceCandidate(candidate: RTCIceCandidateInit): Promise<void> {
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
  const remote: MediaStream[] = []
  const states: RTCPeerConnectionState[] = []
  let peer!: FakePeer
  const tracks = [
    track('audio', 'mic'),
    track('video', 'cam'),
  ]
  const session = new ChatCallSession({
    initiator: true,
    iceServers: [
      {
        urls: ['stun:turn.example.com:3478'],
      },
    ],
    localStream: {
      getTracks: () => tracks,
    } as unknown as MediaStream,
    send: signal => sent.push(signal),
    onRemoteStream: stream => remote.push(stream),
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
    tracks,
  }
}

describe('建立连接', () => {
  it('带上穿透服务器、挂上本端的全部轨道', () => {
    const { peer } = create()

    expect(peer.configuration).toStrictEqual({
      iceServers: [
        {
          urls: ['stun:turn.example.com:3478'],
        },
      ],
    })
    expect(peer.senders.map(sender => sender.track?.id)).toStrictEqual(['mic', 'cam'])
  })

  it('发起方出 offer：先设本端描述再交给对端', async () => {
    const { session, sent, peer } = create()

    await session.start()

    expect(peer.log).toStrictEqual(['local:offer'])
    expect(sent).toStrictEqual([
      { type: 'offer', sdp: 'offer-sdp' },
    ])
  })

  it('接听方收到 offer：设远端描述，回 answer', async () => {
    const { session, sent, peer } = create({ initiator: false })

    await session.handle({ type: 'offer', sdp: 'their-offer' })

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer'])
    expect(sent).toStrictEqual([
      { type: 'answer', sdp: 'answer-sdp' },
    ])
  })

  it('发起方收到 answer：只设远端描述，不再回话', async () => {
    const { session, sent, peer } = create()
    await session.start()

    await session.handle({ type: 'answer', sdp: 'their-answer' })

    expect(peer.log).toStrictEqual(['local:offer', 'remote:answer'])
    expect(sent).toHaveLength(1)
  })
})

describe('iCE 候选', () => {
  it('比会话描述先到的候选先存着，远端描述设好后按到达的先后加上', async () => {
    const { session, peer } = create({ initiator: false })

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
  it('对端的媒体到了、连接状态变了都告诉调用方', () => {
    const { remote, states, peer } = create()
    const stream = { id: 'remote' } as MediaStream

    peer.ontrack?.({
      streams: [stream],
      track: track('audio', 'theirs'),
    })
    peer.connectionState = 'connected'
    peer.onconnectionstatechange?.()

    expect(remote).toStrictEqual([stream])
    expect(states).toStrictEqual(['connected'])
  })

  it('换设备：同一类轨道的发送端原地替换；没有这一类的发送端即没换上', async () => {
    const { session, peer } = create({
      localStream: {
        getTracks: () => [
          track('audio', 'mic'),
        ],
      } as unknown as MediaStream,
    })

    expect(await session.replaceTrack(track('audio', 'mic-2'))).toBe(true)
    expect(peer.senders[0]?.track?.id).toBe('mic-2')
    expect(await session.replaceTrack(track('video', 'cam-2'))).toBe(false)
  })
})

describe('顺序与收场', () => {
  it('信令按到达的先后处理：不等前一条做完就来的也排在后面', async () => {
    const { session, peer } = create({ initiator: false })

    const offered = session.handle({ type: 'offer', sdp: 'their-offer' })
    const added = session.handle({
      type: 'candidate',
      candidate: { candidate: 'late' },
    })
    await Promise.all([offered, added])

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer', 'candidate:late'])
  })

  it('一条失败原样交给调用方，后面的照常处理', async () => {
    const { session, peer } = create({ initiator: false })
    peer.failRemote = true

    await expect(session.handle({ type: 'offer', sdp: 'broken' })).rejects.toThrow('bad sdp')
    await session.handle({ type: 'offer', sdp: 'their-offer' })

    expect(peer.log).toStrictEqual(['remote:offer', 'local:answer'])
  })

  it('关了之后：连接关掉，之后到的信令与事件都不再处理', async () => {
    const { session, sent, remote, states, peer } = create({ initiator: false })
    const candidate = peer.onicecandidate
    const tracked = peer.ontrack
    const changed = peer.onconnectionstatechange

    session.close()
    session.close()
    await session.handle({ type: 'offer', sdp: 'their-offer' })
    candidate?.({
      candidate: {
        toJSON: () => ({ candidate: 'mine' }),
      },
    })
    tracked?.({
      streams: [{} as MediaStream],
      track: track('audio', 'theirs'),
    })
    changed?.()

    expect(peer.closed).toBe(true)
    expect(peer.log).toStrictEqual([])
    expect([sent, remote, states]).toStrictEqual([[], [], []])
    expect(await session.replaceTrack(track('audio', 'mic-2'))).toBe(false)
  })

  it('开关状态的信令不归连接管：收到不动连接', async () => {
    const handle = vi.fn()
    const { session, peer } = create({ initiator: false, send: handle })

    await session.handle({ type: 'state', micOn: false, cameraOn: false })

    expect(peer.log).toStrictEqual([])
    expect(handle).not.toHaveBeenCalled()
  })
})
