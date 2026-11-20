import type { RouteDef } from '~/request/client'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, buildRequest, configureRequest, DEFAULT_TIMEOUT_MS, downloadEndpoint, invoke } from '~/request/client'

const create: RouteDef = {
  method: 'POST',
  template: '/api/users',
  path: [],
  query: [],
  success: 201,
}
const get: RouteDef = {
  method: 'GET',
  template: '/api/users/{id}',
  path: ['id'],
  query: [],
  success: 200,
}
const list: RouteDef = {
  method: 'POST',
  template: '/api/users:list',
  path: [],
  query: [],
  success: 200,
}

function problem(status: number, body: object): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

describe('buildRequest', () => {
  it('fills path parameters and moves the rest of a GET into the query string', () => {
    configureRequest({ baseUrl: '' })
    const { url, body } = buildRequest('users.get', get, {
      id: 'u-1',
      keyword: 'a b',
      tags: ['x', 'y'],
    })
    expect(url).toBe('/api/users/u-1?keyword=a+b&tags=x&tags=y')
    expect(body).toBeUndefined()
  })

  it('sends the whole request as the JSON body for POST', () => {
    configureRequest({ baseUrl: 'https://api.example' })
    const { url, body } = buildRequest('users.list', list, {
      page: { index: 1, size: 20 },
      keyword: 'x',
    })
    expect(url).toBe('https://api.example/api/users:list')
    expect(JSON.parse(body!)).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'x',
    })
  })

  it('rejects a missing path parameter', () => {
    expect(() => buildRequest('users.get', get, {})).toThrow(/'users.get'.*path parameter 'id'/)
  })
})

