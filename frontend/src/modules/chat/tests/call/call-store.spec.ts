/**
 * call-store.ts：这个页签眼里的那一通电话
 *
 * 职责边界：发起与接听先取设备再问服务端；通话的状态以服务端的返回值与推送为准；
 * 只有占着这通电话的页签建媒体连接、处理信令，别的页签只跟着停铃；响铃到点、连不上、断了由端上收场；
 * 连上或重连之后与服务端对一遍。设备、媒体连接、Hub 与提示音都是替身，不碰真的 WebRTC
 */
import type { ChatApiContract, ChatCallItem, ChatCallParticipantItem } from '@/modules/chat/types'
import type { Permissions, Session } from '~/stores'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { setChatApi } from '@/modules/chat/api-contract'
import { ChatCallMediaError } from '@/modules/chat/call/call-media'
import { useChatCallStore } from '@/modules/chat/call/call-store'
import { CHAT_CALL_SESSION_KEY, CHAT_PERMISSIONS } from '@/modules/chat/constants'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'

const fakes = vi.hoisted(() => {
  interface FakeTrack { kind: string, enabled: boolean, stopped: boolean, stop: () => void }

  function createTrack(kind: string): FakeTrack {
    const track: FakeTrack = {
      kind,
      enabled: true,
      stopped: false,
      stop: () => {
        track.stopped = true
      },
    }
    return track
  }

  function createStream(video: boolean) {
    const tracks = [
      createTrack('audio'),
      ...(video
        ? [
            createTrack('video'),
          ]
        : []),
    ]
    return {
      tracks,
      getTracks: () => [...tracks],
      getAudioTracks: () => tracks.filter(track => track.kind === 'audio'),
      getVideoTracks: () => tracks.filter(track => track.kind === 'video'),
      addTrack: (track: FakeTrack) => {
        tracks.push(track)
      },
      removeTrack: (track: FakeTrack) => {
        tracks.splice(tracks.indexOf(track), 1)
      },
    }
  }

  const state = {
    hub: [] as { method: string, args: unknown[] }[],
    sounds: [] as string[],
    acquired: [] as string[],
    streams: [] as ReturnType<typeof createStream>[],
    mediaFailure: null as unknown,
    sessions: [] as FakeSession[],
  }

  class FakeSession {
    readonly handled: unknown[] = []
    readonly replaced: FakeTrack[] = []
    started = 0
    closed = false
    failHandle = false

    constructor(readonly options: {
      initiator: boolean
      iceServers: unknown[]
      localStream: unknown
      onRemoteStream: (stream: unknown) => void
      onStateChange: (state: string) => void
    }) {
      state.sessions.push(this)
    }

    start(): Promise<void> {
      this.started += 1
      return Promise.resolve()
    }

    handle(signal: unknown): Promise<void> {
      if (this.failHandle) {
        return Promise.reject(new Error('bad signal'))
      }
      this.handled.push(signal)
      return Promise.resolve()
    }

    replaceTrack(track: FakeTrack): Promise<boolean> {
      this.replaced.push(track)
      return Promise.resolve(true)
    }

    close(): void {
      this.closed = true
    }
  }

  return {
    state,
    createTrack,
    createStream,
    FakeSession,
  }
})

vi.mock('~/composables/useSignalR', () => ({
  useSignalR: () => ({
    invoke: (method: string, ...args: unknown[]) => {
      fakes.state.hub.push({ method, args })
      return Promise.resolve()
    },
  }),
}))

vi.mock('~/composables/useNotificationSound', () => ({
  playNotificationSound: (kind: string) => {
    fakes.state.sounds.push(kind)
  },
}))

vi.mock('@/modules/chat/call/call-media', async importOriginal => ({
  ...await importOriginal<typeof import('@/modules/chat/call/call-media')>(),
  acquireCallMedia: async (media: string) => {
    if (fakes.state.mediaFailure) {
      throw fakes.state.mediaFailure
    }
    const stream = fakes.createStream(media === 'Video')
    fakes.state.acquired.push(media)
    fakes.state.streams.push(stream)
    return stream
  },
  acquireCallTrack: async (kind: string) => fakes.createTrack(kind === 'videoinput'
    ? 'video'
    : 'audio'),
}))

