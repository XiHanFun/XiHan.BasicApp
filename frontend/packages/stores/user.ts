import type { UserInfo } from '~/types'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { USER_INFO_KEY } from '~/constants'
import { LocalStorage } from '~/utils'

export const useUserStore = defineStore('user', () => {
  const userInfo = ref<UserInfo | null>(LocalStorage.get<UserInfo>(USER_INFO_KEY))

  const isLoggedIn = computed(() => Boolean(userInfo.value?.basicId))
  const username = computed(() => userInfo.value?.userName ?? '')
  const nickname = computed(() => userInfo.value?.nickName ?? '')
  const avatar = computed(() => userInfo.value?.avatar ?? '')
  const roles = computed(() => userInfo.value?.roles ?? [])
  const permissions = computed(() => userInfo.value?.permissions ?? [])

  function setUserInfo(info: UserInfo | null) {
    userInfo.value = info
    if (info) {
      LocalStorage.set(USER_INFO_KEY, info)
    }
    else {
      LocalStorage.remove(USER_INFO_KEY)
    }
  }

  function hasRole(role: string): boolean {
    return roles.value.includes(role)
  }

  /**
   * 照服务端下发的码精确匹配，不把 `*` 当通配：服务端已把超管的 `*` 展开成当前上下文生效的全部码，
   * 作用侧不含当前上下文的码（如平台态里的租户侧码）不下发、鉴权时 `*` 也不放行。前端再拿 `*` 短路，
   * 就会把服务端必拒的操作判成可用
   */
  function hasPermission(permission: string): boolean {
    return permissions.value.includes(permission)
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
