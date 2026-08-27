import { useAccessStore, useUserStore } from '~/stores'

export function usePermission() {
  const userStore = useUserStore()
  const accessStore = useAccessStore()

  /**
   * 数组口径的「或」判定：空串项一律剔除后再比对。
   * 空串代表「没配权限码」，只有单个入参时才有「不限制」的含义；
   * 混在数组里若还享受豁免，常量表漏配一项就会静默放开整组按钮。
   */
  function matchAny(permissions: string[]): boolean {
    return permissions.some(p => !!p && (userStore.hasPermission(p) || accessStore.hasCode(p)))
  }

  function hasPermission(permission: string | string[]): boolean {
    if (Array.isArray(permission))
      return matchAny(permission)
    // 单个空串 = 未配置权限码 = 不限制
    if (!permission)
      return true
    return matchAny([permission])
  }

  function hasRole(role: string | string[]): boolean {
    if (!role)
      return true
    const roles = Array.isArray(role) ? role : [role]
    return roles.some(r => userStore.hasRole(r))
  }

  /** 与 hasPermission 的数组分支同口径，保证同一批入参两条路径结论一致 */
  function hasAnyPermission(permissions: string[]): boolean {
    return matchAny(permissions)
  }

  return {
    hasPermission,
    hasRole,
    hasAnyPermission,
  }
}