vi.mock('@/modules/chat/call/call-session', () => ({ ChatCallSession: fakes.FakeSession }))

const ME = 'me'
const BOB = 'bob'
const START = '2026-11-14T08:00:00.000Z'

function participant(userId: string, status: ChatCallParticipantItem['status']): ChatCallParticipantItem {
  return {
    userId,
    userName: userId,
    displayName: userId === BOB
      ? 'Bob'
      : '我',
    status,
  }
}

/** 我打给 Bob 的一通 */
function outgoing(patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return {
    callId: 'call-1',
    conversationId: 'conv-1',
    initiatorUserId: ME,
    media: 'Voice',
    status: 'Ringing',
    startedAt: START,
    ringExpiresAt: '2026-11-14T08:01:00.000Z',
    participants: [
      participant(ME, 'Joined'),
      participant(BOB, 'Invited'),
    ],
    ...patch,
  }
}

/** Bob 打给我的一通 */
function incoming(patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return outgoing({
    initiatorUserId: BOB,
    participants: [
      participant(BOB, 'Joined'),
      participant(ME, 'Invited'),
    ],
    ...patch,
  })
}

function createApi(overrides: Partial<ChatApiContract> = {}) {
  const calls: string[] = []
  const base: Partial<ChatApiContract> = {
    startCall: async (_, media) => outgoing({ media }),
    acceptCall: async callId => incoming({ callId, status: 'Ongoing' }),
    rejectCall: async callId => incoming({ callId, status: 'Ended', endReason: 'Rejected' }),
    hangupCall: async callId => outgoing({ callId, status: 'Ended', endReason: 'Completed' }),
    activeCall: async () => ({ call: null, now: START }),
    callIceServers: async () => [],
    ...overrides,
  }
  const api = new Proxy(base, {
    get(target, property: string) {
      const implementation = Reflect.get(target, property) as ((...args: unknown[]) => unknown) | undefined
      return (...args: unknown[]) => {
        calls.push([property, ...args].join(':'))
        return implementation
          ? implementation(...args)
          : Promise.reject(new Error(`未预期调用 ${property}`))
      }
    },
  }) as ChatApiContract
  return { api, calls }
}

function setup(overrides: Partial<ChatApiContract> = {}, permissions: string[] = [CHAT_PERMISSIONS.callRead, CHAT_PERMISSIONS.call]) {
  setActivePinia(createPinia())
  const auth = useAuthStore()
  auth.session = {
    accessToken: 'a-1',
    refreshToken: 'r-1',
    sessionId: 's-1',
    tenantId: null,
    tenantName: null,
    impersonatorUserName: null,
    mustChangePassword: false,
    user: { id: ME, userName: ME, displayName: '我' },
  } as unknown as Session
  auth.permissions = {
    isPlatform: false,
    permissions,
    roles: [],
    menus: [],
    pages: [],
  } as unknown as Permissions
  const { api, calls } = createApi(overrides)
  setChatApi(api)
  return {
    store: useChatCallStore(),
    calls,
  }
}

/** 让没被等待的异步（取穿透服务器、建连接）走完 */
const settle = () => vi.advanceTimersByTimeAsync(0)

function signalsSent() {
  return fakes.state.hub.map(call => [
    call.method,
    call.args[0],
    JSON.parse(String(call.args[1])),
  ])
}

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(START))
  sessionStorage.clear()
  Object.assign(fakes.state, {
    hub: [],
    sounds: [],
    acquired: [],
    streams: [],
    mediaFailure: null,
    sessions: [],
  })
})

afterEach(() => {
  vi.useRealTimers()
})

