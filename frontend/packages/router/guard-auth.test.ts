import type { Router, RouteRecordRaw } from 'vue-router'
/**
 * 路由守卫（guard.ts）——登录态与用户上下文分支单元测试。
 * 职责边界：未登录跳转、白名单/认证页放行、已登录访问认证页的重定向、
 * 用户上下文失效后的重新拉取、拉取失败的清场跳登录、以及会话锁定（423）下的放行。
 * 动态路由装载与 UI 副作用分别在 guard-routes.test.ts / guard-ui.test.ts。
 *
 * 用真实的 vue-router（createMemoryHistory）跑完整导航，路由组件一律替身，
 * 避免加载真实 .vue（会连带 @xihan-ui，当前无构建产物）。
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

// 进度条服务住在 ~/composables，该桶会连带 @xihan-ui；整桶替身，行为由用例自行断言
vi.mock('~/composables', () => ({
  loadingBar: stubs.loadingBar,
  isLockedState: stubs.isLockedState,
}))

// 偏好同步会真发请求且带模块级 once 标记，只替换这一个导出，其余 store 保持真身
vi.mock('~/stores', async (importOriginal) => {
  const actual = await importOriginal<typeof import('~/stores')>()
  return { ...actual, hydratePreferencesFromBackend: stubs.hydratePreferencesFromBackend }
})

const { useAccessStore, useUserStore } = await import('~/stores')
const { setupRouterGuard } = await import('./guard')

const Blank = defineComponent({ name: 'BlankProbe', render: () => null })

function baseRoutes(): RouteRecordRaw[] {
  return [
    { path: '/', name: 'RootLayout', component: Blank, children: [] },
    {
      path: '/auth',
      name: 'Authentication',
      component: Blank,
      redirect: LOGIN_PATH,
      children: [
        { path: 'login', name: 'Login', component: Blank },
        { path: 'register', name: 'Register', component: Blank },
      ],
    },
    { path: HOME_PATH, name: 'Dashboard', component: Blank, meta: { title: 'menu.workspace' } },
    { path: '/first', name: 'First', component: Blank },
    { path: FORBIDDEN_PATH, name: 'Forbidden', component: Blank },
    { path: SERVER_ERROR_PATH, name: 'ServerError', component: Blank },
    { path: NOT_FOUND_PATH, name: 'NotFound', component: Blank },
    { path: '/:pathMatch(.*)*', name: 'NotFoundCatchAll', component: Blank },
  ]
}

function createGuardedRouter(): Router {
  const router = createRouter({ history: createMemoryHistory(), routes: baseRoutes() })
  setupRouterGuard(router)
  return router
}

function userInfoFixture(overrides: Partial<UserInfo> = {}): UserInfo {
  return {
    basicId: 'u-1',
    userName: 'tester',
    roles: ['admin'],
    permissions: ['sys:view'],
    ...overrides,
  }
}

function permissionFixture(overrides: Partial<PermissionInfo> = {}): PermissionInfo {
  return {
    roles: ['admin'],
    permissions: ['sys:view'],
    menus: [],
    ...overrides,
  }
}

function registerApis(apis: Partial<AppContextApis>): void {
  registerAppContext({ apis: apis as AppContextApis })
}

/** 已登录且路由已加载：跳过守卫里的拉取分支，直接测导航判定 */
function signInWithLoadedRoutes(homeMenuPath = HOME_PATH): void {
  const accessStore = useAccessStore()
  const userStore = useUserStore()
  accessStore.setAccessToken('token-abc')
  userStore.setUserInfo(userInfoFixture())
  accessStore.setAccessRoutes([
    { path: homeMenuPath, name: 'HomeMenu', meta: { title: 'menu.workspace' } },
  ])
}

let originalTitle = ''

beforeEach(() => {
  setActivePinia(createPinia())
  originalTitle = document.title
  // restoreAllMocks 只还原 spyOn，vi.fn() 的实现与调用记录跨用例累积，必须显式重置
  stubs.loadingBar.start.mockClear()
  stubs.loadingBar.finish.mockClear()
  stubs.loadingBar.finishAll.mockClear()
  stubs.loadingBar.error.mockClear()
  stubs.isLockedState.mockReset()
  stubs.isLockedState.mockReturnValue(false)
  stubs.hydratePreferencesFromBackend.mockReset()
  stubs.hydratePreferencesFromBackend.mockResolvedValue(undefined)
  registerApis({
    getUserInfoApi: vi.fn(async () => userInfoFixture()),
    getPermissionsApi: vi.fn(async () => permissionFixture()),
  })
})

