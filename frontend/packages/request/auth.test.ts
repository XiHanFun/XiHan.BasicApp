/**
 * 401 令牌刷新、并发去重与 423 会话锁定单元测试。
 *
 * 职责边界：锁定响应错误拦截器里与身份相关的全部分支——423 拉起锁定遮罩而非登出、
 * 401 触发一次且仅一次的刷新、挂起请求的重放与整体拒绝、刷新端点自身 401 的死循环防护、
 * 强制登出的清理动作与跳转方式，以及旁路刷新入口 refreshSessionToken。
 * 错误文案、信封拆包、请求头与安全签名在同目录其它测试文件里。
 */
import type { AxiosAdapter, AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import type { Mock } from 'vitest'
import type { Router } from 'vue-router'
import type { SessionLockedPayload } from './index'
import { AxiosError, AxiosHeaders } from 'axios'
import { beforeEach, expect, it, vi } from 'vitest'
import { LOGIN_PATH, REFRESH_TOKEN_KEY, TOKEN_KEY } from '~/constants'
import { clearRequestLogs, LocalStorage, useRequestLogs } from '~/utils'
import {
  bindLockHook,
  bindLogoutHook,
  bindRouter,
  refreshSessionToken,
  RequestClient,
} from './index'

const REFRESH_URL = '/api/Auth/RefreshToken'
const logs = useRequestLogs()

let replace: Mock<() => Promise<void>>
let logout: Mock<() => void>
let lock: Mock<(payload?: SessionLockedPayload) => void>

beforeEach(() => {
  replace = vi.fn<() => Promise<void>>(() => Promise.resolve())
  logout = vi.fn<() => void>()
  lock = vi.fn<(payload?: SessionLockedPayload) => void>()
  bindRouter({ replace } as unknown as Router)
  bindLogoutHook(logout)
  bindLockHook(lock)
  clearRequestLogs()
})

it('423 拉起会话锁定遮罩并透传服务端回传的锁定信息', async () => {
  storeTokens()
  const client = makeClient(config => Promise.reject(httpError(423, {
    code: 423,
    data: { reason: 'lockscreen', displayName: '曦寒', avatarUrl: 'https://cdn/x.png' },
  }, config)))

  await client.getFlat('/Sys/Any')

  expect(lock).toHaveBeenCalledTimes(1)
  expect(lock.mock.calls[0]?.[0]).toEqual({
    reason: 'lockscreen',
    displayName: '曦寒',
    avatarUrl: 'https://cdn/x.png',
  })
})

it('423 绝不登出：不清令牌、不跳登录页、不调登出钩子', async () => {
  // 回归锚点：锁定态下身份仍然有效，走 forceLogout 会把用户直接踢下线
  storeTokens()
  const client = makeClient(config => Promise.reject(httpError(423, { data: {} }, config)))

  await client.getFlat('/Sys/Any')

  expect(logout).not.toHaveBeenCalled()
  expect(replace).not.toHaveBeenCalled()
  expect(LocalStorage.get<string>(TOKEN_KEY)).toBe('old-access')
  expect(LocalStorage.get<string>(REFRESH_TOKEN_KEY)).toBe('old-refresh')
})

it('423 也不触发令牌刷新', async () => {
  storeTokens()
  const requested: string[] = []
  const client = makeClient((config) => {
    requested.push(String(config.url))
    return Promise.reject(httpError(423, { data: {} }, config))
  })

  await client.getFlat('/Sys/Any')

  expect(requested).toEqual(['/api/Sys/Any'])
})

it('423 响应体缺字段或字段类型不对时锁定信息统一为 null', async () => {
  const client = makeClient(config => Promise.reject(httpError(423, {
    data: { reason: 42, displayName: null },
  }, config)))
  const bare = makeClient(config => Promise.reject(httpError(423, null, config)))

  await client.getFlat('/Sys/Any')
  await bare.getFlat('/Sys/Any')

  expect(lock.mock.calls.map(call => call[0])).toEqual([
    { reason: null, displayName: null, avatarUrl: null },
    { reason: null, displayName: null, avatarUrl: null },
  ])
})

it('401 且本地没有刷新令牌时直接登出，不发刷新请求', async () => {
  const requested: string[] = []
  const client = makeClient((config) => {
    requested.push(String(config.url))
    return Promise.reject(httpError(401, null, config))
  })

  const { error } = await client.getFlat('/Sys/Any')

  expect(requested).toEqual(['/api/Sys/Any'])
  expect(logout).toHaveBeenCalledTimes(1)
  expect(replace).toHaveBeenCalledWith(LOGIN_PATH)
  expect(error?.message).toBe('登录已过期，请重新登录')
})

it('401 但只存了访问令牌、缺刷新令牌时同样直接登出', async () => {
  LocalStorage.set(TOKEN_KEY, 'old-access')
  const requested: string[] = []
  const client = makeClient((config) => {
    requested.push(String(config.url))
    return Promise.reject(httpError(401, null, config))
  })

  await client.getFlat('/Sys/Any')

  expect(requested).toEqual(['/api/Sys/Any'])
  expect(logout).toHaveBeenCalledTimes(1)
})

it('401 刷新成功后用新令牌重放原请求并返回业务数据', async () => {
  storeTokens()
  const seenAuthorizations: string[] = []
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      return Promise.resolve(ok(config, { isSuccess: true, code: 200, data: { accessToken: 'new-access' } }))
    }
    seenAuthorizations.push(String(config.headers.get('Authorization')))
    return String(config.headers.get('Authorization')) === 'Bearer new-access'
      ? Promise.resolve(ok(config, { isSuccess: true, code: 200, data: { id: 9 } }))
      : Promise.reject(httpError(401, null, config))
  })

  await expect(client.get('/Sys/Detail')).resolves.toEqual({ id: 9 })
  expect(seenAuthorizations).toEqual(['Bearer old-access', 'Bearer new-access'])
  expect(LocalStorage.get<string>(TOKEN_KEY)).toBe('new-access')
  expect(logout).not.toHaveBeenCalled()
})