describe('发起', () => {
  it('先取设备再让服务端响铃：占下这通电话，回铃响起，记下刷新记号', async () => {
    const { store, calls } = setup()

    await store.start('conv-1', 'Video')

    expect(fakes.state.acquired).toStrictEqual(['Video'])
    expect(calls).toStrictEqual(['startCall:conv-1:Video'])
    expect([
      store.phase,
      store.engaged,
      store.cameraOn,
      store.micOn,
    ]).toStrictEqual(['outgoing', true, true, true])
    expect(store.peer?.userId).toBe(BOB)
    expect(store.localStream).toBe(fakes.state.streams[0])
    expect(fakes.state.sounds).toStrictEqual(['receive'])
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBe('call-1')
    expect(store.working).toBe(false)

    // 回铃隔一阵响一次
    await vi.advanceTimersByTimeAsync(3200)
    expect(fakes.state.sounds).toStrictEqual(['receive', 'receive'])
  })

  it('设备取不到就不打：不问服务端', async () => {
    const { store, calls } = setup()
    fakes.state.mediaFailure = new ChatCallMediaError('denied', 'Voice')

    await expect(store.start('conv-1', 'Voice')).rejects.toMatchObject({ failure: 'denied' })

    expect(calls).toStrictEqual([])
    expect([store.phase, store.working]).toStrictEqual(['idle', false])
  })

  it('服务端不让打（占线、对方不在线）：放掉设备，错误原样抛出', async () => {
    const busy = new Error('对方正在通话中')
    const { store } = setup({
      startCall: () => Promise.reject(busy),
    })

    await expect(store.start('conv-1', 'Voice')).rejects.toBe(busy)

    expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)
    expect([
      store.phase,
      store.localStream,
      sessionStorage.getItem(CHAT_CALL_SESSION_KEY),
    ]).toStrictEqual(['idle', null, null])
  })

  it('眼前已有一通时不再发起', async () => {
    const { store, calls } = setup()
    store.applyCallChanged(incoming())

    await store.start('conv-2', 'Voice')

    expect(calls).toStrictEqual([])
    expect(store.phase).toBe('incoming')
  })

  it('对方接了：取穿透服务器、建连接并出 offer，把开关状态告诉对端', async () => {
    const { store, calls } = setup({
      callIceServers: async () => [
        {
          urls: ['stun:turn.example.com:3478'],
        },
        {
          urls: ['turn:turn.example.com:3478'],
          username: '1700000000:me',
          credential: 'secret',
        },
      ],
    })
    await store.start('conv-1', 'Voice')

    store.applyCallChanged(outgoing({
      status: 'Ongoing',
      participants: [
        participant(ME, 'Joined'),
        participant(BOB, 'Joined'),
      ],
    }))
    await settle()

    const session = fakes.state.sessions[0]!
    expect(calls).toStrictEqual(['startCall:conv-1:Voice', 'callIceServers'])
    expect(session.options.initiator).toBe(true)
    expect(session.options.iceServers).toStrictEqual([
      {
        urls: ['stun:turn.example.com:3478'],
      },
      {
        urls: ['turn:turn.example.com:3478'],
        username: '1700000000:me',
        credential: 'secret',
      },
    ])
    expect(session.options.localStream).toBe(fakes.state.streams[0])
    expect(session.started).toBe(1)
    expect(signalsSent()).toStrictEqual([
      [
        'CallSignal',
        'call-1',
        { type: 'state', micOn: true, cameraOn: false },
      ],
    ])
    expect(store.phase).toBe('connecting')

    // 接通了不再响铃
    fakes.state.sounds.length = 0
    await vi.advanceTimersByTimeAsync(10000)
    expect(fakes.state.sounds).toStrictEqual([])
  })

  it('接听快过发起的响应：先到的接通推送在占下之后补上', async () => {
    let release: (item: ChatCallItem) => void = () => undefined
    const { store } = setup({
      startCall: () => new Promise(resolve => (release = resolve)),
    })

    const starting = store.start('conv-1', 'Voice')
    await settle()
    store.applyCallChanged(outgoing())
    store.applyCallChanged(outgoing({ status: 'Ongoing' }))
    expect(store.phase).toBe('idle')
    release(outgoing())
    await starting
    await settle()

    expect(store.call?.status).toBe('Ongoing')
    expect(fakes.state.sessions).toHaveLength(1)
  })
})

