import { createRouter, createWebHashHistory, createWebHistory } from 'vue-router'
import { staticRoutes } from './routes'

const base = import.meta.env.VITE_BASE || '/'
const history = import.meta.env.VITE_ROUTER_HISTORY === 'history'
  ? createWebHistory(base)
  : createWebHashHistory(base)

export const router = createRouter({
  history,
  routes: staticRoutes,
  scrollBehavior() {
    return { left: 0, top: 0 }
  },
})

export { staticRoutes }
