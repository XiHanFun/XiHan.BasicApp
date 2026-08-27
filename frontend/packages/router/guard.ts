import type { Router, RouteRecordRaw } from 'vue-router'
import type { PermissionInfo } from '~/types'

import { isLockedState, loadingBar } from '~/composables'
import { AUTH_PATH, FORBIDDEN_PATH, HOME_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
import { i18n } from '~/locales'
import { hydratePreferencesFromBackend, useAccessStore, useAppStore, useTabbarStore, useUserStore } from '~/stores'
import { useAppContext } from '~/stores/app-context'
import { mapMenuToRoutes } from './dynamic'
import { filterRoutesByPermission, isStaticRouteMode } from './static'

const WHITE_LIST = [FORBIDDEN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH]

export function setupRouterGuard(router: Router) {
  const ctx = useAppContext()

  const installDynamicRoutes = (routes: RouteRecordRaw[]) => {
    for (const route of routes) {
      const routeName = route.name ? String(route.name) : ''
      const routePathExists = router.getRoutes().some(item => item.path === route.path)
      if (routePathExists) {
        // 路径已装载：正常的去重，静默跳过
        continue
      }
      if (!routeName) {
        // 后端菜单表允许 name 为空，这类菜单装不上。侧边栏照样渲染出条目，点进去却落 404，
        // 而整条链路一行日志都没有——配错的人无从查起。这里必须出声，与下面加载失败的日志同级。
        console.error('[router] 菜单缺少路由名，已跳过装载，导航到该路径会落 404', route.path)
        continue
      }
      if (!router.hasRoute(routeName)) {
        router.addRoute('RootLayout', route)
      }
    }
  }

  router.beforeEach(async (to) => {
    const accessStore = useAccessStore()
    const appStore = useAppStore()
    const userStore = useUserStore()
    const tabbarStore = useTabbarStore()
    let permissionInfo: null | PermissionInfo = null

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
        return true
      }
      return {
        path: LOGIN_PATH,
        query: { redirect: to.fullPath },
        replace: true,
      }
    }

    // 已登录访问认证页
    if (isAuthPage) {
      return { path: accessStore.homePath || HOME_PATH, replace: true }
    }

    // 已登录但用户上下文无效，重新拉取当前用户
    if (!userStore.isLoggedIn || !userStore.userInfo?.basicId) {
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
        // 会话锁定（423）：令牌仍有效，放行进入壳层，锁定遮罩（LockScreen）接管 UI；
        // 解锁后守卫会重新拉取用户信息与权限。
        if (isLockedState()) {
          return true
        }

        accessStore.$reset()
        userStore.$reset()
        return {
          path: LOGIN_PATH,
          query: { redirect: to.fullPath },
          replace: true,
        }
      }
    }

    // 白名单页对已登录用户同样是终点，不能再触发装载：
    // 装载失败时会重定向到 /500，若 /500 自身又进装载分支（isRoutesLoaded 仍为假），
    // 就是失败 → /500 → 再装载 → 再失败的自我循环，最终被 vue-router 判为无限重定向而中止，
    // 用户既看不到 500 页也停在白屏，权限接口还被连打多次。后端菜单接口整体不可用时必然命中。
    if (!accessStore.isRoutesLoaded && !WHITE_LIST.includes(to.path)) {
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
      }
      catch (error) {
        // 会话锁定（423）：权限/菜单接口在解锁前拿不到，直接放行挂壳（遮罩盖住空白内容区），
        // 解锁改密成功后重新走守卫即可完整加载。
        if (isLockedState()) {
          return true
        }

        // 不能静默吞掉：这里失败等于整个会话拿不到任何动态路由，
        // 之后每次导航都匹配不到而落 404，且无从查起
        console.error('[router] 动态路由加载失败', error)
        // 不能调 setAccessRoutes：它会把 isRoutesLoaded 置真，本次会话再也不会重试，
        // 一次偶发失败就把应用锁死在 404
        accessStore.accessRoutes = []
        return { path: SERVER_ERROR_PATH, replace: true }
      }

      // 刷新恢复会话：进入应用前拉取后端偏好并应用（覆盖本地），避免闪烁。
      // 单独 try：偏好同步是锦上添花，失败不该连累已经建好的路由表
      try {
        await hydratePreferencesFromBackend()
      }
      catch (error) {
        console.error('[preferences] 偏好同步失败，已跳过', error)
      }
      // 按 path + query + hash 三段重进，两种写法都不能用：
      // 1) { path: to.fullPath }：vue-router 不解析 path 里的查询串，含 ?query#hash 的 fullPath
      //    塞进 path 会把两者静默丢掉。这条分支只在「本次会话首次装载动态路由」时命中，
      //    也就是刷新页面/直接打开深链——正是查询参数最要紧的场景：带筛选条件的分享链接、
      //    OAuth 回调的 ?code=、登录后按 redirect 回跳的带参地址。
      // 2) { ...to }：会把 name 与 matched 一并带上，而 vue-router 优先按 name 解析；
      //    此刻动态路由刚装好、to 仍是装载前的匹配结果（通常是 404 兜底），重进会直接落回 404。
      return { path: to.path, query: to.query, hash: to.hash, replace: true }
    }

    const resolvedHomePath = accessStore.homePath || HOME_PATH
    if (to.path === '/') {
      return { path: resolvedHomePath, replace: true }
    }
    const isNotFoundRoute = to.name === 'NotFound'
      || to.name === 'NotFoundCatchAll'
      || to.matched.length === 0
    if (to.path === HOME_PATH && resolvedHomePath !== HOME_PATH && isNotFoundRoute) {
      return { path: resolvedHomePath, replace: true }
    }
    if (isNotFoundRoute && to.path !== NOT_FOUND_PATH) {
      return { path: NOT_FOUND_PATH, replace: true }
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
        return { path: FORBIDDEN_PATH, replace: true }
      }
    }

    // 独立公共页（控制中心等，meta.standalone）不挂主布局、不进入标签栏
    if (to.meta?.standalone) {
      return true
    }

    // 标签标题存的是国际化键而不是当次导航解析出的文本：
    // 标签栏与标签总览都按「是键就翻译、不是键就原样显示」渲染，
    // 在这里提前解析会把语言固化在打开标签的那一刻，之后切换语言标签栏不再跟随。
    const routeTitle = (to.meta?.title as string) || (to.name as string) || 'Untitled'
    const pinned = to.path === (accessStore.homePath || HOME_PATH) || Boolean(to.meta?.affixTab)
    tabbarStore.ensureTab({
      key: to.fullPath,
      // 路由名 + keepAlive 标记：供 KeepAlive 的 include 构建（见 tabbarStore.cachedTabNames）
      name: to.name ? String(to.name) : undefined,
      title: routeTitle,
      path: to.fullPath,
      pinned,
      closable: !pinned,
      keepAlive: Boolean(to.meta?.keepAlive),
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

    return true
  })

  router.afterEach((to) => {
    const appStore = useAppStore()

    // 重定向链上 beforeEach 会连开好几笔而 afterEach 只走一次，一次清干净
    loadingBar.finishAll()
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
    loadingBar.error()
    if (appStore.transitionLoading) {
      appStore.setPageLoading(false)
    }
  })
}
