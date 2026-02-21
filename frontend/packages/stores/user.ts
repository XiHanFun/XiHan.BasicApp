import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { UserInfo } from '~/types'
import { USER_INFO_KEY } from '~/constants'
import { storage } from '~/utils'

export const useUserStore = defineStore(
  'user',
  () => {
    const userInfo = ref<UserInfo | null>(storage.get<UserInfo>(USER_INFO_KEY))

    const isLoggedIn = computed(() => userInfo.value !== null)
    const username = computed(() => userInfo.value?.userName ?? '')
    const nickname = computed(() => userInfo.value?.nickName ?? '')
    const avatar = computed(() => userInfo.value?.avatar ?? '')
    const roles = computed(() => userInfo.value?.roles ?? [])
    const permissions = computed(() => userInfo.value?.permissions ?? [])

    function setUserInfo(info: UserInfo | null) {
      userInfo.value = info
      if (info) {
        storage.set(USER_INFO_KEY, info)
      } else {
        storage.remove(USER_INFO_KEY)
      }
    }

    function hasRole(role: string): boolean {
      return roles.value.includes(role)
    }

    function hasPermission(permission: string): boolean {
      return permissions.value.includes(permission) || permissions.value.includes('*')
    }

    function hasAnyRole(roleList: string[]): boolean {
      return roleList.some((role) => hasRole(role))
    }

    function $reset() {
      userInfo.value = null
      storage.remove(USER_INFO_KEY)
    }

    return {
      userInfo,
      isLoggedIn,
      username,
      nickname,
      avatar,
      roles,
      permissions,
      setUserInfo,
      hasRole,
      hasPermission,
      hasAnyRole,
      $reset,
    }
  },
)
