import type { RouteRecordRaw } from 'vue-router'

/**
 * 静态路由模式：基于用户角色/权限过滤前端定义的路由。
 * 只保留用户有权限访问的路由，递归处理子路由。
 */
export function filterRoutesByPermission(
  routes: RouteRecordRaw[],
  userRoles: string[],
  userPermissions: string[],
): RouteRecordRaw[] {
  return routes.reduce<RouteRecordRaw[]>((filtered, route) => {
    const meta = route.meta as {
      roles?: string[]
      permissions?: string[]
    } | undefined

    const requiredRoles = meta?.roles
    const requiredPermissions = meta?.permissions

    const hasRoleAccess = !requiredRoles?.length
      || requiredRoles.some(r => userRoles.includes(r))
    const hasPermissionAccess = !requiredPermissions?.length
      || userPermissions.includes('*')
      || requiredPermissions.some(p => userPermissions.includes(p))

    if (!hasRoleAccess && !hasPermissionAccess) {
      return filtered
    }

    const cloned = { ...route }
    if (cloned.children?.length) {
      cloned.children = filterRoutesByPermission(cloned.children, userRoles, userPermissions)
    }
    filtered.push(cloned)
    return filtered
  }, [])
}

export function isStaticRouteMode(): boolean {
  return import.meta.env.VITE_AUTH_ROUTE_MODE === 'static'
}
