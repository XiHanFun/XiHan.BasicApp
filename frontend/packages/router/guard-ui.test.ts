import type { Router, RouteRecordRaw } from 'vue-router'
/**
 * 路由守卫（guard.ts）——UI 副作用单元测试。
 * 职责边界：顶部进度条的开/收（含异常路径必须收尾）、页面 loading 遮罩、
 * document.title 的国际化解析、以及标签栏的写入/固定/裁剪规则。
 * 登录态分支在 guard-auth.test.ts，路由装载在 guard-routes.test.ts。
 */
import type { AppContextApis, PermissionInfo, UserInfo } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { FORBIDDEN_PATH, HOME_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
import { registerAppContext } from '~/stores/app-context'

const stubs = vi.hoisted(() => ({
  loadingBar: {
    start: vi.fn(),
    finish: vi.fn(),
    finishAll: vi.fn(),
    error: vi.fn(),
  },
  isLockedState: vi.fn(() => false),
  hydratePreferencesFromBackend: vi.fn(async () => {}),
}))

vi.mock('~/composables', () => ({
  loadingBar: stubs.loadingBar,
  isLockedState: stubs.isLockedState,
}))

vi.mock('~/stores', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/stores')>()
  return { ...actual, hydratePreferencesFromBackend: stubs.hydratePreferencesFromBackend }
})

const { useAccessStore, useAppStore, useTabbarStore, useUserStore } = await import('~/stores')
const { setupRouterGuard } = await import('./guard')

const Blank = defineComponent({ name: 'BlankProbe', render: () => null })

function baseRoutes(): RouteRecordRaw[] {
  return [
    { path: '/', name: 'RootLayout', component: Blank, children: [] },
    {
      path: '/auth',
      name: 'Authentication',
      component: Blank,
      children: [{ path: 'login', name: 'Login', component: Blank }],
    },
    { path: HOME_PATH, name: 'Dashboard', component: Blank, meta: { title: 'menu.workbench_dashboard' } },
    { path: '/identity/user', name: 'IdentityUser', component: Blank, meta: { title: 'menu.identity_user', icon: 'mdi:account', keepAlive: true } },
    { path: '/plain', name: 'Plain', component: Blank, meta: { title: '自定义标题🚀' } },
    { path: '/affix', name: 'Affix', component: Blank, meta: { title: 'menu.identity_role', affixTab: true } },
    { path: '/standalone', name: 'Standalone', component: Blank, meta: { title: 'menu.identity_org', standalone: true } },
    { path: '/no-title', name: 'NoTitle', component: Blank },
    { path: '/anonymous', component: Blank },
    { path: '/broken', name: 'Broken', component: () => Promise.reject(new Error('chunk load failed')) },
    { path: FORBIDDEN_PATH, name: 'Forbidden', component: Blank },
    { path: SERVER_ERROR_PATH, name: 'ServerError', component: Blank },
    { path: NOT_FOUND_PATH, name: 'NotFound', component: Blank, meta: { title: 'error.not_found' } },
    { path: '/:pathMatch(.*)*', name: 'NotFoundCatchAll', component: Blank },
  ]
}

function createGuardedRouter(): Router {
  const router = createRouter({ history: createMemoryHistory(), routes: baseRoutes() })
  setupRouterGuard(router)
  return router
}

function userInfoFixture(): UserInfo {
  return { basicId: 'u-1', userName: 'tester', roles: ['admin'], permissions: ['*'] }
}

function permissionFixture(): PermissionInfo {
  return { roles: ['admin'], permissions: ['*'], menus: [] }
}

function signInWithLoadedRoutes(): void {
  useAccessStore().setAccessToken('token-abc')
  useUserStore().setUserInfo(userInfoFixture())
  useAccessStore().setAccessRoutes([{ path: HOME_PATH, name: 'HomeMenu', meta: { title: 'menu.workbench_dashboard' } }])
}

let originalTitle = ''
let consoleErrorSpy: ReturnType<typeof vi.spyOn>

beforeEach(() => {
  setActivePinia(createPinia())
  originalTitle = document.title
  stubs.loadingBar.start.mockClear()
  stubs.loadingBar.finish.mockClear()
  stubs.loadingBar.finishAll.mockClear()
  stubs.loadingBar.error.mockClear()
  stubs.isLockedState.mockReset()
  stubs.isLockedState.mockReturnValue(false)
  stubs.hydratePreferencesFromBackend.mockReset()
  stubs.hydratePreferencesFromBackend.mockResolvedValue(undefined)
  consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
  registerAppContext({
    apis: {
      getUserInfoApi: vi.fn(async () => userInfoFixture()),
      getPermissionsApi: vi.fn(async () => permissionFixture()),
    } as unknown as AppContextApis,
    viewModules: {},
    explicitComponentMap: {},
    getStaticRoutes: () => [],
  })
})

