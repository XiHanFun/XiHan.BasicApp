import type { RouteDef } from './client'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, buildRequest, configureRequest, invoke } from './client'

const create: RouteDef = { method: 'POST', template: '/api/users', path: [], query: [], success: 201 }
const get: RouteDef = { method: 'GET', template: '/api/users/{id}', path: ['id'], query: [], success: 200 }
const list: RouteDef = { method: 'POST', template: '/api/users:list', path: [], query: [], success: 200 }

function problem(status: number, body: object): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/problem+json' } })
}

describe('buildRequest', () => {
  it('fills path parameters and moves the rest of a GET into the query string', () => {
    configureRequest({ baseUrl: '' })
    const { url, body } = buildRequest('users.get', get, { id: 'u-1', keyword: 'a b', tags: ['x', 'y'] })
    expect(url).toBe('/api/users/u-1?keyword=a+b&tags=x&tags=y')
    expect(body).toBeUndefined()
  })

  it('sends the whole request as the JSON body for POST', () => {
    configureRequest({ baseUrl: 'https://api.example' })
    const { url, body } = buildRequest('users.list', list, { page: { index: 1, size: 20 }, keyword: 'x' })
    expect(url).toBe('https://api.example/api/users:list')
    expect(JSON.parse(body!)).toEqual({ page: { index: 1, size: 20 }, keyword: 'x' })
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
      return new Response(JSON.stringify({ id: 'u-1', userName: 'alice' }), { status: 201, headers: { 'Content-Type': 'application/json' } })
    })
    configureRequest({ baseUrl: '', fetch: fetchMock as unknown as typeof fetch, getToken: () => 't-1', getLocale: () => 'zh-CN' })
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
        errors: { userName: ['长度必须 >= 3'], displayName: ['必填'] },
      })) as unknown as typeof fetch,
      getToken: () => null,
    })
    const error = (await invoke('users.create', create, { userName: 'a' }).then(() => null, (e: unknown) => e)) as ApiError
    expect(error).toBeInstanceOf(ApiError)
    expect(error.code).toBe('KERNEL_VALIDATION_003')
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
    configureRequest({ baseUrl: '', fetch: fetchMock as unknown as typeof fetch, getToken: () => 'stale', hooks: { onUnauthenticated } })
    const result = await invoke<object, { ok: boolean }>('users.list', list, {})
    expect(result).toEqual({ ok: true })
    expect(seen).toEqual(['Bearer stale', 'Bearer fresh'])
    expect(onUnauthenticated).toHaveBeenCalledOnce()
  })

  it('invokes the lock hook on IDENTITY_DENIED_002 and still throws', async () => {
    const onLocked = vi.fn()
    configureRequest({ baseUrl: '', fetch: (async () => problem(403, { code: 'IDENTITY_DENIED_002', status: 403, detail: 'locked' })) as unknown as typeof fetch, getToken: () => 't', hooks: { onLocked } })
    await expect(invoke('users.list', list, {})).rejects.toMatchObject({ code: 'IDENTITY_DENIED_002' })
    expect(onLocked).toHaveBeenCalledOnce()
  })
})
