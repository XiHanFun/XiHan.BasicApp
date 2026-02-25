import type { LoginParams } from '~/types'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { loginApi, logoutApi } from '~/api'
import { HOME_PATH, LOGIN_PATH } from '~/constants'
import { useAccessStore, useAppStore, useUserStore } from '~/stores'

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore()
  const appStore = useAppStore()
  const userStore = useUserStore()

  const loginLoading = ref(false)

  async function login(params: LoginParams, redirect?: string) {
    const { router } = await import('@/router')
    loginLoading.value = true
    try {
      const result = await loginApi(params)

      accessStore.setAccessToken(result.accessToken)
      accessStore.setRefreshToken(result.refreshToken)

      // 登录结果直接包含用户信息和权限，无需额外请求
      userStore.setUserInfo({
        ...result.user,
        roles: result.roles,
        permissions: result.permissions,
      })
      appStore.setBranding({
        title: result.user.appTitle,
        logo: result.user.appLogo,
      })
      accessStore.setAccessCodes(result.permissions)
      accessStore.isRoutesLoaded = false

      await router.replace(redirect ? decodeURIComponent(redirect) : HOME_PATH)
    } finally {
      loginLoading.value = false
    }
  }

  async function logout() {
    const { router } = await import('@/router')
    try {
      await logoutApi()
    } catch {
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
    logout,
  }
})
