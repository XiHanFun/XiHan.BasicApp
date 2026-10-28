import type { Router, RouteRecordRaw } from 'vue-router'
/**
 * 路由守卫（guard.ts，与 4.x 同一套行为）：
 * 登录态分支、后端菜单装载与失败收敛、首页与 404、meta 权限校验，以及 UI 副作用——
 * 顶部进度条与页面 loading 遮罩的开收、document.title、标签栏的写入 / 固定 / 裁剪。
 */
import type { MenuNode, Permissions, Session } from '~/stores'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { FORBIDDEN_PATH, HOME_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
import { configureRequest } from '~/request'

const stubs = vi.hoisted(() => ({
  loadingBar: {
    start: vi.fn(),
    finish: vi.fn(),
    finishAll: vi.fn(),
    error: vi.fn(),
  },
  isLockedState: vi.fn(() => false),
  hydratePreferencesFromBackend: vi.fn(async () => {}),
  markPreferenceIdentitySwitched: vi.fn(),
}))

vi.mock('~/composables', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/composables')>()
  return { ...actual, loadingBar: stubs.loadingBar, isLockedState: stubs.isLockedState }
})

vi.mock('~/stores', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/stores')>()
  return {
    ...actual,
    hydratePreferencesFromBackend: stubs.hydratePreferencesFromBackend,
    markPreferenceIdentitySwitched: stubs.markPreferenceIdentitySwitched,
  }
})

// 菜单里的组件按页面登记解析：这里换成可控的空页面，避免加载真实的能力包视图
vi.mock('./views', () => ({
  resolveView: (component: string) => {
    if (component.includes('missing'))
      throw new Error(`View '${component}' is not found`)
    return async () => ({ default: defineComponent({ name: 'MenuPageProbe', render: () => null }) })
  },
  viewKeys: () => [],
}))

const { useAppStore, useAuthStore, useTabbarStore } = await import('~/stores')
const { setupRouterGuard } = await import('./guard')

const Blank = defineComponent({ name: 'BlankProbe', render: () => null })

function baseRoutes(): RouteRecordRaw[] {
  return [
    { path: '/', name: 'RootLayout', component: Blank, children: [] },
    {
      path: '/auth',
      name: 'Authentication',
      component: Blank,
      children: [
        { path: 'login', name: 'Login', component: Blank },
        { path: 'register', name: 'Register', component: Blank },
      ],
    },
    { path: HOME_PATH, name: 'Dashboard', component: Blank, meta: { title: 'page.control_center.title' } },
    { path: '/identity/user', name: 'IdentityUser', component: Blank, meta: { title: 'error.forbidden', icon: 'mdi:account', keepAlive: true } },
    { path: '/plain', name: 'Plain', component: Blank, meta: { title: '自定义标题🚀' } },
    { path: '/affix', name: 'Affix', component: Blank, meta: { title: 'error.server_error', affixTab: true } },
    { path: '/standalone', name: 'Standalone', component: Blank, meta: { title: 'page.control_center.title', standalone: true } },
    { path: '/no-title', name: 'NoTitle', component: Blank },
    { path: '/anonymous', component: Blank },
    { path: '/broken', name: 'Broken', component: () => Promise.reject(new Error('chunk load failed')) },
    { path: FORBIDDEN_PATH, name: 'Forbidden', component: Blank },
    { path: SERVER_ERROR_PATH, name: 'ServerError', component: Blank },
    { path: NOT_FOUND_PATH, name: 'NotFound', component: Blank, meta: { title: 'error.not_found' } },
    { path: '/:pathMatch(.*)*', name: 'NotFoundCatchAll', component: Blank },
  ]
}

function createGuardedRouter(extra: RouteRecordRaw[] = []): Router {
  const router = createRouter({ history: createMemoryHistory(), routes: [...baseRoutes(), ...extra] })
  setupRouterGuard(router)
  return router
}

const user = { id: 'u-1', userName: 'alice', displayName: 'Alice', email: null, phoneNumber: null, homeTenantId: 'platform', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null } as Session['user']

function session(): Session {
  return { accessToken: 'token-abc', accessExpiresAt: '2026-10-28T02:00:00Z', refreshToken: 'r-1', refreshExpiresAt: '2026-11-04T00:00:00Z', sessionId: 's-1', tenantId: null, tenantName: null, impersonatorUserName: null, mustChangePassword: false, user } as Session
}

