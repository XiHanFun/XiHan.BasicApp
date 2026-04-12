import type { LoginParams, LoginResponse, LoginToken, OAuthProviderItem, PhoneLoginParams } from '~/types'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useSignalR } from '~/composables'
import { HOME_PATH, LOGIN_PATH } from '~/constants'
import { mapMenuToRoutes } from '~/router/dynamic'
import { useAccessStore, useAppStore, useTabbarStore, useUserStore } from '~/stores'
import { useAppContext } from './app-context'

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore()
  const appStore = useAppStore()
  const userStore = useUserStore()
  const tabbarStore = useTabbarStore()

  const loginLoading = ref(false)

  async function afterLogin(result: LoginToken, redirect?: string) {
    const ctx = useAppContext()
    const router = await ctx.getRouter()

    accessStore.setAccessToken(result.accessToken)
    accessStore.setRefreshToken(result.refreshToken)

    let userInfo: any
    let permissionInfo: any
    try {
      ;[userInfo, permissionInfo] = await Promise.all([
        ctx.apis.getUserInfoApi(),
        ctx.apis.getPermissionsApi(),
      ])
    }
    catch (error) {
      accessStore.$reset()
      userStore.$reset()
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
      if (routeName && !router.hasRoute(routeName)) {
        router.addRoute('RootLayout', route)
      }
    }

    const homePath = accessStore.homePath || HOME_PATH
    if (redirect) {
      const target = decodeURIComponent(redirect)
      const resolved = router.resolve(target)
      const isValid = resolved.matched.length > 0 && resolved.name !== 'NotFound'
      await router.replace(isValid ? target : homePath)
    }
    else {
      await router.replace(homePath)
    }
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

  function startOAuthLogin(provider: OAuthProviderItem, tenantId?: null | number) {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || ''
    const apiPrefix = import.meta.env.VITE_API_PREFIX || '/api'
    let url = `${baseUrl}${apiPrefix}/OAuth/ExternalLogin?provider=${encodeURIComponent(provider.name)}`
    if (tenantId && tenantId > 0) {
      url += `&tenantId=${tenantId}`
    }
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

  // Dashboard/About 等业务菜单由后端动态注入，不在此保留
  const STATIC_ROUTE_NAMES = new Set([
    'RootLayout',
    'Profile',
    'EditorDemo',
    'Authentication',
    'Login',
    'CodeLogin',
    'QrCodeLogin',
    'ForgetPassword',
    'Register',
    'OAuthCallback',
    'Forbidden',
    'ServerError',
    'NotFound',
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
    startOAuthLogin,
    handleOAuthCallback,
    logout,
  }
})
