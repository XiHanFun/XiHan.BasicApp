import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { MenuRoute } from '~/types'
import { TOKEN_KEY, REFRESH_TOKEN_KEY } from '~/constants'
import { storage } from '~/utils'

export const useAccessStore = defineStore(
  'access',
  () => {
    const accessToken = ref<string | null>(storage.get<string>(TOKEN_KEY))
    const refreshToken = ref<string | null>(storage.get<string>(REFRESH_TOKEN_KEY))
    const accessRoutes = ref<MenuRoute[]>([])
    const accessCodes = ref<string[]>([])
    const isRoutesLoaded = ref(false)
    const loginExpired = ref(false)

    function setAccessToken(token: string | null) {
      accessToken.value = token
      if (token) {
        storage.set(TOKEN_KEY, token)
      } else {
        storage.remove(TOKEN_KEY)
      }
    }

    function setRefreshToken(token: string | null) {
      refreshToken.value = token
      if (token) {
        storage.set(REFRESH_TOKEN_KEY, token)
      } else {
        storage.remove(REFRESH_TOKEN_KEY)
      }
    }

    function setAccessRoutes(routes: MenuRoute[]) {
      accessRoutes.value = routes
      isRoutesLoaded.value = true
    }

    function setAccessCodes(codes: string[]) {
      accessCodes.value = codes
    }

    function setLoginExpired(expired: boolean) {
      loginExpired.value = expired
    }

    function hasCode(code: string): boolean {
      return accessCodes.value.includes(code) || accessCodes.value.includes('*')
    }

    function $reset() {
      accessToken.value = null
      refreshToken.value = null
      accessRoutes.value = []
      accessCodes.value = []
      isRoutesLoaded.value = false
      loginExpired.value = false
      storage.remove(TOKEN_KEY)
      storage.remove(REFRESH_TOKEN_KEY)
    }

    return {
      accessToken,
      refreshToken,
      accessRoutes,
      accessCodes,
      isRoutesLoaded,
      loginExpired,
      setAccessToken,
      setRefreshToken,
      setAccessRoutes,
      setAccessCodes,
      setLoginExpired,
      hasCode,
      $reset,
    }
  },
)
