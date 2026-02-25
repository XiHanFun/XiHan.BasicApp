import type { RouteRecordRaw } from 'vue-router'
import { coreRoutes } from '~/router/routes/core'
import { analyticsRoute, workspaceRoute } from './modules/dashboard'
import { playgroundRoutes } from './modules/playground'
import { systemRoutes } from './modules/system'

const BasicLayout = () => import('~/layouts/basic/index.vue')

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: '/workspace',
    children: [
      workspaceRoute,
      analyticsRoute,
      playgroundRoutes,
      systemRoutes,
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
    ],
  },
  ...coreRoutes,
]
