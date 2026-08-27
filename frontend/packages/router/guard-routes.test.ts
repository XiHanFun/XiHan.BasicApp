import type { Router, RouteRecordRaw } from 'vue-router'
/**
 * 路由守卫（guard.ts）——动态/静态路由装载与导航判定单元测试。
 * 职责边界：后端菜单驱动的路由安装与「重进当前地址」、静态模式的权限过滤安装、
 * 装载失败的兜底与可重试性、首页/404 的收敛跳转、meta 上的角色与权限校验。
 * 登录态分支在 guard-auth.test.ts，进度条/标题/标签栏在 guard-ui.test.ts。
 */
import type { AppContextApis, MenuRoute, PermissionInfo, UserInfo } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { FORBIDDEN_PATH, HOME_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
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

const { useAccessStore, useUserStore } = await import('~/stores')
const { setupRouterGuard } = await import('./guard')

const Blank = defineComponent({ name: 'BlankProbe', render: () => null })

interface RouterOptionsLike {
  /** 去掉 HOME_PATH 静态路由，用于验证「首页匹配不到时收敛到 homePath」 */
  withoutHome?: boolean
}

function baseRoutes(options: RouterOptionsLike = {}): RouteRecordRaw[] {
  const home: RouteRecordRaw[] = options.withoutHome
    ? []
    : [{ path: HOME_PATH, name: 'Dashboard', component: Blank, meta: { title: 'menu.workspace' } }]
  return [
    { path: '/', name: 'RootLayout', component: Blank, children: [] },
    {
      path: '/auth',
      name: 'Authentication',
      component: Blank,
      children: [{ path: 'login', name: 'Login', component: Blank }],
    },
    ...home,
    { path: '/first', name: 'First', component: Blank },
    { path: '/role-guarded', name: 'RoleGuarded', component: Blank, meta: { roles: ['admin'] } },
    { path: '/perm-guarded', name: 'PermGuarded', component: Blank, meta: { permissions: ['sys:view'] } },
    { path: '/both-guarded', name: 'BothGuarded', component: Blank, meta: { roles: ['nobody'], permissions: ['sys:view'] } },
    { path: '/deny', name: 'Deny', component: Blank, meta: { roles: ['nobody'], permissions: ['nope'] } },
    { path: '/empty-guard', name: 'EmptyGuard', component: Blank, meta: { roles: [], permissions: [] } },
    { path: FORBIDDEN_PATH, name: 'Forbidden', component: Blank },
    { path: SERVER_ERROR_PATH, name: 'ServerError', component: Blank },
    { path: NOT_FOUND_PATH, name: 'NotFound', component: Blank },
    { path: '/:pathMatch(.*)*', name: 'NotFoundCatchAll', component: Blank },
  ]
}

function createGuardedRouter(options: RouterOptionsLike = {}): Router {
  const router = createRouter({ history: createMemoryHistory(), routes: baseRoutes(options) })
  setupRouterGuard(router)
  return router
}

function userInfoFixture(overrides: Partial<UserInfo> = {}): UserInfo {
  return { basicId: 'u-1', userName: 'tester', roles: ['admin'], permissions: ['sys:view'], ...overrides }
}

function permissionFixture(overrides: Partial<PermissionInfo> = {}): PermissionInfo {
  return { roles: ['admin'], permissions: ['sys:view'], menus: [], ...overrides }
}

function registerApis(apis: Partial<AppContextApis>): void {
  registerAppContext({ apis: apis as AppContextApis })
}

/** 已登录但动态路由未加载：守卫会走装载分支 */
function signInWithoutRoutes(): void {
  useAccessStore().setAccessToken('token-abc')
  useUserStore().setUserInfo(userInfoFixture())
}

function signInWithLoadedRoutes(overrides: Partial<UserInfo> = {}): void {
  useAccessStore().setAccessToken('token-abc')
  useUserStore().setUserInfo(userInfoFixture(overrides))
  useAccessStore().setAccessRoutes([{ path: HOME_PATH, name: 'HomeMenu', meta: { title: 'menu.workspace' } }])
}

function menuFixture(path: string, name: string, component = 'Identity/User'): MenuRoute {
  return { path, name, component, meta: { title: 'menu.identity.user' } }
}

let consoleErrorSpy: ReturnType<typeof vi.spyOn>

beforeEach(() => {
  setActivePinia(createPinia())
  // restoreAllMocks 只还原 spyOn，vi.fn() 的调用记录跨用例累积，必须显式清
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
    viewModules: {
      '/src/views/identity/user.vue': () => Promise.resolve({ default: Blank }),
      '/src/views/identity/role.vue': () => Promise.resolve({ default: Blank }),
    },
    explicitComponentMap: {},
    getStaticRoutes: () => [],
  })
  registerApis({
    getUserInfoApi: vi.fn(async () => userInfoFixture()),
    getPermissionsApi: vi.fn(async () => permissionFixture()),
  })
})