it('刷新请求带上本地的旧访问令牌与刷新令牌作为请求体', async () => {
  storeTokens()
  let refreshBody: unknown
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      refreshBody = JSON.parse(String(config.data))
      return Promise.resolve(ok(config, { isSuccess: true, data: { accessToken: 'new-access' } }))
    }
    return String(config.headers.get('Authorization')) === 'Bearer new-access'
      ? Promise.resolve(ok(config, { isSuccess: true, data: null }))
      : Promise.reject(httpError(401, null, config))
  })

  await client.get('/Sys/Detail')

  expect(refreshBody).toEqual({ accessToken: 'old-access', refreshToken: 'old-refresh' })
})

it('刷新响应未包裹信封时直接读顶层的 accessToken', async () => {
  storeTokens()
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      return Promise.resolve(ok(config, { accessToken: 'flat-access', refreshToken: 'flat-refresh' }))
    }
    return String(config.headers.get('Authorization')) === 'Bearer flat-access'
      ? Promise.resolve(ok(config, { isSuccess: true, data: 'done' }))
      : Promise.reject(httpError(401, null, config))
  })

  await expect(client.get('/Sys/Detail')).resolves.toBe('done')
  expect(LocalStorage.get<string>(REFRESH_TOKEN_KEY)).toBe('flat-refresh')
})

it('刷新响应带新刷新令牌时一并落盘，不带时保留原刷新令牌', async () => {
  storeTokens()
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      return Promise.resolve(ok(config, { isSuccess: true, data: { accessToken: 'new-access' } }))
    }
    return String(config.headers.get('Authorization')) === 'Bearer new-access'
      ? Promise.resolve(ok(config, { isSuccess: true, data: null }))
      : Promise.reject(httpError(401, null, config))
  })

  await client.get('/Sys/Detail')

  expect(LocalStorage.get<string>(REFRESH_TOKEN_KEY)).toBe('old-refresh')
})

it('刷新响应缺 accessToken 时判定刷新失败并登出', async () => {
  storeTokens()
  const client = makeClient(config => (config.url === REFRESH_URL
    ? Promise.resolve(ok(config, { isSuccess: true, data: { refreshToken: 'only-refresh' } }))
    : Promise.reject(httpError(401, null, config))))

  const { error } = await client.getFlat('/Sys/Detail')

  expect(error?.message).toBe('登录已过期，请重新登录')
  expect(logout).toHaveBeenCalledTimes(1)
  expect(LocalStorage.get<string>(TOKEN_KEY)).toBeNull()
  expect(LocalStorage.get<string>(REFRESH_TOKEN_KEY)).toBeNull()
})

it('刷新端点返回 500 时刷新失败并登出', async () => {
  storeTokens()
  const client = makeClient(config => (config.url === REFRESH_URL
    ? Promise.reject(httpError(500, null, config))
    : Promise.reject(httpError(401, null, config))))

  await client.getFlat('/Sys/Detail')

  expect(logout).toHaveBeenCalled()
  expect(replace).toHaveBeenCalledWith(LOGIN_PATH)
})

