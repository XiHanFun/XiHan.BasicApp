/**
 * useSignalR 连接管理单元测试。
 * 职责：用假 Hub 连接注入（绝不真连），锁定「按 hubPath 各持一条连接」
 * 「无 token 不发起连接」「已连接不重复建连」「事件订阅在重建连接时被重新绑定」
 * 「negotiate 401 借道刷新后只重试一次」「渐进式重连延迟与登出后放弃重连」
 * 以及 stop / destroy 的清理彻底性。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TOKEN_KEY } from '~/constants'
import { LocalStorage } from '~/utils'

interface ReconnectOptions {
  nextRetryDelayInMilliseconds: (ctx: { previousRetryCount: number }) => null | number
}

interface UrlOptions {
  accessTokenFactory: () => string
  transport: number
}

const hoisted = vi.hoisted(() => ({
  connections: [] as Array<Record<string, unknown>>,
  startAttempts: 0,
  startBehavior: null as null | ((attempt: number) => Promise<void>),
}))

vi.mock('@microsoft/signalr', () => {
  const HubConnectionState = {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  }

  class FakeConnection {
    state: string = HubConnectionState.Disconnected
    url = ''
    urlOptions: UrlOptions | null = null
    reconnectOptions: ReconnectOptions | null = null
    handlers = new Map<string, Set<(...args: unknown[]) => void>>()
    offAllCalls: string[] = []
    stopCalls = 0
    invokeCalls: Array<{ method: string, args: unknown[] }> = []
    lifecycle: Record<string, () => void> = {}

    on(method: string, handler: (...args: unknown[]) => void) {
      const set = this.handlers.get(method) ?? new Set()
      set.add(handler)
      this.handlers.set(method, set)
    }

    off(method: string, handler?: (...args: unknown[]) => void) {
      if (handler) {
        this.handlers.get(method)?.delete(handler)
      }
      else {
        this.offAllCalls.push(method)
        this.handlers.delete(method)
      }
    }

    onreconnecting(cb: () => void) {
      this.lifecycle.reconnecting = cb
    }

    onreconnected(cb: () => void) {
      this.lifecycle.reconnected = cb
    }

    onclose(cb: () => void) {
      this.lifecycle.close = cb
    }

    async start() {
      hoisted.startAttempts += 1
      if (hoisted.startBehavior) {
        await hoisted.startBehavior(hoisted.startAttempts)
      }
      this.state = HubConnectionState.Connected
    }

    async stop() {
      this.stopCalls += 1
      this.state = HubConnectionState.Disconnected
    }

    async invoke(method: string, ...args: unknown[]) {
      this.invokeCalls.push({ method, args })
      return `${method}:ok`
    }
  }

  class HubConnectionBuilder {
    private connection = new FakeConnection()

    withUrl(url: string, options: UrlOptions) {
      this.connection.url = url
      this.connection.urlOptions = options
      return this
    }

    withAutomaticReconnect(options: ReconnectOptions) {
      this.connection.reconnectOptions = options
      return this
    }

    configureLogging() {
      return this
    }

    build() {
      hoisted.connections.push(this.connection as unknown as Record<string, unknown>)
      return this.connection
    }
  }

  return {
    HubConnectionState,
    HubConnectionBuilder,
    HttpTransportType: { WebSockets: 1, ServerSentEvents: 2, LongPolling: 4 },
    LogLevel: { Information: 1, Warning: 3 },
  }
})

const refreshSessionToken = vi.hoisted(() => vi.fn<() => Promise<null | string>>())
vi.mock('~/request', () => ({ refreshSessionToken }))

interface FakeConn {
  state: string
  url: string
  urlOptions: UrlOptions
  reconnectOptions: ReconnectOptions
  handlers: Map<string, Set<(...args: unknown[]) => void>>
  offAllCalls: string[]
  stopCalls: number
  invokeCalls: Array<{ method: string, args: unknown[] }>
  lifecycle: Record<string, () => void>
}

/** 每个用例一份全新模块状态：instances 注册表是模块级的 */
async function loadModule() {
  vi.resetModules()
  hoisted.connections.length = 0
  hoisted.startAttempts = 0
  hoisted.startBehavior = null
  return import('./useSignalR')
}