afterEach(() => {
  consoleErrorSpy.mockRestore()
})

describe('动态模式：后端菜单装载', () => {
  it('菜单被写进 accessStore 并装成可导航的路由', async () => {
    signInWithoutRoutes()
    const menus = [menuFixture('/identity/user', 'IdentityUser')]
    registerApis({ getPermissionsApi: vi.fn(async () => permissionFixture({ menus })) })

    const router = createGuardedRouter()
    await router.push('/identity/user')

    expect(router.hasRoute('IdentityUser')).toBe(true)
    expect(router.currentRoute.value.name).toBe('IdentityUser')
    expect(useAccessStore().accessRoutes).toEqual(menus)
    expect(useAccessStore().isRoutesLoaded).toBe(true)
  })

  it('装载完成后重进当前地址，最终停在目标动态路由上', async () => {
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({ menus: [menuFixture('/identity/user', 'IdentityUser')] })),
    })

    const router = createGuardedRouter()
    await router.push('/identity/user')

    expect(router.currentRoute.value.name).toBe('IdentityUser')
    expect(router.currentRoute.value.path).toBe('/identity/user')
  })

  it('装载完成后重进深链，query 与 hash 必须原样保留', async () => {
    // 回归锚点：重进曾写成 { path: to.fullPath }，vue-router 不解析 path 里的查询串，
    // 导致刷新页面/直接打开深链时 ?query#hash 被静默丢掉 —— 带筛选条件的分享链接、
    // OAuth 回调的 ?code=、登录后按 redirect 回跳全部丢参。改回 { path: to.fullPath } 即失败。
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({ menus: [menuFixture('/identity/user', 'IdentityUser')] })),
    })

    const router = createGuardedRouter()
    await router.push('/identity/user?keyword=abc#tail')

    expect(router.currentRoute.value.name).toBe('IdentityUser')
    expect(router.currentRoute.value.fullPath).toBe('/identity/user?keyword=abc#tail')
    expect(router.currentRoute.value.query).toEqual({ keyword: 'abc' })
    expect(router.currentRoute.value.hash).toBe('#tail')
  })

  it('路由已加载后再带 query 导航，查询串正常保留（对照组：与装载那一次行为一致）', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/first?keyword=abc#tail')

    expect(router.currentRoute.value.query.keyword).toBe('abc')
    expect(router.currentRoute.value.hash).toBe('#tail')
  })

  it('嵌套菜单的子路由一并装载并可直达', async () => {
    signInWithoutRoutes()
    const menus: MenuRoute[] = [{
      path: '/identity',
      name: 'Identity',
      meta: { title: 'menu.identity.root' },
      children: [
        menuFixture('/identity/user', 'IdentityUser'),
        menuFixture('/identity/role', 'IdentityRole', 'Identity/Role'),
      ],
    }]
    registerApis({ getPermissionsApi: vi.fn(async () => permissionFixture({ menus })) })

    const router = createGuardedRouter()
    await router.push('/identity/role')
    expect(router.currentRoute.value.name).toBe('IdentityRole')
  })

  it('父菜单本身带自动重定向，直达父路径会落到第一个子页面', async () => {
    signInWithoutRoutes()
    const menus: MenuRoute[] = [{
      path: '/identity',
      name: 'Identity',
      meta: { title: 'menu.identity.root' },
      children: [menuFixture('/identity/user', 'IdentityUser')],
    }]
    registerApis({ getPermissionsApi: vi.fn(async () => permissionFixture({ menus })) })

    const router = createGuardedRouter()
    await router.push('/identity')
    expect(router.currentRoute.value.path).toBe('/identity/user')
  })

  it('路径已存在的菜单不重复注册，静态路由不会被后端菜单顶掉', async () => {
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({
        menus: [menuFixture(HOME_PATH, 'DashboardFromMenu')],
      })),
    })

    const router = createGuardedRouter()
    await router.push(HOME_PATH)

    expect(router.hasRoute('DashboardFromMenu')).toBe(false)
    expect(router.currentRoute.value.name).toBe('Dashboard')
  })

  it('路由名已被占用的菜单被跳过，新路径不会挂上去', async () => {
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({
        menus: [menuFixture('/brand-new', 'First')],
      })),
    })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(router.getRoutes().some(route => route.path === '/brand-new')).toBe(false)
  })

  it('无路由名的菜单不会被装载，且必须留下错误日志', async () => {
    // 回归锚点：这类菜单原本被静默跳过。侧边栏照样渲染出条目、点进去落 404，
    // 而整条链路一行日志都没有，配错菜单的人无从查起。
    signInWithoutRoutes()
    const nameless = { path: '/nameless', name: '', component: 'Identity/User', meta: { title: 't' } } as MenuRoute
    registerApis({ getPermissionsApi: vi.fn(async () => permissionFixture({ menus: [nameless] })) })
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

    try {
      const router = createGuardedRouter()
      await router.push('/first')

      expect(router.getRoutes().some(route => route.path === '/nameless')).toBe(false)
      expect(errorSpy).toHaveBeenCalledWith(expect.stringContaining('菜单缺少路由名'), '/nameless')
    }
    finally {
      errorSpy.mockRestore()
    }
  })

  it('路径已装载的菜单静默跳过，不当成配置错误报错', async () => {
    // 与上一条对照：去重是正常行为，不能和「配错了 name」混在一起刷日志。
    signInWithoutRoutes()
    const duplicated = [menuFixture('/identity/user', 'IdentityUser'), menuFixture('/identity/user', 'IdentityUserAgain')]
    registerApis({ getPermissionsApi: vi.fn(async () => permissionFixture({ menus: duplicated })) })
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

    try {
      const router = createGuardedRouter()
      await router.push('/identity/user')

      expect(router.getRoutes().filter(route => route.path === '/identity/user')).toHaveLength(1)
      expect(errorSpy).not.toHaveBeenCalled()
    }
    finally {
      errorSpy.mockRestore()
    }
  })

  it('外链菜单不落地成路由，但仍完整保存在 accessStore 供菜单渲染', async () => {
    signInWithoutRoutes()
    const menus: MenuRoute[] = [
      { path: '/outer', name: 'Outer', meta: { title: 'menu.outer', link: 'https://example.com' } },
      menuFixture('/identity/user', 'IdentityUser'),
    ]
    registerApis({ getPermissionsApi: vi.fn(async () => permissionFixture({ menus })) })

    const router = createGuardedRouter()
    await router.push('/identity/user')

    expect(router.hasRoute('Outer')).toBe(false)
    expect(useAccessStore().accessRoutes).toHaveLength(2)
  })

  it('装载成功后拉取后端偏好，且只在装载那一次导航里拉', async () => {
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({ menus: [menuFixture('/identity/user', 'IdentityUser')] })),
    })

    const router = createGuardedRouter()
    await router.push('/identity/user')
    await router.push('/first')

    expect(stubs.hydratePreferencesFromBackend).toHaveBeenCalledTimes(1)
  })

  it('偏好同步失败只记日志，不影响已经建好的路由表', async () => {
    signInWithoutRoutes()
    stubs.hydratePreferencesFromBackend.mockRejectedValue(new Error('sync down'))
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({ menus: [menuFixture('/identity/user', 'IdentityUser')] })),
    })

    const router = createGuardedRouter()
    await router.push('/identity/user')

    expect(router.currentRoute.value.name).toBe('IdentityUser')
    expect(consoleErrorSpy).toHaveBeenCalledWith('[preferences] 偏好同步失败，已跳过', expect.any(Error))
  })
})

