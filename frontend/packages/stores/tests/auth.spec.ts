import type { Router } from 'vue-router'
import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SESSION_LOCK_CHANGED_EVENT } from '~/composables/session-lock'
import { destroyAllSignalRConnections } from '~/composables/useSignalR'
import { HOME_PATH, LOCK_STATE_KEY, LOGIN_PATH, TABS_LIST_KEY } from '~/constants'
import { configureRequest } from '~/request'
import { registerAppContext } from '~/stores/app-context'
import { oauthChallengeUrl, takeIdentitySwitchedMarker, takeOAuthPending, useAuthStore } from '~/stores/auth'
import { resetPreferenceBackendSync } from '~/stores/helpers'
import { relaunchApp } from '~/stores/relaunch'
import { useSplitViewStore } from '~/stores/split-view'
import { useTabbarStore } from '~/stores/tabbar'

// 切换上下文 / 身份后整页重建：测试里只核对它被调用
vi.mock('~/stores/relaunch', () => ({
  relaunchApp: vi.fn(),
}))
vi.mock('~/composables/useSignalR', () => ({
  destroyAllSignalRConnections: vi.fn(async () => {}),
}))
vi.mock('~/stores/helpers', async importOriginal => ({
  ...await importOriginal<typeof import('~/stores/helpers')>(),
  resetPreferenceBackendSync: vi.fn(),
}))

const user = {
  id: 'u-1',
  userName: 'alice',
  displayName: 'Alice',
  email: null,
  phoneNumber: null,
  homeTenantId: 'acme',
  status: 'Active',
  lastLoginAt: null,
  createdAt: '2026-10-28T00:00:00Z',
  createdBy: null,
}