function lastConnection(): FakeConn {
  return hoisted.connections.at(-1) as unknown as FakeConn
}

beforeEach(() => {
  refreshSessionToken.mockReset()
  vi.spyOn(console, 'warn').mockImplementation(() => {})
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('useSignalR 建连前置条件', () => {
  it('本地没有 token 时不发起连接，避免必然 401 的 negotiate', async () => {
    const { useSignalR } = await loadModule()
    const hub = useSignalR()

    await hub.start()

    expect(hoisted.connections).toHaveLength(0)
    expect(hub.connection.value).toBeNull()
    expect(hub.connected.value).toBe(false)
  })

  it('有 token 时建连成功，连接标记置真', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()

    await hub.start()

    expect(hoisted.connections).toHaveLength(1)
    expect(hub.connected.value).toBe(true)
  })

  it('已连接时再次 start 直接返回，不重复建连', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()

    await hub.start()
    await hub.start()

    expect(hoisted.connections).toHaveLength(1)
    expect(hoisted.startAttempts).toBe(1)
  })

  it('accessTokenFactory 每次现取本地 token，而不是建连时的快照', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    await useSignalR().start()

    LocalStorage.set(TOKEN_KEY, 'tok-2')

    expect(lastConnection().urlOptions.accessTokenFactory()).toBe('tok-2')
  })

  it('token 被清空后 accessTokenFactory 返回空串而不是 null', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    await useSignalR().start()

    LocalStorage.remove(TOKEN_KEY)

    expect(lastConnection().urlOptions.accessTokenFactory()).toBe('')
  })
})

describe('useSignalR Hub 地址拼接', () => {
  it('基址末尾的 API 前缀被剥掉后再拼 Hub 路径', async () => {
    vi.stubEnv('VITE_API_BASE_URL', 'http://localhost:5000/api')
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    await useSignalR().start()

    expect(lastConnection().url).toBe('http://localhost:5000/hubs/notification')
  })

  it('基址末尾多余的斜杠被清理，不产生双斜杠', async () => {
    vi.stubEnv('VITE_API_BASE_URL', 'http://localhost:5000/api///')
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    await useSignalR().start()

    expect(lastConnection().url).toBe('http://localhost:5000/hubs/notification')
  })

  it('基址为空时退化为同源相对路径', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '')
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    await useSignalR().start()

    expect(lastConnection().url).toBe('/hubs/notification')
  })

  it('传入的 Hub 路径缺少前导斜杠时自动补齐', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '')
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    await useSignalR('hubs/chat').start()

    expect(lastConnection().url).toBe('/hubs/chat')
  })
})

describe('useSignalR 多 Hub 隔离', () => {
  it('不同 hubPath 各持一条独立连接与独立连接标记', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    const notification = useSignalR('/hubs/notification')
    const chat = useSignalR('/hubs/chat')
    await notification.start()

    expect(notification.connected.value).toBe(true)
    expect(chat.connected.value).toBe(false)
    expect(hoisted.connections).toHaveLength(1)
  })

  it('归一化后的同一路径复用同一实例状态', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    await useSignalR('hubs/chat').start()

    expect(useSignalR('/hubs/chat').connected.value).toBe(true)
  })

  it('一个 Hub 的订阅不会绑到另一个 Hub 上', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const handler = vi.fn()

    useSignalR('/hubs/chat').on('Message', handler)
    await useSignalR('/hubs/notification').start()

    expect(lastConnection().handlers.has('Message')).toBe(false)
  })
})

