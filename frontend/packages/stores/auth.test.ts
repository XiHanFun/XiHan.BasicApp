/**
 * 认证 Store（auth）单元测试。
 * 职责边界：登录（密码/短信/邮箱/OAuth 回调）后的令牌落地、用户与权限装载、动态路由注册、
 * 智能落点与 redirect 校验、会话锁定时的特殊放行，以及登出的清场顺序与容错。
 * 路由、API、SignalR 均以替身注入；不发真实请求、不做真实导航。
 */
import type { RouteRecordRaw } from 'vue-router'
import type { AppContextApis, LoginToken, MenuRoute, PermissionInfo, UserInfo } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { HOME_PATH, LOCK_REASON_KEY, LOCK_STATE_KEY, LOGIN_PATH, REFRESH_TOKEN_KEY, TOKEN_KEY, USER_INFO_KEY } from '~/constants'
import { useAccessStore } from './access'
import { useAppStore } from './app'
import { registerAppContext } from './app-context'
import { useAuthStore } from './auth'
import { resetPreferenceBackendSync } from './helpers'
import { useTabbarStore } from './tabbar'
import { useUserStore } from './user'

const destroyAllSignalRConnections = vi.fn<() => Promise<void>>()
const mapMenuToRoutes = vi.fn<(menus: MenuRoute[]) => RouteRecordRaw[]>()

vi.mock('~/composables/useSignalR', () => ({
  destroyAllSignalRConnections: (...args: []) => destroyAllSignalRConnections(...args),
}))

vi.mock('~/router/dynamic', () => ({
  mapMenuToRoutes: (menus: MenuRoute[]) => mapMenuToRoutes(menus),
}))

interface FakeRoute { name?: string, path: string }

/** 最小可用的 vue-router 替身：只实现 auth store 真正用到的那几个方法 */
function createFakeRouter(initial: FakeRoute[] = []) {
  const routes: FakeRoute[] = [...initial]
  return {
    routes,
    replace: vi.fn<(to: string) => Promise<void>>().mockResolvedValue(undefined),
    getRoutes: () => routes.map(item => ({ ...item })),
    hasRoute: (name: string) => routes.some(item => item.name === name),
    addRoute: vi.fn((_parent: string, route: FakeRoute) => {
      routes.push(route)
    }),
    removeRoute: vi.fn((name: string) => {
      const index = routes.findIndex(item => item.name === name)
      if (index >= 0) {
        routes.splice(index, 1)
      }
    }),
    resolve: vi.fn((target: string) => ({
      matched: target.startsWith('/known') ? [{ path: target }] : [],
      name: target.startsWith('/known') ? 'Known' : 'NotFound',
    })),
  }
}

type FakeRouter = ReturnType<typeof createFakeRouter>

const loginApi = vi.fn()
const phoneLoginApi = vi.fn()
const emailLoginApi = vi.fn()
const logoutApi = vi.fn<() => Promise<void>>()
const getUserInfoApi = vi.fn<() => Promise<UserInfo>>()
const getPermissionsApi = vi.fn<() => Promise<PermissionInfo>>()
const userSettingGet = vi.fn()
const userSettingSave = vi.fn()

let router: FakeRouter
let locationDescriptor: PropertyDescriptor | undefined

function token(overrides?: Partial<LoginToken>): LoginToken {
  return {
    accessToken: 'access-1',
    refreshToken: 'refresh-1',
    tokenType: 'Bearer',
    expiresIn: 3600,
    issuedAt: '2024-01-01T00:00:00.000Z',
    expiresAt: '2024-01-01T01:00:00.000Z',
    ...overrides,
  }
}

function user(overrides?: Partial<UserInfo>): UserInfo {
  return {
    basicId: 'u-1',
    userName: 'admin',
    appTitle: '租户甲',
    appLogo: '/tenant-a.png',
    tenantId: 't-1',
    roles: [],
    permissions: [],
    ...overrides,
  }
}

function permissions(overrides?: Partial<PermissionInfo>): PermissionInfo {
  return { roles: ['admin'], permissions: ['system:user:list'], menus: [], ...overrides }
}

function registerContext(options?: {
  staticRoutes?: RouteRecordRaw[]
  controlCenter?: string
}): void {
  registerAppContext({
    getRouter: () => Promise.resolve(router as never),
    getStaticRoutes: () => options?.staticRoutes ?? [],
    // shellRoutes 是逐字段合并的，用空串把上一个用例设过的控制中心关掉
    shellRoutes: { controlCenter: options?.controlCenter ?? '' },
    apis: {
      loginApi,
      phoneLoginApi,
      emailLoginApi,
      logoutApi,
      getUserInfoApi,
      getPermissionsApi,
      userSettingApi: { get: userSettingGet, save: userSettingSave },
    } as unknown as AppContextApis,
  })
}

