import type { RouteRecordRaw } from 'vue-router'
import { createRouter, createWebHistory } from 'vue-router'
import { resolveView } from './views'

export { resolveView, viewKeys } from './views'

declare module 'vue-router' {
  interface RouteMeta {
    /** 页面登记的权限码；N1 的 auth.permissions 下发后由守卫校验 */
    permission?: string
    /** 标题文案键 */
    titleKey?: string
  }
}

// N0 的静态路由表：N1 起由后端菜单驱动（页面登记 → 菜单 → 动态路由），这里只剩核心静态路由
export const routes: RouteRecordRaw[] = [
  { path: '/', redirect: '/identity/users' },
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