afterEach(() => {
  document.title = originalTitle
  consoleErrorSpy.mockRestore()
})

describe('顶部进度条', () => {
  it('开启过渡进度条时每次导航都起一笔', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(stubs.loadingBar.start).toHaveBeenCalledTimes(1)
  })

  it('关闭过渡进度条时不起笔', async () => {
    signInWithLoadedRoutes()
    useAppStore().transitionProgress = false
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(stubs.loadingBar.start).not.toHaveBeenCalled()
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
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
    expect(stubs.loadingBar.start.mock.calls.length).toBeGreaterThan(1)
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)
  })

  it('未登录被弹回登录页这条异常路径同样收尾', async () => {
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)
  })

  it('无权限被弹到 403 这条异常路径同样收尾', async () => {
    signInWithLoadedRoutes()
    useUserStore().setUserInfo({ basicId: 'u-1', userName: 't', roles: ['guest'], permissions: ['other'] })
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        ...baseRoutes(),
        { path: '/deny', name: 'Deny', component: Blank, meta: { roles: ['nobody'], permissions: ['nope'] } },
      ],
    })
    setupRouterGuard(router)
    await router.push('/deny')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)
  })

  it('动态路由装载失败弹到 500 这条异常路径同样收尾', async () => {
    useAccessStore().setAccessToken('token-abc')
    useUserStore().setUserInfo(userInfoFixture())
    registerAppContext({
      apis: {
        getUserInfoApi: vi.fn(async () => userInfoFixture()),
        getPermissionsApi: vi.fn()
          .mockRejectedValueOnce(new Error('menus down'))
          .mockResolvedValue(permissionFixture()),
      } as unknown as AppContextApis,
    })

    const router = createGuardedRouter()
    await router.push('/identity/user')

    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
    expect(stubs.loadingBar.finishAll).toHaveBeenCalledTimes(1)
  })

  it('组件加载失败时进度条以 error 收尾——afterEach 根本不会跑，不收就永远停在半路', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()

    await expect(router.push('/broken')).rejects.toThrow(/chunk load failed/)

    expect(stubs.loadingBar.error).toHaveBeenCalledTimes(1)
    expect(stubs.loadingBar.finishAll).not.toHaveBeenCalled()
  })

  it('组件加载失败时页面 loading 遮罩一并复位，不留下盖死的遮罩', async () => {
    signInWithLoadedRoutes()
    const appStore = useAppStore()
    const router = createGuardedRouter()

    await expect(router.push('/broken')).rejects.toThrow(/chunk load failed/)

    expect(appStore.pageLoading).toBe(false)
  })
})

describe('页面 loading 遮罩', () => {
  it('导航期间置起遮罩，导航结束后复位', async () => {
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
  })

  it('关闭过渡 loading 时全程不置遮罩', async () => {
    signInWithLoadedRoutes()
    const appStore = useAppStore()
    appStore.transitionLoading = false
    const router = createGuardedRouter()
    let duringNavigation: boolean | null = null
    router.beforeEach(() => {
      duringNavigation = appStore.pageLoading
      return true
    })

    await router.push('/identity/user')

    expect(duringNavigation).toBe(false)
    expect(appStore.pageLoading).toBe(false)
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

  it('meta.title 是国际化键时按当前语言翻译后再拼应用名', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(document.title).toBe('用户管理 - XiHan BasicApp')
  })

  it('meta.title 不是国际化键时原样使用，中文与 emoji 不被转义', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/plain')
    expect(document.title).toBe('自定义标题🚀 - XiHan BasicApp')
  })

  it('路由没有 meta.title 时不动标题，保留上一次的值', async () => {
    signInWithLoadedRoutes()
    document.title = '上一页标题'
    const router = createGuardedRouter()
    await router.push('/no-title')
    expect(document.title).toBe('上一页标题')
  })

  it('重定向后标题取的是最终落点的 meta.title', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/no-such-page')
    expect(document.title).toBe('页面不存在 - XiHan BasicApp')
  })
})