beforeEach(() => {
  vi.clearAllMocks()
  resetPreferenceBackendSync()
  router = createFakeRouter()
  destroyAllSignalRConnections.mockResolvedValue(undefined)
  mapMenuToRoutes.mockReturnValue([])
  logoutApi.mockResolvedValue(undefined)
  getUserInfoApi.mockResolvedValue(user())
  getPermissionsApi.mockResolvedValue(permissions())
  userSettingGet.mockResolvedValue({ scene: 0, settingKey: 'global', settingValue: '{}' })
  userSettingSave.mockResolvedValue({ scene: 0, settingKey: 'global' })
  registerContext()
  setActivePinia(createPinia())
  // 先建 appStore，让偏好注册表指向当前 pinia 的 ref
  useAppStore()
})

afterEach(() => {
  if (locationDescriptor) {
    Object.defineProperty(window, 'location', locationDescriptor)
    locationDescriptor = undefined
  }
})

/** 用可写替身接管 window.location，afterEach 会还原 */
function stubLocation(): { href: string } {
  locationDescriptor = Object.getOwnPropertyDescriptor(window, 'location')
  const fake = { href: '' }
  Object.defineProperty(window, 'location', { configurable: true, writable: true, value: fake })
  return fake
}

describe('密码登录', () => {
  it('需要双因素时原样返回响应，不落令牌也不导航', async () => {
    const response = { requiresTwoFactor: true, availableTwoFactorMethods: ['totp'], token: null }
    loginApi.mockResolvedValue(response)
    const auth = useAuthStore()

    const result = await auth.login({ username: 'a', password: 'b' })

    expect(result).toBe(response)
    expect(useAccessStore().accessToken).toBeNull()
    expect(router.replace).not.toHaveBeenCalled()
  })

  it('登录成功返回 null，并把两枚令牌写入 store 与 localStorage', async () => {
    loginApi.mockResolvedValue({ requiresTwoFactor: false, token: token() })
    const auth = useAuthStore()

    const result = await auth.login({ username: 'a', password: 'b' })

    expect(result).toBeNull()
    expect(useAccessStore().accessToken).toBe('access-1')
    expect(localStorage.getItem(TOKEN_KEY)).toBe(JSON.stringify('access-1'))
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe(JSON.stringify('refresh-1'))
  })

  it('响应既不要求双因素又没有 token 时什么都不做', async () => {
    loginApi.mockResolvedValue({ requiresTwoFactor: false, token: null })
    const auth = useAuthStore()

    const result = await auth.login({ username: 'a', password: 'b' })

    expect(result).toBeNull()
    expect(getUserInfoApi).not.toHaveBeenCalled()
    expect(router.replace).not.toHaveBeenCalled()
  })

  it('loginLoading 在调用期间为 true、结束后复位', async () => {
    let duringCall = false
    const auth = useAuthStore()
    loginApi.mockImplementation(() => {
      duringCall = auth.loginLoading
      return Promise.resolve({ requiresTwoFactor: false, token: null })
    })

    await auth.login({ username: 'a', password: 'b' })

    expect(duringCall).toBe(true)
    expect(auth.loginLoading).toBe(false)
  })

  it('登录接口抛错时 loginLoading 也会复位，错误继续上抛', async () => {
    loginApi.mockRejectedValue(new Error('账号或密码错误'))
    const auth = useAuthStore()

    await expect(auth.login({ username: 'a', password: 'b' })).rejects.toThrow(/账号或密码错误/)
    expect(auth.loginLoading).toBe(false)
  })
})