describe('useSignalR 事件订阅', () => {
  it('建连前注册的订阅在建连时被重新绑定到新连接', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    const handler = vi.fn()

    hub.on('Notify', handler)
    await hub.start()

    expect(lastConnection().handlers.get('Notify')?.has(handler)).toBe(true)
  })

  it('同一事件可注册多个处理器，全部被绑定', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    const first = vi.fn()
    const second = vi.fn()

    hub.on('Notify', first)
    hub.on('Notify', second)
    await hub.start()

    expect(lastConnection().handlers.get('Notify')?.size).toBe(2)
  })

  it('建连后注册的订阅立即绑到现有连接上', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    await hub.start()
    const handler = vi.fn()

    hub.on('Notify', handler)

    expect(lastConnection().handlers.get('Notify')?.has(handler)).toBe(true)
  })

  it('off 传处理器时只摘掉该处理器，其余保留', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    const first = vi.fn()
    const second = vi.fn()
    hub.on('Notify', first)
    hub.on('Notify', second)
    await hub.start()

    hub.off('Notify', first)

    expect(lastConnection().handlers.get('Notify')?.has(first)).toBe(false)
    expect(lastConnection().handlers.get('Notify')?.has(second)).toBe(true)
  })

  it('off 不传处理器时摘掉该事件的全部订阅', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    hub.on('Notify', vi.fn())
    await hub.start()

    hub.off('Notify')

    expect(lastConnection().offAllCalls).toContain('Notify')
    expect(lastConnection().handlers.has('Notify')).toBe(false)
  })

  it('未连接时 off 不抛错，仅清掉登记表', async () => {
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    const handler = vi.fn()
    hub.on('Notify', handler)

    expect(() => hub.off('Notify', handler)).not.toThrow()
  })

  it('被 off 掉的订阅不会在重建连接时复活', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    const handler = vi.fn()

    hub.on('Notify', handler)
    hub.off('Notify')
    await hub.start()

    expect(lastConnection().handlers.has('Notify')).toBe(false)
  })

  it('服务端 Connected 事件被空处理器占位，避免控制台警告', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()

    await useSignalR().start()

    expect(lastConnection().handlers.has('Connected')).toBe(true)
  })
})

describe('useSignalR 连接生命周期回调', () => {
  it('重连中把连接标记置假，重连成功后置真', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    await hub.start()

    lastConnection().lifecycle.reconnecting?.()
    expect(hub.connected.value).toBe(false)

    lastConnection().lifecycle.reconnected?.()
    expect(hub.connected.value).toBe(true)
  })

  it('连接关闭时连接标记置假', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    await hub.start()

    lastConnection().lifecycle.close?.()

    expect(hub.connected.value).toBe(false)
  })
})

describe('useSignalR 重连退避策略', () => {
  it('重连延迟按 1s/2s/5s/10s/30s 渐进', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    await useSignalR().start()
    const next = lastConnection().reconnectOptions.nextRetryDelayInMilliseconds

    expect([0, 1, 2, 3, 4].map(n => next({ previousRetryCount: n })))
      .toEqual([1000, 2000, 5000, 10000, 30000])
  })

  it('重试次数超出档位后固定停在 30s，不再增长', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    await useSignalR().start()
    const next = lastConnection().reconnectOptions.nextRetryDelayInMilliseconds

    expect(next({ previousRetryCount: 99 })).toBe(30000)
  })

  it('token 已清除（已登出）时放弃重连', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    await useSignalR().start()
    const next = lastConnection().reconnectOptions.nextRetryDelayInMilliseconds

    LocalStorage.remove(TOKEN_KEY)

    expect(next({ previousRetryCount: 0 })).toBeNull()
  })
})