it('刷新端点自身返回 401 时立即登出且不再递归刷新', async () => {
  // 回归锚点：刷新请求带 _isRefresh 标记，漏判会让 401 拦截器无限自我调用
  storeTokens()
  let refreshCount = 0
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      refreshCount += 1
    }
    return Promise.reject(httpError(401, null, config))
  })

  await client.getFlat('/Sys/Detail')

  expect(refreshCount).toBe(1)
  expect(logout).toHaveBeenCalled()
})

it('重放后再次 401 时不发起第二次刷新，直接登出', async () => {
  storeTokens()
  let refreshCount = 0
  let businessCount = 0
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      refreshCount += 1
      return Promise.resolve(ok(config, { isSuccess: true, data: { accessToken: `token-${refreshCount}` } }))
    }
    businessCount += 1
    return Promise.reject(httpError(401, null, config))
  })

  await client.getFlat('/Sys/Detail')

  expect(refreshCount).toBe(1)
  expect(businessCount).toBe(2)
  expect(logout).toHaveBeenCalledTimes(1)
})

it('多个并发 401 只触发一次刷新，并用新令牌重放全部挂起请求', async () => {
  storeTokens()
  let refreshCount = 0
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      refreshCount += 1
      return delayed(() => ok(config, { isSuccess: true, data: { accessToken: 'shared-access' } }))
    }
    return String(config.headers.get('Authorization')) === 'Bearer shared-access'
      ? Promise.resolve(ok(config, { isSuccess: true, data: config.url }))
      : Promise.reject(httpError(401, null, config))
  })

  const results = await Promise.all([
    client.get('/Sys/A'),
    client.get('/Sys/B'),
    client.get('/Sys/C'),
  ])

  expect(refreshCount).toBe(1)
  expect(results).toEqual(['/api/Sys/A', '/api/Sys/B', '/api/Sys/C'])
  expect(logout).not.toHaveBeenCalled()
})

it('并发 401 遇刷新失败时全部请求被拒绝且只刷新一次', async () => {
  storeTokens()
  let refreshCount = 0
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      refreshCount += 1
      return delayed(() => Promise.reject(httpError(500, null, config)))
    }
    return Promise.reject(httpError(401, null, config))
  })

  const results = await Promise.all([
    client.getFlat('/Sys/A'),
    client.getFlat('/Sys/B'),
    client.getFlat('/Sys/C'),
  ])

  expect(refreshCount).toBe(1)
  expect(results.map(item => item.data)).toEqual([null, null, null])
  expect(results.map(item => item.error?.message)).toEqual([
    '登录已过期，请重新登录',
    '登录已过期，请重新登录',
    '登录已过期，请重新登录',
  ])
  expect(LocalStorage.get<string>(TOKEN_KEY)).toBeNull()
  // 锁定当前真实行为：每个被拒绝的挂起请求各自触发一次强制登出
  expect(logout).toHaveBeenCalledTimes(3)
  expect(replace).toHaveBeenCalledTimes(3)
})

it('刷新失败清空挂起队列，下一轮 401 仍能重新发起刷新', async () => {
  storeTokens()
  let refreshCount = 0
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      refreshCount += 1
      return Promise.reject(httpError(500, null, config))
    }
    return Promise.reject(httpError(401, null, config))
  })

  await client.getFlat('/Sys/A')
  storeTokens()
  await client.getFlat('/Sys/B')

  expect(refreshCount).toBe(2)
})

it('401 重放不会重复登记请求日志，最终状态收敛为 success', async () => {
  storeTokens()
  const client = makeClient((config) => {
    if (config.url === REFRESH_URL) {
      return Promise.resolve(ok(config, { isSuccess: true, data: { accessToken: 'new-access' } }))
    }
    return String(config.headers.get('Authorization')) === 'Bearer new-access'
      ? Promise.resolve(ok(config, { isSuccess: true, code: 200, data: null }))
      : Promise.reject(httpError(401, null, config))
  })

  await client.get('/Sys/Detail')

  const businessLogs = logs.value.filter(item => item.url === '/api/Sys/Detail')
  expect(businessLogs).toHaveLength(1)
  expect(businessLogs[0]?.status).toBe('success')
})

it('403 不触发刷新也不登出', async () => {
  storeTokens()
  const requested: string[] = []
  const client = makeClient((config) => {
    requested.push(String(config.url))
    return Promise.reject(httpError(403, null, config))
  })

  const { error } = await client.getFlat('/Sys/Any')

  expect(requested).toEqual(['/api/Sys/Any'])
  expect(logout).not.toHaveBeenCalled()
  expect(replace).not.toHaveBeenCalled()
  expect(error?.message).toBe('没有操作权限')
})