describe('invoke', () => {
  it('puts the token, locale and traceparent on the request and returns the JSON body', async () => {
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      const headers = init!.headers as Record<string, string>
      expect(headers.Authorization).toBe('Bearer t-1')
      expect(headers['Accept-Language']).toBe('zh-CN')
      expect(headers.traceparent).toMatch(/^00-[0-9a-f]{32}-[0-9a-f]{16}-01$/)
      expect(headers['Content-Type']).toBe('application/json')
      return new Response(JSON.stringify({ id: 'u-1', userName: 'alice' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      })
    })
    configureRequest({
      baseUrl: '',
      fetch: fetchMock as unknown as typeof fetch,
      getToken: () => 't-1',
      getLocale: () => 'zh-CN',
    })
    const user = await invoke<{ userName: string }, { id: string, userName: string }>('users.create', create, { userName: 'alice' })
    expect(user).toEqual({ id: 'u-1', userName: 'alice' })
    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('unpacks Problem Details into ApiError with field errors', async () => {
    configureRequest({
      baseUrl: '',
      fetch: (async () => problem(400, {
        type: 'urn:xihan:error:KERNEL_VALIDATION_003',
        title: 'VALIDATION',
        status: 400,
        detail: '请求不合法。',
        code: 'KERNEL_VALIDATION_003',
        traceId: 'abc',
        errors: {
          userName: ['长度必须 >= 3'],
          displayName: ['必填'],
        },
      })) as unknown as typeof fetch,
      getToken: () => null,
    })
    const error = (await invoke('users.create', create, { userName: 'a' }).then(() => null, (e: unknown) => e)) as ApiError
    expect(error).toBeInstanceOf(ApiError)
    expect(error.code).toBe('KERNEL_VALIDATION_003')
    // 页面直接把 message 给用户看，错误码不进去
    expect(error.message).toBe('请求不合法。')
    expect(error.detail).toBe('请求不合法。')
    expect(error.status).toBe(400)
    expect(error.kind).toBe('VALIDATION')
    expect(error.traceId).toBe('abc')
    expect(error.hasFieldErrors).toBe(true)
    expect(error.fieldError('userName')).toBe('长度必须 >= 3')
    expect(error.fieldError('email')).toBeUndefined()
  })

  it('refreshes once on SECURITY_DENIED_001 and replays with the new token', async () => {
    const seen: string[] = []
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      const auth = (init!.headers as Record<string, string>).Authorization ?? 'none'
      seen.push(auth)
      return auth === 'Bearer fresh'
        ? new Response(JSON.stringify({ ok: true }), { status: 200 })
        : problem(401, { code: 'SECURITY_DENIED_001', status: 401, detail: 'auth' })
    })
    const onUnauthenticated = vi.fn(async () => 'fresh')
    configureRequest({
      baseUrl: '',
      fetch: fetchMock as unknown as typeof fetch,
      getToken: () => 'stale',
      hooks: { onUnauthenticated },
    })
    const result = await invoke<object, { ok: boolean }>('users.list', list, {})
    expect(result).toEqual({ ok: true })
    expect(seen).toEqual(['Bearer stale', 'Bearer fresh'])
    expect(onUnauthenticated).toHaveBeenCalledOnce()
  })

  it('treats a rejected access token (SECURITY_DENIED_003, e.g. expired) like unauthenticated: refresh once, then replay', async () => {
    const seen: string[] = []
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      const auth = (init!.headers as Record<string, string>).Authorization ?? 'none'
      seen.push(auth)
      return auth === 'Bearer fresh'
        ? new Response(JSON.stringify({ ok: true }), { status: 200 })
        : problem(403, { code: 'SECURITY_DENIED_003', status: 403, detail: 'expired' })
    })
    const onUnauthenticated = vi.fn(async () => 'fresh')
    configureRequest({
      baseUrl: '',
      fetch: fetchMock as unknown as typeof fetch,
      getToken: () => 'expired',
      hooks: { onUnauthenticated },
    })
    expect(await invoke<object, { ok: boolean }>('users.list', list, {})).toEqual({ ok: true })
    expect(seen).toEqual(['Bearer expired', 'Bearer fresh'])

    // 刷新也换不回令牌：钩子返回 null（由它清会话回登录页），原错误照常抛出
    onUnauthenticated.mockResolvedValueOnce(null as unknown as string)
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'SECURITY_DENIED_003' })
  })

  it('sends anonymous calls without a token and never refreshes them', async () => {
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      expect((init!.headers as Record<string, string>).Authorization).toBeUndefined()
      return problem(401, { code: 'SECURITY_DENIED_001', status: 401, detail: 'auth' })
    })
    const onUnauthenticated = vi.fn(async () => 'fresh')
    configureRequest({
      baseUrl: '',
      fetch: fetchMock as unknown as typeof fetch,
      getToken: () => 'stale',
      hooks: { onUnauthenticated },
    })
    await expect(invoke('auth.login', list, {}, { anonymous: true })).rejects.toMatchObject({ code: 'SECURITY_DENIED_001' })
    expect(fetchMock).toHaveBeenCalledOnce()
    expect(onUnauthenticated).not.toHaveBeenCalled()
  })

  it('invokes the session hook on IDENTITY_DENIED_001 and still throws', async () => {
    const onSessionInvalid = vi.fn()
    configureRequest({
      baseUrl: '',
      fetch: (async () => problem(403, { code: 'IDENTITY_DENIED_001', status: 403, detail: 'revoked' })) as unknown as typeof fetch,
      getToken: () => 't',
      hooks: { onSessionInvalid },
    })
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'IDENTITY_DENIED_001' })
    expect(onSessionInvalid).toHaveBeenCalledOnce()
  })

  it('invokes the lock hook on IDENTITY_DENIED_002 and still throws', async () => {
    const onLocked = vi.fn()
    configureRequest({
      baseUrl: '',
      fetch: (async () => problem(403, { code: 'IDENTITY_DENIED_002', status: 403, detail: 'locked' })) as unknown as typeof fetch,
      getToken: () => 't',
      hooks: { onLocked },
    })
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'IDENTITY_DENIED_002' })
    expect(onLocked).toHaveBeenCalledOnce()
  })

  it('invokes the maintenance hook while the database is upgraded or the kernel is not serving, and still throws', async () => {
    const onMaintenance = vi.fn()
    let code = 'RUNTIME_UNAVAILABLE_003'
    configureRequest({
      baseUrl: '',
      fetch: (async () => problem(503, { code, status: 503, detail: 'upgrading' })) as unknown as typeof fetch,
      getToken: () => 't',
      hooks: { onMaintenance },
    })
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'RUNTIME_UNAVAILABLE_003' })
    code = 'KERNEL_UNAVAILABLE_001'
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'KERNEL_UNAVAILABLE_001' })
    code = 'KERNEL_UNEXPECTED_001'
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'KERNEL_UNEXPECTED_001' })
    expect(onMaintenance).toHaveBeenCalledTimes(2)
  })
})