describe('静态模式：前端路由 + 权限过滤', () => {
  it('按用户权限过滤静态路由后装载，不通过的那条不进路由表', async () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', 'static')
    signInWithoutRoutes()
    registerAppContext({
      getStaticRoutes: () => [{
        path: '/',
        name: 'StaticRoot',
        component: Blank,
        children: [
          { path: '/static-allow', name: 'StaticAllow', component: Blank, meta: { roles: ['admin'], permissions: ['x'] } },
          { path: '/static-deny', name: 'StaticDeny', component: Blank, meta: { roles: ['nobody'], permissions: ['nope'] } },
        ],
      }],
    })

    const router = createGuardedRouter()
    await router.push('/static-allow')

    expect(router.hasRoute('StaticAllow')).toBe(true)
    expect(router.hasRoute('StaticDeny')).toBe(false)
    expect(router.currentRoute.value.name).toBe('StaticAllow')
  })

  it('静态模式下 accessRoutes 置空，菜单不由后端菜单驱动', async () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', 'static')
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => permissionFixture({ menus: [menuFixture('/identity/user', 'IdentityUser')] })),
    })
    registerAppContext({
      getStaticRoutes: () => [{ path: '/', name: 'StaticRoot', component: Blank, children: [] }],
    })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(useAccessStore().accessRoutes).toEqual([])
    expect(useAccessStore().isRoutesLoaded).toBe(true)
    expect(router.hasRoute('IdentityUser')).toBe(false)
  })

  it('静态路由表里没有根路由时不抛错，装载为空后照常放行', async () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', 'static')
    signInWithoutRoutes()
    registerAppContext({ getStaticRoutes: () => [{ path: '/nope', name: 'NoRoot', component: Blank }] })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(router.currentRoute.value.path).toBe('/first')
    expect(useAccessStore().isRoutesLoaded).toBe(true)
  })
})