it('刷新端点路径可由构造参数改写', async () => {
  storeTokens()
  const requested: string[] = []
  const client = makeClient((config) => {
    requested.push(String(config.url))
    if (config.url === '/api/Identity/Renew') {
      return Promise.resolve(ok(config, { isSuccess: true, data: { accessToken: 'new-access' } }))
    }
    return String(config.headers.get('Authorization')) === 'Bearer new-access'
      ? Promise.resolve(ok(config, { isSuccess: true, data: null }))
      : Promise.reject(httpError(401, null, config))
  }, { refreshTokenUrl: '/Identity/Renew' })

  await client.get('/Sys/Detail')

  expect(requested).toContain('/api/Identity/Renew')
})

it('未绑定 router 时强制登出退化为整页跳转到登录页', async () => {
  bindRouter(null as unknown as Router)
  vi.stubGlobal('location', { href: 'http://localhost:3000/dashboard' })
  const client = makeClient(config => Promise.reject(httpError(401, null, config)))

  await client.getFlat('/Sys/Any')

  expect((globalThis.location as unknown as { href: string }).href).toBe(LOGIN_PATH)
  expect(logout).toHaveBeenCalledTimes(1)
})

it('旁路刷新入口刷新成功时返回新令牌且不登出', async () => {
  storeTokens()
  makeClient(config => Promise.resolve(ok(config, { isSuccess: true, data: { accessToken: 'signalr-access' } })))

  await expect(refreshSessionToken()).resolves.toBe('signalr-access')
  expect(logout).not.toHaveBeenCalled()
  expect(LocalStorage.get<string>(TOKEN_KEY)).toBe('signalr-access')
})

it('旁路刷新入口刷新失败时返回 null 并完成强制登出', async () => {
  storeTokens()
  makeClient(config => Promise.reject(httpError(500, null, config)))

  await expect(refreshSessionToken()).resolves.toBeNull()
  expect(logout).toHaveBeenCalledTimes(1)
  expect(replace).toHaveBeenCalledWith(LOGIN_PATH)
  expect(LocalStorage.get<string>(TOKEN_KEY)).toBeNull()
})

it('旁路刷新入口在没有刷新令牌时返回 null 并登出，不发任何请求', async () => {
  const requested: string[] = []
  makeClient((config) => {
    requested.push(String(config.url))
    return Promise.resolve(ok(config, { isSuccess: true, data: null }))
  })

  await expect(refreshSessionToken()).resolves.toBeNull()
  expect(requested).toEqual([])
  expect(logout).toHaveBeenCalledTimes(1)
})

it('从未创建过 RequestClient 时旁路刷新入口是空操作，不登出', async () => {
  vi.resetModules()
  const fresh = await import('./index')
  const freshLogout = vi.fn()
  fresh.bindLogoutHook(freshLogout)

  await expect(fresh.refreshSessionToken()).resolves.toBeNull()
  expect(freshLogout).not.toHaveBeenCalled()
})

/** 写入一对已过期的旧令牌。 */
function storeTokens(): void {
  LocalStorage.set(TOKEN_KEY, 'old-access')
  LocalStorage.set(REFRESH_TOKEN_KEY, 'old-refresh')
}

/** 创建注入了自定义适配器的客户端。 */
function makeClient(
  handler: (config: InternalAxiosRequestConfig) => Promise<AxiosResponse>,
  extra: { refreshTokenUrl?: string } = {},
): RequestClient {
  return new RequestClient({ ...extra, adapter: handler as unknown as AxiosAdapter })
}

/** 构造成功响应。 */
function ok(config: InternalAxiosRequestConfig, data: unknown): AxiosResponse {
  return { config, data, headers: new AxiosHeaders(), status: 200, statusText: 'OK' }
}

/** 把结果推迟到下一个宏任务，制造真实的刷新窗口期。 */
function delayed<T>(factory: () => T | Promise<T>): Promise<T> {
  return new Promise<void>((resolve) => {
    setTimeout(resolve, 0)
  }).then(factory)
}

/** 构造带 response 的 axios HTTP 错误。 */
function httpError(status: number, data: unknown, config: InternalAxiosRequestConfig) {
  return new AxiosError(
    `Request failed with status code ${status}`,
    AxiosError.ERR_BAD_RESPONSE,
    config,
    null,
    { config, data, headers: new AxiosHeaders(), status, statusText: '' },
  )
}
