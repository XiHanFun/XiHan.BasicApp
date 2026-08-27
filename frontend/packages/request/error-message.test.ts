/**
 * 请求层错误文案归一化单元测试。
 *
 * 职责边界：只锁定「axios 抛出的原始错误 → error.message」这一层映射契约。
 * 本仓库约定业务侧只读 `(e as Error).message`，所以每个 HTTP 状态码、网络错误、
 * 超时、取消，以及后端业务消息的优先级都必须逐条锁死，改动即视为破坏契约。
 * 不覆盖信封解析、令牌刷新与安全签名，那些在同目录其它测试文件里。
 */
import type { AxiosAdapter, InternalAxiosRequestConfig } from 'axios'
import type { Router } from 'vue-router'
import { AxiosError, AxiosHeaders } from 'axios'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { i18n } from '~/locales'
import { bindLockHook, bindLogoutHook, bindRouter, RequestClient } from './index'

const originalLocale = i18n.global.locale.value

beforeEach(() => {
  // forceLogout 在没有 router 时会写 window.location，这里统一给一个假 router 兜住跳转
  bindRouter({ replace: () => Promise.resolve() } as unknown as Router)
  bindLogoutHook(() => {})
  bindLockHook(() => {})
})

afterEach(() => {
  i18n.global.locale.value = originalLocale
})

it('无响应且 code 为 ECONNABORTED 时归一为超时文案', async () => {
  expect(await messageOf(config => timeoutError(config))).toBe('请求超时，请稍后重试')
})

it('无响应但消息里含 timeout（大小写不敏感）时同样归一为超时文案', async () => {
  const error = new AxiosError('Network TIMEOUT while connecting', 'ERR_BAD_REQUEST')
  expect(await messageOf(() => error)).toBe('请求超时，请稍后重试')
})

it('无响应且 code 为 ERR_CANCELED 时归一为取消文案', async () => {
  const error = new AxiosError('canceled', 'ERR_CANCELED')
  expect(await messageOf(() => error)).toBe('请求已取消')
})

it('已中止的 signal 让请求在到达适配器前就以取消文案失败', async () => {
  const controller = new AbortController()
  controller.abort()
  let adapterCalls = 0
  const client = new RequestClient({
    adapter: (async (config) => {
      adapterCalls += 1
      return okResponse(config)
    }) as AxiosAdapter,
  })

  const { data, error } = await client.getFlat('/Cancel/Now', { signal: controller.signal })

  expect(data).toBeNull()
  expect(error?.message).toBe('请求已取消')
  expect(adapterCalls).toBe(0)
})

it('无响应且无可识别 code 时归一为网络错误文案', async () => {
  const error = new AxiosError('Network Error', 'ERR_NETWORK')
  expect(await messageOf(() => error)).toBe('网络连接失败，请检查网络后重试')
})

it('适配器抛出的普通 Error（无 code 无 response）也归一为网络错误文案', async () => {
  expect(await messageOf(() => new Error('socket hang up'))).toBe('网络连接失败，请检查网络后重试')
})

it('每个约定的 HTTP 状态码都映射到唯一的中文兜底文案', async () => {
  const expected: Array<[number, string]> = [
    [400, '请求参数有误'],
    [401, '登录已过期，请重新登录'],
    [403, '没有操作权限'],
    [404, '请求的资源不存在'],
    [408, '请求超时，请稍后重试'],
    [409, '请求冲突，请刷新后重试'],
    [422, '请求参数校验失败'],
    [429, '请求过于频繁，请稍后再试'],
    [500, '服务器内部错误'],
    [502, '网关错误'],
    [503, '服务暂时不可用'],
    [504, '网关超时'],
  ]

  for (const [status, message] of expected) {
    expect(await messageOf(config => httpError(status, null, config))).toBe(message)
  }
})

it('未收录的状态码回落到带状态码的通用文案', async () => {
  expect(await messageOf(config => httpError(418, null, config))).toBe('请求失败（418）')
  expect(await messageOf(config => httpError(451, null, config))).toBe('请求失败（451）')
})

it('切换到 en-US 后同一状态码返回英文文案', async () => {
  i18n.global.locale.value = 'en-US'

  expect(await messageOf(config => httpError(403, null, config))).toBe('No operation permission')
  expect(await messageOf(config => httpError(418, null, config))).toBe('Request failed (418)')
  expect(await messageOf(() => new AxiosError('boom', 'ERR_NETWORK')))
    .toBe('Network connection failed, please check and retry')
})

it('后端业务消息优先于状态码文案，且优先取 data 而非 message', async () => {
  const body = { code: 500, message: '服务器内部错误', data: '订单已被锁定，无法重复提交' }

  expect(await messageOf(config => httpError(500, body, config))).toBe('订单已被锁定，无法重复提交')
})

it('data 不是字符串时退回 message 字段', async () => {
  const body = { code: 400, message: '参数 orderNo 不能为空', data: { field: 'orderNo' } }

  expect(await messageOf(config => httpError(400, body, config))).toBe('参数 orderNo 不能为空')
})

it('data 与 message 都是空白字符串时退回状态码文案', async () => {
  const body = { code: 400, message: '   ', data: '' }

  expect(await messageOf(config => httpError(400, body, config))).toBe('请求参数有误')
})

it('后端消息两端空白被裁剪后返回', async () => {
  const body = { data: '  租户配额已用尽  ' }

  expect(await messageOf(config => httpError(409, body, config))).toBe('租户配额已用尽')
})

it('中文、emoji 与超长后端消息原样透出不被截断', async () => {
  const long = `边界用例：${'长'.repeat(600)}🚀`
  const body = { data: long }

  expect(await messageOf(config => httpError(400, body, config))).toBe(long)
})

