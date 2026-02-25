import type { RouteRecordRaw } from 'vue-router'

export const workspaceRoute: RouteRecordRaw = {
  path: '/workspace',
  name: 'Workspace',
  component: () => import('@/views/dashboard/workspace/index.vue'),
  meta: {
    title: 'menu.workspace',
    icon: 'lucide:home',
    keepAlive: true,
    order: 1,
  },
}

export const analyticsRoute: RouteRecordRaw = {
  path: '/analytics',
  name: 'Analytics',
  component: () => import('@/views/dashboard/analytics/index.vue'),
  meta: {
    title: 'menu.analytics',
    icon: 'lucide:bar-chart-2',
    keepAlive: true,
    order: 2,
  },
}
