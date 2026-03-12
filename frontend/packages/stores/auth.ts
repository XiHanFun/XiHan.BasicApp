import type { LoginParams, LoginResult, PhoneLoginParams } from '~/types'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getPermissionsApi, getUserInfoApi, loginApi, logoutApi, phoneLoginApi } from '@/api'
import { HOME_PATH, LOGIN_PATH } from '~/constants'
import { mapMenuToRoutes } from '~/router/dynamic'
import { useAccessStore, useAppStore, useUserStore } from '~/stores'

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore()
  const appStore = useAppStore()
  const userStore = useUserStore()

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

    await router.replace(redirect ? decodeURIComponent(redirect) : accessStore.homePath || HOME_PATH)
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

  async function logout() {
    const { router } = await import('@/router')
    try {
      await logoutApi()
    }
    catch {
      // ignore logout api error
    }

    accessStore.$reset()
    userStore.$reset()

    await router.replace({
      path: LOGIN_PATH,
      query: {
        redirect: encodeURIComponent(router.currentRoute.value.fullPath),
      },
    })
  }

  return {
    loginLoading,
    login,
    loginByPhoneCode,
    logout,
  }
})
