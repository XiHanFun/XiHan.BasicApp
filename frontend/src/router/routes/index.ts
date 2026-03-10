import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import { coreRoutes } from '~/router/routes/core'

const BasicLayout = () => import('~/layouts/basic/index.vue')

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    children: [
      {
        path: 'dashboard',
        name: 'DashboardWorkspace',
        component: () => import('~/views/_core/dashboard/index.vue'),
        meta: {
          title: 'menu.workspace',
          icon: 'mdi:view-dashboard-outline',
          affixTab: true,
        },
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('~/views/_core/profile/index.vue'),
        meta: {
          title: 'menu.profile',
          icon: 'lucide:user',
          hidden: true,
        },
      },
      {
        path: 'about',
        name: 'About',
        component: () => import('~/views/_core/about/index.vue'),
        meta: {
          title: 'menu.about',
          icon: 'lucide:info',
        },
      },
    ],
  },
  ...coreRoutes,
]
