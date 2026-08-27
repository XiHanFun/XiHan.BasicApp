/**
 * 请求拦截器与前端请求日志单元测试。
 *
 * 职责边界：锁定每条请求出站前被写入的头部（Authorization / X-Timezone / X-Language /
 * X-Request-Id）、FormData 的 Content-Type 摘除，以及请求日志从 pending 到 success/error
 * 的完整生命周期。错误文案映射、信封拆包、令牌刷新、安全签名在同目录其它测试文件里。
 */
import type { AxiosAdapter, InternalAxiosRequestConfig } from 'axios'
import type { Router } from 'vue-router'
import type { FrontendRequestLog } from '~/types'
import { AxiosError, AxiosHeaders } from 'axios'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { APP_TIMEZONE_KEY, LOCALE_KEY, TOKEN_KEY } from '~/constants'
import { clearRequestLogs, LocalStorage, useRequestLogs } from '~/utils'
import { bindLockHook, bindLogoutHook, bindRouter, RequestClient } from './index'

const logs = useRequestLogs()

beforeEach(() => {
  bindRouter({ replace: () => Promise.resolve() } as unknown as Router)
  bindLogoutHook(() => {})
  bindLockHook(() => {})
  clearRequestLogs()
})

afterEach(() => {
  vi.useRealTimers()
  restoreBrowserTimeZone()
  clearRequestLogs()
})

it('本地存有令牌时带上 Bearer 授权头', async () => {
  LocalStorage.set(TOKEN_KEY, 'jwt-abc.def')
  const config = await captureRequest()

  expect(config.headers.get('Authorization')).toBe('Bearer jwt-abc.def')
})

it('未登录（无令牌）时不带授权头', async () => {
  const config = await captureRequest()

  expect(config.headers.get('Authorization')).toBeUndefined()
})

it('令牌存成了非 JSON 文本时按未登录处理而不是发出 Bearer undefined', async () => {
  // LocalStorage.get 内部 JSON.parse 失败返回 null，这里锁定它不会污染授权头
  localStorage.setItem(TOKEN_KEY, '裸字符串没有引号')
  const config = await captureRequest()

  expect(config.headers.get('Authorization')).toBeUndefined()
})

it('已选时区优先于浏览器时区写入 X-Timezone', async () => {
  stubBrowserTimeZone('Europe/Berlin')
  LocalStorage.set(APP_TIMEZONE_KEY, 'Asia/Tokyo')
  const config = await captureRequest()

  expect(config.headers.get('X-Timezone')).toBe('Asia/Tokyo')
})

it('未选时区时跟随浏览器解析出的时区', async () => {
  stubBrowserTimeZone('Europe/Berlin')
  const config = await captureRequest()

  expect(config.headers.get('X-Timezone')).toBe('Europe/Berlin')
})

it('浏览器解析不出时区时不写空的 X-Timezone 头', async () => {
  stubBrowserTimeZone('')
  const config = await captureRequest()

  expect(config.headers.get('X-Timezone')).toBeUndefined()
})

it('已选语言优先写入 X-Language', async () => {
  LocalStorage.set(LOCALE_KEY, 'en-US')
  const config = await captureRequest()

  expect(config.headers.get('X-Language')).toBe('en-US')
})

it('未选语言时回落到默认语言 zh-CN', async () => {
  const config = await captureRequest()

  expect(config.headers.get('X-Language')).toBe('zh-CN')
})

it('请求标识头以毫秒时间戳编码且同一批请求互不重复', async () => {
  vi.useFakeTimers()
  // 显式给定时区，避免假定时器期间还要走 Intl.DateTimeFormat
  LocalStorage.set(APP_TIMEZONE_KEY, 'UTC')
  vi.setSystemTime(new Date('2026-08-27T00:00:00.000Z'))
  const calls: InternalAxiosRequestConfig[] = []
  const client = capturingClient(calls)

  await Promise.all([client.get('/A'), client.get('/B'), client.get('/C')])

  const ids = calls.map(item => String(item.headers.get('X-Request-Id')))
  expect(ids.every(id => id.startsWith(`req_${Date.parse('2026-08-27T00:00:00.000Z')}_`))).toBe(true)
  expect(new Set(ids).size).toBe(3)
})