describe('登录后的用户与权限装载', () => {
  it('用户信息与权限信息合并写入 user store', async () => {
    getPermissionsApi.mockResolvedValue(permissions({ roles: ['admin', 'ops'], permissions: ['a', 'b'] }))
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    const userStore = useUserStore()
    expect(userStore.roles).toEqual(['admin', 'ops'])
    expect(userStore.permissions).toEqual(['a', 'b'])
    expect(userStore.username).toBe('admin')
    expect(JSON.parse(localStorage.getItem(USER_INFO_KEY) ?? 'null')).toMatchObject({ roles: ['admin', 'ops'] })
  })

  it('权限码与菜单写入 access store，并标记路由已装载', async () => {
    const menus: MenuRoute[] = [{ path: '/system', name: 'System', meta: { title: '系统' } }]
    getPermissionsApi.mockResolvedValue(permissions({ permissions: ['x'], menus }))
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    const accessStore = useAccessStore()
    expect(accessStore.hasCode('x')).toBe(true)
    expect(accessStore.accessRoutes).toEqual(menus)
    expect(accessStore.isRoutesLoaded).toBe(true)
  })

  it('后端下发的品牌信息覆盖 appStore 的标题与 Logo', async () => {
    getUserInfoApi.mockResolvedValue(user({ appTitle: '租户乙', appLogo: '/tenant-b.png' }))
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    const appStore = useAppStore()
    expect(appStore.brandTitle).toBe('租户乙')
    expect(appStore.brandLogo).toBe('/tenant-b.png')
  })

  it('用户信息里 appLogo 为空串时品牌 Logo 回落默认', async () => {
    getUserInfoApi.mockResolvedValue(user({ appLogo: '' }))
    const appStore = useAppStore()
    appStore.setBrandLogo('/上一个租户.png')
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(appStore.brandLogo).toBe('/favicon.png')
  })

  it('用户信息与权限信息并发拉取（Promise.all），两个接口各调用一次', async () => {
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(getUserInfoApi).toHaveBeenCalledTimes(1)
    expect(getPermissionsApi).toHaveBeenCalledTimes(1)
  })

  it('登录流程会拉取后端偏好并覆盖本地', async () => {
    userSettingGet.mockResolvedValue({
      scene: 0,
      settingKey: 'global',
      settingValue: JSON.stringify({ xihan_theme_color: '#ff00ff' }),
    })
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(useAppStore().themeColor).toBe('#ff00ff')
  })
})

describe('动态路由注册', () => {
  it('菜单映射出的新路由被挂到 RootLayout 之下', async () => {
    mapMenuToRoutes.mockReturnValue([{ path: '/order', name: 'Order', component: {} } as RouteRecordRaw])
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.addRoute).toHaveBeenCalledTimes(1)
    expect(router.addRoute.mock.calls[0]?.[0]).toBe('RootLayout')
  })

  it('路径已存在的路由不重复注册', async () => {
    router = createFakeRouter([{ name: 'Existing', path: '/order' }])
    registerContext()
    mapMenuToRoutes.mockReturnValue([{ path: '/order', name: 'Order', component: {} } as RouteRecordRaw])
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.addRoute).not.toHaveBeenCalled()
  })

  it('同名路由已注册时不重复注册', async () => {
    router = createFakeRouter([{ name: 'Order', path: '/other' }])
    registerContext()
    mapMenuToRoutes.mockReturnValue([{ path: '/order', name: 'Order', component: {} } as RouteRecordRaw])
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.addRoute).not.toHaveBeenCalled()
  })

  it('没有路由名的映射结果被跳过（KeepAlive 与移除都依赖名字）', async () => {
    mapMenuToRoutes.mockReturnValue([{ path: '/anonymous', component: {} } as RouteRecordRaw])
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.addRoute).not.toHaveBeenCalled()
  })
})

describe('登录落点', () => {
  it('无 redirect 时跳到 access store 派生的首页', async () => {
    getPermissionsApi.mockResolvedValue(permissions({
      menus: [{ path: '/workbench', name: 'Workbench', meta: { title: '工作台' } }],
    }))
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.replace).toHaveBeenCalledWith('/workbench')
  })

  it('菜单为空时回落到 HOME_PATH', async () => {
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.replace).toHaveBeenCalledWith(HOME_PATH)
  })

  it('redirect 能解析到有效路由时跳向它', async () => {
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token(), '/known/page')

    expect(router.replace).toHaveBeenCalledWith('/known/page')
  })

  it('redirect 会先做 URL 解码再解析', async () => {
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token(), encodeURIComponent('/known/page?a=1&b=2'))

    expect(router.resolve).toHaveBeenCalledWith('/known/page?a=1&b=2')
  })

  it('redirect 解析不到路由时回落首页，不把用户丢到 404', async () => {
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token(), '/unknown/page')

    expect(router.replace).toHaveBeenCalledWith(HOME_PATH)
  })

  it('未进入租户且应用注册了控制中心时落到控制中心', async () => {
    getUserInfoApi.mockResolvedValue(user({ tenantId: null }))
    registerContext({ controlCenter: '/control-center' })
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token(), '/known/page')

    expect(router.replace).toHaveBeenCalledWith('/control-center')
  })

  it('未进入租户但应用没有控制中心概念时按普通首页走', async () => {
    getUserInfoApi.mockResolvedValue(user({ tenantId: null }))
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.replace).toHaveBeenCalledWith(HOME_PATH)
  })

  it('已进入唯一租户时即使配了控制中心也走正常首页', async () => {
    registerContext({ controlCenter: '/control-center' })
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(router.replace).toHaveBeenCalledWith(HOME_PATH)
  })
})