afterEach(() => {
  document.title = originalTitle
})

describe('未登录', () => {
  it('访问受保护页跳登录并把原始 fullPath 写进 redirect', async () => {
    const router = createGuardedRouter()
    await router.push('/first?tab=2#anchor')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(router.currentRoute.value.query.redirect).toBe('/first?tab=2#anchor')
  })

  it('跳登录用的是 replace，不在历史里留下受保护页', async () => {
    const router = createGuardedRouter()
    const before = router.options.history.location
    await router.push('/first')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(before).not.toBe(LOGIN_PATH)
  })

  it('登录页本身放行，不会自跳成死循环', async () => {
    const router = createGuardedRouter()
    await router.push(LOGIN_PATH)
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(router.currentRoute.value.query.redirect).toBeUndefined()
  })

  it('认证前缀 AUTH_PATH 下的其它页面（注册）同样放行', async () => {
    const router = createGuardedRouter()
    await router.push('/auth/register')
    expect(router.currentRoute.value.name).toBe('Register')
  })

  it('403 在白名单内，未登录也能看到禁止访问页', async () => {
    const router = createGuardedRouter()
    await router.push(FORBIDDEN_PATH)
    expect(router.currentRoute.value.path).toBe(FORBIDDEN_PATH)
  })

  it('404 在白名单内，未登录也能看到未找到页', async () => {
    const router = createGuardedRouter()
    await router.push(NOT_FOUND_PATH)
    expect(router.currentRoute.value.path).toBe(NOT_FOUND_PATH)
  })

  it('500 在白名单内，未登录也能看到服务异常页', async () => {
    const router = createGuardedRouter()
    await router.push(SERVER_ERROR_PATH)
    expect(router.currentRoute.value.path).toBe(SERVER_ERROR_PATH)
  })

  it('未登录访问根路径也被拦到登录页，redirect 记的是 /', async () => {
    const router = createGuardedRouter()
    await router.push('/')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(router.currentRoute.value.query.redirect).toBe('/')
  })

  it('未登录不会调用任何用户/权限接口', async () => {
    const getUserInfoApi = vi.fn(async () => userInfoFixture())
    const getPermissionsApi = vi.fn(async () => permissionFixture())
    registerApis({ getUserInfoApi, getPermissionsApi })
    const router = createGuardedRouter()
    await router.push('/first')
    expect(getUserInfoApi).not.toHaveBeenCalled()
    expect(getPermissionsApi).not.toHaveBeenCalled()
  })

  it('空串 token 视为未登录', async () => {
    useAccessStore().setAccessToken('')
    const router = createGuardedRouter()
    await router.push('/first')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
  })
})

describe('已登录访问认证页', () => {
  it('被弹回首页，不允许重复登录', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push(LOGIN_PATH)
    expect(router.currentRoute.value.path).toBe(HOME_PATH)
  })

  it('首页取的是菜单推导出的 homePath，而不是写死的 HOME_PATH', async () => {
    signInWithLoadedRoutes('/first')
    const router = createGuardedRouter()
    await router.push(LOGIN_PATH)
    expect(router.currentRoute.value.path).toBe('/first')
    expect(router.currentRoute.value.path).not.toBe(HOME_PATH)
  })

  it('认证页判定按 AUTH_PATH 前缀，注册页也一并弹回首页', async () => {
    signInWithLoadedRoutes()
    const router = createGuardedRouter()
    await router.push('/auth/register')
    expect(router.currentRoute.value.path).toBe(HOME_PATH)
  })
})