function session(accessToken: string, refreshToken: string) {
  return {
    accessToken,
    accessExpiresAt: '2026-10-28T02:00:00Z',
    refreshToken,
    refreshExpiresAt: '2026-11-04T00:00:00Z',
    sessionId: 's-1',
    tenantId: 'acme',
    mustChangePassword: false,
    user,
  }
}

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.mocked(relaunchApp).mockClear()
    vi.mocked(resetPreferenceBackendSync).mockClear()
  })

  it('logs in anonymously, persists the session, follows the redirect and signs out', async () => {
    const calls: { url: string, authorization?: string }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 'stale',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({
          url: String(url),
          authorization: (init!.headers as Record<string, string>).Authorization,
        })
        if (String(url).endsWith('/api/auth:login')) {
          return json(200, {
            token: session('a-1', 'r-1'),
            twoFactor: null,
          })
        }
        return json(200, { sessionId: 's-1', revokedAt: '2026-10-28T01:00:00Z' })
      }) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace,
        currentRoute: {
          value: { path: '/' },
        },
      } as unknown as Router),
    })
    const store = useAuthStore()
    expect(store.isAuthenticated).toBe(false)

    expect(await store.login({ username: 'alice', password: 'Alice@12345', tenant: 'acme' }, '/settings/configs')).toBeNull()
    expect(replace).toHaveBeenCalledWith('/settings/configs')
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

  it('returns the two-factor challenge without a session, then finishes with the ticket and code', async () => {
    const bodies: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        bodies.push({
          url: String(url),
          body: JSON.parse(String(init!.body)),
        })
        if (String(url).endsWith('/api/auth:login')) {
          return json(200, {
            token: null,
            twoFactor: {
              ticket: 't-1',
              methods: ['Totp'],
              method: 'Totp',
              codeSent: false,
              expiresAt: '2026-10-28T00:10:00Z',
            },
          })
        }
        return json(200, session('a-1', 'r-1'))
      }) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace,
        currentRoute: {
          value: { path: '/' },
        },
      } as unknown as Router),
    })
    const store = useAuthStore()

    const challenge = await store.login({
      username: ' alice ',
      password: 'Alice@12345',
      captchaId: 'c-1',
      captchaCode: '1234',
    })
    expect(challenge).toEqual({
      requiresTwoFactor: true,
      availableTwoFactorMethods: ['totp'],
      twoFactorMethod: 'totp',
      codeSent: false,
      twoFactorTicket: 't-1',
      token: null,
    })
    expect(bodies[0]?.body).toEqual({
      userName: 'alice',
      password: 'Alice@12345',
      tenant: null,
      captchaId: 'c-1',
      captchaCode: '1234',
    })
    expect(store.isAuthenticated).toBe(false)

    // 第二段：外部回跳地址一律落回首页
    expect(await store.login({
      username: 'alice',
      password: 'Alice@12345',
      twoFactorTicket: 't-1',
      twoFactorMethod: 'totp',
      twoFactorCode: '654321',
    }, '//evil.example')).toBeNull()
    expect(bodies[1]).toEqual({
      url: '/api/auth:verify-two-factor',
      body: { ticket: 't-1', method: 'Totp', code: '654321' },
    })
    expect(store.accessToken).toBe('a-1')
    expect(replace).toHaveBeenCalledWith('/')
  })

  it('a ticket without a code asks the server to send the email or SMS code, then the code finishes the login', async () => {
    const calls: { url: string, body: unknown, auth: string | null }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({
          url: String(url),
          body: JSON.parse(String(init!.body)),
          auth: new Headers(init!.headers).get('Authorization'),
        })
        return String(url).endsWith(':send-two-factor-code')
          ? json(200, { expiresInSeconds: 600 })
          : json(200, session('a-1', 'r-1'))
      }) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace,
        currentRoute: {
          value: { path: '/' },
        },
      } as unknown as Router),
    })
    const store = useAuthStore()

    // 登录页选好邮箱方式（或点重发）：只发码，回来的挑战标明方式与已发送，票据照旧
    expect(await store.login({
      username: 'alice',
      password: 'Alice@12345',
      twoFactorTicket: 't-1',
      twoFactorMethod: 'email',
    }))
      .toEqual({
        requiresTwoFactor: true,
        twoFactorMethod: 'email',
        codeSent: true,
        twoFactorTicket: 't-1',
        token: null,
      })
    expect(calls[0]).toEqual({
      url: '/api/auth:send-two-factor-code',
      body: { ticket: 't-1', method: 'Email' },
      auth: null,
    })
    expect(store.isAuthenticated).toBe(false)

    expect(await store.login({
      username: 'alice',
      password: 'Alice@12345',
      twoFactorTicket: 't-1',
      twoFactorMethod: 'phone',
      twoFactorCode: '246801',
    })).toBeNull()
    expect(calls[1]).toEqual({
      url: '/api/auth:verify-two-factor',
      body: { ticket: 't-1', method: 'Phone', code: '246801' },
      auth: null,
    })
    expect(store.accessToken).toBe('a-1')
  })

  it('code login sends channel, trimmed target and tenant anonymously; a token lands like a password login, a two-factor challenge is kept for the login page', async () => {
    const bodies: { url: string, body: unknown, auth: string | null }[] = []
    let twoFactor = false
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        bodies.push({
          url: String(url),
          body: JSON.parse(String(init!.body)),
          auth: new Headers(init!.headers).get('Authorization'),
        })
        return json(200, twoFactor
          ? {
              token: null,
              twoFactor: {
                ticket: 't-2',
                methods: ['Totp'],
                method: 'Totp',
                codeSent: false,
                expiresAt: '2026-10-28T00:10:00Z',
              },
            }
          : {
              token: session('a-2', 'r-2'),
              twoFactor: null,
            })
      }) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace,
        currentRoute: {
          value: { path: '/' },
        },
      } as unknown as Router),
    })
    const store = useAuthStore()

    await store.loginByEmailCode({ email: ' ann@example.com ', code: '135790', tenant: ' acme ' }, '/files/library')
    expect(bodies[0]).toEqual({
      url: '/api/auth:login-by-code',
      body: {
        channel: 'Email',
        target: 'ann@example.com',
        code: '135790',
        tenant: 'acme',
      },
      auth: null,
    })
    expect(store.accessToken).toBe('a-2')
    expect(replace).toHaveBeenLastCalledWith('/files/library')

    twoFactor = true
    await store.loginByPhoneCode({ phone: '13800000000', code: '246810' }, '/files/library')
    expect(bodies[1]?.body).toEqual({
      channel: 'Phone',
      target: '13800000000',
      code: '246810',
      tenant: null,
    })
    expect(replace).toHaveBeenLastCalledWith({
      path: '/auth/login',
      query: { redirect: '/files/library' },
    })
    expect(store.takePendingTwoFactor()).toMatchObject({
      requiresTwoFactor: true,
      twoFactorTicket: 't-2',
      availableTwoFactorMethods: ['totp'],
    })
    expect(store.takePendingTwoFactor()).toBeNull()
  })

  it('refreshes once for concurrent callers and rotates the refresh token', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      expect(JSON.parse(String(init!.body))).toEqual({ refreshToken: 'r-1' })
      expect((init!.headers as Record<string, string>).Authorization).toBeUndefined()
      return json(200, session('a-2', 'r-2'))
    })
    configureRequest({
      baseUrl: '',
      getToken: () => 'a-1',
      fetch: fetchMock as unknown as typeof fetch,
    })
    const store = useAuthStore()
    expect(store.accessToken).toBe('a-1')

    const [first, second] = await Promise.all([
      store.refresh(),
      store.refresh(),
    ])
    expect(first).toBe('a-2')
    expect(second).toBe('a-2')
    expect(fetchMock).toHaveBeenCalledOnce()
    expect(store.session?.refreshToken).toBe('r-2')
  })

  it('clears the session when the refresh token is rejected', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    configureRequest({
      baseUrl: '',
      getToken: () => 'a-1',
      fetch: (async () => json(403, { code: 'IDENTITY_DENIED_001', status: 403, detail: 'revoked' }, 'application/problem+json')) as unknown as typeof fetch,
    })
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
    const switchTenant = vi.fn(async () => ({
      ...session('a-2', 'r-2'),
      tenantId: 'globex',
      tenantName: 'Globex',
    }))
    registerAppContext({
      apis: {
        tenantApi: {
          switchTenant,
          myAvailableTenants: vi.fn(),
        },
      } as unknown as AppContextApis,
    })
    const store = useAuthStore()

    await store.switchTenant('globex')
    expect(switchTenant).toHaveBeenCalledWith({ tenantId: 'globex' })
    expect(store.tenantId).toBe('globex')
    expect(store.session?.sessionId).toBe('s-1')
    expect(JSON.parse(localStorage.getItem('xihan.next.session')!)).toMatchObject({ accessToken: 'a-2', refreshToken: 'r-2', tenantId: 'globex' })
    expect(relaunchApp).toHaveBeenCalledOnce()
    // 切换上下文是同一个人：不留身份切换标记，重建后照常可用本地偏好播种，回写通道也不用关
    expect(takeIdentitySwitchedMarker()).toBe(false)
    expect(resetPreferenceBackendSync).not.toHaveBeenCalled()

    await store.switchTenant(null)
    expect(switchTenant).toHaveBeenLastCalledWith({ tenantId: null })
  })

  it('startImpersonation stores the target session and relaunches; the loading flag stays up until the reload', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const start = vi.fn(async () => ({
      ...session('a-9', 'r-9'),
      sessionId: 's-9',
      tenantId: 'acme',
      impersonatorUserName: 'alice',
      user: { ...user, id: 'u-2', userName: 'bob' },
    }))
    registerAppContext({
      apis: {
        impersonationApi: {
          start,
          stop: vi.fn(),
          tenants: vi.fn(),
          candidates: vi.fn(),
        },
      } as unknown as AppContextApis,
    })
    const store = useAuthStore()

    await store.startImpersonation({ targetUserId: 'u-2', tenantId: 'acme', reason: 'ticket 42' })
    expect(start).toHaveBeenCalledWith({ targetUserId: 'u-2', tenantId: 'acme', reason: 'ticket 42' })
    expect(store.session).toMatchObject({
      sessionId: 's-9',
      impersonatorUserName: 'alice',
      user: { userName: 'bob' },
    })
    expect(relaunchApp).toHaveBeenCalledOnce()
    expect(store.impersonationLoading).toBe(true)
    // 换了身份：重建后的偏好水合不拿本地那份给被模仿者播种；标记取一次即清
    expect(takeIdentitySwitchedMarker()).toBe(true)
    expect(takeIdentitySwitchedMarker()).toBe(false)
    // 偏好属于人：上一身份还没发出去的回写不带着新令牌写进新身份
    expect(resetPreferenceBackendSync).toHaveBeenCalledOnce()
    // 重建前的重复点击不再发请求
    await store.startImpersonation({ targetUserId: 'u-3' })
    expect(start).toHaveBeenCalledOnce()
  })

  it('stopImpersonation failure releases the loading flag, keeps the session and rethrows', async () => {
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
    const stop = vi.fn(async () => {
      throw new Error('IDENTITY_VALIDATION_006')
    })
    registerAppContext({
      apis: {
        impersonationApi: {
          start: vi.fn(),
          stop,
          tenants: vi.fn(),
          candidates: vi.fn(),
        },
      } as unknown as AppContextApis,
    })
    const store = useAuthStore()

    await expect(store.stopImpersonation()).rejects.toThrow('IDENTITY_VALIDATION_006')
    expect(store.impersonationLoading).toBe(false)
    expect(store.accessToken).toBe('a-1')
    expect(relaunchApp).not.toHaveBeenCalled()
    expect(takeIdentitySwitchedMarker()).toBe(false)
  })
})

