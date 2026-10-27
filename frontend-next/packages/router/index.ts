import type { RouteRecordRaw } from 'vue-router'
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '~/stores'
import { resolveView } from './views'

export { resolveView, viewKeys } from './views'

declare module 'vue-router' {
  interface RouteMeta {
    /** 不要求登录的页面（登录页） */
    public?: boolean
    /** 页面登记的权限码；N1 的 auth.permissions 下发后由守卫校验 */
    permission?: string
    /** 标题文案键 */
    titleKey?: string
  }
}

export const LOGIN_ROUTE = 'identity.login'

// 静态路由表：登录、个人（改密）与核心页面；业务页面随 N1 的后端菜单驱动（页面登记 → 菜单 → 动态路由）
export const routes: RouteRecordRaw[] = [
  { path: '/', redirect: '/identity/users' },
  {
    path: '/login',
    name: LOGIN_ROUTE,
    component: resolveView('identity/login/index'),
    meta: { public: true, titleKey: 'identity.login.title' },
  },
  {
    path: '/identity/profile/change-password',
    name: 'identity.profile.change-password',
    component: resolveView('identity/profile/change-password/index'),
    meta: { titleKey: 'identity.profile.change_password.title' },
  },
  {
    path: '/identity/users',
    name: 'identity.users',
    component: resolveView('identity/users/index'),
    meta: { permission: 'identity.users.read', titleKey: 'identity.users.title' },
  },
]

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

/** 守卫：未登录去登录页（带回跳地址）；已登录访问登录页回首页。权限门控随 auth.permissions 接入。 */
router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.public)
    return auth.isAuthenticated && to.name === LOGIN_ROUTE ? { path: '/' } : true
  if (!auth.isAuthenticated)
    return { name: LOGIN_ROUTE, query: to.fullPath === '/' ? {} : { redirect: to.fullPath } }
  return true
})
