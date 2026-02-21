import type { Router } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { i18n } from '~/locales'
import { useAccessStore, useAppStore, useTabbarStore, useUserStore } from '~/stores'
import { LOGIN_PATH, HOME_PATH } from '~/constants'
import { getUserInfoApi, getUserMenuRoutesApi } from '@/api'
import { mapMenuToRoutes } from './dynamic'

const WHITE_LIST = [LOGIN_PATH, '/403', '/404', '/500']

export function setupRouterGuard(router: Router) {
  const installDynamicRoutes = (routes: RouteRecordRaw[]) => {
    for (const route of routes) {
      const routeName = route.name ? String(route.name) : ''
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

    const isWhiteListed = WHITE_LIST.includes(to.path)

    // 未登录
    if (!accessStore.accessToken) {
      if (isWhiteListed) return next()
      return next({
        path: LOGIN_PATH,
        query: { redirect: encodeURIComponent(to.fullPath) },
        replace: true,
      })
    }

    // 已登录访问登录页
    if (to.path === LOGIN_PATH) {
      return next({ path: HOME_PATH, replace: true })
    }

    // 已登录但无用户信息，尝试获取
    if (!userStore.isLoggedIn) {
      try {
        const userInfo = await getUserInfoApi()
        userStore.setUserInfo(userInfo)
      } catch {
        accessStore.$reset()
        userStore.$reset()
        return next({
          path: LOGIN_PATH,
          query: { redirect: encodeURIComponent(to.fullPath) },
          replace: true,
        })
      }
    }

    if (!accessStore.isRoutesLoaded) {
      try {
        const dynamicMenus = await getUserMenuRoutesApi()
        accessStore.setAccessRoutes(dynamicMenus)
        installDynamicRoutes(mapMenuToRoutes(dynamicMenus))
        return next({ ...to, replace: true })
      } catch {
        accessStore.setAccessRoutes([])
      }
    }

    // 权限检查
    const { roles, permissions } = to.meta as {
      roles?: string[]
      permissions?: string[]
    }

    if (roles?.length || permissions?.length) {
      const hasAccess =
        roles?.some((r) => userStore.hasRole(r)) ||
        permissions?.some((p) => userStore.hasPermission(p))

      if (!hasAccess) {
        return next({ path: '/403', replace: true })
      }
    }

    const routeTitle = (to.meta?.title as string) || (to.name as string) || 'Untitled'
    const pinned = to.path === HOME_PATH || Boolean(to.meta?.affixTab)
    tabbarStore.ensureTab({
      key: to.fullPath,
      title: routeTitle,
      path: to.fullPath,
      pinned,
      closable: !pinned,
    })
    if (appStore.tabbarMaxCount > 0 && tabbarStore.tabs.length > appStore.tabbarMaxCount) {
      const removable = tabbarStore.tabs.find((tab) => tab.closable && tab.path !== to.fullPath)
      if (removable) {
        tabbarStore.removeTab(removable.path)
      }
    }

    next()
  })

  router.afterEach((to) => {
    const appStore = useAppStore()
    if (!appStore.dynamicTitle) {
      document.title = 'XiHan BasicApp'
      return
    }
    const title = to.meta?.title as string | undefined
    if (title) {
      document.title = `${i18n.global.t(title, title)} - XiHan BasicApp`
    }
  })
}
