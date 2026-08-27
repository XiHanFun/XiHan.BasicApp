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

    // 判据必须与 guard.ts 里 meta 权限检查的口径一致：
    // 声明了任一侧就要门控，满足「任一声明项」即放行（角色与权限之间是或，不是与）。
    //
    // 原写法是 hasRoleAccess = 未声明 || 命中，hasPermissionAccess 同理，
    // 再以 `!hasRoleAccess && !hasPermissionAccess` 排除。未声明的那一侧恒为 true，
    // 于是只写 meta.roles 的路由 hasPermissionAccess 恒真、排除条件恒假 —— 任何用户都拿得到；
    // 只写 meta.permissions 的由角色侧同样兜住。而路由 meta 通常只写一侧，
    // 等于静态模式下的路由级过滤整体失效，无权限用户会拿到本不该出现的菜单与路由。
    const isRestricted = Boolean(requiredRoles?.length) || Boolean(requiredPermissions?.length)
    const isGranted = userPermissions.includes('*')
      || (requiredRoles?.some(r => userRoles.includes(r)) ?? false)
      || (requiredPermissions?.some(p => userPermissions.includes(p)) ?? false)

    if (isRestricted && !isGranted) {
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
