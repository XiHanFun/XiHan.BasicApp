/**
 * call-store.ts：这个页签眼里的那一通电话
 *
 * 职责边界：发起、接听与加入先取设备再问服务端；通话的状态以服务端的返回值与推送为准；
 * 只有占着这通电话的页签建媒体连接、处理信令，别的页签只跟着停铃；通话里每个人各一条连接，后加入的一方出 offer；
 * 响铃到点、连不上、断了由端上收场；屏幕共享只是往各条连接上换轨道，单独发起时先挑画面、一发起就带着；群里还没结束的通话另记在会话名下；
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
  interface FakeTrack {
    kind: string
    enabled: boolean
    stopped: boolean
    stop: () => void
    listeners: Record<string, (() => void)[]>
    addEventListener: (type: string, listener: () => void) => void
  }

  function createTrack(kind: string): FakeTrack {
    const track: FakeTrack = {
      kind,
      enabled: true,
      stopped: false,
      stop: () => {
        track.stopped = true
      },
      listeners: {},
      addEventListener: (type, listener) => {
        (track.listeners[type] ??= []).push(listener)
      },
    }
    return track
  }

  function createStream(kinds: string[]) {
    const tracks = kinds.map(createTrack)
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
    /** 下一次取屏幕的结果：给画面、用户取消，或抛出 */
    screen: 'stream' as 'cancel' | 'stream' | Error,
    screens: [] as ReturnType<typeof createStream>[],
    sessions: [] as FakeSession[],
  }

  class FakeSession {
    readonly handled: unknown[] = []
    readonly tracks: [string, FakeTrack | null][] = []
    started = 0
    closed = false
    failHandle = false

    constructor(readonly options: {
      offerer: boolean
      iceServers: unknown[]
      tracks: Record<string, FakeTrack | null>
      onRemoteTrack: (slot: string, track: unknown) => void
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

    setTrack(slot: string, track: FakeTrack | null): Promise<void> {
      this.tracks.push([slot, track])
      return Promise.resolve()
    }

    close(): void {
      this.closed = true
    }
  }

  /** MediaStream 的替身：只记下装了哪些轨道 */
  class FakeMediaStream {
    constructor(readonly tracks: unknown[]) {}
  }

  return {
    state,
    createTrack,
    createStream,
    FakeSession,
    FakeMediaStream,
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
    const stream = fakes.createStream(media === 'Video'
      ? ['audio', 'video']
      : ['audio'])
    fakes.state.acquired.push(media)
    fakes.state.streams.push(stream)
    return stream
  },
  acquireCallTrack: async (kind: string) => fakes.createTrack(kind === 'videoinput'
    ? 'video'
    : 'audio'),
  acquireScreen: async () => {
    if (fakes.state.screen === 'cancel') {
      return null
    }
    if (fakes.state.screen instanceof Error) {
      throw fakes.state.screen
    }
    const stream = fakes.createStream(['video'])
    fakes.state.screens.push(stream)
    return stream
  },
}))

vi.mock('@/modules/chat/call/call-session', () => ({ ChatCallSession: fakes.FakeSession }))

const ME = 'me'
const BOB = 'bob'
const CAROL = 'carol'
const START = '2026-11-14T08:00:00.000Z'
/** 之后先后加入的三个时刻 */
const T1 = '2026-11-14T08:00:05.000Z'
const T2 = '2026-11-14T08:00:09.000Z'
const T3 = '2026-11-14T08:00:30.000Z'

function participant(userId: string, status: ChatCallParticipantItem['status'], joinedAt?: string): ChatCallParticipantItem {
  return {
    userId,
    userName: userId,
    displayName: userId === ME
      ? '我'
      : userId.toUpperCase(),
    status,
    ...(joinedAt
      ? { joinedAt }
      : {}),
  }
}

/** 我打给 Bob 的一通，还在响 */
function outgoing(patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return {
    callId: 'call-1',
    conversationId: 'conv-1',
    conversationType: 'Single',
    initiatorUserId: ME,
    media: 'Voice',
    status: 'Ringing',
    startedAt: START,
    ringExpiresAt: '2026-11-14T08:01:00.000Z',
    participants: [
      participant(ME, 'Joined', START),
      participant(BOB, 'Invited'),
    ],
    ...patch,
  }
}

/** 我打给 Bob、Bob 接了 */
function answered(patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return outgoing({
    status: 'Ongoing',
    participants: [
      participant(ME, 'Joined', START),
      participant(BOB, 'Joined', T1),
    ],
    ...patch,
  })
}

/** Bob 打给我的一通，还在响 */
function incoming(patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return outgoing({
    initiatorUserId: BOB,
    participants: [
      participant(BOB, 'Joined', START),
      participant(ME, 'Invited'),
    ],
    ...patch,
  })
}

/** Bob 打给我、我接了 */
function accepted(patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return incoming({
    status: 'Ongoing',
    participants: [
      participant(BOB, 'Joined', START),
      participant(ME, 'Joined', T1),
    ],
    ...patch,
  })
}

/** 群里的一通 */
function group(participants: ChatCallParticipantItem[], patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return outgoing({
    callId: 'call-g',
    conversationId: 'conv-g',
    conversationType: 'Group',
    conversationName: 'Team',
    participants,
    ...patch,
  })
}

/** 我在群里发起、点了 Bob 与 Carol 的一通：各人此刻的状态由调用方给 */
function mine(bob: ChatCallParticipantItem, carol: ChatCallParticipantItem, patch: Partial<ChatCallItem> = {}): ChatCallItem {
  return group([
    participant(ME, 'Joined', START),
    bob,
    carol,
  ], { status: 'Ongoing', ...patch })
}