describe('标签栏写入', () => {
  it('导航后按 fullPath 建标签，key 与 path 都是 fullPath', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user?keyword=abc')

    const tab = useTabbarStore().tabs.find(item => item.key === '/identity/user?keyword=abc')
    expect(tab?.path).toBe('/identity/user?keyword=abc')
  })

  it('标签标题存的是国际化键而不是当次解析出的译文，切语言时标签栏才能跟着变', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')

    const tab = useTabbarStore().tabs.find(item => item.key === '/identity/user')
    expect(tab?.title).toBe('menu.identity_user')
    expect(tab?.title).not.toBe('用户管理')
  })

  it('路由名与 keepAlive 一并写入，KeepAlive 的 include 依赖这两项', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')

    const tabbarStore = useTabbarStore()
    const tab = tabbarStore.tabs.find(item => item.key === '/identity/user')
    expect(tab?.name).toBe('IdentityUser')
    expect(tab?.keepAlive).toBe(true)
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

  it('meta.icon 落到标签的 meta 上，供标签栏渲染图标', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    expect(useTabbarStore().tabs.find(item => item.key === '/identity/user')?.meta?.icon).toBe('mdi:account')
  })

  it('首页标签被固定且不可关闭', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push(HOME_PATH)

    const tab = useTabbarStore().tabs.find(item => item.key === HOME_PATH)
    expect(tab?.pinned).toBe(true)
    expect(tab?.closable).toBe(false)
  })

  it('meta.affixTab 的页面同样固定且不可关闭', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/affix')

    const tab = useTabbarStore().tabs.find(item => item.key === '/affix')
    expect(tab?.pinned).toBe(true)
    expect(tab?.closable).toBe(false)
  })

  it('普通业务页可关闭且不固定', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')

    const tab = useTabbarStore().tabs.find(item => item.key === '/identity/user')
    expect(tab?.pinned).toBe(false)
    expect(tab?.closable).toBe(true)
  })

  it('无 meta.title 时标签标题回落到路由名', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/no-title')
    expect(useTabbarStore().tabs.find(item => item.key === '/no-title')?.title).toBe('NoTitle')
  })

  it('既无 meta.title 又无路由名时标签标题兜底 Untitled', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/anonymous')

    const tab = useTabbarStore().tabs.find(item => item.key === '/anonymous')
    expect(tab?.title).toBe('Untitled')
    expect(tab?.name).toBeUndefined()
  })

  it('meta.standalone 的独立公共页不挂主布局、不进标签栏', async () => {
    signInWithLoadedRoutes()
    const tabbarStore = useTabbarStore()
    const before = tabbarStore.tabs.length
    const router = createGuardedRouter()

    await router.push('/standalone')

    expect(router.currentRoute.value.path).toBe('/standalone')
    expect(tabbarStore.tabs).toHaveLength(before)
    expect(tabbarStore.tabs.some(item => item.key === '/standalone')).toBe(false)
  })

  it('同一路径重复导航不会建出第二个标签', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user')
    await router.push('/plain')
    await router.push('/identity/user')

    const tabbarStore = useTabbarStore()
    expect(tabbarStore.tabs.filter(item => item.key === '/identity/user')).toHaveLength(1)
  })

  it('查询串不同视为不同标签，各自独立', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/identity/user?page=1')
    await router.push('/identity/user?page=2')

    const keys = useTabbarStore().tabs.map(item => item.key)
    expect(keys).toContain('/identity/user?page=1')
    expect(keys).toContain('/identity/user?page=2')
  })
})

describe('标签栏裁剪', () => {
  it('关闭访问历史时每次导航只留当前标签与不可关闭标签', async () => {
    signInWithLoadedRoutes()
    useAppStore().tabbarVisitHistory = false
    const router = createGuardedRouter()

    await router.push('/identity/user')
    await router.push('/plain')

    const tabbarStore = useTabbarStore()
    expect(tabbarStore.tabs.some(item => item.key === '/identity/user')).toBe(false)
    expect(tabbarStore.tabs.some(item => item.key === '/plain')).toBe(true)
    // 首页默认标签不可关闭，始终留着
    expect(tabbarStore.tabs.some(item => item.closable === false)).toBe(true)
  })

  it('开启访问历史时旧标签保留', async () => {
    signInWithLoadedRoutes()
    useAppStore().tabbarVisitHistory = true
    const router = createGuardedRouter()

    await router.push('/identity/user')
    await router.push('/plain')

    const keys = useTabbarStore().tabs.map(item => item.key)
    expect(keys).toContain('/identity/user')
    expect(keys).toContain('/plain')
  })

  it('超过上限时淘汰第一个可关闭且不是当前页的标签', async () => {
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
    expect(tabbarStore.tabs.some(item => item.key === '/plain')).toBe(true)
  })

  it('上限为 0 表示不限制，标签一直累积', async () => {
    signInWithLoadedRoutes()
    const appStore = useAppStore()
    appStore.tabbarVisitHistory = true
    appStore.tabbarMaxCount = 0
    const router = createGuardedRouter()

    await router.push('/identity/user')
    await router.push('/plain')
    await router.push('/affix')

    expect(useTabbarStore().tabs.length).toBeGreaterThanOrEqual(4)
  })

  it('淘汰时不会误删不可关闭的固定标签', async () => {
    signInWithLoadedRoutes()
    const appStore = useAppStore()
    appStore.tabbarVisitHistory = true
    appStore.tabbarMaxCount = 1
    const router = createGuardedRouter()

    await router.push('/identity/user')
    await router.push('/plain')

    const tabbarStore = useTabbarStore()
    expect(tabbarStore.tabs.some(item => item.key === HOME_PATH && item.closable === false)).toBe(true)
  })
})
