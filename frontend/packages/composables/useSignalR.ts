import type { HubConnection } from '@microsoft/signalr'
import type { Ref, ShallowRef } from 'vue'
import {
  HttpTransportType,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { ref, shallowRef } from 'vue'
import { TOKEN_KEY } from '~/constants'
import { refreshSessionToken } from '~/request'
import { LocalStorage } from '~/utils'

export type SignalREventHandler = Parameters<HubConnection['on']>[1]

interface SignalRInstanceState {
  connection: ShallowRef<HubConnection | null>
  connected: Ref<boolean>
  eventHandlers: Map<string, Set<SignalREventHandler>>
}

// 按 hubPath 各持一条连接：通知 /hubs/notification 与聊天 /hubs/chat 互不干扰
const instances = new Map<string, SignalRInstanceState>()

/** negotiate 阶段的 401（token 过期/失效）：signalr 的 HttpError 带 statusCode，包装错误只剩 message */
function isUnauthorizedError(error: unknown): boolean {
  if ((error as { statusCode?: number } | null)?.statusCode === 401) {
    return true
  }
  const message = error instanceof Error ? error.message : String(error ?? '')
  return message.includes('401')
}

function getInstanceState(hubPath: string): SignalRInstanceState {
  let state = instances.get(hubPath)
  if (!state) {
    state = {
      connection: shallowRef<HubConnection | null>(null),
      connected: ref(false),
      eventHandlers: new Map(),
    }
    instances.set(hubPath, state)
  }
  return state
}

/**
 * SignalR 连接管理（按 hubPath 的全局单例注册表）
 * Hub 路径默认 /hubs/notification，认证时自动携带 JWT
 */
export function useSignalR(hubPath = '/hubs/notification') {
  const normalizedHubPath = hubPath.startsWith('/') ? hubPath : `/${hubPath}`
  const state = getInstanceState(normalizedHubPath)

  function normalizeBaseForHub(rawBase: string): string {
    const base = (rawBase || '').trim().replace(/\/+$/g, '')
    if (!base) {
      return ''
    }

    const apiPrefix = (import.meta.env.VITE_API_PREFIX || '/api').trim()
    if (apiPrefix && base.toLowerCase().endsWith(apiPrefix.toLowerCase())) {
      return base.slice(0, base.length - apiPrefix.length).replace(/\/+$/g, '')
    }

    if (base.toLowerCase().endsWith('/api')) {
      return base.slice(0, -4).replace(/\/+$/g, '')
    }

    return base
  }

  function buildHubUrl(): string {
    const base = normalizeBaseForHub(import.meta.env.VITE_API_BASE_URL || '')
    return `${base}${normalizedHubPath}`
  }

  async function start() {
    await startCore(false)
  }

  async function startCore(isRetryAfterRefresh: boolean) {
    if (state.connection.value && state.connection.value.state !== HubConnectionState.Disconnected) {
      return
    }

    // 无 token 时不发起连接，避免 401
    const token = LocalStorage.get<string>(TOKEN_KEY)
    if (!token) {
      return
    }

    const hubUrl = buildHubUrl()

    const conn = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => LocalStorage.get<string>(TOKEN_KEY) ?? '',
        transport:
          HttpTransportType.WebSockets
          | HttpTransportType.ServerSentEvents
          | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // token 已清除（已登出），放弃重连
          if (!LocalStorage.get<string>(TOKEN_KEY)) {
            return null
          }
          // 渐进式重连：1s, 2s, 5s, 10s, 30s
          const delays = [1000, 2000, 5000, 10000, 30000]
          return delays[Math.min(retryContext.previousRetryCount, delays.length - 1)] ?? null
        },
      })
      .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
      .build()

    conn.onreconnecting(() => {
      state.connected.value = false
    })
    conn.onreconnected(() => {
      state.connected.value = true
    })
    conn.onclose(() => {
      state.connected.value = false
    })

    // 服务端推送的 Connected 事件（空处理，避免控制台警告）
    conn.on('Connected', () => {})

    // 重新绑定已注册的事件监听
    for (const [method, handlers] of state.eventHandlers) {
      for (const handler of handlers) {
        conn.on(method, handler)
      }
    }

    state.connection.value = conn

    try {
      await conn.start()
      state.connected.value = true
    }
    catch (error) {
      state.connected.value = false
      state.connection.value = null

      // negotiate 401：token 已过期，借道统一刷新后重试一次；刷新失败时内部已强制登出并清 token，
      // 调用方的 token watch 会随之停掉重连定时器——避免拿着过期 token 无限打 negotiate
      if (!isRetryAfterRefresh && isUnauthorizedError(error)) {
        const nextToken = await refreshSessionToken()
        if (nextToken) {
          await startCore(true)
        }
        return
      }

      if (import.meta.env.DEV) {
        console.warn('[SignalR] 连接失败，请检查 Hub 地址与后端服务状态', {
          hubUrl,
          error,
        })
      }
    }
  }

  async function stop() {
    if (state.connection.value) {
      try {
        await state.connection.value.stop()
      }
      catch {
        // 忽略停止时的错误
      }
      state.connection.value = null
      state.connected.value = false
    }
  }

  function on(method: string, handler: SignalREventHandler) {
    if (!state.eventHandlers.has(method)) {
      state.eventHandlers.set(method, new Set())
    }
    state.eventHandlers.get(method)!.add(handler)

    if (state.connection.value) {
      state.connection.value.on(method, handler)
    }
  }

  function off(method: string, handler?: SignalREventHandler) {
    if (handler) {
      state.eventHandlers.get(method)?.delete(handler)
      state.connection.value?.off(method, handler)
    }
    else {
      state.eventHandlers.delete(method)
      state.connection.value?.off(method)
    }
  }

  /**
   * 调用服务端 Hub 方法（未连接时抛错，调用方自行 catch 降级）
   */
  async function invoke<TResult = unknown>(method: string, ...args: unknown[]): Promise<TResult> {
    const conn = state.connection.value
    if (!conn || conn.state !== HubConnectionState.Connected) {
      throw new Error(`[SignalR] Hub ${normalizedHubPath} 未连接，无法调用 ${method}`)
    }
    return conn.invoke(method, ...args) as Promise<TResult>
  }

  /**
   * 销毁本 Hub 的所有事件监听和连接
   */
  async function destroy() {
    state.eventHandlers = new Map()
    await stop()
  }

  return {
    connection: state.connection,
    connected: state.connected,
    start,
    stop,
    destroy,
    on,
    off,
    invoke,
  }
}

/**
 * 销毁全部 Hub 连接（登出时调用，避免逐个枚举 hubPath）
 */
export async function destroyAllSignalRConnections() {
  const paths = [...instances.keys()]
  await Promise.all(paths.map(path => useSignalR(path).destroy()))
}
