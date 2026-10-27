import type { RouteRecordRaw } from 'vue-router'
import type { MenuNode } from '~/stores'
import { createRouter, createWebHistory } from 'vue-router'
import { LOGIN_PATH } from '~/constants'
import { i18n } from '~/locales'
import { useAuthStore } from '~/stores'
import { firstRoute, menuRoutes } from './menus'
import { StatusView } from './status-view'
import { resolveView } from './views'

export { firstRoute, menuRoutes } from './menus'
export { resolveView, viewKeys } from './views'

declare module 'vue-router' {
  interface RouteMeta {
    /** 不要求登录的页面（登录页） */
    public?: boolean
    /** 页面登记的权限码；守卫按 auth.permissions 精确校验 */
    permission?: string
    /** 标题文案键 */
    titleKey?: string
    /** 壳：认证页用 auth，其余缺省管理端壳 */
    layout?: 'basic' | 'auth'
    /** 壳层读的标题（与 titleKey 同值）与图标名（页面登记的 `lucide:<名>`） */
    title?: string
    icon?: string
    /** 标签页是否参与 KeepAlive 缓存 */
    keepAlive?: boolean
  }
}

export const LOGIN_ROUTE = 'identity.login'
export const FORBIDDEN_ROUTE = 'forbidden'
export const HOME_ROUTE = 'home'

// 静态核心路由：登录、个人（改密）、状态页；业务页面由后端菜单驱动（auth.permissions → installMenuRoutes）
export const routes: RouteRecordRaw[] = [
  { path: '/', name: HOME_ROUTE, component: StatusView, props: { kind: 'home' } },
  {
    // 路径取壳层的 LOGIN_PATH（/auth/login）：认证壳的入口标签带与登录页工具条按它认登录页
    path: LOGIN_PATH,
    name: LOGIN_ROUTE,
    component: resolveView('identity/login/index'),
    meta: { public: true, titleKey: 'identity.login.title', layout: 'auth' },
  },
  {
    path: '/identity/profile/change-password',
    name: 'identity.profile.change-password',
    component: resolveView('identity/profile/change-password/index'),
    meta: { titleKey: 'identity.profile.change_password.title' },
  },
  { path: '/forbidden', name: FORBIDDEN_ROUTE, component: StatusView, props: { kind: 'forbidden' } },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: StatusView, props: { kind: 'not_found' } },
]

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

let installed: string[] = []

/** 把菜单树装进路由表（重复装载先卸掉上一份：换账号后菜单不同）。 */
export function installMenuRoutes(menus: readonly MenuNode[]): void {
  for (const name of installed)
    router.removeRoute(name)
  installed = []
  for (const record of menuRoutes(menus)) {
    router.addRoute(record)
    installed.push(String(record.name))
  }
}

/**
 * 守卫顺序（方案 6.5）：白名单 → 登录态 → 装载权限与菜单路由（首次或刷新后）→ 首页落到第一个菜单 → 权限校验。
 */
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.meta.public)
    return auth.isAuthenticated && to.name === LOGIN_ROUTE ? { path: '/' } : true
  if (!auth.isAuthenticated)
    return { name: LOGIN_ROUTE, query: to.fullPath === '/' ? {} : { redirect: to.fullPath } }
  if (!auth.permissions) {
    const loaded = await auth.loadPermissions().catch(() => null)
    if (!loaded) {
      auth.clear()
      return { name: LOGIN_ROUTE }
    }
    installMenuRoutes(loaded.menus)
    // 路由表变了：按新表重新解析目标
    return { path: to.fullPath, replace: true }
  }
  if (to.name === HOME_ROUTE) {
    const first = firstRoute(auth.menus)
    return first ? { path: first, replace: true } : true
  }
  if (to.meta.permission && !auth.can(to.meta.permission))
    return { name: FORBIDDEN_ROUTE, replace: true }
  return true
})

// 文档标题跟随页面：「页面标题 · 应用名」，没有标题键的只留应用名
router.afterEach((to) => {
  const t = i18n.global.t
  document.title = to.meta.titleKey ? `${t(to.meta.titleKey)} · ${t('common.app_name')}` : t('common.app_name')
})
