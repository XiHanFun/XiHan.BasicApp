import type { LoginParams, LoginResult, PhoneLoginParams } from '~/types'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getPermissionsApi, getUserInfoApi, loginApi, logoutApi, phoneLoginApi } from '@/api'
import { HOME_PATH, LOGIN_PATH } from '~/constants'
import { mapMenuToRoutes } from '~/router/dynamic'
import { useAccessStore, useAppStore, useTabbarStore, useUserStore } from '~/stores'

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore()
  const appStore = useAppStore()
  const userStore = useUserStore()
  const tabbarStore = useTabbarStore()

  const loginLoading = ref(false)

  async function afterLogin(result: LoginResult, redirect?: string) {
    const { router } = await import('@/router')

    accessStore.setAccessToken(result.accessToken)
    accessStore.setRefreshToken(result.refreshToken)

    let userInfo: Awaited<ReturnType<typeof getUserInfoApi>>
    let permissionInfo: Awaited<ReturnType<typeof getPermissionsApi>>
    try {
      ;[userInfo, permissionInfo] = await Promise.all([
        getUserInfoApi(),
        getPermissionsApi(),
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

  async function login(params: LoginParams, redirect?: string) {
    loginLoading.value = true
    try {
      const result = await loginApi(params)
      await afterLogin(result, redirect)
    }
    finally {
      loginLoading.value = false
    }
  }

  async function loginByPhoneCode(params: PhoneLoginParams, redirect?: string) {
    loginLoading.value = true
    try {
      const result = await phoneLoginApi(params)
      await afterLogin(result, redirect)
    }
    finally {
      loginLoading.value = false
    }
  }

  const STATIC_ROUTE_NAMES = new Set([
    'RootLayout',
    'DashboardWorkspace',
    'Profile',
    'About',
    'Authentication',
    'Login',
    'CodeLogin',
    'QrCodeLogin',
    'ForgetPassword',
    'Register',
    'Forbidden',
    'ServerError',
    'NotFound',
  ])

  async function logout() {
    const { router } = await import('@/router')
    try {
      await logoutApi()
    }
    catch {
      // ignore logout api error
    }

    const allRoutes = router.getRoutes()
    for (const route of allRoutes) {
      if (route.name && !STATIC_ROUTE_NAMES.has(route.name as string)) {
        router.removeRoute(route.name)
      }
    }

    accessStore.$reset()
    userStore.$reset()
    tabbarStore.closeAll()
    sessionStorage.clear()

    await router.replace(LOGIN_PATH)
  }

  return {
    loginLoading,
    login,
    loginByPhoneCode,
    logout,
  }
})