function page(code: string, route: string, extra: Partial<MenuNode> = {}): MenuNode {
  return { code, kind: 'Page', route, component: `${route.slice(1)}/index`, titleKey: `${code}.title`, title: null, icon: null, order: 0, permission: null, externalUrl: null, hidden: false, keepAlive: false, affix: false, children: [], ...extra }
}

function permissions(menus: MenuNode[] = [page('workbench.dashboard', HOME_PATH)], codes: string[] = [], roles: string[] = ['admin']): Permissions {
  return { permissions: codes, roles: roles.map(code => ({ id: `r-${code}`, code, name: code })), menus, pages: [], isPlatform: true }
}

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

/** 已登录、权限与菜单已装载（不触发装载分支） */
function signInWithLoadedRoutes(loaded: Permissions = permissions()): void {
  const auth = useAuthStore()
  auth.session = session()
  auth.permissions = loaded
}

/** 已登录、权限未装载：守卫会经 auth.permissions 拉一次菜单 */
function signInPending(respond: () => Response | Promise<Response>): { calls: () => number } {
  useAuthStore().session = session()
  let count = 0
  configureRequest({
    baseUrl: '',
    getToken: () => 'token-abc',
    fetch: (async (url: RequestInfo | URL) => {
      if (String(url).endsWith('/api/auth:permissions')) {
        count++
        return respond()
      }
      return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: String(url) }, 'application/problem+json')
    }) as unknown as typeof fetch,
  })
  return { calls: () => count }
}

function serverDown(): Response {
  return json(500, { code: 'KERNEL_UNEXPECTED_000', status: 500, detail: 'menus down', traceId: 't' }, 'application/problem+json')
}

let originalTitle = ''
let consoleErrorSpy: ReturnType<typeof vi.spyOn>

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  sessionStorage.clear()
  originalTitle = document.title
  stubs.loadingBar.start.mockClear()
  stubs.loadingBar.finish.mockClear()
  stubs.loadingBar.finishAll.mockClear()
  stubs.loadingBar.error.mockClear()
  stubs.isLockedState.mockReset()
  stubs.isLockedState.mockReturnValue(false)
  stubs.hydratePreferencesFromBackend.mockReset()
  stubs.hydratePreferencesFromBackend.mockResolvedValue(undefined)
  stubs.markPreferenceIdentitySwitched.mockClear()
  consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  document.title = originalTitle
  consoleErrorSpy.mockRestore()
})

describe('未登录', () => {
  it('访问受保护页跳登录并把原始 fullPath 写进 redirect', async () => {
    const router = createGuardedRouter()
    await router.push('/identity/user?keyword=a')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(router.currentRoute.value.query.redirect).toBe('/identity/user?keyword=a')
  })

  it('登录页与认证前缀下的其它页面放行，不会自跳成死循环', async () => {
    const router = createGuardedRouter()
    await router.push(LOGIN_PATH)
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    await router.push('/auth/register')
    expect(router.currentRoute.value.path).toBe('/auth/register')
  })

  it('403 / 404 / 500 在白名单内，未登录也能看到', async () => {
    const router = createGuardedRouter()
    for (const path of [FORBIDDEN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH]) {
      await router.push(path)
      expect(router.currentRoute.value.path).toBe(path)
    }
  })

  it('未登录不会请求权限接口', async () => {
    const pending = signInPending(() => json(200, permissions()))
    useAuthStore().session = null
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(pending.calls()).toBe(0)
  })
})

describe('已登录访问认证页', () => {
  it('被弹回菜单推导出的首页，认证前缀下的注册页同样弹回', async () => {
    signInWithLoadedRoutes(permissions([page('identity.users', '/identity/user')]))
    const router = createGuardedRouter()
    await router.push(LOGIN_PATH)
    expect(router.currentRoute.value.path).toBe('/identity/user')
    await router.push('/auth/register')
    expect(router.currentRoute.value.path).toBe('/identity/user')
  })
})