describe('来电', () => {
  it('推来响铃的来电：卡片出现、来电铃响；不是我的通话、自己在别处发起的都不理', () => {
    const { store } = setup()

    store.applyCallChanged(incoming({
      callId: 'stranger',
      participants: [
        participant(BOB, 'Joined'),
        participant('carol', 'Invited'),
      ],
    }))
    store.applyCallChanged(outgoing({ callId: 'elsewhere' }))
    expect(store.phase).toBe('idle')

    store.applyCallChanged(incoming())

    expect([store.phase, store.engaged, store.peer?.displayName]).toStrictEqual(['incoming', false, 'Bob'])
    expect(fakes.state.sounds).toStrictEqual(['notification'])
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBeNull()
  })

  it('接听：取设备、占下、告诉服务端，等对方的 offer；先到的信令存着，按先后交给连接', async () => {
    let releaseIce: (servers: never[]) => void = () => undefined
    const { store, calls } = setup({
      callIceServers: () => new Promise(resolve => (releaseIce = resolve)),
    })
    store.applyCallChanged(incoming({ media: 'Video' }))

    await store.accept()
    expect(fakes.state.acquired).toStrictEqual(['Video'])
    expect(calls).toStrictEqual(['acceptCall:call-1', 'callIceServers'])
    expect([
      store.phase,
      store.engaged,
      sessionStorage.getItem(CHAT_CALL_SESSION_KEY),
    ]).toStrictEqual(['connecting', true, 'call-1'])

    // 连接还在建：对方的 offer 与候选先到
    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: '{"type":"offer","sdp":"v=0"}' })
    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: '{"type":"candidate","candidate":{"candidate":"a"}}' })
    expect(fakes.state.sessions).toHaveLength(0)
    releaseIce([])
    await settle()

    const session = fakes.state.sessions[0]!
    expect(session.options.initiator).toBe(false)
    expect(session.started).toBe(0)
    expect(session.handled).toStrictEqual([
      { type: 'offer', sdp: 'v=0' },
      {
        type: 'candidate',
        candidate: { candidate: 'a' },
      },
    ])

    // 连接建好之后的直接交给它
    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: '{"type":"candidate","candidate":{"candidate":"b"}}' })
    expect(session.handled).toHaveLength(3)
  })

  it('接通的推送比接听的响应先到：不当成在别处接了，连接只建一次', async () => {
    let release: (item: ChatCallItem) => void = () => undefined
    const { store } = setup({
      acceptCall: () => new Promise(resolve => (release = resolve)),
    })
    store.applyCallChanged(incoming())

    const accepting = store.accept()
    await settle()
    store.applyCallChanged(incoming({ status: 'Ongoing' }))
    await settle()
    expect([store.phase, store.engaged]).toStrictEqual(['connecting', true])
    release(incoming({ status: 'Ongoing' }))
    await accepting
    await settle()

    expect(fakes.state.sessions).toHaveLength(1)
    expect(store.call?.status).toBe('Ongoing')
  })

  it('在别处接了：停铃收起，不提示', async () => {
    const { store } = setup()
    store.applyCallChanged(incoming())

    store.applyCallChanged(incoming({ status: 'Ongoing' }))
    fakes.state.sounds.length = 0
    await vi.advanceTimersByTimeAsync(10000)

    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
    expect(fakes.state.sounds).toStrictEqual([])
    expect(fakes.state.sessions).toHaveLength(0)
  })

  it('对方取消：收起并提示；到点没接的收起不提示', () => {
    const { store } = setup()
    store.applyCallChanged(incoming())

    store.applyCallChanged(incoming({ status: 'Ended', endReason: 'Cancelled' }))
    expect(store.phase).toBe('idle')
    expect(store.notice).toStrictEqual({ kind: 'cancelled', peerName: 'bob' })

    store.notice = null
    store.applyCallChanged(incoming({ callId: 'call-2' }))
    store.applyCallChanged(incoming({ callId: 'call-2', status: 'Ended', endReason: 'Missed' }))
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
  })

  it('拒绝：告诉服务端并收起；服务端说它已经不在响铃时照样收起、不抛', async () => {
    const { store, calls } = setup()
    store.applyCallChanged(incoming())

    await store.reject()
    expect(calls).toStrictEqual(['rejectCall:call-1'])
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])

    const gone = setup({
      rejectCall: () => Promise.reject(new ApiError(409, { code: 'CHAT_CONFLICT_005', detail: '这通电话已经被接听或已结束。' })),
    })
    gone.store.applyCallChanged(incoming())
    await gone.store.reject()
    expect(gone.store.phase).toBe('idle')
  })

  it('接听时设备取不到：来电照旧响着；服务端说接不了：收起并抛出', async () => {
    const { store, calls } = setup()
    store.applyCallChanged(incoming())
    fakes.state.mediaFailure = new ChatCallMediaError('missing', 'Voice')

    await expect(store.accept()).rejects.toMatchObject({ failure: 'missing' })
    expect([store.phase, store.engaged, calls.length]).toStrictEqual(['incoming', false, 0])

    fakes.state.mediaFailure = null
    const taken = setup({
      acceptCall: () => Promise.reject(new ApiError(409, { code: 'CHAT_CONFLICT_005', detail: '这通电话已经被接听或已结束。' })),
    })
    taken.store.applyCallChanged(incoming())
    await expect(taken.store.accept()).rejects.toBeInstanceOf(ApiError)
    expect([taken.store.phase, taken.store.localStream]).toStrictEqual(['idle', null])
    expect(fakes.state.streams.at(-1)?.tracks.every(track => track.stopped)).toBe(true)
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBeNull()
  })

  it('接听的请求没发到：放掉设备，来电照旧响着，还能再接', async () => {
    let fail = true
    const { store } = setup({
      acceptCall: callId => (fail
        ? Promise.reject(new TypeError('Failed to fetch'))
        : Promise.resolve(incoming({ callId, status: 'Ongoing' }))),
    })
    store.applyCallChanged(incoming())

    await expect(store.accept()).rejects.toBeInstanceOf(TypeError)
    expect([store.phase, store.engaged, store.localStream]).toStrictEqual(['incoming', false, null])
    expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)

    fail = false
    await store.accept()
    expect(store.phase).toBe('connecting')
  })

  it('眼前已有一通时别的来电不打断', async () => {
    const { store } = setup()
    await store.start('conv-1', 'Voice')

    store.applyCallChanged(incoming({ callId: 'call-2' }))
    store.applyCallChanged(incoming({ callId: 'call-2', status: 'Ended', endReason: 'Missed' }))

    expect([store.call?.callId, store.phase, store.notice]).toStrictEqual(['call-1', 'outgoing', null])
  })
})

