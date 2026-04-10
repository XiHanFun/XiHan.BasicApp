import type { UserInfo } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { USER_INFO_KEY } from '~/constants'
import { LocalStorage } from '~/utils'

function normalizeUserInfo(info: null | UserInfo): null | UserInfo {
  if (!info) {
    return null
  }

  const resolvedId = Number(info.basicId ?? 0)

  return {
    ...info,
    basicId: resolvedId > 0 ? resolvedId : 0,
  }
}

export const useUserStore = defineStore('user', () => {
  const userInfo = ref<UserInfo | null>(normalizeUserInfo(LocalStorage.get<UserInfo>(USER_INFO_KEY)))

  const isLoggedIn = computed(() => Number(userInfo.value?.basicId ?? 0) > 0)
  const username = computed(() => userInfo.value?.userName ?? '')
  const nickname = computed(() => userInfo.value?.nickName ?? '')
  const avatar = computed(() => userInfo.value?.avatar ?? '')
  const roles = computed(() => userInfo.value?.roles ?? [])
  const permissions = computed(() => userInfo.value?.permissions ?? [])

  function setUserInfo(info: UserInfo | null) {
    const normalized = normalizeUserInfo(info)
    userInfo.value = normalized
    if (normalized) {
      LocalStorage.set(USER_INFO_KEY, normalized)
    }
    else {
      LocalStorage.remove(USER_INFO_KEY)
    }
  }

  function hasRole(role: string): boolean {
    return roles.value.includes(role)
  }

  function hasPermission(permission: string): boolean {
    return permissions.value.includes(permission) || permissions.value.includes('*')
  }

  function hasAnyRole(roleList: string[]): boolean {
    return roleList.some(role => hasRole(role))
  }

  function $reset() {
    userInfo.value = null
    LocalStorage.remove(USER_INFO_KEY)
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
})