describe('装载失败', () => {
  it('跳 500，并把失败原因打进控制台而不是静默吞掉', async () => {
    signInWithoutRoutes()
    // 只失败一次：/500 上的那次重试成功，导航才能稳定停住（持续失败的形态见下一条）
    registerApis({
      getPermissionsApi: vi.fn()
        .mockRejectedValueOnce(new Error('menus down'))
        .mockResolvedValue(permissionFixture()),
    })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
    expect(consoleErrorSpy).toHaveBeenCalledWith('[router] 动态路由加载失败', expect.any(Error))
  })

  it('失败后不得置 isRoutesLoaded，否则本次会话再也不会重试而永久 404', async () => {
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => {
        throw new Error('menus down')
      }),
    })

    const router = createGuardedRouter()
    await router.push('/first').catch(() => {})

    expect(useAccessStore().isRoutesLoaded).toBe(false)
    expect(useAccessStore().accessRoutes).toEqual([])
  })

  it('装载持续失败时停在 /500，不再自我循环重试', async () => {
    // 回归锚点：装载分支曾不检查白名单，已登录用户被重定向到 /500 后，
    // /500 自身又进装载分支（isRoutesLoaded 仍为假）→ 再失败 → 再跳 /500，
    // 最终被 vue-router 判为无限重定向而中止，用户看不到 500 页且权限接口被连打。
    // 后端菜单接口整体不可用（网关 502、菜单服务宕机）时必然命中。
    signInWithoutRoutes()
    const getPermissionsApi = vi.fn(async () => {
      throw new Error('menus down')
    })
    registerApis({ getPermissionsApi })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
    // 落到 /500 后不得再触发一次装载：整次导航只打一次权限接口
    expect(getPermissionsApi).toHaveBeenCalledTimes(1)
  })

  it('下一次导航会重新尝试装载，一次偶发失败不把应用锁死', async () => {
    signInWithoutRoutes()
    const getPermissionsApi = vi.fn()
      .mockRejectedValueOnce(new Error('flaky'))
      .mockResolvedValue(permissionFixture({ menus: [menuFixture('/identity/user', 'IdentityUser')] }))
    registerApis({ getPermissionsApi })

    const router = createGuardedRouter()
    await router.push('/first')
    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)

    await router.push('/identity/user')
    expect(router.currentRoute.value.name).toBe('IdentityUser')
    expect(getPermissionsApi).toHaveBeenCalledTimes(2)
  })

  it('会话锁定时装载失败直接放行挂壳，不跳 500', async () => {
    stubs.isLockedState.mockReturnValue(true)
    signInWithoutRoutes()
    registerApis({
      getPermissionsApi: vi.fn(async () => {
        throw new Error('423')
      }),
    })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(router.currentRoute.value.path).toBe('/first')
    expect(useAccessStore().isRoutesLoaded).toBe(false)
    expect(consoleErrorSpy).not.toHaveBeenCalledWith('[router] 动态路由加载失败', expect.any(Error))
  })
})