describe('后端菜单装载', () => {
  it('菜单装成可导航的路由，装载完成后重进当前地址，query 与 hash 原样保留', async () => {
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users', { permission: 'identity.users.read' })], ['identity.users.read'])))
    const router = createGuardedRouter()
    await router.push('/identity/users?keyword=abc#top')
    expect(router.currentRoute.value.name).toBe('identity.users')
    expect(router.currentRoute.value.fullPath).toBe('/identity/users?keyword=abc#top')
  })

  it('目录节点带子路由并自动重定向到第一个子页面', async () => {
    const directory: MenuNode = { ...page('custom.system', ''), kind: 'Directory', component: '', titleKey: '', title: 'System', children: [page('access.roles', '/access/roles'), page('identity.users', '/identity/users')] }
    signInPending(() => json(200, permissions([directory])))
    const router = createGuardedRouter()
    await router.push('/custom.system')
    expect(router.currentRoute.value.path).toBe('/access/roles')
  })

  it('外链菜单不落地成路由，但仍完整保存在菜单投影里供菜单渲染', async () => {
    const link: MenuNode = { ...page('custom.docs', ''), kind: 'Link', component: '', titleKey: '', title: 'Docs', externalUrl: 'https://docs.xihanfun.com' }
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users'), link])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.hasRoute('custom.docs')).toBe(false)
    expect(useAuthStore().menus.some(node => node.code === 'custom.docs')).toBe(true)
  })

  it('组件找不到的菜单回落到 404 兜底视图并留下错误日志，不连累其它页面', async () => {
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users'), page('identity.broken', '/identity/missing')])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.currentRoute.value.name).toBe('identity.users')
    expect(router.hasRoute('identity.broken')).toBe(true)
    expect(consoleErrorSpy).toHaveBeenCalledWith(expect.stringContaining('[router]'), 'identity/missing/index', expect.anything())
  })

  it('换账号后重新装载，上一份菜单装上的路由先被卸掉', async () => {
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users')])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.hasRoute('identity.users')).toBe(true)

    useAuthStore().clear()
    signInPending(() => json(200, permissions([page('access.roles', '/access/roles')])))
    await router.push('/access/roles')
    expect(router.hasRoute('identity.users')).toBe(false)
    expect(router.currentRoute.value.name).toBe('access.roles')
  })

  it('装载成功后拉取后端偏好，且只在装载那一次导航里拉', async () => {
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users')])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    await router.push('/plain')
    expect(stubs.hydratePreferencesFromBackend).toHaveBeenCalledTimes(1)
  })

  it('偏好同步失败只记日志，不影响已经建好的路由表', async () => {
    stubs.hydratePreferencesFromBackend.mockRejectedValueOnce(new Error('sync down'))
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users')])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.currentRoute.value.name).toBe('identity.users')
    expect(consoleErrorSpy).toHaveBeenCalledWith('[preferences] 偏好同步失败，已跳过', expect.any(Error))
  })

  it('刚经模仿换了身份：装载后标记偏好身份已切换，标记只用一次', async () => {
    sessionStorage.setItem('xihan.next.identity-switched', '1')
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users')])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(stubs.markPreferenceIdentitySwitched).toHaveBeenCalledTimes(1)
    expect(sessionStorage.getItem('xihan.next.identity-switched')).toBeNull()
  })
})

describe('装载失败', () => {
  it('跳 500，并把失败原因打进控制台而不是静默吞掉', async () => {
    signInPending(serverDown)
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
    expect(consoleErrorSpy).toHaveBeenCalledWith('[router] 动态路由加载失败', expect.anything())
  })

  it('失败后路由仍是未装载态：停在 /500 不自我循环，下一次导航重新尝试', async () => {
    let fail = true
    const pending = signInPending(() => (fail ? serverDown() : json(200, permissions([page('identity.users', '/identity/users')]))))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
    expect(pending.calls()).toBe(1)

    fail = false
    await router.push('/identity/users')
    expect(router.currentRoute.value.name).toBe('identity.users')
    expect(pending.calls()).toBe(2)
  })

  it('会话已失效（请求层清掉了会话）时回登录页并带上原路径，而不是 500', async () => {
    signInPending(() => {
      useAuthStore().clear()
      return json(401, { code: 'SECURITY_UNAUTHENTICATED_000', status: 401, detail: 'expired', traceId: 't' }, 'application/problem+json')
    })
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(router.currentRoute.value.query.redirect).toBe('/identity/users')
  })

  it('会话锁定时装载失败直接放行挂壳，不跳 500', async () => {
    stubs.isLockedState.mockReturnValue(true)
    signInPending(serverDown)
    const router = createGuardedRouter()
    await router.push('/plain')
    expect(router.currentRoute.value.path).toBe('/plain')
  })
})

describe('首页与 404 收敛', () => {
  it('根路径收敛到 homePath', async () => {
    signInWithLoadedRoutes(permissions([page('identity.users', '/identity/user')]))
    const router = createGuardedRouter()
    await router.push('/')
    expect(router.currentRoute.value.path).toBe('/identity/user')
  })

  it('匹配不到的路径统一落 404，404 页自身不再被重定向', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/no-such-page')
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
    await router.push(NOT_FOUND_PATH)
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
  })
})