describe('拉取用户信息失败的两条分支', () => {
  it('会话被服务端锁定时保留令牌并进入应用壳层（强制改密遮罩接管）', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'PasswordChangeRequired')
    getUserInfoApi.mockRejectedValue(new Error('423 Locked'))
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(useAccessStore().accessToken).toBe('access-1')
    expect(router.replace).toHaveBeenCalledWith(HOME_PATH)
  })

  it('未锁定时清空令牌与用户信息并把错误上抛', async () => {
    getPermissionsApi.mockRejectedValue(new Error('500'))
    const auth = useAuthStore()

    await expect(auth.handleOAuthCallback(token())).rejects.toThrow(/500/)

    expect(useAccessStore().accessToken).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
    expect(useUserStore().userInfo).toBeNull()
    expect(router.replace).not.toHaveBeenCalled()
  })

  it('锁定分支不会继续装载权限或注册动态路由', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    getUserInfoApi.mockRejectedValue(new Error('423'))
    mapMenuToRoutes.mockReturnValue([{ path: '/x', name: 'X', component: {} } as RouteRecordRaw])
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token())

    expect(useAccessStore().isRoutesLoaded).toBe(false)
    expect(router.addRoute).not.toHaveBeenCalled()
  })
})

describe('短信 / 邮箱验证码登录', () => {
  it('短信登录走 phoneLoginApi 并复用同一套登录后处理', async () => {
    phoneLoginApi.mockResolvedValue(token({ accessToken: 'phone-token' }))
    const auth = useAuthStore()

    await auth.loginByPhoneCode({ phone: '13800000000', code: '123456' })

    expect(phoneLoginApi).toHaveBeenCalledWith({ phone: '13800000000', code: '123456' })
    expect(useAccessStore().accessToken).toBe('phone-token')
  })

  it('邮箱登录走 emailLoginApi 并复用同一套登录后处理', async () => {
    emailLoginApi.mockResolvedValue(token({ accessToken: 'email-token' }))
    const auth = useAuthStore()

    await auth.loginByEmailCode({ email: 'a@b.c', code: '654321' }, '/known/page')

    expect(emailLoginApi).toHaveBeenCalledWith({ email: 'a@b.c', code: '654321' })
    expect(router.replace).toHaveBeenCalledWith('/known/page')
  })

  it('验证码登录失败时 loginLoading 复位并上抛', async () => {
    phoneLoginApi.mockRejectedValue(new Error('验证码错误'))
    const auth = useAuthStore()

    await expect(auth.loginByPhoneCode({ phone: '1', code: '2' })).rejects.toThrow(/验证码错误/)
    expect(auth.loginLoading).toBe(false)
  })

  it('第三方回调复用同一套登录后处理并复位 loading', async () => {
    const auth = useAuthStore()

    await auth.handleOAuthCallback(token({ accessToken: 'oauth-token' }))

    expect(useAccessStore().accessToken).toBe('oauth-token')
    expect(auth.loginLoading).toBe(false)
  })
})

describe('第三方登录跳转', () => {
  it('跳转地址带上编码后的 provider 名', () => {
    const fakeLocation = stubLocation()
    const auth = useAuthStore()

    auth.startOAuthLogin({ name: 'GitHub 登录', displayName: 'GitHub' })

    expect(fakeLocation.href).toContain('/OAuth/ExternalLogin?provider=')
    expect(fakeLocation.href).toContain(encodeURIComponent('GitHub 登录'))
    expect(fakeLocation.href).not.toContain('GitHub 登录')
  })
})

