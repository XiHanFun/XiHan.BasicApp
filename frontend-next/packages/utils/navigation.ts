export interface NavigableRouteLike {
  path?: string
  redirect?: unknown
  // 索引签名：兼容 vue-router 的 RouteMeta（辅助函数仅读取 hidden，其余字段透传），避免弱类型不匹配
  meta?: {
    hidden?: boolean
    [key: string]: unknown
  } | null
  children?: NavigableRouteLike[]
}

export function resolveRouteFullPath(path = '', parentPath = ''): string {
  if (!path) {
    return parentPath || '/'
  }
  if (path.startsWith('/')) {
    return path
  }
  return `${parentPath.replace(/\/$/, '')}/${path}`.replace(/\/{2,}/g, '/')
}

function isRouteHidden(route: NavigableRouteLike): boolean {
  return Boolean(route.meta?.hidden)
}

export function routeListContainsVisiblePath(
  routes: NavigableRouteLike[] | undefined,
  targetPath: string,
  parentPath = '',
): boolean {
  return (routes ?? []).some((route) => {
    const fullPath = resolveRouteFullPath(route.path, parentPath)
    if (!isRouteHidden(route) && fullPath === targetPath) {
      return true
    }

    return routeListContainsVisiblePath(route.children, targetPath, fullPath)
  })
}

export function resolveFirstNavigableRoutePath(
  route: NavigableRouteLike,
  parentPath = '',
): string {
  const fullPath = resolveRouteFullPath(route.path, parentPath)
  const visibleChildren = route.children?.filter(child => !isRouteHidden(child)) ?? []
  const redirect = typeof route.redirect === 'string' ? route.redirect : ''

  if (
    redirect
    && redirect !== fullPath
    && routeListContainsVisiblePath(visibleChildren, redirect, fullPath)
  ) {
    return redirect
  }

  const firstVisibleChild = visibleChildren[0]
  if (!firstVisibleChild) {
    return fullPath
  }

  return resolveFirstNavigableRoutePath(firstVisibleChild, fullPath)
}

export function resolveFirstNavigableRouteListPath(
  routes: NavigableRouteLike[] | undefined,
  parentPath = '',
): string {
  for (const route of routes ?? []) {
    const fullPath = resolveRouteFullPath(route.path, parentPath)
    if (isRouteHidden(route)) {
      const childPath = resolveFirstNavigableRouteListPath(route.children, fullPath)
      if (childPath) {
        return childPath
      }
      continue
    }

    const routePath = resolveFirstNavigableRoutePath(route, parentPath)
    if (routePath) {
      return routePath
    }
  }

  return ''
}
