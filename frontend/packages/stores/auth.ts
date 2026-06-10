import type {
  EmailLoginParams,
  LoginParams,
  LoginResponse,
  LoginToken,
  OAuthProviderItem,
  PermissionInfo,
  PhoneLoginParams,
  UserInfo,
} from '~/types'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useSignalR } from '~/composables'
import { islandStart } from '~/composables/useDynamicIsland'
import { HOME_PATH, LOGIN_PATH } from '~/constants'
import { mapMenuToRoutes } from '~/router/dynamic'
import { CORE_ROUTE_NAMES } from '~/router/routes/core'
import { useAccessStore, useAppStore, useTabbarStore, useUserStore } from '~/stores'
import { useAppContext } from './app-context'
import { hydratePreferencesFromBackend, resetPreferenceBackendSync } from './helpers'

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore()
  const appStore = useAppStore()
  const userStore = useUserStore()
  const tabbarStore = useTabbarStore()

  const loginLoading = ref(false)

  async function afterLogin(result: LoginToken, redirect?: string) {
    const loginTask = islandStart('auth:login', '正在登录…')
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
      accessStore.$reset()
      userStore.$reset()
      loginTask.error('登录失败')
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
    if (!userInfo.tenantId) {
      await router.replace('/control-center')
      loginTask.success('登录成功')
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

    loginTask.success('登录成功')
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

  // 从 coreRoutes 自动派生，新增认证/错误路由时无需手动维护
  const STATIC_ROUTE_NAMES = new Set([
    'RootLayout',
    'Profile',
    'ControlCenter',
    'EditorDemo',
    ...CORE_ROUTE_NAMES,
  ])

  async function logout() {
    const ctx = useAppContext()
    const router = await ctx.getRouter()
    try {
      const signalR = useSignalR()
      await signalR.destroy()
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
        if (route.name && !STATIC_ROUTE_NAMES.has(route.name as string)) {
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
    login,
    loginByPhoneCode,
    loginByEmailCode,
    startOAuthLogin,
    handleOAuthCallback,
    logout,
  }
})