describe('登出清场', () => {
  it('销毁 SignalR 连接、调用登出接口、跳回登录页', async () => {
    const auth = useAuthStore()

    await auth.logout()

    expect(destroyAllSignalRConnections).toHaveBeenCalledTimes(1)
    expect(logoutApi).toHaveBeenCalledTimes(1)
    expect(router.replace).toHaveBeenCalledWith(LOGIN_PATH)
  })

  it('清空令牌、用户信息、标签页与会话存储', async () => {
    const accessStore = useAccessStore()
    accessStore.setAccessToken('a')
    accessStore.setRefreshToken('r')
    useUserStore().setUserInfo(user())
    const tabbar = useTabbarStore()
    tabbar.ensureTab({ key: '/a', title: 'A', path: '/a', closable: true })
    sessionStorage.setItem('some_session_key', '1')

    await useAuthStore().logout()

    expect(accessStore.accessToken).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
    expect(localStorage.getItem(USER_INFO_KEY)).toBeNull()
    expect(tabbar.tabs.every(item => !item.closable)).toBe(true)
    expect(sessionStorage.getItem('some_session_key')).toBeNull()
  })

  it('清除锁屏标记 —— 它在 localStorage，sessionStorage.clear() 清不掉', async () => {
    localStorage.setItem(LOCK_STATE_KEY, '1')
    localStorage.setItem(LOCK_REASON_KEY, 'ScreenLock')

    await useAuthStore().logout()

    expect(localStorage.getItem(LOCK_STATE_KEY)).toBeNull()
    expect(localStorage.getItem(LOCK_REASON_KEY)).toBeNull()
  })

  it('移除动态路由，保留核心路由（登录 / 错误页）', async () => {
    router = createFakeRouter([
      { name: 'Login', path: LOGIN_PATH },
      { name: 'NotFound', path: '/404' },
      { name: 'DynamicOrder', path: '/order' },
    ])
    registerContext()

    await useAuthStore().logout()

    expect(router.removeRoute).toHaveBeenCalledTimes(1)
    expect(router.removeRoute).toHaveBeenCalledWith('DynamicOrder')
    expect(router.routes.map(r => r.name)).toEqual(['Login', 'NotFound'])
  })

  it('保留应用侧注册的静态路由 —— 名单是真派生的，新增静态页无需回来改这里', async () => {
    router = createFakeRouter([
      { name: 'AboutProject', path: '/about' },
      { name: 'OAuthAuthorize', path: '/oauth/authorize' },
      { name: 'DynamicOrder', path: '/order' },
    ])
    registerContext({
      staticRoutes: [
        { path: '/root', name: 'RootLayout', component: {}, children: [
          { path: '/about', name: 'AboutProject', component: {} },
          { path: '/oauth/authorize', name: 'OAuthAuthorize', component: {} },
        ] } as RouteRecordRaw,
      ],
    })

    await useAuthStore().logout()

    expect(router.routes.map(r => r.name)).toEqual(['AboutProject', 'OAuthAuthorize'])
  })

  it('个人中心由后端菜单动态注册，登出时仍被显式保留', async () => {
    router = createFakeRouter([{ name: 'Profile', path: '/profile' }])
    registerContext()

    await useAuthStore().logout()

    expect(router.removeRoute).not.toHaveBeenCalled()
  })

  it('无名路由不会被尝试移除', async () => {
    router = createFakeRouter([{ path: '/anonymous' }])
    registerContext()

    await useAuthStore().logout()

    expect(router.removeRoute).not.toHaveBeenCalled()
  })

  it('实时连接销毁失败不影响后续清场', async () => {
    destroyAllSignalRConnections.mockRejectedValue(new Error('hub 已断开'))

    await expect(useAuthStore().logout()).resolves.toBeUndefined()
    expect(logoutApi).toHaveBeenCalledTimes(1)
    expect(router.replace).toHaveBeenCalledWith(LOGIN_PATH)
  })

  it('登出接口失败不影响本地清场', async () => {
    logoutApi.mockRejectedValue(new Error('网络不可用'))
    const accessStore = useAccessStore()
    accessStore.setAccessToken('a')

    await useAuthStore().logout()

    expect(accessStore.accessToken).toBeNull()
    expect(router.replace).toHaveBeenCalledWith(LOGIN_PATH)
  })

  it('单条路由移除抛错不影响其余路由的移除', async () => {
    router = createFakeRouter([
      { name: 'BadOne', path: '/bad' },
      { name: 'GoodOne', path: '/good' },
    ])
    router.removeRoute.mockImplementation((name: string) => {
      if (name === 'BadOne') {
        throw new Error('无法移除')
      }
      const index = router.routes.findIndex(item => item.name === name)
      if (index >= 0) {
        router.routes.splice(index, 1)
      }
    })
    registerContext()

    await useAuthStore().logout()

    expect(router.routes.map(r => r.name)).toEqual(['BadOne'])
    expect(router.replace).toHaveBeenCalledWith(LOGIN_PATH)
  })

  it('路由跳转失败时退化为整页跳转登录页', async () => {
    const fakeLocation = stubLocation()
    router.replace.mockRejectedValue(new Error('导航被中断'))

    await useAuthStore().logout()

    expect(fakeLocation.href).toContain(LOGIN_PATH)
  })
})