function createApi(overrides: Partial<ChatApiContract> = {}) {
  const calls: string[] = []
  const base: Partial<ChatApiContract> = {
    startCall: async (_, media, inviteeUserIds) => (inviteeUserIds
      ? group([
          participant(ME, 'Joined', START),
          ...inviteeUserIds.map(userId => participant(userId, 'Invited')),
        ], { media })
      : outgoing({ media })),
    acceptCall: async callId => accepted({ callId }),
    rejectCall: async callId => incoming({ callId, status: 'Ended', endReason: 'Rejected' }),
    hangupCall: async callId => outgoing({ callId, status: 'Ended', endReason: 'Completed' }),
    activeCall: async () => ({ call: null, now: START }),
    ongoingCall: async () => ({ call: null, now: START }),
    callSettings: async () => ({ ringSeconds: 60, maxVoiceParticipants: 8, maxVideoParticipants: 6 }),
    callIceServers: async () => [],
    ...overrides,
  }
  const api = new Proxy(base, {
    get(target, property: string) {
      const implementation = Reflect.get(target, property) as ((...args: unknown[]) => unknown) | undefined
      return (...args: unknown[]) => {
        calls.push([
          property,
          ...args.filter(arg => arg !== undefined),
        ].join(':'))
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
async function settle(): Promise<void> {
  await vi.advanceTimersByTimeAsync(0)
  await vi.advanceTimersByTimeAsync(0)
}

/** 发到 Hub 的信令：方法、通话、指给谁（null 即通话里的所有人）与内容 */
function signalsSent() {
  return fakes.state.hub.map(call => [
    call.method,
    call.args[0],
    call.args[1],
    JSON.parse(String(call.args[2])),
  ])
}

function signal(fromUserId: string, data: unknown, callId = 'call-1') {
  return {
    callId,
    fromUserId,
    data: JSON.stringify(data),
  }
}

/** 与某个人的那条连接（一个人可能先后建过几条，取最新的） */
function sessionWith(index: number) {
  return fakes.state.sessions[index]!
}

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(START))
  vi.stubGlobal('MediaStream', fakes.FakeMediaStream)
  sessionStorage.clear()
  Object.assign(fakes.state, {
    hub: [],
    sounds: [],
    acquired: [],
    streams: [],
    mediaFailure: null,
    screen: 'stream',
    screens: [],
    sessions: [],
  })
})

afterEach(() => {
  vi.unstubAllGlobals()
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
      store.screenOn,
    ]).toStrictEqual(['outgoing', true, true, true, false])
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

  describe('屏幕共享', () => {
    it('先让用户挑画面，再取麦克风、让服务端响铃：占下之后屏幕就带着，对方接了建连接时直接在屏幕位上', async () => {
      const { store, calls } = setup()

      await store.start('conv-1', 'Screen')

      const screen = fakes.state.screens[0]!
      expect(fakes.state.acquired).toStrictEqual(['Screen'])
      expect(calls).toStrictEqual(['startCall:conv-1:Screen'])
      expect([
        store.phase,
        store.micOn,
        store.cameraOn,
        store.screenOn,
        store.screenStream,
      ]).toStrictEqual(['outgoing', true, false, true, screen])
      // 只要麦克风：屏幕另是一路
      expect(fakes.state.streams[0]?.tracks.map(track => track.kind)).toStrictEqual(['audio'])

      store.applyCallChanged(answered({ media: 'Screen' }))
      await settle()

      expect(sessionWith(0).options.tracks.screen).toBe(screen.tracks[0])
      expect(signalsSent().at(-1)?.[3]).toStrictEqual({
        type: 'state',
        micOn: true,
        cameraOn: false,
        screenOn: true,
      })
    })

    it('在选择框里取消：不取麦克风、不问服务端；屏幕取不到的原样抛出', async () => {
      const { store, calls } = setup()
      fakes.state.screen = 'cancel'

      await store.start('conv-1', 'Screen')

      expect([
        fakes.state.acquired,
        calls,
        store.phase,
        store.working,
      ]).toStrictEqual([[], [], 'idle', false])

      fakes.state.screen = new ChatCallMediaError('unsupported', 'Video')
      await expect(store.start('conv-1', 'Screen')).rejects.toMatchObject({ failure: 'unsupported' })
      expect([calls, store.phase, store.working]).toStrictEqual([[], 'idle', false])
    })

    it('麦克风取不到、服务端不让打：挑好的屏幕也放掉', async () => {
      const first = setup()
      fakes.state.mediaFailure = new ChatCallMediaError('denied', 'Screen')

      await expect(first.store.start('conv-1', 'Screen')).rejects.toMatchObject({ failure: 'denied' })

      expect([first.calls, fakes.state.screens[0]?.tracks[0]?.stopped, first.store.screenOn]).toStrictEqual([[], true, false])

      fakes.state.mediaFailure = null
      const busy = new Error('对方正在通话中')
      const second = setup({
        startCall: () => Promise.reject(busy),
      })

      await expect(second.store.start('conv-1', 'Screen')).rejects.toBe(busy)

      expect(fakes.state.screens[1]?.tracks[0]?.stopped).toBe(true)
      expect(fakes.state.streams.at(-1)?.tracks.every(track => track.stopped)).toBe(true)
      expect([second.store.screenOn, second.store.phase]).toStrictEqual([false, 'idle'])
    })

    it('发起之后从浏览器自己的条上停掉：跟着停止共享，通话照旧', async () => {
      const { store } = setup()
      await store.start('conv-1', 'Screen')

      fakes.state.screens[0]?.tracks[0]?.listeners.ended?.forEach(listener => listener())

      expect([store.screenOn, store.phase]).toStrictEqual([false, 'outgoing'])
    })
  })

  it('对方接了：取穿透服务器、与他建连接，后加入的他出 offer，把开关状态告诉他', async () => {
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

    store.applyCallChanged(answered())
    await settle()

    const session = sessionWith(0)
    expect(calls).toStrictEqual(['startCall:conv-1:Voice', 'callIceServers'])
    expect([session.options.offerer, session.started]).toStrictEqual([false, 0])
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
    expect(session.options.tracks).toStrictEqual({
      mic: fakes.state.streams[0]?.tracks[0],
      camera: null,
      screen: null,
    })
    expect(signalsSent()).toStrictEqual([
      [
        'CallSignal',
        'call-1',
        BOB,
        {
          type: 'state',
          micOn: true,
          cameraOn: false,
          screenOn: false,
        },
      ],
    ])
    expect(store.phase).toBe('connecting')
    expect(store.peers.map(peer => [peer.userId, peer.connection])).toStrictEqual([
      [BOB, 'connecting'],
    ])

    // 接通了不再响铃
    fakes.state.sounds.length = 0
    await vi.advanceTimersByTimeAsync(10000)
    expect(fakes.state.sounds).toStrictEqual([])
  })

  it('连接还在建时先到的信令存着，建好后按先后交给它；之后的直接给', async () => {
    let releaseIce: (servers: never[]) => void = () => undefined
    const { store } = setup({
      callIceServers: () => new Promise(resolve => (releaseIce = resolve)),
    })
    await store.start('conv-1', 'Voice')
    store.applyCallChanged(answered())

    store.applyCallSignal(signal(BOB, { type: 'offer', sdp: 'v=0' }))
    store.applyCallSignal(signal(BOB, {
      type: 'candidate',
      candidate: { candidate: 'a' },
    }))
    expect(fakes.state.sessions).toHaveLength(0)
    releaseIce([])
    await settle()

    const session = sessionWith(0)
    expect(session.handled).toStrictEqual([
      { type: 'offer', sdp: 'v=0' },
      {
        type: 'candidate',
        candidate: { candidate: 'a' },
      },
    ])

    store.applyCallSignal(signal(BOB, {
      type: 'candidate',
      candidate: { candidate: 'b' },
    }))
    expect(session.handled).toHaveLength(3)
  })

  it('接听快过发起的响应：先到的接通推送在占下之后补上', async () => {
    let release: (item: ChatCallItem) => void = () => undefined
    const { store } = setup({
      startCall: () => new Promise(resolve => (release = resolve)),
    })

    const starting = store.start('conv-1', 'Voice')
    await settle()
    store.applyCallChanged(outgoing())
    store.applyCallChanged(answered())
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
        participant(BOB, 'Joined', START),
        participant(CAROL, 'Invited'),
      ],
    }))
    store.applyCallChanged(outgoing({ callId: 'elsewhere' }))
    expect(store.phase).toBe('idle')

    store.applyCallChanged(incoming())

    expect([store.phase, store.engaged, store.peer?.displayName]).toStrictEqual(['incoming', false, 'BOB'])
    expect(fakes.state.sounds).toStrictEqual(['notification'])
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBeNull()
  })

  it('接听：取设备、占下、告诉服务端，后加入的我出 offer', async () => {
    const { store, calls } = setup({
      acceptCall: async callId => accepted({ callId, media: 'Video' }),
    })
    store.applyCallChanged(incoming({ media: 'Video' }))

    await store.accept()
    await settle()

    expect(fakes.state.acquired).toStrictEqual(['Video'])
    expect(calls).toStrictEqual(['acceptCall:call-1', 'callIceServers'])
    expect([
      store.phase,
      store.engaged,
      store.cameraOn,
      sessionStorage.getItem(CHAT_CALL_SESSION_KEY),
    ]).toStrictEqual(['connecting', true, true, 'call-1'])
    const session = sessionWith(0)
    expect([session.options.offerer, session.started]).toStrictEqual([true, 1])
    expect(session.options.tracks.camera).toBe(fakes.state.streams[0]?.tracks[1])
  })

  it('接通的推送比接听的响应先到：不当成在别处接了，连接只建一次', async () => {
    let release: (item: ChatCallItem) => void = () => undefined
    const { store } = setup({
      acceptCall: () => new Promise(resolve => (release = resolve)),
    })
    store.applyCallChanged(incoming())

    const accepting = store.accept()
    await settle()
    expect([store.phase, store.engaged]).toStrictEqual(['connecting', true])
    store.applyCallChanged(accepted())
    await settle()
    release(accepted())
    await accepting
    await settle()

    expect(fakes.state.sessions).toHaveLength(1)
    expect(store.call?.status).toBe('Ongoing')
  })

  it('接听的响应比别人加入的推送晚到：以推送为准，不照旧响应把刚建好的连接关掉', async () => {
    let release: (item: ChatCallItem) => void = () => undefined
    const { store } = setup({
      acceptCall: () => new Promise(resolve => (release = resolve)),
    })
    const seenBy = (me: ChatCallParticipantItem, carol: ChatCallParticipantItem, status: ChatCallItem['status']) => group([
      participant(BOB, 'Joined', START),
      me,
      carol,
    ], { initiatorUserId: BOB, status })
    store.applyCallChanged(seenBy(participant(ME, 'Invited'), participant(CAROL, 'Invited'), 'Ringing'))

    const accepting = store.accept()
    await settle()
    // 我接听的那一步提交了，Carol 紧跟着也接了：两条推送都先于我那一步的响应到
    const afterMe = seenBy(participant(ME, 'Joined', T1), participant(CAROL, 'Invited'), 'Ongoing')
    const afterCarol = seenBy(participant(ME, 'Joined', T1), participant(CAROL, 'Joined', T2), 'Ongoing')
    store.applyCallChanged(afterMe)
    store.applyCallChanged(afterCarol)
    await settle()
    expect(store.peers.map(peer => peer.userId)).toStrictEqual([BOB, CAROL])

    release(afterMe)
    await accepting
    await settle()

    expect(store.peers.map(peer => peer.userId)).toStrictEqual([BOB, CAROL])
    expect(fakes.state.sessions.map(session => session.closed)).toStrictEqual([false, false])
  })

  it('在别处接了：停铃收起，不提示', async () => {
    const { store } = setup()
    store.applyCallChanged(incoming())

    store.applyCallChanged(accepted())
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
    expect(store.notice).toStrictEqual({ kind: 'cancelled', peerName: 'bob', group: false })

    store.notice = null
    store.applyCallChanged(incoming({ callId: 'call-2' }))
    store.applyCallChanged(incoming({ callId: 'call-2', status: 'Ended', endReason: 'Missed' }))
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
  })

  it('拒绝：告诉服务端并收起；服务端说它已经不在等我答复时照样收起、不抛', async () => {
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
        : Promise.resolve(accepted({ callId }))),
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

  it('多人通话的来电：别人先接了，我还没答复就接着响；我的邀请没了才收起', async () => {
    const { store } = setup()
    const ringing = group([
      participant(BOB, 'Joined', START),
      participant(ME, 'Invited'),
      participant(CAROL, 'Invited'),
    ], { initiatorUserId: BOB })
    store.applyCallChanged(ringing)
    expect([store.phase, store.peer?.userId]).toStrictEqual(['incoming', BOB])

    store.applyCallChanged({
      ...ringing,
      status: 'Ongoing',
      participants: [
        participant(BOB, 'Joined', START),
        participant(ME, 'Invited'),
        participant(CAROL, 'Joined', T1),
      ],
    })
    fakes.state.sounds.length = 0
    await vi.advanceTimersByTimeAsync(2200)
    expect([store.phase, store.call?.status]).toStrictEqual(['incoming', 'Ongoing'])
    expect(fakes.state.sounds).toStrictEqual(['notification'])

    // 在别的端拒绝了
    store.applyCallChanged({
      ...ringing,
      status: 'Ongoing',
      participants: [
        participant(BOB, 'Joined', START),
        participant(ME, 'Rejected'),
        participant(CAROL, 'Joined', T1),
      ],
    })
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
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
    expect(store.notice).toStrictEqual({ kind: 'no-answer', peerName: 'bob', group: false })
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

  it('多人通话已经有人接了：受邀端到点挂掉的只是自己那份邀请', async () => {
    const { store, calls } = setup()
    store.applyCallChanged(group([
      participant(BOB, 'Joined', START),
      participant(ME, 'Invited'),
      participant(CAROL, 'Joined', T1),
    ], { initiatorUserId: BOB, status: 'Ongoing' }))

    await vi.advanceTimersByTimeAsync(62000)

    expect(calls).toStrictEqual(['hangupCall:call-g'])
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
  })
})

describe('通话中', () => {
  async function connected(media: 'Video' | 'Voice' = 'Voice') {
    const context = setup()
    await context.store.start('conv-1', media)
    context.store.applyCallChanged(answered({ media }))
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

  it('媒体连上：记下时刻；对端各轨道位的轨道落到他的声音、摄像头与屏幕上', async () => {
    const { store, session } = await connected()
    const [mic, camera, screen] = [
      { id: 'mic' },
      { id: 'cam' },
      { id: 'screen' },
    ]

    session.options.onRemoteTrack('mic', mic)
    session.options.onRemoteTrack('camera', camera)
    session.options.onRemoteTrack('screen', screen)

    expect([store.phase, store.connectedAt]).toStrictEqual([
      'connected',
      Date.parse(START),
    ])
    const peer = store.peers[0]!
    expect(peer.connection).toBe('connected')
    expect([peer.audio, peer.camera, peer.screen]).toStrictEqual([
      new fakes.FakeMediaStream([mic]),
      new fakes.FakeMediaStream([camera]),
      new fakes.FakeMediaStream([screen]),
    ])
  })

  it('静音与关摄像头：改本端轨道并告诉通话里的所有人', async () => {
    const { store, stream } = await connected('Video')

    store.setMicOn(false)
    store.setCameraOn(false)

    expect(stream.tracks.map(track => track.enabled)).toStrictEqual([false, false])
    expect([store.micOn, store.cameraOn]).toStrictEqual([false, false])
    expect(signalsSent()).toStrictEqual([
      [
        'CallSignal',
        'call-1',
        null,
        {
          type: 'state',
          micOn: false,
          cameraOn: true,
          screenOn: false,
        },
      ],
      [
        'CallSignal',
        'call-1',
        null,
        {
          type: 'state',
          micOn: false,
          cameraOn: false,
          screenOn: false,
        },
      ],
    ])
  })

  it('对端的开关随 state 信令更新；别的通话的、自己发的、读不懂的都不理', async () => {
    const { store, session } = await connected('Video')
    expect([store.peers[0]?.micOn, store.peers[0]?.cameraOn, store.peers[0]?.screenOn]).toStrictEqual([true, true, false])

    store.applyCallSignal(signal(BOB, {
      type: 'state',
      micOn: false,
      cameraOn: false,
      screenOn: true,
    }))
    expect([store.peers[0]?.micOn, store.peers[0]?.cameraOn, store.peers[0]?.screenOn]).toStrictEqual([false, false, true])

    store.applyCallSignal(signal(BOB, { type: 'offer', sdp: 'v=0' }, 'call-9'))
    store.applyCallSignal(signal(ME, { type: 'offer', sdp: 'v=0' }))
    store.applyCallSignal({ callId: 'call-1', fromUserId: BOB, data: 'garbage' })
    expect(session.handled).toStrictEqual([])
  })

  it('没占着这通电话的页签不处理信令', () => {
    const { store } = setup()
    store.applyCallChanged(incoming())

    store.applyCallSignal(signal(BOB, { type: 'offer', sdp: 'v=0' }))

    expect(fakes.state.sessions).toHaveLength(0)
    expect(store.phase).toBe('incoming')
  })

  it('连接断开：8 秒内恢复不挂，过了才挂并提示中断', async () => {
    const { store, calls, session } = await connected()

    session.options.onStateChange('disconnected')
    expect(store.peers[0]?.connection).toBe('connecting')
    await vi.advanceTimersByTimeAsync(7000)
    session.options.onStateChange('connected')
    await vi.advanceTimersByTimeAsync(5000)
    expect(store.phase).toBe('connected')

    session.options.onStateChange('disconnected')
    await vi.advanceTimersByTimeAsync(8000)

    expect(store.phase).toBe('idle')
    expect(store.notice).toStrictEqual({ kind: 'interrupted', peerName: 'bob', group: false })
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
    never.store.applyCallChanged(answered())
    await settle()
    sessionWith(0).options.onStateChange('failed')
    expect(never.store.notice?.kind).toBe('connect-failed')
    expect(never.calls.at(-1)).toBe('hangupCall:call-1')
  })

  it('接通后 30 秒媒体还没连上即挂断；连上了就不再管', async () => {
    const { store, calls } = setup()
    await store.start('conv-1', 'Voice')
    store.applyCallChanged(answered())
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

    store.applyCallChanged(answered())
    await settle()

    expect([store.phase, store.notice?.kind]).toStrictEqual(['idle', 'connect-failed'])
    expect(calls).toStrictEqual(['startCall:conv-1:Voice', 'callIceServers', 'hangupCall:call-1'])
    expect(fakes.state.sessions).toHaveLength(0)
  })

  it('对端的信令处理不了：这条连接作废，只有他一个人时收场', async () => {
    const { store } = setup()
    await store.start('conv-1', 'Voice')
    store.applyCallChanged(answered())
    await settle()
    sessionWith(0).failHandle = true

    store.applyCallSignal(signal(BOB, { type: 'offer', sdp: 'broken' }))
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
      store.peers,
      store.connectedAt,
    ]).toStrictEqual(['idle', null, [], null])
    expect([
      session.closed,
      stream.tracks.every(track => track.stopped),
    ]).toStrictEqual([true, true])
    expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBeNull()

    store.applyCallChanged(answered({ status: 'Ended', endReason: 'Completed' }))
    expect(store.notice).toBeNull()
  })

  it('对端挂断：收场不提示；对端掉线记的中断要提示；对方拒绝要提示', async () => {
    const hungUp = await connected()
    hungUp.store.applyCallChanged(answered({ status: 'Ended', endReason: 'Completed' }))
    expect([hungUp.store.phase, hungUp.store.notice, hungUp.session.closed]).toStrictEqual(['idle', null, true])

    fakes.state.sessions.length = 0
    const dropped = await connected()
    dropped.store.applyCallChanged(answered({ status: 'Ended', endReason: 'Interrupted' }))
    expect(dropped.store.notice).toStrictEqual({ kind: 'interrupted', peerName: 'bob', group: false })

    const rejected = setup()
    await rejected.store.start('conv-1', 'Voice')
    rejected.store.applyCallChanged(outgoing({ status: 'Ended', endReason: 'Rejected' }))
    expect(rejected.store.notice).toStrictEqual({ kind: 'rejected', peerName: 'bob', group: false })
  })

  it('换设备：新轨道沿用当前的开关，换到连接的位上与本端媒体里，旧的停掉', async () => {
    const { store, session, stream } = await connected('Video')
    const [oldMic, oldCamera] = stream.tracks
    store.setCameraOn(false)

    await store.switchDevice('audioinput', 'mic-2')
    await store.switchDevice('videoinput', 'cam-2')

    expect(session.tracks.map(([slot, track]) => [slot, track?.kind, track?.enabled])).toStrictEqual([
      ['mic', 'audio', true],
      ['camera', 'video', false],
    ])
    expect([oldMic?.stopped, oldCamera?.stopped]).toStrictEqual([true, true])
    expect(stream.tracks).toHaveLength(2)
    expect(stream.tracks.every(track => !track.stopped)).toBe(true)
  })

  describe('屏幕共享', () => {
    it('开始：屏幕的轨道换到每条连接的屏幕位上，告诉所有人；停止：位上清空、轨道停掉、再告诉一次', async () => {
      const { store, session } = await connected()

      await store.startScreenShare()

      const screen = fakes.state.screens[0]!
      expect([store.screenOn, store.screenStream]).toStrictEqual([true, screen])
      expect(session.tracks).toStrictEqual([
        ['screen', screen.tracks[0]],
      ])
      expect(signalsSent()).toStrictEqual([
        [
          'CallSignal',
          'call-1',
          null,
          {
            type: 'state',
            micOn: true,
            cameraOn: false,
            screenOn: true,
          },
        ],
      ])

      store.stopScreenShare()

      expect([store.screenOn, screen.tracks[0]?.stopped]).toStrictEqual([false, true])
      expect(session.tracks.at(-1)).toStrictEqual(['screen', null])
      expect(signalsSent().at(-1)?.[3]).toStrictEqual({
        type: 'state',
        micOn: true,
        cameraOn: false,
        screenOn: false,
      })
    })

    it('用户在选择框里取消：什么都不做；取不到的原样抛出', async () => {
      const { store, session } = await connected()
      fakes.state.screen = 'cancel'

      await store.startScreenShare()

      expect([
        store.screenOn,
        session.tracks,
        signalsSent(),
      ]).toStrictEqual([false, [], []])

      fakes.state.screen = new ChatCallMediaError('busy', 'Video')
      await expect(store.startScreenShare()).rejects.toMatchObject({ failure: 'busy' })
      expect(store.screenOn).toBe(false)
    })

    it('用户从浏览器自己的条上停掉：跟着停止共享', async () => {
      const { store, session } = await connected()
      await store.startScreenShare()

      fakes.state.screens[0]?.tracks[0]?.listeners.ended?.forEach(listener => listener())

      expect(store.screenOn).toBe(false)
      expect(session.tracks.at(-1)).toStrictEqual(['screen', null])
    })

    it('正在共享时不再取一次；没占着通话的不能共享', async () => {
      const { store } = await connected()
      await store.startScreenShare()
      await store.startScreenShare()
      expect(fakes.state.screens).toHaveLength(1)

      const idle = setup()
      idle.store.applyCallChanged(incoming())
      await idle.store.startScreenShare()
      expect(fakes.state.screens).toHaveLength(1)
    })

    it('挂断时连同共享的屏幕一起放掉', async () => {
      const { store } = await connected()
      await store.startScreenShare()

      await store.hangup()

      expect([store.screenOn, fakes.state.screens[0]?.tracks[0]?.stopped]).toStrictEqual([false, true])
    })
  })
})

