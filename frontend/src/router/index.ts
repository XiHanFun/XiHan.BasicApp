import { createRouter, createWebHashHistory, createWebHistory } from 'vue-router'
import { routes } from './routes'
import { setupRouterGuard } from './guard'

export const router = createRouter({
  history:
    import.meta.env.VITE_ROUTER_HISTORY === 'history'
      ? createWebHistory(import.meta.env.VITE_BASE || '/')
      : createWebHashHistory(import.meta.env.VITE_BASE || '/'),
  routes,
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) return savedPosition
    if (to.hash) return { behavior: 'smooth', el: to.hash }
    return { left: 0, top: 0 }
  },
})

setupRouterGuard(router)

export default router