it('表单上传请求摘掉调用方设置的 Content-Type，避免 axios 把 FormData 转成 JSON', async () => {
  const form = new FormData()
  form.append('file', new Blob(['bytes']), 'a.txt')
  const calls: InternalAxiosRequestConfig[] = []
  const client = capturingClient(calls)

  await client.post('/File/Upload', form, { headers: { 'Content-Type': 'application/json' } })

  expect(calls[0]?.data).toBeInstanceOf(FormData)
  expect(calls[0]?.headers.get('Content-Type')).not.toBe('application/json')
})

it('非 FormData 请求保留调用方显式设置的 Content-Type', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = capturingClient(calls)

  await client.post('/Sys/Raw', 'a=1&b=2', {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  })

  expect(calls[0]?.headers.get('Content-Type')).toBe('application/x-www-form-urlencoded')
})

it('请求刚出站时日志处于 pending，方法名大写且带上拼接后的地址', async () => {
  vi.useFakeTimers()
  // 显式给定时区，避免假定时器期间还要走 Intl.DateTimeFormat
  LocalStorage.set(APP_TIMEZONE_KEY, 'UTC')
  vi.setSystemTime(new Date('2026-08-27T01:00:00.000Z'))
  let inFlight: FrontendRequestLog | undefined
  const client = respondingClient(() => {
    inFlight = logs.value[0]
    return { isSuccess: true, code: 200, data: null }
  })

  await client.post('/Sys/Save', { a: 1 })

  expect(inFlight?.status).toBe('pending')
  expect(inFlight?.method).toBe('POST')
  expect(inFlight?.url).toBe('/api/Sys/Save')
  expect(inFlight?.startedAt).toBe(Date.parse('2026-08-27T01:00:00.000Z'))
  expect(inFlight?.finishedAt).toBeUndefined()
})

it('日志中记录的 requestId 与实际发出的 X-Request-Id 头一致', async () => {
  const calls: InternalAxiosRequestConfig[] = []
  const client = capturingClient(calls)

  await client.get('/Sys/Ping')

  expect(logs.value[0]?.requestId).toBe(String(calls[0]?.headers.get('X-Request-Id')))
})

it('成功响应把日志更新为 success 并回填状态码、业务码、消息与 traceId', async () => {
  vi.useFakeTimers()
  // 显式给定时区，避免假定时器期间还要走 Intl.DateTimeFormat
  LocalStorage.set(APP_TIMEZONE_KEY, 'UTC')
  vi.setSystemTime(new Date('2026-08-27T02:00:00.000Z'))
  const client = respondingClient(() => {
    vi.advanceTimersByTime(120)
    return { isSuccess: true, code: 200, message: '操作成功', traceId: 'trace-9', data: { id: 1 } }
  })

  await client.get('/Sys/Detail')

  expect(logs.value[0]).toMatchObject({
    status: 'success',
    statusCode: 200,
    responseCode: 200,
    message: '操作成功',
    traceId: 'trace-9',
    duration: 120,
  })
})

it('系统时钟回拨时耗时被夹到 0 而不是记成负数', async () => {
  vi.useFakeTimers()
  // 显式给定时区，避免假定时器期间还要走 Intl.DateTimeFormat
  LocalStorage.set(APP_TIMEZONE_KEY, 'UTC')
  vi.setSystemTime(new Date('2026-08-27T03:00:00.000Z'))
  const client = respondingClient(() => {
    vi.setSystemTime(new Date('2026-08-27T02:59:55.000Z'))
    return { isSuccess: true, code: 200, data: null }
  })

  await client.get('/Sys/ClockSkew')

  expect(logs.value[0]?.duration).toBe(0)
})

it('traceId 不是字符串时不写进日志', async () => {
  const client = respondingClient(() => ({ isSuccess: true, code: 200, traceId: 12345, data: null }))

  await client.get('/Sys/Detail')

  expect(logs.value[0]?.traceId).toBeUndefined()
})

it('状态码错误把日志更新为 error 并优先记录后端返回的消息', async () => {
  const client = failingClient(config => httpError(500, { code: 500, message: '后端记账消息' }, config))

  await client.getFlat('/Sys/Boom')

  expect(logs.value[0]).toMatchObject({ status: 'error', statusCode: 500, responseCode: 500, message: '后端记账消息' })
})

it('状态码错误无后端消息时日志记录 axios 原始消息，而不是归一化后的中文文案', async () => {
  // 锁定当前真实行为：日志在 error.message 被覆盖之前写入
  const client = failingClient(config => httpError(500, null, config))

  const { error } = await client.getFlat('/Sys/Boom')

  expect(logs.value[0]?.message).toBe('Request failed with status code 500')
  expect(error?.message).toBe('服务器内部错误')
})