describe('退出登录的本地清理', () => {
  let replace: ReturnType<typeof vi.fn>
  let order: string[]

  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    sessionStorage.clear()
    order = []
    vi.mocked(resetPreferenceBackendSync).mockClear()
    vi.mocked(destroyAllSignalRConnections).mockClear()
    vi.mocked(destroyAllSignalRConnections).mockImplementation(async () => {
      order.push('hub')
    })
    configureRequest({
      baseUrl: '',
      getToken: () => 'a-1',
      fetch: (async () => {
        order.push('api')
        return json(200, { sessionId: 's-1', revokedAt: '2026-10-28T01:00:00Z' })
      }) as unknown as typeof fetch,
    })
    replace = vi.fn(async () => {
      order.push('route')
    })
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace,
        currentRoute: {
          value: { path: '/ai/providers' },
        },
      } as unknown as Router),
    })
    localStorage.setItem('xihan.next.session', JSON.stringify(session('a-1', 'r-1')))
  })

  it('先断开实时连接再吊销会话，最后回登录页；标签栏只剩首页（置顶的也不留）', async () => {
    const tabbar = useTabbarStore()
    tabbar.ensureTab({
      key: '/ai/providers',
      title: 'x',
      path: '/ai/providers',
      closable: true,
    })
    tabbar.togglePin('/ai/providers')
    const store = useAuthStore()

    await store.logout()

    expect(order).toEqual(['hub', 'api', 'route'])
    expect(replace).toHaveBeenCalledWith(LOGIN_PATH)
    expect(store.isAuthenticated).toBe(false)
    expect(store.permissions).toBeNull()
    expect(tabbar.tabs.map(tab => tab.key)).toEqual([HOME_PATH])
    expect(sessionStorage.getItem(TABS_LIST_KEY)).toBeNull()
  })

  it('分屏、会话级存储、锁屏标记与偏好回写通道一起清掉，遮罩当场收到变更', async () => {
    const split = useSplitViewStore()
    split.open('/ai/providers', '/ai/prompts')
    sessionStorage.setItem('xihan.next.oauth-pending', '{}')
    sessionStorage.setItem('any-other-key', '1')
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const lockChanged = vi.fn()
    window.addEventListener(SESSION_LOCK_CHANGED_EVENT, lockChanged)

    await useAuthStore().logout()
    window.removeEventListener(SESSION_LOCK_CHANGED_EVENT, lockChanged)

    expect(split.active).toBe(false)
    expect(sessionStorage.length).toBe(0)
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(lockChanged).toHaveBeenCalledOnce()
    expect(resetPreferenceBackendSync).toHaveBeenCalledOnce()
  })

  it('模仿之后权限没装载成就退出：身份切换标记留给下一次水合，不拿上一身份的偏好播种', async () => {
    sessionStorage.setItem('xihan.next.identity-switched', '1')

    await useAuthStore().logout()

    expect(takeIdentitySwitchedMarker()).toBe(true)
  })

  it('会话已经没了（刷新失败之后从锁屏退出）：不调接口，本地照样清', async () => {
    localStorage.removeItem('xihan.next.session')
    localStorage.setItem(LOCK_STATE_KEY, '1')
    const tabbar = useTabbarStore()
    tabbar.ensureTab({
      key: '/ai/providers',
      title: 'x',
      path: '/ai/providers',
      closable: true,
    })

    await useAuthStore().logout()

    expect(order).toEqual(['hub', 'route'])
    expect(tabbar.tabs.map(tab => tab.key)).toEqual([HOME_PATH])
    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
  })

  it('断开实时连接或吊销失败都不挡退出', async () => {
    vi.mocked(destroyAllSignalRConnections).mockRejectedValueOnce(new Error('hub down'))
    configureRequest({
      baseUrl: '',
      getToken: () => 'a-1',
      fetch: (async () => json(500, { code: 'X' })) as unknown as typeof fetch,
    })
    const store = useAuthStore()

    await store.logout()

    expect(store.isAuthenticated).toBe(false)
    expect(replace).toHaveBeenCalledWith(LOGIN_PATH)
  })

  it('已经在登录页不再跳；路由走不通时整页跳到登录页', async () => {
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace,
        currentRoute: {
          value: { path: LOGIN_PATH },
        },
      } as unknown as Router),
    })
    await useAuthStore().logout()
    expect(replace).not.toHaveBeenCalled()

    const descriptor = Object.getOwnPropertyDescriptor(window, 'location')
    const assign = vi.fn()
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { assign },
    })
    const failing = vi.fn(async () => {
      throw new Error('navigation failed')
    })
    registerAppContext({
      getRouter: () => Promise.resolve({
        replace: failing,
        currentRoute: {
          value: { path: '/' },
        },
      } as unknown as Router),
    })
    await useAuthStore().logout()
    if (descriptor)
      Object.defineProperty(window, 'location', descriptor)
    expect(assign).toHaveBeenCalledOnce()
    expect(String(assign.mock.calls[0]![0])).toContain(LOGIN_PATH)
  })
})