describe('meta 上的角色与权限校验', () => {
  const guarded: RouteRecordRaw[] = [
    { path: '/by-role', name: 'ByRole', component: Blank, meta: { roles: ['admin'] } },
    { path: '/by-code', name: 'ByCode', component: Blank, meta: { permissions: ['identity.users.read'] } },
    { path: '/deny', name: 'Deny', component: Blank, meta: { roles: ['nobody'], permissions: ['nope'] } },
  ]

  it('命中所需角色或权限码即放行', async () => {
    signInWithLoadedRoutes(permissions(undefined, ['identity.users.read']))
    const router = createGuardedRouter(guarded)
    await router.push('/by-role')
    expect(router.currentRoute.value.path).toBe('/by-role')
    await router.push('/by-code')
    expect(router.currentRoute.value.path).toBe('/by-code')
  })

  it('角色与权限都不命中才落 403', async () => {
    signInWithLoadedRoutes(permissions(undefined, ['other'], ['guest']))
    const router = createGuardedRouter(guarded)
    await router.push('/deny')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })

  it('* 不顶替具体权限码：没下发所需码时照样落 403', async () => {
    signInWithLoadedRoutes(permissions(undefined, ['*'], ['guest']))
    const router = createGuardedRouter(guarded)
    await router.push('/by-code')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })

  it('动态菜单 meta 里的权限声明同样被守卫执行', async () => {
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users', { permission: 'identity.users.read' })], [])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })
})

describe('顶部进度条', () => {
  it('开启过渡进度条时每次导航都起一笔，关闭时不起笔', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(stubs.loadingBar.start).toHaveBeenCalledTimes(1)

    useAppStore().transitionProgress = false
    await router.push('/plain')
    expect(stubs.loadingBar.start).toHaveBeenCalledTimes(1)
  })

  it('即使关闭了进度条，afterEach 仍无条件 finishAll，防止残留的条子卡死', async () => {
    signInWithLoadedRoutes()
    useAppStore().transitionProgress = false
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)
  })

  it('重定向链上 beforeEach 连开好几笔，afterEach 只收一次且一次清干净', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/no-such-page')
    expect(stubs.loadingBar.start.mock.calls.length).toBeGreaterThan(1)
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)
  })

  it('未登录被弹回登录页、装载失败弹到 500 这些异常路径同样收尾', async () => {
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)

    signInPending(serverDown)
    await router.push('/plain')
    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(2)
  })

  it('组件加载失败时进度条以 error 收尾，页面 loading 遮罩一并复位', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await expect(router.push('/broken')).rejects.toThrow(/chunk load failed/)
    expect(stubs.loadingBar.error).toHaveBeenCalledTimes(1)
    expect(stubs.loadingBar.finishAll).not.toHaveBeenCalled()
    expect(useAppStore().pageLoading).toBe(false)
  })
})

describe('页面 loading 遮罩', () => {
  it('导航期间置起遮罩，导航结束后复位；关闭过渡 loading 时全程不置', async () => {
    signInWithLoadedRoutes()
    const appStore = useAppStore()
    const router = createGuardedRouter()
    // 在守卫之后注册，用于观察守卫已经把遮罩置起
    let duringNavigation: boolean | null = null
    router.beforeEach(() => {
      duringNavigation = appStore.pageLoading
      return true
    })

    await router.push('/identity/user')
    expect(duringNavigation).toBe(true)
    expect(appStore.pageLoading).toBe(false)

    appStore.transitionLoading = false
    await router.push('/plain')
    expect(duringNavigation).toBe(false)
  })
})

describe('document.title', () => {
  it('关闭动态标题时固定为应用名，不带页面名', async () => {
    signInWithLoadedRoutes()
    useAppStore().dynamicTitle = false
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(document.title).toBe('XiHan BasicApp')
  })

  it('meta.title 是国际化键时按当前语言翻译后再拼应用名，不是键时原样使用', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(document.title).toBe('无权限访问 - XiHan BasicApp')
    await router.push('/plain')
    expect(document.title).toBe('自定义标题🚀 - XiHan BasicApp')
  })

  it('路由没有 meta.title 时不动标题；重定向后取最终落点的 meta.title', async () => {
    signInWithLoadedRoutes()
    document.title = '上一页标题'
    const router = createGuardedRouter()
    await router.push('/no-title')
    expect(document.title).toBe('上一页标题')
    await router.push('/no-such-page')
    expect(document.title).toBe('页面不存在 - XiHan BasicApp')
  })
})