describe('首页与 404 收敛', () => {
  it('根路径收敛到 homePath', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/')
    expect(router.currentRoute.value.path).toBe(HOME_PATH)
  })

  it('匹配不到的路径统一落 404', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/no-such-page')
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
  })

  it('404 页自身不再被重定向，避免自跳死循环', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push(NOT_FOUND_PATH)
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
  })

  it('兜底首页匹配不到时改投菜单推导的 homePath，而不是丢进 404', async () => {
    useAccessStore().setAccessToken('token-abc')
    useUserStore().setUserInfo(userInfoFixture())
    useAccessStore().setAccessRoutes([{ path: '/first', name: 'FirstMenu', meta: { title: 'menu.first' } }])

    const router = createGuardedRouter({ withoutHome: true })
    await router.push(HOME_PATH)

    expect(router.currentRoute.value.path).toBe('/first')
  })

  it('homePath 与兜底首页相同且首页匹配不到时，仍然落 404', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter({ withoutHome: true })
    await router.push(HOME_PATH)
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
  })
})

describe('meta 上的角色与权限校验', () => {
  it('命中所需角色即放行', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/role-guarded')
    expect(router.currentRoute.value.path).toBe('/role-guarded')
  })

  it('只声明角色且用户无该角色时落 403', async () => {
    signInWithLoadedRoutes({ roles: ['guest'] })
    const router = createGuardedRouter()
    await router.push('/role-guarded')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })

  it('命中所需权限码即放行', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/perm-guarded')
    expect(router.currentRoute.value.path).toBe('/perm-guarded')
  })

  it('只声明权限且用户无该权限时落 403', async () => {
    signInWithLoadedRoutes({ permissions: ['other'] })
    const router = createGuardedRouter()
    await router.push('/perm-guarded')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })

  it('权限通配 * 顶替具体权限码', async () => {
    signInWithLoadedRoutes({ roles: ['guest'], permissions: ['*'] })
    const router = createGuardedRouter()
    await router.push('/perm-guarded')
    expect(router.currentRoute.value.path).toBe('/perm-guarded')
  })

  it('角色与权限同时声明时任一命中即放行，不是「与」的关系', async () => {
    signInWithLoadedRoutes({ roles: ['guest'], permissions: ['sys:view'] })
    const router = createGuardedRouter()
    await router.push('/both-guarded')
    expect(router.currentRoute.value.path).toBe('/both-guarded')
  })

  it('角色与权限都不命中才落 403', async () => {
    signInWithLoadedRoutes({ roles: ['guest'], permissions: ['other'] })
    const router = createGuardedRouter()
    await router.push('/deny')
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })

  it('roles/permissions 为空数组视同未声明，不触发校验', async () => {
    signInWithLoadedRoutes({ roles: [], permissions: [] })
    const router = createGuardedRouter()
    await router.push('/empty-guard')
    expect(router.currentRoute.value.path).toBe('/empty-guard')
  })

  it('未声明任何权限的路由对所有登录用户放行', async () => {
    signInWithLoadedRoutes({ roles: [], permissions: [] })
    const router = createGuardedRouter()
    await router.push('/first')
    expect(router.currentRoute.value.path).toBe('/first')
  })

  it('动态菜单 meta 里的权限声明同样被守卫执行', async () => {
    signInWithoutRoutes()
    const menus: MenuRoute[] = [{
      path: '/identity/user',
      name: 'IdentityUser',
      component: 'Identity/User',
      meta: { title: 'menu.identity.user', roles: ['nobody'], permissions: ['nope'] },
    }]
    registerApis({
      getUserInfoApi: vi.fn(async () => userInfoFixture({ roles: ['guest'], permissions: ['other'] })),
      getPermissionsApi: vi.fn(async () => permissionFixture({ roles: ['guest'], permissions: ['other'], menus })),
    })

    const router = createGuardedRouter()
    await router.push('/identity/user')

    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })
})