describe('invoke deadline', () => {
  /** 不回应、只在被中止时以中止原因拒绝的 fetch：模拟后端挂死 */
  function hangingFetch() {
    return vi.fn((_url: RequestInfo | URL, init?: RequestInit) => new Promise<Response>((_resolve, reject) => {
      init!.signal!.addEventListener('abort', () => reject(init!.signal!.reason), { once: true })
    }))
  }

  afterEach(() => {
    vi.useRealTimers()
    configureRequest({ timeoutMs: undefined, getTimeoutMessage: undefined })
  })

  it('writes the deadline into xihan-deadline: the configured timeout, or the per-call one, from now', async () => {
    vi.useFakeTimers({
      now: new Date('2026-11-21T08:00:00Z'),
      toFake: ['Date', 'setTimeout', 'clearTimeout'],
    })
    const deadlines: (string | undefined)[] = []
    configureRequest({
      baseUrl: '',
      fetch: (async (_url: RequestInfo | URL, init?: RequestInit) => {
        deadlines.push((init!.headers as Record<string, string>)['xihan-deadline'])
        return new Response('{}', { status: 200 })
      }) as unknown as typeof fetch,
      getToken: () => 't',
    })

    await invoke('users.list', list, {})
    configureRequest({ timeoutMs: 5_000 })
    await invoke('users.list', list, {})
    await invoke('users.list', list, {}, { timeoutMs: 300_000 })
    await invoke('users.list', list, {}, { timeoutMs: 0 })

    expect(deadlines).toEqual([
      new Date(Date.parse('2026-11-21T08:00:00Z') + DEFAULT_TIMEOUT_MS).toISOString(),
      '2026-11-21T08:00:05.000Z',
      '2026-11-21T08:05:00.000Z',
      undefined,
    ])
  })

  it('aborts a call that outlives its deadline and reports it with the backend timeout code', async () => {
    vi.useFakeTimers({
      toFake: ['setTimeout', 'clearTimeout'],
    })
    const fetchMock = hangingFetch()
    configureRequest({
      baseUrl: '',
      fetch: fetchMock as unknown as typeof fetch,
      getToken: () => 't',
      timeoutMs: 1_000,
      getTimeoutMessage: () => '请求超时',
    })

    const call = invoke('users.list', list, {})
    const settled = expect(call).rejects.toMatchObject({ name: 'ApiError', code: 'KERNEL_TIMEOUT_001', message: '请求超时' })
    // 截止时间到了还在宽限期里：后端可能正在回超时，前端不抢先中止
    await vi.advanceTimersByTimeAsync(1_000)
    expect(fetchMock.mock.calls[0]![1]!.signal!.aborted).toBe(false)
    await vi.advanceTimersByTimeAsync(5_000)
    await settled
  })

  it('passes a caller abort through as it is, not as a timeout', async () => {
    configureRequest({
      baseUrl: '',
      fetch: hangingFetch() as unknown as typeof fetch,
      getToken: () => 't',
    })
    const controller = new AbortController()

    const call = invoke('users.list', list, {}, { signal: controller.signal })
    controller.abort(new DOMException('left the page', 'AbortError'))

    const error = await call.catch((reason: unknown) => reason)
    expect(error).not.toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ name: 'AbortError', message: 'left the page' })
  })
})

describe('downloadEndpoint', () => {
  it('sends the token to the plain endpoint and returns the content with the file name', async () => {
    const fetchMock = vi.fn(async (url: RequestInfo | URL, init?: RequestInit) => {
      expect(String(url)).toBe('https://api.example/api/codegen-histories/h-1/package')
      expect((init!.headers as Record<string, string>).Authorization).toBe('Bearer t-1')
      return new Response(new Uint8Array([80, 75]), {
        status: 200,
        headers: { 'Content-Type': 'application/zip', 'Content-Disposition': 'attachment; filename="x.zip"; filename*=UTF-8\'\'%E5%95%86%E5%93%81.zip' },
      })
    })
    configureRequest({
      baseUrl: 'https://api.example',
      fetch: fetchMock as unknown as typeof fetch,
      getToken: () => 't-1',
    })
    const file = await downloadEndpoint('/api/codegen-histories/h-1/package')
    expect(file.fileName).toBe('商品.zip')
    expect(file.blob.size).toBe(2)
  })

  it('throws the problem as an ApiError', async () => {
    configureRequest({
      baseUrl: '',
      fetch: (async () => problem(404, { code: 'CODEGENERATION_NOTFOUND_005', status: 404, detail: 'gone' })) as unknown as typeof fetch,
      getToken: () => 't',
    })
    await expect(downloadEndpoint('/api/codegen-histories/h-2/package')).rejects.toMatchObject({ code: 'CODEGENERATION_NOTFOUND_005' })
  })
})