describe('第三方登录与绑定', () => {
  let locationDescriptor: PropertyDescriptor | undefined
  let assign: ReturnType<typeof vi.fn>

  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    sessionStorage.clear()
    configureRequest({ baseUrl: '' })
    // 发起是整页跳转：换掉 window.location，只记下跳去的地址
    locationDescriptor = Object.getOwnPropertyDescriptor(window, 'location')
    assign = vi.fn()
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { assign },
    })
  })

  afterEach(() => {
    if (locationDescriptor)
      Object.defineProperty(window, 'location', locationDescriptor)
  })

  it('登录整页跳到 challenge 端点，带上注册地租户；回跳地址随发起记录带回来，记录取一次即清', () => {
    useAuthStore().startOAuthLogin({ name: 'GitHub', displayName: 'GitHub', type: 'github' }, { tenant: ' acme ', redirect: '/oauth/authorize?client_id=c' })
    expect(assign).toHaveBeenCalledWith('/api/oauth/GitHub/challenge?mode=login&tenant=acme')
    expect(takeOAuthPending()).toMatchObject({ mode: 'login', provider: 'github', redirect: '/oauth/authorize?client_id=c' })
    expect(takeOAuthPending()).toBeNull()
  })

  it('平台账号（租户留空）不带 tenant；名称按路径段编码，接口源取请求层的 baseUrl', () => {
    useAuthStore().startOAuthLogin({ name: 'corp sso', displayName: 'SSO', type: 'wecom' }, { tenant: '  ' })
    expect(assign).toHaveBeenCalledWith('/api/oauth/corp%20sso/challenge?mode=login')
    configureRequest({ baseUrl: 'https://api.example' })
    expect(oauthChallengeUrl('github')).toBe('https://api.example/api/oauth/github/challenge?mode=login')
  })

  it('绑定先领本人的绑定意图，整页跳去后端给的地址（接口源取请求层的 baseUrl），发起记录标明用途；领不到即不跳、不留记录', async () => {
    const beginOAuthBindApi = vi.fn().mockResolvedValue('/api/oauth/wecom/challenge?mode=bind&intent=i-1')
    registerAppContext({
      apis: { beginOAuthBindApi } as unknown as AppContextApis,
    })
    configureRequest({ baseUrl: 'https://api.example' })
    await useAuthStore().startOAuthBind('WeCom')
    expect(beginOAuthBindApi).toHaveBeenCalledWith('WeCom')
    expect(assign).toHaveBeenCalledWith('https://api.example/api/oauth/wecom/challenge?mode=bind&intent=i-1')
    expect(takeOAuthPending()).toMatchObject({ mode: 'bind', provider: 'wecom' })

    assign.mockClear()
    beginOAuthBindApi.mockRejectedValue(new Error('denied'))
    await expect(useAuthStore().startOAuthBind('wecom')).rejects.toThrow('denied')
    expect(assign).not.toHaveBeenCalled()
    expect(takeOAuthPending()).toBeNull()
  })

  it('发起记录过期（超过后端 state 的 10 分钟）或被改坏即作废', () => {
    sessionStorage.setItem('xihan.next.oauth-pending', JSON.stringify({ mode: 'login', provider: 'github', startedAt: Date.now() - 11 * 60 * 1000 }))
    expect(takeOAuthPending()).toBeNull()
    sessionStorage.setItem('xihan.next.oauth-pending', JSON.stringify({
      mode: 'admin',
      provider: 'github',
      startedAt: Date.now(),
    }))
    expect(takeOAuthPending()).toBeNull()
    sessionStorage.setItem('xihan.next.oauth-pending', '{')
    expect(takeOAuthPending()).toBeNull()
  })

  it('回调票据经 auth.external-login 匿名换令牌，与密码登录同样存会话、跳回跳地址', async () => {
    const calls: { url: string, body: unknown, authorization?: string }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 'stale',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({
          url: String(url),
          body: JSON.parse(String(init!.body)),
          authorization: (init!.headers as Record<string, string>).Authorization,
        })
        return json(200, {
          token: session('a-1', 'r-1'),
          twoFactor: null,
        })
      }) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({ replace } as unknown as Router),
    })
    const store = useAuthStore()

    await store.handleOAuthCallback('x-1', '/settings/configs')
    expect(calls).toEqual([
      {
        url: '/api/auth:external-login',
        body: { ticket: 'x-1' },
        authorization: undefined,
      },
    ])
    expect(store.accessToken).toBe('a-1')
    expect(replace).toHaveBeenCalledWith('/settings/configs')
    expect(store.loginLoading).toBe(false)
    expect(store.takePendingTwoFactor()).toBeNull()
  })

  it('账号开了双因素：不存会话，记下挑战带着回跳地址回登录页；登录页取走挑战后第二段照常凭票据验证', async () => {
    const bodies: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        bodies.push({
          url: String(url),
          body: JSON.parse(String(init!.body)),
        })
        if (String(url).endsWith('/api/auth:external-login')) {
          return json(200, {
            token: null,
            twoFactor: {
              ticket: 't-1',
              methods: ['Totp'],
              method: 'Totp',
              codeSent: false,
              expiresAt: '2026-10-28T00:10:00Z',
            },
          })
        }
        return json(200, session('a-1', 'r-1'))
      }) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({ replace } as unknown as Router),
    })
    const store = useAuthStore()

    await store.handleOAuthCallback('x-1', '/oauth/authorize?client_id=c')
    expect(store.isAuthenticated).toBe(false)
    expect(store.loginLoading).toBe(false)
    expect(replace).toHaveBeenCalledWith({
      path: '/auth/login',
      query: { redirect: '/oauth/authorize?client_id=c' },
    })
    expect(store.takePendingTwoFactor()).toEqual({
      requiresTwoFactor: true,
      availableTwoFactorMethods: ['totp'],
      twoFactorMethod: 'totp',
      codeSent: false,
      twoFactorTicket: 't-1',
      token: null,
    })
    expect(store.takePendingTwoFactor()).toBeNull()

    // 登录页的验证阶段走的就是密码登录的第二段
    expect(await store.login({
      username: '',
      password: '',
      twoFactorTicket: 't-1',
      twoFactorMethod: 'totp',
      twoFactorCode: '654321',
    }, '/oauth/authorize?client_id=c')).toBeNull()
    expect(bodies[1]).toEqual({
      url: '/api/auth:verify-two-factor',
      body: { ticket: 't-1', method: 'Totp', code: '654321' },
    })
    expect(store.accessToken).toBe('a-1')
  })

  it('站外回跳地址不带回登录页；换票据失败原样抛给回调页并放开加载态', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async () => json(200, {
        token: null,
        twoFactor: {
          ticket: 't-1',
          methods: ['Totp'],
          method: 'Totp',
          codeSent: false,
          expiresAt: '2026-10-28T00:10:00Z',
        },
      })) as unknown as typeof fetch,
    })
    const replace = vi.fn(async () => {})
    registerAppContext({
      getRouter: () => Promise.resolve({ replace } as unknown as Router),
    })
    const store = useAuthStore()
    await store.handleOAuthCallback('x-1', '//evil.example')
    expect(replace).toHaveBeenCalledWith({ path: '/auth/login', query: {} })

    configureRequest({
      baseUrl: '',
      getToken: () => null,
      fetch: (async () => json(403, { code: 'IDENTITY_DENIED_015', status: 403, detail: 'not bound' }, 'application/problem+json')) as unknown as typeof fetch,
    })
    await expect(store.handleOAuthCallback('x-2')).rejects.toMatchObject({ code: 'IDENTITY_DENIED_015', detail: 'not bound' })
    expect(store.loginLoading).toBe(false)
    expect(store.isAuthenticated).toBe(false)
  })
})