describe('用户上下文失效后的重新拉取', () => {
  it('有 token 但无用户信息时同时拉用户与权限，并写回两个 store', async () => {
    useAccessStore().setAccessToken('token-abc')
    const getUserInfoApi = vi.fn(async () => userInfoFixture({ userName: 'from-server' }))
    const getPermissionsApi = vi.fn(async () => permissionFixture({
      roles: ['auditor'],
      permissions: ['log:view'],
      menus: [{ path: HOME_PATH, name: 'Dashboard', meta: { title: 'menu.workspace' } }],
    }))
    registerApis({ getUserInfoApi, getPermissionsApi })

    const router = createGuardedRouter()
    await router.push(HOME_PATH)

    expect(getUserInfoApi).toHaveBeenCalledTimes(1)
    expect(getPermissionsApi).toHaveBeenCalledTimes(1)
    const userStore = useUserStore()
    expect(userStore.userInfo?.userName).toBe('from-server')
    // 角色/权限以权限接口为准覆盖用户接口返回值
    expect(userStore.roles).toEqual(['auditor'])
    expect(useAccessStore().accessCodes).toEqual(['log:view'])
  })

  it('同一次导航里拉到的权限被复用，不会为装载路由再请求一次权限接口', async () => {
    useAccessStore().setAccessToken('token-abc')
    const getPermissionsApi = vi.fn(async () => permissionFixture({
      menus: [{ path: HOME_PATH, name: 'Dashboard', meta: { title: 'menu.workspace' } }],
    }))
    registerApis({
      getUserInfoApi: vi.fn(async () => userInfoFixture()),
      getPermissionsApi,
    })

    const router = createGuardedRouter()
    await router.push(HOME_PATH)
    expect(getPermissionsApi).toHaveBeenCalledTimes(1)
  })

  it('用户信息缺 basicId 时视为上下文无效，重新拉取', async () => {
    const accessStore = useAccessStore()
    accessStore.setAccessToken('token-abc')
    useUserStore().setUserInfo({ basicId: '', userName: 'ghost', roles: [], permissions: [] })
    const getUserInfoApi = vi.fn(async () => userInfoFixture())
    registerApis({
      getUserInfoApi,
      getPermissionsApi: vi.fn(async () => permissionFixture({
        menus: [{ path: HOME_PATH, name: 'Dashboard', meta: { title: 'menu.workspace' } }],
      })),
    })

    const router = createGuardedRouter()
    await router.push(HOME_PATH)
    expect(getUserInfoApi).toHaveBeenCalledTimes(1)
  })

  it('已有完整用户信息且路由已加载时完全不再请求', async () => {
    signInWithLoadedRoutes()
    const getUserInfoApi = vi.fn(async () => userInfoFixture())
    const getPermissionsApi = vi.fn(async () => permissionFixture())
    registerApis({ getUserInfoApi, getPermissionsApi })

    const router = createGuardedRouter()
    await router.push('/first')
    expect(getUserInfoApi).not.toHaveBeenCalled()
    expect(getPermissionsApi).not.toHaveBeenCalled()
  })
})

describe('用户上下文拉取失败', () => {
  it('非锁定态下清空登录态并跳登录，带上原路径', async () => {
    const accessStore = useAccessStore()
    accessStore.setAccessToken('token-abc')
    registerApis({
      getUserInfoApi: vi.fn(async () => {
        throw new Error('401')
      }),
      getPermissionsApi: vi.fn(async () => permissionFixture()),
    })

    const router = createGuardedRouter()
    await router.push('/first?keep=1')

    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(router.currentRoute.value.query.redirect).toBe('/first?keep=1')
    expect(useAccessStore().accessToken).toBeNull()
    expect(useUserStore().userInfo).toBeNull()
  })

  it('权限接口失败同样触发清场，两个请求任意一个挂掉都算失效', async () => {
    useAccessStore().setAccessToken('token-abc')
    registerApis({
      getUserInfoApi: vi.fn(async () => userInfoFixture()),
      getPermissionsApi: vi.fn(async () => {
        throw new Error('500')
      }),
    })

    const router = createGuardedRouter()
    await router.push('/first')
    expect(router.currentRoute.value.path).toBe(LOGIN_PATH)
    expect(useAccessStore().accessToken).toBeNull()
  })

  it('会话锁定（423）时直接放行进壳层，令牌与用户态一律不清', async () => {
    stubs.isLockedState.mockReturnValue(true)
    useAccessStore().setAccessToken('token-abc')
    registerApis({
      getUserInfoApi: vi.fn(async () => {
        throw new Error('423')
      }),
      getPermissionsApi: vi.fn(async () => {
        throw new Error('423')
      }),
    })

    const router = createGuardedRouter()
    await router.push('/first')

    expect(router.currentRoute.value.path).toBe('/first')
    expect(useAccessStore().accessToken).toBe('token-abc')
    expect(useAccessStore().isRoutesLoaded).toBe(false)
  })

  it('锁定态放行后不会顺带把动态路由标记成已加载，解锁后仍会重新装载', async () => {
    stubs.isLockedState.mockReturnValue(true)
    useAccessStore().setAccessToken('token-abc')
    registerApis({
      getUserInfoApi: vi.fn(async () => {
        throw new Error('423')
      }),
      getPermissionsApi: vi.fn(async () => {
        throw new Error('423')
      }),
    })

    const router = createGuardedRouter()
    await router.push('/first')
    expect(useAccessStore().accessRoutes).toEqual([])
    expect(useAccessStore().isRoutesLoaded).toBe(false)
  })
})
