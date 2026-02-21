import type { RouteRecordRaw } from 'vue-router'

export const dashboardRoutes: RouteRecordRaw = {
  path: '/dashboard',
  name: 'Dashboard',
  redirect: '/dashboard/workspace',
  meta: {
    title: 'menu.dashboard',
    icon: 'lucide:layout-dashboard',
    order: 1,
  },
  children: [
    {
      path: 'workspace',
      name: 'DashboardWorkspace',
      component: () => import('@/views/dashboard/workspace/index.vue'),
      meta: {
        title: 'menu.workspace',
        icon: 'lucide:home',
        keepAlive: true,
      },
    },
    {
      path: 'analytics',
      name: 'DashboardAnalytics',
      component: () => import('@/views/dashboard/analytics/index.vue'),
      meta: {
        title: 'menu.analytics',
        icon: 'lucide:bar-chart-2',
        keepAlive: true,
      },
    },
  ],
}
