import type { Router, RouteRecordRaw } from 'vue-router'
import { createDiscreteApi } from 'naive-ui'
import { AUTH_PATH, HOME_PATH, LOGIN_PATH } from '~/constants'
import { i18n } from '~/locales'
import { useAccessStore, useAppStore, useTabbarStore, useUserStore } from '~/stores'
import { useAppContext } from '~/stores/app-context'
import { mapMenuToRoutes } from './dynamic'
import { filterRoutesByPermission, isStaticRouteMode } from './static'

const { loadingBar } = createDiscreteApi(['loadingBar'])

const WHITE_LIST = ['/403', '/404', '/500']

export function setupRouterGuard(router: Router) {
  const ctx = useAppContext()

  const installDynamicRoutes = (routes: RouteRecordRaw[]) => {
    for (const route of routes) {
      const routeName = route.name ? String(route.name) : ''
      const routePathExists = router.getRoutes().some(item => item.path === route.path)
      if (routePathExists) {
        continue
      }
      if (routeName && !router.hasRoute(routeName)) {
        router.addRoute('RootLayout', route)
      }
    }
  }

  router.beforeEach(async (to, _from, next) => {
    const accessStore = useAccessStore()
    const appStore = useAppStore()
    const userStore = useUserStore()
    const tabbarStore = useTabbarStore()
    let permissionInfo: any = null

    if (appStore.transitionProgress) {
      loadingBar.start()
    }
    if (appStore.transitionLoading) {
      appStore.setPageLoading(true)
    }

    const isAuthPage = to.path.startsWith(AUTH_PATH)
    const isWhiteListed = isAuthPage || WHITE_LIST.includes(to.path)

    // 未登录
    if (!accessStore.accessToken) {
      if (isWhiteListed) {
        return next()
      }
      return next({
        path: LOGIN_PATH,
        query: { redirect: to.fullPath },
        replace: true,
      })
    }

    // 已登录访问认证页
    if (isAuthPage) {
      return next({ path: accessStore.homePath || HOME_PATH, replace: true })
    }

    // 已登录但用户上下文无效，重新拉取当前用户
    if (!userStore.isLoggedIn || Number(userStore.userInfo?.basicId ?? 0) <= 0) {
      try {
        const [userInfo, authPermission] = await Promise.all([
          ctx.apis.getUserInfoApi(),
          ctx.apis.getPermissionsApi(),
        ])
        permissionInfo = authPermission
        userStore.setUserInfo({
          ...userInfo,
          roles: authPermission.roles,
          permissions: authPermission.permissions,
        })
        accessStore.setAccessCodes(authPermission.permissions)
      }
      catch {
        accessStore.$reset()
        userStore.$reset()
        return next({
          path: LOGIN_PATH,
          query: { redirect: to.fullPath },
          replace: true,
        })
      }
    }

    if (!accessStore.isRoutesLoaded) {
      try {
        if (!permissionInfo) {
          permissionInfo = await ctx.apis.getPermissionsApi()
          accessStore.setAccessCodes(permissionInfo.permissions)
          if (userStore.userInfo) {
            userStore.setUserInfo({
              ...userStore.userInfo,
              roles: permissionInfo.roles,
              permissions: permissionInfo.permissions,
            })
          }
        }

        if (isStaticRouteMode()) {
          // 静态模式：基于前端路由定义 + 用户权限过滤
          const staticRoutes = ctx.getStaticRoutes()
          const rootRoute = staticRoutes.find(r => r.path === '/')
          const children = rootRoute?.children ?? []
          const filtered = filterRoutesByPermission(
            children,
            permissionInfo.roles,
            permissionInfo.permissions,
          )
          installDynamicRoutes(filtered)
          accessStore.setAccessRoutes([])
        }
        else {
          // 动态模式：后端菜单驱动
          const dynamicMenus = permissionInfo.menus
          accessStore.setAccessRoutes(dynamicMenus)
          installDynamicRoutes(mapMenuToRoutes(dynamicMenus))
        }
        return next({ path: to.fullPath, replace: true })
      }
      catch {
        accessStore.setAccessRoutes([])
      }
    }

    const resolvedHomePath = accessStore.homePath || HOME_PATH
    if (to.path === '/') {
      return next({ path: resolvedHomePath, replace: true })
    }
    if (
      to.path === HOME_PATH
      && resolvedHomePath !== HOME_PATH
      && (to.name === 'NotFound' || to.matched.length === 0)
    ) {
      return next({ path: resolvedHomePath, replace: true })
    }

    // 权限检查
    const { roles, permissions } = to.meta as {
      roles?: string[]
      permissions?: string[]
    }

    if (roles?.length || permissions?.length) {
      const hasAccess = roles?.some(r => userStore.hasRole(r))
        || permissions?.some(p => userStore.hasPermission(p))

      if (!hasAccess) {
        return next({ path: '/403', replace: true })
      }
    }

    const rawTitle = (to.meta?.title as string) || (to.name as string) || 'Untitled'
    const routeTitle = i18n.global.te(rawTitle) ? i18n.global.t(rawTitle) : rawTitle
    const pinned = to.path === (accessStore.homePath || HOME_PATH) || Boolean(to.meta?.affixTab)
    tabbarStore.ensureTab({
      key: to.fullPath,
      title: routeTitle,
      path: to.fullPath,
      pinned,
      closable: !pinned,
      meta: {
        icon: to.meta?.icon as string | undefined,
      },
    })
    if (!appStore.tabbarVisitHistory) {
      tabbarStore.closeOthers(to.fullPath)
    }
    if (appStore.tabbarMaxCount > 0 && tabbarStore.tabs.length > appStore.tabbarMaxCount) {
      const removable = tabbarStore.tabs.find(tab => tab.closable && tab.path !== to.fullPath)
      if (removable) {
        tabbarStore.removeTab(removable.path)
      }
    }

    next()
  })

  router.afterEach((to) => {
    const appStore = useAppStore()

    if (appStore.transitionProgress) {
      loadingBar.finish()
    }
    if (appStore.transitionLoading) {
      appStore.setPageLoading(false)
    }

    if (!appStore.dynamicTitle) {
      document.title = 'XiHan BasicApp'
      return
    }
    const title = to.meta?.title as string | undefined
    if (title) {
      const translated = i18n.global.te(title) ? i18n.global.t(title) : title
      document.title = `${translated} - XiHan BasicApp`
    }
  })

  router.onError(() => {
    const appStore = useAppStore()
    if (appStore.transitionProgress) {
      loadingBar.error()
    }
    if (appStore.transitionLoading) {
      appStore.setPageLoading(false)
    }
  })
}
