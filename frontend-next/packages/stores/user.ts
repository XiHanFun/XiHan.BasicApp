import type { UserInfo } from '~/types'
import { defineStore } from 'pinia'
import { computed } from 'vue'
import { PLATFORM_TENANT_ID } from '~/constants'
import { useAuthStore } from './auth'
import { SetupStoreId } from './store-ids'

/**
 * 当前用户（壳层视角）：auth 会话里的用户资料投影成 4.x 壳层认识的 UserInfo。
 * 角色与权限码来自 auth.permissions；当前上下文（平台 / 租户名）与模仿态来自会话（`tenantName`、`impersonatorUserName`）；
 * 头像与应用品牌字段 6.0 还没有，留空。
 */
export const useUserStore = defineStore(SetupStoreId.User, () => {
  const auth = useAuthStore()

  const userInfo = computed<UserInfo | null>(() => {
    const user = auth.user
    if (!user)
      return null
    const impersonatorUserName = auth.session?.impersonatorUserName ?? null
    return {
      basicId: user.id,
      userName: user.userName,
      nickName: user.displayName,
      email: user.email ?? undefined,
      phone: user.phoneNumber ?? undefined,
      tenantId: auth.tenantId,
      tenantName: auth.session?.tenantName ?? null,
      isPlatform: auth.tenantId === null,
      canAccessPlatform: user.homeTenantId === PLATFORM_TENANT_ID,
      roles: (auth.permissions?.roles ?? []).map(role => role.code),
      permissions: auth.permissions?.permissions ?? [],
      // 模仿态里不能再模仿（后端 IDENTITY_CONFLICT_003），入口随之收起
      canImpersonate: impersonatorUserName === null && auth.can('identity.impersonation.start'),
      isImpersonating: impersonatorUserName !== null,
      impersonatorUserName,
    }
  })
  const isLoggedIn = computed(() => auth.isAuthenticated)
  const username = computed(() => userInfo.value?.userName ?? '')
  const nickname = computed(() => userInfo.value?.nickName ?? '')
  const avatar = computed(() => userInfo.value?.avatar ?? '')
  const roles = computed(() => userInfo.value?.roles ?? [])
  const permissions = computed(() => userInfo.value?.permissions ?? [])

  function hasRole(role: string): boolean {
    return roles.value.includes(role)
  }

  /** 精确匹配，`*` 不当通配：后端已把超管的 `*` 展开成当前上下文生效的全部码 */
  function hasPermission(permission: string): boolean {
    return auth.can(permission)
  }

  function hasAnyRole(roleList: string[]): boolean {
    return roleList.some(role => hasRole(role))
  }

  function $reset() {
    auth.clear()
  }

  return { userInfo, isLoggedIn, username, nickname, avatar, roles, permissions, hasRole, hasPermission, hasAnyRole, $reset }
})