it('网络错误（无响应）同样写入 error 日志且不带状态码', async () => {
  const client = failingClient(config => new AxiosError('Network Error', 'ERR_NETWORK', config))

  await client.getFlat('/Sys/Offline')

  expect(logs.value[0]).toMatchObject({ status: 'error', message: 'Network Error' })
  expect(logs.value[0]?.statusCode).toBeUndefined()
})

it('业务信封失败时也把日志更新为 error 并记录业务消息', async () => {
  const client = respondingClient(() => ({ isSuccess: false, code: 4003, message: '余额不足', traceId: 'trace-x' }))

  await client.getFlat('/Pay/Deduct')

  expect(logs.value[0]).toMatchObject({
    status: 'error',
    statusCode: 200,
    responseCode: 4003,
    message: '余额不足',
    traceId: 'trace-x',
  })
})

it('配置里已带 _meta 的请求不重复登记日志也不再覆盖 X-Request-Id', async () => {
  // 回归锚点：401 重放会把同一份 config 再次送进请求拦截器，必须复用原有 meta
  const calls: InternalAxiosRequestConfig[] = []
  const client = capturingClient(calls)

  await client.get('/Sys/Replay', {
    _meta: { requestId: 'req_reused', startedAt: Date.now(), method: 'GET', url: '/api/Sys/Replay' },
  } as never)

  expect(logs.value).toHaveLength(0)
  expect(calls[0]?.headers.get('X-Request-Id')).toBeUndefined()
})

it('多次请求的日志按最新在前排列', async () => {
  const client = respondingClient(() => ({ isSuccess: true, code: 200, data: null }))

  await client.get('/First')
  await client.get('/Second')

  expect(logs.value.map(item => item.url)).toEqual(['/api/Second', '/api/First'])
})

/** 本用例覆盖 Intl.DateTimeFormat 之前的原始属性描述符。 */
let originalDateTimeFormat: PropertyDescriptor | undefined

/**
 * 用固定时区替换浏览器时区解析。
 *
 * 不用 vi.spyOn：假定时器会自行接管 Intl.DateTimeFormat，两者的还原顺序一旦交错，
 * 就会在无关用例里留下一个不可 new 的构造器。这里自己记录描述符、由 afterEach 原样还原。
 */
function stubBrowserTimeZone(timeZone: string): void {
  originalDateTimeFormat ??= Object.getOwnPropertyDescriptor(Intl, 'DateTimeFormat')
  Object.defineProperty(Intl, 'DateTimeFormat', {
    configurable: true,
    writable: true,
    value: function FakeDateTimeFormat() {
      return { resolvedOptions: () => ({ timeZone }) }
    },
  })
}

/** 还原被 stubBrowserTimeZone 覆盖的 Intl.DateTimeFormat。 */
function restoreBrowserTimeZone(): void {
  if (!originalDateTimeFormat) {
    return
  }
  Object.defineProperty(Intl, 'DateTimeFormat', originalDateTimeFormat)
  originalDateTimeFormat = undefined
}

/** 发一次请求并返回适配器收到的最终配置。 */
async function captureRequest(): Promise<InternalAxiosRequestConfig> {
  const calls: InternalAxiosRequestConfig[] = []
  await capturingClient(calls).get('/Sys/Ping')
  const config = calls[0]
  if (!config) {
    throw new Error('适配器未被调用')
  }
  return config
}

/** 记录每次出站配置并返回成功信封的客户端。 */
function capturingClient(calls: InternalAxiosRequestConfig[]): RequestClient {
  return new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => {
      calls.push(config)
      return Promise.resolve({
        config,
        data: { isSuccess: true, code: 200, data: null },
        headers: new AxiosHeaders(),
        status: 200,
        statusText: 'OK',
      })
    }) as AxiosAdapter,
  })
}

/** 按回调结果返回响应体的客户端。 */
function respondingClient(body: () => unknown): RequestClient {
  return new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) => Promise.resolve({
      config,
      data: body(),
      headers: new AxiosHeaders(),
      status: 200,
      statusText: 'OK',
    })) as AxiosAdapter,
  })
}

/** 按回调结果拒绝的客户端。 */
function failingClient(reject: (config: InternalAxiosRequestConfig) => unknown): RequestClient {
  return new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) =>
      Promise.reject(reject(config))) as AxiosAdapter,
  })
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
