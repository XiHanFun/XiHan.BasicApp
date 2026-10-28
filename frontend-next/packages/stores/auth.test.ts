import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureRequest } from '~/request'
import { registerAppContext } from './app-context'
import { takeIdentitySwitchedMarker, useAuthStore } from './auth'
import { relaunchApp } from './relaunch'

// 切换上下文 / 身份后整页重建：测试里只核对它被调用
vi.mock('./relaunch', () => ({ relaunchApp: vi.fn() }))

const user = { id: 'u-1', userName: 'alice', displayName: 'Alice', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

function session(accessToken: string, refreshToken: string) {
  return { accessToken, accessExpiresAt: '2026-10-28T02:00:00Z', refreshToken, refreshExpiresAt: '2026-11-04T00:00:00Z', sessionId: 's-1', tenantId: 'acme', mustChangePassword: false, user }
}

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.mocked(relaunchApp).mockClear()
  })

  it('logs in anonymously, persists the session and signs out', async () => {
    const calls: { url: string, authorization?: string }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 'stale',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({ url: String(url), authorization: (init!.headers as Record<string, string>).Authorization })
        if (String(url).endsWith('/api/auth:login'))
          return json(200, session('a-1', 'r-1'))
        return json(200, { sessionId: 's-1', revokedAt: '2026-10-28T01:00:00Z' })
      }) as unknown as typeof fetch,
    })
    const store = useAuthStore()
    expect(store.isAuthenticated).toBe(false)

    await store.login({ userName: 'alice', password: 'Alice@12345', tenant: 'acme' })
    expect(store.isAuthenticated).toBe(true)
    expect(store.accessToken).toBe('a-1')
    expect(store.user?.displayName).toBe('Alice')
    expect(store.tenantId).toBe('acme')
    expect(calls[0]).toEqual({ url: '/api/auth:login', authorization: undefined })
    expect(JSON.parse(localStorage.getItem('xihan.next.session')!)).toMatchObject({ accessToken: 'a-1', refreshToken: 'r-1' })

    await store.logout()
    expect(calls[1]?.url).toBe('/api/auth:logout')
    expect(store.isAuthenticated).toBe(false)
    expect(localStorage.getItem('xihan.next.session')).toBeNull()
  })

  it('refreshes once for concurrent callers and rotates the refresh token', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      expect(JSON.parse(String(init!.body))).toEqual({ refreshToken: 'r-1' })
      expect((init!.headers as Record<string, string>).Authorization).toBeUndefined()
      return json(200, session('a-2', 'r-2'))
    })
    configureRequest({ baseUrl: '', getToken: () => 'a-1', fetch: fetchMock as unknown as typeof fetch })
    const store = useAuthStore()
    expect(store.accessToken).toBe('a-1')

    const [first, second] = await Promise.all([store.refresh(), store.refresh()])
    expect(first).toBe('a-2')
    expect(second).toBe('a-2')
    expect(fetchMock).toHaveBeenCalledOnce()
    expect(store.session?.refreshToken).toBe('r-2')
  })

  it('clears the session when the refresh token is rejected', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    configureRequest({ baseUrl: '', getToken: () => 'a-1', fetch: (async () => json(403, { code: 'IDENTITY_DENIED_001', status: 403, detail: 'revoked' }, 'application/problem+json')) as unknown as typeof fetch })
    const store = useAuthStore()
    expect(await store.refresh()).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(localStorage.getItem('xihan.next.session')).toBeNull()
  })

  it('ignores a malformed stored session', () => {
    localStorage.setItem('xihan.next.session', '{"accessToken":1}')
    expect(useAuthStore().isAuthenticated).toBe(false)
  })

  it('switchTenant stores the continued session (new tenant, same sid) and relaunches the app', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const switchTenant = vi.fn(async () => ({ ...session('a-2', 'r-2'), tenantId: 'globex', tenantName: 'Globex' }))
    registerAppContext({ apis: { tenantApi: { switchTenant, myAvailableTenants: vi.fn() } } as unknown as AppContextApis })
    const store = useAuthStore()

    await store.switchTenant('globex')
    expect(switchTenant).toHaveBeenCalledWith({ tenantId: 'globex' })
    expect(store.tenantId).toBe('globex')
    expect(store.session?.sessionId).toBe('s-1')
    expect(JSON.parse(localStorage.getItem('xihan.next.session')!)).toMatchObject({ accessToken: 'a-2', refreshToken: 'r-2', tenantId: 'globex' })
    expect(relaunchApp).toHaveBeenCalledOnce()
    // 切换上下文是同一个人：不留身份切换标记，重建后照常可用本地偏好播种
    expect(takeIdentitySwitchedMarker()).toBe(false)

    await store.switchTenant(null)
    expect(switchTenant).toHaveBeenLastCalledWith({ tenantId: null })
  })

  it('startImpersonation stores the target session and relaunches; the loading flag stays up until the reload', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const start = vi.fn(async () => ({ ...session('a-9', 'r-9'), sessionId: 's-9', tenantId: 'acme', impersonatorUserName: 'alice', user: { ...user, id: 'u-2', userName: 'bob' } }))
    registerAppContext({ apis: { impersonationApi: { start, stop: vi.fn(), tenants: vi.fn(), candidates: vi.fn() } } as unknown as AppContextApis })
    const store = useAuthStore()

    await store.startImpersonation({ targetUserId: 'u-2', tenantId: 'acme', reason: 'ticket 42' })
    expect(start).toHaveBeenCalledWith({ targetUserId: 'u-2', tenantId: 'acme', reason: 'ticket 42' })
    expect(store.session).toMatchObject({ sessionId: 's-9', impersonatorUserName: 'alice', user: { userName: 'bob' } })
    expect(relaunchApp).toHaveBeenCalledOnce()
    expect(store.impersonationLoading).toBe(true)
    // 换了身份：重建后的偏好水合不拿本地那份给被模仿者播种；标记取一次即清
    expect(takeIdentitySwitchedMarker()).toBe(true)
    expect(takeIdentitySwitchedMarker()).toBe(false)
    // 重建前的重复点击不再发请求
    await store.startImpersonation({ targetUserId: 'u-3' })
    expect(start).toHaveBeenCalledOnce()
  })

  it('stopImpersonation failure releases the loading flag, keeps the session and rethrows', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const stop = vi.fn(async () => {
      throw new Error('IDENTITY_VALIDATION_006')
    })
    registerAppContext({ apis: { impersonationApi: { start: vi.fn(), stop, tenants: vi.fn(), candidates: vi.fn() } } as unknown as AppContextApis })
    const store = useAuthStore()

    await expect(store.stopImpersonation()).rejects.toThrow('IDENTITY_VALIDATION_006')
    expect(store.impersonationLoading).toBe(false)
    expect(store.accessToken).toBe('a-1')
    expect(relaunchApp).not.toHaveBeenCalled()
    expect(takeIdentitySwitchedMarker()).toBe(false)
  })
})