describe('响铃到点', () => {
  it('发起端准点挂断并提示没人接', async () => {
    const { store, calls } = setup()
    await store.start('conv-1', 'Voice')

    await vi.advanceTimersByTimeAsync(59999)
    expect(store.phase).toBe('outgoing')
    await vi.advanceTimersByTimeAsync(1)

    expect(calls).toStrictEqual(['startCall:conv-1:Voice', 'hangupCall:call-1'])
    expect(store.phase).toBe('idle')
    expect(store.notice).toStrictEqual({ kind: 'no-answer', peerName: 'bob' })
    expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)

    // 服务端随后推来的结束不再重复提示
    store.notice = null
    store.applyCallChanged(outgoing({ status: 'Ended', endReason: 'Missed' }))
    expect(store.notice).toBeNull()
  })

  it('受邀端晚 2 秒替发起端挂，不提示', async () => {
    const { store, calls } = setup()
    store.applyCallChanged(incoming())

    await vi.advanceTimersByTimeAsync(61999)
    expect([store.phase, calls.length]).toStrictEqual(['incoming', 0])
    await vi.advanceTimersByTimeAsync(1)

    expect(calls).toStrictEqual(['hangupCall:call-1'])
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
  })

  it('正在接听的不挂：接听的结果马上就到', async () => {
    const { store, calls } = setup({
      acceptCall: () => new Promise(() => undefined),
    })
    store.applyCallChanged(incoming())

    void store.accept()
    await vi.advanceTimersByTimeAsync(70000)

    expect(calls).toStrictEqual(['acceptCall:call-1'])
    expect(store.phase).toBe('connecting')
  })
})