describe('useSignalR negotiate 401 处理', () => {
  it('带 statusCode 401 的失败会刷新令牌并重试一次', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    hoisted.startBehavior = async (attempt) => {
      if (attempt === 1) {
        throw Object.assign(new Error('Unauthorized'), { statusCode: 401 })
      }
    }
    refreshSessionToken.mockResolvedValue('tok-2')
    const hub = useSignalR()

    await hub.start()

    expect(refreshSessionToken).toHaveBeenCalledTimes(1)
    expect(hoisted.startAttempts).toBe(2)
    expect(hub.connected.value).toBe(true)
  })

  it('错误信息里带 401 字样时同样触发刷新重试', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    hoisted.startBehavior = async (attempt) => {
      if (attempt === 1) {
        throw new Error('Failed to complete negotiation with the server: 401')
      }
    }
    refreshSessionToken.mockResolvedValue('tok-2')

    await useSignalR().start()

    expect(refreshSessionToken).toHaveBeenCalledTimes(1)
  })

  it('刷新失败（返回空）时不再重试，避免拿过期 token 无限 negotiate', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    hoisted.startBehavior = async () => {
      throw Object.assign(new Error('Unauthorized'), { statusCode: 401 })
    }
    refreshSessionToken.mockResolvedValue(null)
    const hub = useSignalR()

    await hub.start()

    expect(hoisted.startAttempts).toBe(1)
    expect(hub.connected.value).toBe(false)
  })

  it('刷新后重试仍 401 时只重试一次就收手', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    hoisted.startBehavior = async () => {
      throw Object.assign(new Error('Unauthorized'), { statusCode: 401 })
    }
    refreshSessionToken.mockResolvedValue('tok-2')

    await useSignalR().start()

    expect(hoisted.startAttempts).toBe(2)
    expect(refreshSessionToken).toHaveBeenCalledTimes(1)
  })

  it('非 401 的建连失败不触发令牌刷新，连接被清空', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    hoisted.startBehavior = async () => {
      throw new Error('ECONNREFUSED')
    }
    const hub = useSignalR()

    await hub.start()

    expect(refreshSessionToken).not.toHaveBeenCalled()
    expect(hub.connection.value).toBeNull()
    expect(hub.connected.value).toBe(false)
  })

  it('建连失败后连接被清空，因此下一次 start 会重新建连', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    hoisted.startBehavior = async (attempt) => {
      if (attempt === 1) {
        throw new Error('ECONNREFUSED')
      }
    }
    const hub = useSignalR()

    await hub.start()
    await hub.start()

    expect(hoisted.connections).toHaveLength(2)
    expect(hub.connected.value).toBe(true)
  })
})

describe('useSignalR 调用与清理', () => {
  it('未连接时 invoke 抛错并点名 Hub 与方法，供调用方降级', async () => {
    const { useSignalR } = await loadModule()

    await expect(useSignalR('/hubs/chat').invoke('SendMessage')).rejects.toThrow(/未连接/)
    await expect(useSignalR('/hubs/chat').invoke('SendMessage')).rejects.toThrow(/SendMessage/)
  })

  it('已连接时 invoke 透传方法名与参数', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    await hub.start()

    const result = await hub.invoke<string>('SendMessage', 'room-1', 42)

    expect(lastConnection().invokeCalls).toEqual([{ method: 'SendMessage', args: ['room-1', 42] }])
    expect(result).toBe('SendMessage:ok')
  })

  it('stop 关闭底层连接并清空连接引用与标记', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    await hub.start()
    const conn = lastConnection()

    await hub.stop()

    expect(conn.stopCalls).toBe(1)
    expect(hub.connection.value).toBeNull()
    expect(hub.connected.value).toBe(false)
  })

  it('未连接时 stop 静默返回，不抛错', async () => {
    const { useSignalR } = await loadModule()

    await expect(useSignalR().stop()).resolves.toBeUndefined()
  })

  it('底层 stop 抛错被吞掉，状态照样清干净', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    await hub.start()
    const conn = lastConnection() as unknown as { stop: () => Promise<void> }
    conn.stop = () => Promise.reject(new Error('stop failed'))

    await hub.stop()

    expect(hub.connection.value).toBeNull()
    expect(hub.connected.value).toBe(false)
  })

  it('destroy 同时清空订阅登记表与连接，重连后不会复活旧订阅', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR } = await loadModule()
    const hub = useSignalR()
    hub.on('Notify', vi.fn())
    await hub.start()

    await hub.destroy()
    await hub.start()

    expect(hub.connection.value).not.toBeNull()
    expect(lastConnection().handlers.has('Notify')).toBe(false)
  })

  it('destroyAllSignalRConnections 关掉全部已注册的 Hub', async () => {
    LocalStorage.set(TOKEN_KEY, 'tok-1')
    const { useSignalR, destroyAllSignalRConnections } = await loadModule()
    const notification = useSignalR('/hubs/notification')
    const chat = useSignalR('/hubs/chat')
    await notification.start()
    await chat.start()

    await destroyAllSignalRConnections()

    expect(notification.connected.value).toBe(false)
    expect(chat.connected.value).toBe(false)
    expect(notification.connection.value).toBeNull()
    expect(chat.connection.value).toBeNull()
  })

  it('从未建连过时 destroyAllSignalRConnections 也不抛错', async () => {
    const { destroyAllSignalRConnections } = await loadModule()

    await expect(destroyAllSignalRConnections()).resolves.toBeUndefined()
  })
})
