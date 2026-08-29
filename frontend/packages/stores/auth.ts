import type {
  EmailLoginParams,
  ImpersonationCandidate,
  LoginParams,
  LoginResponse,
  LoginToken,
  OAuthProviderItem,
  PermissionInfo,
  PhoneLoginParams,
  StartImpersonationParams,
  UserInfo,
} from '~/types'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { clearLockState, isLockedState } from '~/composables/session-lock'
import { islandStart } from '~/composables/useDynamicIsland'
import { destroyAllSignalRConnections } from '~/composables/useSignalR'
import { HOME_PATH, LOGIN_PATH, TABS_LIST_KEY } from '~/constants'
import { i18n } from '~/locales'
import { mapMenuToRoutes } from '~/router/dynamic'
import { collectRouteNames, CORE_ROUTE_NAMES } from '~/router/routes/core'
import { useAccessStore } from './access'
import { useAppStore } from './app'
import { useAppContext } from './app-context'
import { hydratePreferencesFromBackend, markPreferenceIdentitySwitched, resetPreferenceBackendSync } from './helpers'
import { useTabbarStore } from './tabbar'
import { useUserStore } from './user'

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore()
  const appStore = useAppStore()
  const userStore = useUserStore()
  const tabbarStore = useTabbarStore()

  const loginLoading = ref(false)
  const impersonationLoading = ref(false)

  async function afterLogin(result: LoginToken, redirect?: string) {
    const loginTask = islandStart('auth:login', i18n.global.t('island.auth.logging_in'))
    const ctx = useAppContext()
    const router = await ctx.getRouter()

    accessStore.setAccessToken(result.accessToken)
    accessStore.setRefreshToken(result.refreshToken)

    let userInfo: UserInfo
    let permissionInfo: PermissionInfo
    try {
      ;[userInfo, permissionInfo] = await Promise.all([
        ctx.apis.getUserInfoApi(),
        ctx.apis.getPermissionsApi(),
      ])
    }
    catch (error) {
      // 会话被服务端锁定（默认密码登录 → 强制改密）：登录本身已成功、令牌有效。
      // 不能清令牌跳登录页——锁定遮罩（LockScreen 强制改密模式）要接管 UI；
      // 用户信息/权限接口解锁前都会被 423，直接进入应用壳层，解锁后守卫会重新拉取。
      if (isLockedState()) {
        await router.replace(accessStore.homePath || HOME_PATH)
        loginTask.success(i18n.global.t('island.auth.login_success'))
        return
      }

      accessStore.$reset()
      userStore.$reset()
      loginTask.error(i18n.global.t('island.auth.login_failed'))
      throw error
    }

    userStore.setUserInfo({
      ...userInfo,
      roles: permissionInfo.roles,
      permissions: permissionInfo.permissions,
    })
    appStore.setBranding({
      title: userInfo.appTitle,
      logo: userInfo.appLogo,
    })
    accessStore.setAccessCodes(permissionInfo.permissions)
    accessStore.setAccessButtons(permissionInfo.buttons ?? [])
    accessStore.setAccessRoutes(permissionInfo.menus)

    for (const route of mapMenuToRoutes(permissionInfo.menus)) {
      const routeName = route.name ? String(route.name) : ''
      const routePathExists = router.getRoutes().some(item => item.path === route.path)
      if (!routePathExists && routeName && !router.hasRoute(routeName)) {
        router.addRoute('RootLayout', route)
      }
    }

    // 跨端同步：登录后拉取后端偏好并应用（覆盖本地），在进入应用前完成以避免闪烁
    // showIsland:false — 同步过程由登录灵动岛统一覆盖，避免重复提示
    await hydratePreferencesFromBackend({ showIsland: false })

    // 智能落点（先登录后选租户）：后端按成员关系决定登录态——
    // 未进入租户（tenantId 为空：平台账号/超管/多租户成员待选择）→ 控制中心；
    // 已直进唯一租户 → 正常首页/重定向。
    // 控制中心路由由应用注册（shellRoutes）；未配置的应用没有租户切换概念，直接走正常首页
    if (!userInfo.tenantId && ctx.shellRoutes.controlCenter) {
      await router.replace(ctx.shellRoutes.controlCenter)
      loginTask.success(i18n.global.t('island.auth.login_success'))
      return
    }

    const homePath = accessStore.homePath || HOME_PATH
    if (redirect) {
      const target = decodeURIComponent(redirect)
      const resolved = router.resolve(target)
      const isValid
        = resolved.matched.length > 0
          && resolved.name !== 'NotFound'
          && resolved.name !== 'NotFoundCatchAll'
      await router.replace(isValid ? target : homePath)
    }
    else {
      await router.replace(homePath)
    }

    loginTask.success(i18n.global.t('island.auth.login_success'))
  }

  async function login(params: LoginParams, redirect?: string): Promise<LoginResponse | null> {
    loginLoading.value = true
    try {
      const ctx = useAppContext()
      const response = await ctx.apis.loginApi(params)
      if (response.requiresTwoFactor) {
        return response
      }
      if (response.token) {
        await afterLogin(response.token, redirect)
      }
      return null
    }
    finally {
      loginLoading.value = false
    }
  }

  async function loginByPhoneCode(params: PhoneLoginParams, redirect?: string) {
    loginLoading.value = true
    try {
      const ctx = useAppContext()
      const result = await ctx.apis.phoneLoginApi(params)
      await afterLogin(result, redirect)
    }
    finally {
      loginLoading.value = false
    }
  }

  async function loginByEmailCode(params: EmailLoginParams, redirect?: string) {
    loginLoading.value = true
    try {
      const ctx = useAppContext()
      const result = await ctx.apis.emailLoginApi(params)
      await afterLogin(result, redirect)
    }
    finally {
      loginLoading.value = false
    }
  }

  function startOAuthLogin(provider: OAuthProviderItem) {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || ''
    const apiPrefix = import.meta.env.VITE_API_PREFIX || '/api'
    const url = `${baseUrl}${apiPrefix}/OAuth/ExternalLogin?provider=${encodeURIComponent(provider.name)}`
    window.location.href = url
  }

  async function handleOAuthCallback(token: LoginToken, redirect?: string) {
    loginLoading.value = true
    try {
      await afterLogin(token, redirect)
    }
    finally {
      loginLoading.value = false
    }
  }

  /**
   * 身份切换后重建整个应用壳层：换令牌 → 清掉上一身份留在本地的标签页/偏好回写通道 → 整页重载。
   * 走整页重载而不是热切换，是因为路由表、权限码、菜单、SignalR 连接全都绑在旧身份上。
   */
  function applySwitchedIdentity(token: LoginToken) {
    accessStore.setAccessToken(token.accessToken)
    accessStore.setRefreshToken(token.refreshToken)
    userStore.$reset()
    tabbarStore.closeAll()
    sessionStorage.removeItem(TABS_LIST_KEY)
    localStorage.removeItem(TABS_LIST_KEY)
    resetPreferenceBackendSync()
    // 新身份的偏好由后端水合，本地这份属于上一身份：标记后让水合阶段不拿它当种子回写进新账号
    markPreferenceIdentitySwitched()
    clearLockState()
    void destroyAllSignalRConnections()
    window.location.href = import.meta.env.VITE_ROUTER_HISTORY === 'history' ? '/' : './'
  }

  /** 可模仿的候选用户 */
  function impersonationCandidates(keyword?: string): Promise<ImpersonationCandidate[]> {
    return useAppContext().apis.impersonationApi.candidates(keyword)
  }

  /** 发起模仿登录：以目标用户身份重建会话 */
  async function startImpersonation(input: StartImpersonationParams) {
    if (impersonationLoading.value) {
      return
    }
    impersonationLoading.value = true
    try {
      const token = await useAppContext().apis.impersonationApi.start(input)
      applySwitchedIdentity(token)
    }
    catch (error) {
      impersonationLoading.value = false
      throw error
    }
  }

  /** 结束模仿登录：回到发起人身份 */
  async function stopImpersonation() {
    if (impersonationLoading.value) {
      return
    }
    impersonationLoading.value = true
    try {
      const token = await useAppContext().apis.impersonationApi.stop()
      applySwitchedIdentity(token)
    }
    catch (error) {
      impersonationLoading.value = false
      throw error
    }
  }

  async function logout() {
    const ctx = useAppContext()
    const router = await ctx.getRouter()
    // 登出时保留的路由 = 应用侧静态路由（经 AppContext 注入，packages 不认识 RootLayout/EditorDemo 这些应用页名）
    //                   + packages 自己的核心路由（登录/错误页）
    // 原先是一份手写常量，却顶着「自动派生」的注释：它漏了 AboutProject / OAuthAuthorize，
    // 导致登出再登录后这两页 404。改为真派生后，应用侧新增静态路由无需再回来改这里。
    const staticRouteNames = new Set<string>([
      ...collectRouteNames(ctx.getStaticRoutes()),
      ...CORE_ROUTE_NAMES,
      // 个人中心由后端菜单动态注册（coreComponentMap 的 '_core/profile/index'），不在静态路由表里
      'Profile',
    ])
    try {
      // 登出销毁全部 Hub 连接（通知/聊天等），不逐个枚举 hubPath
      await destroyAllSignalRConnections()
    }
    catch {
      // ignore signalr destroy error
    }

    try {
      await ctx.apis.logoutApi()
    }
    catch {
      // ignore logout api error
    }

    try {
      const allRoutes = router.getRoutes()
      for (const route of allRoutes) {
        if (route.name && !staticRouteNames.has(route.name as string)) {
          try {
            router.removeRoute(route.name)
          }
          catch {
            // ignore remove route error
          }
        }
      }
    }
    catch {
      // ignore dynamic route clear error
    }

    accessStore.$reset()
    userStore.$reset()
    tabbarStore.closeAll()
    sessionStorage.clear()
    // 锁屏状态在 localStorage（需跨标签页共享），sessionStorage.clear() 清不掉它；
    // 不显式清除会导致重新登录后仍卡在锁屏。走 clearLockState 而非直接删 key：
    // 它会广播锁定态变更，让本标签页的遮罩当场收起（storage 事件只通知其他标签页）。
    clearLockState()
    resetPreferenceBackendSync()

    try {
      await router.replace(LOGIN_PATH)
    }
    catch {
      const base = import.meta.env.VITE_BASE || '/'
      const normalizedBase = base.endsWith('/') ? base : `${base}/`
      if (import.meta.env.VITE_ROUTER_HISTORY === 'history') {
        window.location.href = LOGIN_PATH
      }
      else {
        window.location.href = `${normalizedBase}#${LOGIN_PATH}`
      }
    }
  }

  return {
    loginLoading,
    impersonationLoading,
    impersonationCandidates,
    startImpersonation,
    stopImpersonation,
    login,
    loginByPhoneCode,
    loginByEmailCode,
    startOAuthLogin,
    handleOAuthCallback,
    logout,
  }
})