describe('标签栏写入', () => {
  it('导航后按 fullPath 建标签，标题存国际化键，路由名、keepAlive 与图标一并写入', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user?keyword=abc')

    const tabbarStore = useTabbarStore()
    const tab = tabbarStore.tabs.find(item => item.key === '/identity/user?keyword=abc')
    expect(tab).toMatchObject({ path: '/identity/user?keyword=abc', title: 'error.forbidden', name: 'IdentityUser', keepAlive: true, pinned: false, closable: true })
    expect(tab?.meta?.icon).toBe('mdi:account')
    expect(tabbarStore.cachedTabNames).toContain('IdentityUser')
  })

  it('未开 keepAlive 的页面不进缓存名单', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/plain')
    const tabbarStore = useTabbarStore()
    expect(tabbarStore.tabs.find(item => item.key === '/plain')?.keepAlive).toBe(false)
    expect(tabbarStore.cachedTabNames).not.toContain('Plain')
  })

  it('首页与 meta.affixTab 的页面固定且不可关闭', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push(HOME_PATH)
    await router.push('/affix')
    const tabs = useTabbarStore().tabs
    expect(tabs.find(item => item.key === HOME_PATH)).toMatchObject({ pinned: true, closable: false })
    expect(tabs.find(item => item.key === '/affix')).toMatchObject({ pinned: true, closable: false })
  })

  it('无 meta.title 时标签标题回落到路由名，连路由名都没有时兜底 Untitled', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/no-title')
    await router.push('/anonymous')
    const tabs = useTabbarStore().tabs
    expect(tabs.find(item => item.key === '/no-title')?.title).toBe('NoTitle')
    expect(tabs.find(item => item.key === '/anonymous')).toMatchObject({ title: 'Untitled', name: undefined })
  })

  it('meta.standalone 的独立公共页不进标签栏', async () => {
    signInWithLoadedRoutes()
    const tabbarStore = useTabbarStore()
    const before = tabbarStore.tabs.length
    const router = createGuardedRouter()
    await router.push('/standalone')
    expect(router.currentRoute.value.path).toBe('/standalone')
    expect(tabbarStore.tabs).toHaveLength(before)
  })

  it('同一路径重复导航不建第二个标签，查询串不同视为不同标签', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    await router.push('/plain')
    await router.push('/identity/user')
    await router.push('/identity/user?page=2')
    const keys = useTabbarStore().tabs.map(item => item.key)
    expect(keys.filter(key => key === '/identity/user')).toHaveLength(1)
    expect(keys).toContain('/identity/user?page=2')
  })

  it('菜单装上的页面以页面码为路由名进标签，KeepAlive 按它命中', async () => {
    signInPending(() => json(200, permissions([page('identity.users', '/identity/users', { keepAlive: true })])))
    const router = createGuardedRouter()
    await router.push('/identity/users')
    const tabbarStore = useTabbarStore()
    expect(tabbarStore.tabs.find(item => item.key === '/identity/users')).toMatchObject({ name: 'identity.users', title: 'identity.users.title', keepAlive: true })
    expect(tabbarStore.cachedTabNames).toContain('identity.users')
  })
})

describe('标签栏裁剪', () => {
  it('关闭访问历史时每次导航只留当前标签与不可关闭标签', async () => {
    signInWithLoadedRoutes()
    useAppStore().tabbarVisitHistory = false
    const router = createGuardedRouter()
    await router.push('/identity/user')
    await router.push('/plain')
    const tabs = useTabbarStore().tabs
    expect(tabs.some(item => item.key === '/identity/user')).toBe(false)
    expect(tabs.some(item => item.key === '/plain')).toBe(true)
    expect(tabs.some(item => item.closable === false)).toBe(true)
  })

  it('超过上限时淘汰第一个可关闭且不是当前页的标签，不误删固定标签；上限 0 表示不限制', async () => {
    signInWithLoadedRoutes()
    const appStore = useAppStore()
    appStore.tabbarVisitHistory = true
    appStore.tabbarMaxCount = 2
    const router = createGuardedRouter()
    await router.push('/identity/user')
    await router.push('/plain')
    const tabbarStore = useTabbarStore()
    expect(tabbarStore.tabs).toHaveLength(2)
    expect(tabbarStore.tabs.some(item => item.key === '/identity/user')).toBe(false)
    expect(tabbarStore.tabs.some(item => item.key === HOME_PATH && item.closable === false)).toBe(true)

    appStore.tabbarMaxCount = 0
    await router.push('/identity/user')
    await router.push('/affix')
    expect(tabbarStore.tabs.length).toBeGreaterThanOrEqual(4)
  })
})