it('二进制 Blob 响应体不被当作业务消息解析，回落到状态码文案', async () => {
  const blob = new Blob(['{"data":"不该被读出来"}'], { type: 'application/json' })

  expect(await messageOf(config => httpError(404, blob, config))).toBe('请求的资源不存在')
})

it('字符串响应体不是对象，回落到状态码文案', async () => {
  expect(await messageOf(config => httpError(502, 'Bad Gateway', config))).toBe('网关错误')
})

it('数组响应体没有 data/message 字段，回落到状态码文案', async () => {
  expect(await messageOf(config => httpError(503, [1, 2, 3], config))).toBe('服务暂时不可用')
})

it('状态码文案优先走 i18n 的 error.http_<status> 键而不是内置常量', async () => {
  const spy = vi.spyOn(i18n.global, 't')

  await messageOf(config => httpError(429, null, config))

  expect(spy.mock.calls.map(call => call[0])).toContain('error.http_429')
})

it('i18n 缺键时回退到内置中文兜底文案而不是暴露裸 key', async () => {
  // 模拟运行期 error.http_409 被删除：t 按 vue-i18n 约定返回 key 本身
  vi.spyOn(i18n.global, 't').mockImplementation(((key: string) => key) as typeof i18n.global.t)

  expect(await messageOf(config => httpError(409, null, config))).toBe('请求冲突，请刷新后重试')
})

it('归一化后的消息覆盖 axios 默认英文消息', async () => {
  const { error } = await failWith(config => httpError(500, null, config))

  expect(error?.message).not.toMatch(/Request failed with status code/)
  expect(error?.message).toBe('服务器内部错误')
})

it('flat 模式把归一化后的错误放进 error 字段且 data 为 null', async () => {
  const { data, error } = await failWith(config => httpError(422, null, config))

  expect(data).toBeNull()
  expect(error).toBeInstanceOf(Error)
  expect(error?.message).toBe('请求参数校验失败')
})

it('适配器以非 Error 值拒绝时 flat 模式包装出的消息丢失归一化文案', async () => {
  // 锁定当前真实行为：requestFlat 用 String(err) 兜底包装，
  // 拦截器算好的中文文案在这条路径上拿不到
  const { data, error } = await failWith(() => ({ status: 'weird' }))

  expect(data).toBeNull()
  expect(error?.message).toBe('[object Object]')
})

it('请求层用到的每个状态码在中英文里都有非空文案', () => {
  const statuses = [400, 401, 403, 404, 408, 409, 422, 429, 500, 502, 503, 504]
  const missing: string[] = []

  for (const locale of ['zh-CN', 'en-US']) {
    const messages = errorMessagesOf(locale)
    for (const status of statuses) {
      const text = messages[`http_${status}`]
      if (typeof text !== 'string' || text.trim() === '') {
        missing.push(`${locale}:http_${status}`)
      }
    }
    for (const key of ['network_error', 'timeout', 'canceled', 'request_failed']) {
      const text = messages[key]
      if (typeof text !== 'string' || text.trim() === '') {
        missing.push(`${locale}:${key}`)
      }
    }
  }

  expect(missing).toEqual([])
})

it('中英文 error 命名空间的键集合完全一致', () => {
  expect(Object.keys(errorMessagesOf('en-US')).sort()).toEqual(Object.keys(errorMessagesOf('zh-CN')).sort())
})

it('通用失败文案在两种语言里都保留 status 占位符', () => {
  expect(errorMessagesOf('zh-CN').request_failed).toContain('{status}')
  expect(errorMessagesOf('en-US').request_failed).toContain('{status}')
})

/** 取出指定语言的 error 命名空间文案表。 */
function errorMessagesOf(locale: string): Record<string, string> {
  const messages = i18n.global.getLocaleMessage(locale) as unknown as Record<string, Record<string, string>>
  const namespace = messages.error
  if (!namespace) {
    throw new Error(`${locale} 缺少 error 命名空间`)
  }
  return namespace
}

/** 用一次必然失败的请求取回归一化后的 error.message。 */
async function messageOf(
  reject: (config: InternalAxiosRequestConfig) => unknown,
): Promise<string | undefined> {
  const { error } = await failWith(reject)
  return error?.message
}

/** 用注入的适配器发一次请求，返回 flat 结果。 */
async function failWith(reject: (config: InternalAxiosRequestConfig) => unknown) {
  const client = new RequestClient({
    adapter: ((config: InternalAxiosRequestConfig) =>
      Promise.reject(reject(config))) as AxiosAdapter,
  })
  return client.getFlat('/Probe/Fail')
}

/** 构造带 response 的 axios HTTP 错误。 */
function httpError(status: number, data: unknown, config: InternalAxiosRequestConfig) {
  return new AxiosError(
    `Request failed with status code ${status}`,
    AxiosError.ERR_BAD_RESPONSE,
    config,
    null,
    {
      config,
      data,
      headers: new AxiosHeaders(),
      status,
      statusText: '',
    },
  )
}

/** 构造 axios 超时错误。 */
function timeoutError(config: InternalAxiosRequestConfig) {
  return new AxiosError('timeout of 30000ms exceeded', 'ECONNABORTED', config)
}

/** 构造一个成功响应（仅用于验证适配器未被调用的场景）。 */
function okResponse(config: InternalAxiosRequestConfig) {
  return {
    config,
    data: { isSuccess: true, data: null },
    headers: new AxiosHeaders(),
    status: 200,
    statusText: 'OK',
  }
}