describe('多人通话', () => {
  /** 我在群里发起，Bob 已经接了、Carol 还在响 */
  async function started() {
    const context = setup()
    await context.store.start('conv-g', 'Voice', [BOB, CAROL])
    context.store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Invited')))
    await settle()
    fakes.state.hub.length = 0
    return context
  }

  it('发起带上点名邀请的人；每进来一个人各建一条连接，后加入的一方出 offer', async () => {
    const { store, calls } = setup()

    await store.start('conv-g', 'Voice', [BOB, CAROL])
    expect(calls).toStrictEqual(['startCall:conv-g:Voice:bob,carol'])
    expect([store.phase, store.peers]).toStrictEqual(['outgoing', []])

    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Invited')))
    await settle()
    expect(store.peers.map(peer => peer.userId)).toStrictEqual([BOB])
    expect(fakes.state.sessions.map(session => session.options.offerer)).toStrictEqual([false])

    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Joined', T2)))
    await settle()
    expect(store.peers.map(peer => peer.userId)).toStrictEqual([BOB, CAROL])
    expect(fakes.state.sessions.map(session => session.options.offerer)).toStrictEqual([false, false])
    // 各人只收到指给自己的开关状态
    expect(signalsSent().map(sent => sent[2])).toStrictEqual([BOB, CAROL])
  })

  it('信令各走各的连接；还没看到他加入的人发来的先存着，等他进来再给', async () => {
    const { store } = await started()

    store.applyCallSignal(signal(BOB, { type: 'offer', sdp: 'from-bob' }, 'call-g'))
    store.applyCallSignal(signal(CAROL, { type: 'offer', sdp: 'from-carol' }, 'call-g'))
    expect(sessionWith(0).handled).toStrictEqual([
      { type: 'offer', sdp: 'from-bob' },
    ])
    expect(fakes.state.sessions).toHaveLength(1)

    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Joined', T2)))
    await settle()

    expect(sessionWith(1).handled).toStrictEqual([
      { type: 'offer', sdp: 'from-carol' },
    ])
    expect(sessionWith(0).handled).toHaveLength(1)
  })

  it('有人离开：只关与他的那条连接，通话照常；里面只剩我而还有人在响时等着', async () => {
    const { store, calls } = await started()
    sessionWith(0).options.onStateChange('connected')
    expect(store.phase).toBe('connected')

    store.applyCallChanged(mine(participant(BOB, 'Left', T1), participant(CAROL, 'Invited')))

    expect(sessionWith(0).closed).toBe(true)
    expect([store.phase, store.peers, store.engaged]).toStrictEqual(['waiting', [], true])
    expect(calls).toStrictEqual(['startCall:conv-g:Voice:bob,carol', 'callIceServers'])
  })

  it('有人离开又回来（加入的时刻变了）：旧连接作废、重新建；同一次加入的时刻读到的精度不同不算', async () => {
    const { store } = await started()

    store.applyCallChanged(mine(participant(BOB, 'Joined', '2026-11-14T08:00:05.0004567Z'), participant(CAROL, 'Invited')))
    await settle()
    expect(fakes.state.sessions).toHaveLength(1)
    expect(sessionWith(0).closed).toBe(false)

    store.applyCallChanged(mine(participant(BOB, 'Joined', T3), participant(CAROL, 'Invited')))
    await settle()

    expect(fakes.state.sessions).toHaveLength(2)
    expect([sessionWith(0).closed, sessionWith(1).closed]).toStrictEqual([true, false])
    expect(store.peers.map(peer => [peer.userId, peer.connection])).toStrictEqual([
      [BOB, 'connecting'],
    ])
  })

  it('与一个人连不上：他那一格标成连不上，别人照常；与所有人都连不上才收场', async () => {
    const { store, calls } = await started()
    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Joined', T2)))
    await settle()
    sessionWith(1).options.onStateChange('connected')

    sessionWith(0).options.onStateChange('failed')

    expect(store.peers.map(peer => [peer.userId, peer.connection])).toStrictEqual([
      [BOB, 'failed'],
      [CAROL, 'connected'],
    ])
    expect([store.phase, sessionWith(0).closed, store.notice]).toStrictEqual(['connected', true, null])

    // 连不上的人再发来的信令不理、不存
    store.applyCallSignal(signal(BOB, { type: 'offer', sdp: 'late' }, 'call-g'))
    expect(sessionWith(0).handled).toStrictEqual([])

    sessionWith(1).options.onStateChange('failed')

    expect(store.phase).toBe('idle')
    expect(store.notice).toStrictEqual({ kind: 'interrupted', peerName: 'me', group: true })
    expect(calls.at(-1)).toBe('hangupCall:call-g')
  })

  it('服务端把我记成离开了（它以为我掉线了）：这个页签退出来并提示中断', async () => {
    const { store } = await started()

    store.applyCallChanged(group([
      participant(ME, 'Left', START),
      participant(BOB, 'Joined', T1),
      participant(CAROL, 'Joined', T2),
    ], { status: 'Ongoing' }))

    expect([store.phase, store.localStream, sessionWith(0).closed]).toStrictEqual(['idle', null, true])
    expect(store.notice).toStrictEqual({ kind: 'interrupted', peerName: 'me', group: true })
  })

  it('受邀的都没来：不点谁拒绝了，说没人接；通话正常结束不提示', async () => {
    const declined = setup()
    await declined.store.start('conv-g', 'Voice', [BOB, CAROL])
    declined.store.applyCallChanged(mine(participant(BOB, 'Rejected'), participant(CAROL, 'Rejected'), { status: 'Ended', endReason: 'Rejected' }))
    expect(declined.store.notice).toStrictEqual({ kind: 'no-answer', peerName: 'me', group: true })

    const { store } = await started()
    store.applyCallChanged(mine(participant(BOB, 'Left', T1), participant(CAROL, 'Missed'), { status: 'Ended', endReason: 'Completed' }))
    expect([store.phase, store.notice]).toStrictEqual(['idle', null])
  })

  describe('加入', () => {
    const target = group([
      participant(BOB, 'Joined', START),
      participant(CAROL, 'Joined', T1),
    ], { initiatorUserId: BOB, status: 'Ongoing' })
    const joined = {
      ...target,
      participants: [
        ...target.participants,
        participant(ME, 'Joined', T2),
      ],
    }

    it('先取设备再告诉服务端：与里面的每个人各建一条连接、都由我出 offer，穿透服务器合取一次', async () => {
      const { store, calls } = setup({
        joinCall: async () => joined,
      })

      await store.join(target)
      await settle()

      expect(fakes.state.acquired).toStrictEqual(['Voice'])
      expect(calls).toStrictEqual(['joinCall:call-g', 'callIceServers'])
      expect([
        store.phase,
        store.engaged,
        sessionStorage.getItem(CHAT_CALL_SESSION_KEY),
      ]).toStrictEqual(['connecting', true, 'call-g'])
      expect(fakes.state.sessions.map(session => [session.options.offerer, session.started])).toStrictEqual([
        [true, 1],
        [true, 1],
      ])
    })

    it('加入的响应没到、先到的推送比它早（我还是上次离开的样子）：等着，不当成被请出去；响应来了也不再用它', async () => {
      let release: (item: ChatCallItem) => void = () => undefined
      const { store } = setup({
        joinCall: () => new Promise(resolve => (release = resolve)),
      })
      const before = {
        ...target,
        participants: [
          ...target.participants,
          participant(ME, 'Left', T1),
        ],
      }

      const joining = store.join(before)
      await settle()
      store.applyCallChanged(before)
      release(joined)
      await joining
      await settle()
      // 响应没用上：还在等自己已加入的那条推送
      expect([store.phase, store.engaged, fakes.state.sessions.length]).toStrictEqual(['connecting', true, 0])

      // 比我那一步还早的推送接着到：照样不当成被请出去
      store.applyCallChanged(before)
      expect([store.engaged, store.notice]).toStrictEqual([true, null])

      store.applyCallChanged(joined)
      await settle()
      expect(fakes.state.sessions).toHaveLength(2)

      // 之后再被记成离开才是真的被请出去了
      store.applyCallChanged(before)
      expect([store.phase, store.notice?.kind]).toStrictEqual(['idle', 'interrupted'])
    })

    it('进不去（人数满了、已经结束）：放掉设备、不再占着，错误原样抛出', async () => {
      const { store } = setup({
        joinCall: () => Promise.reject(new ApiError(409, { code: 'CHAT_CONFLICT_008', detail: '通话人数已满。' })),
      })

      await expect(store.join(target)).rejects.toMatchObject({ code: 'CHAT_CONFLICT_008' })

      expect([store.phase, store.engaged, store.localStream]).toStrictEqual(['idle', false, null])
      expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)
      expect(sessionStorage.getItem(CHAT_CALL_SESSION_KEY)).toBeNull()
    })

    it('它正是眼前在响的来电：加入即接听；眼前有别的通话时不加入', async () => {
      const ringing = group([
        participant(BOB, 'Joined', START),
        participant(ME, 'Invited'),
      ], { initiatorUserId: BOB })
      const invited = setup({
        acceptCall: async () => ({
          ...ringing,
          status: 'Ongoing',
          participants: [
            participant(BOB, 'Joined', START),
            participant(ME, 'Joined', T1),
          ],
        }),
      })
      invited.store.applyCallChanged(ringing)

      await invited.store.join(ringing)

      expect(invited.calls[0]).toBe('acceptCall:call-g')
      expect(invited.store.engaged).toBe(true)

      const busy = setup()
      await busy.store.start('conv-1', 'Voice')
      await busy.store.join(target)
      expect(busy.calls).toStrictEqual(['startCall:conv-1:Voice'])
    })
  })

  describe('会话里还没结束的通话', () => {
    const theirs = group([
      participant(BOB, 'Joined', START),
      participant(CAROL, 'Invited'),
    ], { initiatorUserId: BOB })

    it('群通话的推送记到会话名下，结束了拿掉；一对一的不记；不是我的通话不出来电', () => {
      const { store } = setup()

      store.applyCallChanged(theirs)
      store.applyCallChanged(incoming({ callId: 'single', status: 'Ended', endReason: 'Missed' }))
      expect(Object.keys(store.ongoingCalls)).toStrictEqual(['conv-g'])
      expect(store.phase).toBe('idle')

      store.applyCallChanged({ ...theirs, status: 'Ended', endReason: 'Cancelled' })
      expect(store.ongoingCalls).toStrictEqual({})
    })

    it('打开群会话时取一遍：有就记下，没有就拿掉；没有通话读权限不问', async () => {
      let found: ChatCallItem | null = theirs
      const { store, calls } = setup({
        ongoingCall: async () => ({ call: found, now: START }),
      })

      await store.loadOngoing('conv-g')
      expect(store.ongoingCalls['conv-g']?.callId).toBe('call-g')

      found = null
      await store.loadOngoing('conv-g')
      expect(store.ongoingCalls).toStrictEqual({})
      expect(calls).toStrictEqual(['ongoingCall:conv-g', 'ongoingCall:conv-g'])

      const denied = setup({}, [CHAT_PERMISSIONS.read])
      await denied.store.loadOngoing('conv-g')
      expect(denied.calls).toStrictEqual([])
    })

    it('取的时候推送先到了：以推送为准', async () => {
      let release: (result: { call: ChatCallItem | null, now: string }) => void = () => undefined
      const { store } = setup({
        ongoingCall: () => new Promise(resolve => (release = resolve)),
      })

      const loading = store.loadOngoing('conv-g')
      store.applyCallChanged({ ...theirs, status: 'Ended', endReason: 'Cancelled' })
      release({ call: theirs, now: START })
      await loading

      expect(store.ongoingCalls).toStrictEqual({})
    })

    it('换一个会话看：离开的那个不再有推送，不是我的通话就丢掉；我在里面的留着；重连之后重取', async () => {
      const { store, calls } = setup({
        ongoingCall: async conversationId => ({
          call: conversationId === 'conv-g'
            ? theirs
            : null,
          now: START,
        }),
      })

      await store.watchConversation('conv-g')
      expect(Object.keys(store.ongoingCalls)).toStrictEqual(['conv-g'])
      await store.watchConversation('conv-h')
      expect(store.ongoingCalls).toStrictEqual({})

      // 我是这通电话的参与者（哪怕已经离开）：照样收得到推送，留着
      await store.watchConversation('conv-g')
      store.applyCallChanged(group([
        participant(BOB, 'Joined', START),
        participant(ME, 'Left', T1),
        participant(CAROL, 'Joined', T2),
      ], { initiatorUserId: BOB, status: 'Ongoing' }))
      await store.watchConversation(null)
      expect(Object.keys(store.ongoingCalls)).toStrictEqual(['conv-g'])

      calls.length = 0
      await store.watchConversation('conv-h')
      await store.restore()
      expect(calls).toStrictEqual(['ongoingCall:conv-h', 'ongoingCall:conv-h', 'activeCall'])
    })

    it('通话的设置取回来记着', async () => {
      const { store } = setup()

      await expect(store.loadSettings()).resolves.toStrictEqual({ ringSeconds: 60, maxVoiceParticipants: 8, maxVideoParticipants: 6 })

      expect(store.settings?.maxVideoParticipants).toBe(6)
    })
  })

  it('正在共享屏幕时进来的人：建连接时屏幕位上直接带着屏幕的轨道', async () => {
    const { store } = await started()
    await store.startScreenShare()

    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Joined', T2)))
    await settle()

    expect(sessionWith(0).tracks).toStrictEqual([
      ['screen', fakes.state.screens[0]?.tracks[0]],
    ])
    expect(sessionWith(1).options.tracks.screen).toBe(fakes.state.screens[0]?.tracks[0])
    expect(signalsSent().at(-1)).toStrictEqual([
      'CallSignal',
      'call-g',
      CAROL,
      {
        type: 'state',
        micOn: true,
        cameraOn: false,
        screenOn: true,
      },
    ])
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

  it('还在等我答复的来电认回来，剩余时间按服务端的钟算', async () => {
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
        call: answered(),
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
        call: accepted(),
        now: START,
      }),
    })
    await ongoing.store.restore()
    expect([ongoing.store.phase, ongoing.calls]).toStrictEqual([
      'idle',
      ['activeCall'],
    ])

    const elsewhere = setup({
      activeCall: async () => ({
        call: outgoing(),
        now: START,
      }),
    })
    await elsewhere.store.restore()
    expect([elsewhere.store.phase, elsewhere.calls]).toStrictEqual([
      'idle',
      ['activeCall'],
    ])
  })

  it('对的时候推送先到了：以推送为准，不拿取回来的旧状态覆盖', async () => {
    let release: (result: { call: ChatCallItem, now: string }) => void = () => undefined
    const { store } = setup({
      activeCall: () => new Promise(resolve => (release = resolve)),
    })
    await store.start('conv-g', 'Voice', [BOB, CAROL])
    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Invited')))
    await settle()

    const restoring = store.restore()
    store.applyCallChanged(mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Joined', T2)))
    release({
      call: mine(participant(BOB, 'Joined', T1), participant(CAROL, 'Invited')),
      now: START,
    })
    await restoring
    await settle()

    expect(store.peers.map(peer => peer.userId)).toStrictEqual([BOB, CAROL])
    expect(fakes.state.sessions.map(session => session.closed)).toStrictEqual([false, false])
  })

  it('还是眼前这一通：以服务端的状态为准，漏掉的接通推送补上', async () => {
    const { store } = setup({
      activeCall: async () => ({
        call: answered(),
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
    await store.start('conv-g', 'Voice', [BOB])
    store.notice = { kind: 'rejected', peerName: 'bob', group: false }

    store.reset()

    expect(calls).toStrictEqual(['startCall:conv-g:Voice:bob'])
    expect([
      store.phase,
      store.engaged,
      store.localStream,
      store.notice,
      store.ongoingCalls,
    ]).toStrictEqual(['idle', false, null, null, {}])
    expect(fakes.state.streams[0]?.tracks.every(track => track.stopped)).toBe(true)
  })
})
