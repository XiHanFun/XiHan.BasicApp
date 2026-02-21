import { useUserStore } from '~/stores'
import { useAccessStore } from '~/stores'

export function usePermission() {
  const userStore = useUserStore()
  const accessStore = useAccessStore()

  function hasPermission(permission: string | string[]): boolean {
    if (!permission) return true
    const permissions = Array.isArray(permission) ? permission : [permission]
    return permissions.some(
      (p) => userStore.hasPermission(p) || accessStore.hasCode(p),
    )
  }

  function hasRole(role: string | string[]): boolean {
    if (!role) return true
    const roles = Array.isArray(role) ? role : [role]
    return roles.some((r) => userStore.hasRole(r))
  }

  function hasAnyPermission(permissions: string[]): boolean {
    return permissions.some((p) => hasPermission(p))
  }

  return {
    hasPermission,
    hasRole,
    hasAnyPermission,
  }
}