describe('通话中', () => {
  async function connected(media: 'Video' | 'Voice' = 'Voice') {
    const context = setup()
    await context.store.start('conv-1', media)
    context.store.applyCallChanged(outgoing({ media, status: 'Ongoing' }))
    await settle()
    // 一个用例里可能先后起几通：取最新的那一个
    const session = fakes.state.sessions.at(-1)!
    session.options.onStateChange('connected')
    fakes.state.hub.length = 0
    return {
      ...context,
      session,
      stream: fakes.state.streams.at(-1)!,
    }
  }

  it('媒体连上：记下时刻，对端的媒体归位', async () => {
    const { store, session } = await connected()
    const remote = { id: 'remote' }

    session.options.onRemoteStream(remote)

    expect([store.phase, store.connectionState, store.connectedAt]).toStrictEqual([
      'connected',
      'connected',
      Date.parse(START),
    ])
    expect(store.remoteStream).toBe(remote)
  })

  it('静音与关摄像头：改本端轨道并告诉对端', async () => {
    const { store, stream } = await connected('Video')

    store.setMicOn(false)
    store.setCameraOn(false)

    expect(stream.tracks.map(track => track.enabled)).toStrictEqual([false, false])
    expect([store.micOn, store.cameraOn]).toStrictEqual([false, false])
    expect(signalsSent()).toStrictEqual([
      [
        'CallSignal',
        'call-1',
        { type: 'state', micOn: false, cameraOn: true },
      ],
      [
        'CallSignal',
        'call-1',
        { type: 'state', micOn: false, cameraOn: false },
      ],
    ])
  })

  it('对端的开关随 state 信令更新；别的通话的、自己发的、读不懂的都不理', async () => {
    const { store, session } = await connected('Video')

    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: '{"type":"state","micOn":false,"cameraOn":false}' })
    expect([store.peerMicOn, store.peerCameraOn]).toStrictEqual([false, false])

    store.applyCallSignal({ callId: 'call-9', fromUserId: BOB, data: '{"type":"offer","sdp":"v=0"}' })
    store.applyCallSignal({ callId: 'call-1', fromUserId: ME, data: '{"type":"offer","sdp":"v=0"}' })
    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: 'garbage' })
    expect(session.handled).toStrictEqual([])
  })

  it('没占着这通电话的页签不处理信令', () => {
    const { store } = setup()
    store.applyCallChanged(incoming())

    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: '{"type":"offer","sdp":"v=0"}' })

    expect(fakes.state.sessions).toHaveLength(0)
    expect(store.phase).toBe('incoming')
  })

  it('连接断开：8 秒内恢复不挂，过了才挂并提示中断', async () => {
    const { store, calls, session } = await connected()

    session.options.onStateChange('disconnected')
    await vi.advanceTimersByTimeAsync(7000)
    session.options.onStateChange('connected')
    await vi.advanceTimersByTimeAsync(5000)
    expect(store.phase).toBe('connected')

    session.options.onStateChange('disconnected')
    await vi.advanceTimersByTimeAsync(8000)

    expect(store.phase).toBe('idle')
    expect(store.notice).toStrictEqual({ kind: 'interrupted', peerName: 'bob' })
    expect(calls.at(-1)).toBe('hangupCall:call-1')
    expect(session.closed).toBe(true)
  })

  it('连接失败：连上过的是中断，没连上的是连不上', async () => {
    const dropped = await connected()
    dropped.session.options.onStateChange('failed')
    expect(dropped.store.notice?.kind).toBe('interrupted')

    fakes.state.sessions.length = 0
    const never = setup()
    await never.store.start('conv-1', 'Voice')
    never.store.applyCallChanged(outgoing({ status: 'Ongoing' }))
    await settle()
    fakes.state.sessions[0]!.options.onStateChange('failed')
    expect(never.store.notice?.kind).toBe('connect-failed')
    expect(never.calls.at(-1)).toBe('hangupCall:call-1')
  })

  it('接通后 30 秒媒体还没连上即挂断；连上了就不再管', async () => {
    const { store, calls } = setup()
    await store.start('conv-1', 'Voice')
    store.applyCallChanged(outgoing({ status: 'Ongoing' }))
    await settle()

    await vi.advanceTimersByTimeAsync(30000)
    expect([
      store.phase,
      store.notice?.kind,
      calls.at(-1),
    ]).toStrictEqual(['idle', 'connect-failed', 'hangupCall:call-1'])

    const fine = await connected()
    await vi.advanceTimersByTimeAsync(60000)
    expect(fine.store.phase).toBe('connected')
  })

  it('取穿透服务器失败：收场并提示连不上', async () => {
    const { store, calls } = setup({
      callIceServers: () => Promise.reject(new Error('offline')),
    })
    await store.start('conv-1', 'Voice')

    store.applyCallChanged(outgoing({ status: 'Ongoing' }))
    await settle()

    expect([store.phase, store.notice?.kind]).toStrictEqual(['idle', 'connect-failed'])
    expect(calls).toStrictEqual(['startCall:conv-1:Voice', 'callIceServers', 'hangupCall:call-1'])
    expect(fakes.state.sessions).toHaveLength(0)
  })

  it('对端的信令处理不了：收场并提示连不上', async () => {
    const { store, session } = await connected()
    session.failHandle = true

    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: '{"type":"answer","sdp":"broken"}' })
    await settle()

    expect([store.phase, store.notice?.kind]).toStrictEqual(['idle', 'connect-failed'])
  })

  it('挂断：先放掉设备与连接，再告诉服务端；随后推来的结束不提示', async () => {
    const { store, calls, session, stream } = await connected()

    await store.hangup()

    expect(calls.at(-1)).toBe('hangupCall:call-1')
    expect([
      store.phase,
      store.localStream,
      store.remoteStream,
      store.connectedAt,
    ]).toStrictEqual(['idle', null, null, null])
    expect([
      session.closed,
      stream.tracks.every(track => track.stopped),
    ]).toStrictEqual([true, true])
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBeNull()

    store.applyCallChanged(outgoing({ status: 'Ended', endReason: 'Completed' }))
    expect(store.notice).toBeNull()
  })

  it('对端挂断：收场不提示；对端掉线记的中断要提示；对方拒绝要提示', async () => {
    const hungUp = await connected()
    hungUp.store.applyCallChanged(outgoing({ status: 'Ended', endReason: 'Completed' }))
    expect([hungUp.store.phase, hungUp.store.notice, hungUp.session.closed]).toStrictEqual(['idle', null, true])

    fakes.state.sessions.length = 0
    const dropped = await connected()
    dropped.store.applyCallChanged(outgoing({ status: 'Ended', endReason: 'Interrupted' }))
    expect(dropped.store.notice).toStrictEqual({ kind: 'interrupted', peerName: 'bob' })

    const rejected = setup()
    await rejected.store.start('conv-1', 'Voice')
    rejected.store.applyCallChanged(outgoing({ status: 'Ended', endReason: 'Rejected' }))
    expect(rejected.store.notice).toStrictEqual({ kind: 'rejected', peerName: 'bob' })
  })

  it('换设备：新轨道沿用当前的开关，替换进连接与本端媒体，旧的停掉', async () => {
    const { store, session, stream } = await connected('Video')
    const [oldMic, oldCamera] = stream.tracks
    store.setCameraOn(false)

    await store.switchDevice('audioinput', 'mic-2')
    await store.switchDevice('videoinput', 'cam-2')

    expect(session.replaced.map(track => [track.kind, track.enabled])).toStrictEqual([
      ['audio', true],
      ['video', false],
    ])
    expect([oldMic?.stopped, oldCamera?.stopped]).toStrictEqual([true, true])
    expect(stream.tracks).toHaveLength(2)
    expect(stream.tracks.every(track => !track.stopped)).toBe(true)
  })
})

