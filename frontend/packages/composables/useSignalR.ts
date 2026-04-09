import type { HubConnection } from '@microsoft/signalr'
import {
  HttpTransportType,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { ref, shallowRef } from 'vue'
import { TOKEN_KEY } from '~/constants'
import { LocalStorage } from '~/utils'

export type SignalREventHandler = (...args: any[]) => void

const connection = shallowRef<HubConnection | null>(null)
const connected = ref(false)

let eventHandlers: Map<string, Set<SignalREventHandler>> = new Map()

/**
 * SignalR 连接管理（全局单例）
 * Hub 路径默认 /hubs/notification，认证时自动携带 JWT
 */
export function useSignalR(hubPath = '/hubs/notification') {
  function buildHubUrl(): string {
    const base = import.meta.env.VITE_API_BASE_URL || ''
    return `${base}${hubPath}`
  }

  async function start() {
    if (connection.value && connection.value.state !== HubConnectionState.Disconnected) {
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
      connected.value = false
    })
    conn.onreconnected(() => {
      connected.value = true
    })
    conn.onclose(() => {
      connected.value = false
    })

    // 服务端推送的 Connected 事件（空处理，避免控制台警告）
    conn.on('Connected', () => {})

    // 重新绑定已注册的事件监听
    for (const [method, handlers] of eventHandlers) {
      for (const handler of handlers) {
        conn.on(method, handler)
      }
    }

    connection.value = conn

    try {
      await conn.start()
      connected.value = true
    }
    catch {
      connected.value = false
    }
  }

  async function stop() {
    if (connection.value) {
      try {
        await connection.value.stop()
      }
      catch {
        // 忽略停止时的错误
      }
      connection.value = null
      connected.value = false
    }
  }

  function on(method: string, handler: SignalREventHandler) {
    if (!eventHandlers.has(method)) {
      eventHandlers.set(method, new Set())
    }
    eventHandlers.get(method)!.add(handler)

    if (connection.value) {
      connection.value.on(method, handler)
    }
  }

  function off(method: string, handler?: SignalREventHandler) {
    if (handler) {
      eventHandlers.get(method)?.delete(handler)
      connection.value?.off(method, handler)
    }
    else {
      eventHandlers.delete(method)
      connection.value?.off(method)
    }
  }

  /**
   * 销毁所有事件监听和连接（登出时调用）
   */
  async function destroy() {
    eventHandlers = new Map()
    await stop()
  }

  return {
    connection,
    connected,
    start,
    stop,
    destroy,
    on,
    off,
  }
}