describe('与服务端对一遍', () => {
  it('没有通话权限的人不出来电：推来的与认回来的都不响', async () => {
    const { store } = setup({
      activeCall: async () => ({
        call: incoming(),
        now: START,
      }),
    }, [CHAT_PERMISSIONS.callRead])

    store.applyCallChanged(incoming())
    await store.restore()

    expect(store.phase).toBe('idle')
    expect(fakes.state.sounds).toStrictEqual([])
  })

  it('没有通话读权限不问服务端', async () => {
    const { store, calls } = setup({}, [CHAT_PERMISSIONS.read])

    await store.restore()

    expect(calls).toStrictEqual([])
  })

  it('断线期间结束了的收场，不再提示', async () => {
    const { store } = setup()
    await store.start('conv-1', 'Voice')

    await store.restore()

    expect([
      store.phase,
      store.notice,
      sessionStorage.getItem(CHAT_CALL_SESSION_KEY),
    ]).toStrictEqual(['idle', null, null])
    expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)
  })

  it('还在响的来电认回来，剩余时间按服务端的钟算', async () => {
    // 服务端的钟比本机快一小时：响铃还剩 20 秒
    const { store, calls } = setup({
      activeCall: async () => ({
        call: incoming({ startedAt: '2026-11-14T09:00:00.000Z', ringExpiresAt: '2026-11-14T09:01:00.000Z' }),
        now: '2026-11-14T09:00:40.000Z',
      }),
    })

    await store.restore()
    expect(store.phase).toBe('incoming')
    expect(fakes.state.sounds).toStrictEqual(['notification'])

    await vi.advanceTimersByTimeAsync(21999)
    expect(calls).toStrictEqual(['activeCall'])
    await vi.advanceTimersByTimeAsync(1)
    expect(calls).toStrictEqual(['activeCall', 'hangupCall:call-1'])
  })

  it('刷新之前这个页签占着的那一通：媒体已经没了，替自己挂断', async () => {
    sessionStorage.setItem(CHAT_CALL_SESSION_KEY, 'call-1')
    const { store, calls } = setup({
      activeCall: async () => ({
        call: outgoing({ status: 'Ongoing' }),
        now: START,
      }),
    })

    await store.restore()

    expect(calls).toStrictEqual(['activeCall', 'hangupCall:call-1'])
    expect([
      store.phase,
      sessionStorage.getItem(CHAT_CALL_SESSION_KEY),
    ]).toStrictEqual(['idle', null])
  })

  it('在别的页签或别的端通着的、自己在别处发起的都不管', async () => {
    const ongoing = setup({
      activeCall: async () => ({
        call: incoming({ status: 'Ongoing' }),
        now: START,
      }),
    })
    await ongoing.store.restore()
    expect([ongoing.store.phase, ongoing.calls]).toStrictEqual([
      'idle',
      ['activeCall'],
    ])

    const mine = setup({
      activeCall: async () => ({
        call: outgoing(),
        now: START,
      }),
    })
    await mine.store.restore()
    expect([mine.store.phase, mine.calls]).toStrictEqual([
      'idle',
      ['activeCall'],
    ])
  })

  it('还是眼前这一通：以服务端的状态为准，漏掉的接通推送补上', async () => {
    const { store } = setup({
      activeCall: async () => ({
        call: outgoing({ status: 'Ongoing' }),
        now: START,
      }),
    })
    await store.start('conv-1', 'Voice')

    await store.restore()
    await settle()

    expect(store.call?.status).toBe('Ongoing')
    expect(fakes.state.sessions).toHaveLength(1)
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBe('call-1')
  })

  it('登出：放掉一切，不问服务端', async () => {
    const { store, calls } = setup()
    await store.start('conv-1', 'Voice')
    store.notice = { kind: 'rejected', peerName: 'bob' }

    store.reset()

    expect(calls).toStrictEqual(['startCall:conv-1:Voice'])
    expect([
      store.phase,
      store.engaged,
      store.localStream,
      store.notice,
    ]).toStrictEqual(['idle', false, null, null])
    expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)
  })
})
