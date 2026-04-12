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
        path: HOME_PATH,
        name: 'Dashboard',
        redirect: '/dashboard/workspace',
        meta: {
          title: '工作台',
          icon: 'mdi:view-dashboard-outline',
        },
        children: [
          {
            path: '/dashboard/workspace',
            name: 'DashboardWorkspace',
            component: () => import('~/views/_core/dashboard/index.vue'),
            meta: {
              title: '工作台',
              icon: 'mdi:view-dashboard-outline',
              affixTab: true,
            },
          },
          {
            path: '/dashboard/inbox',
            name: 'DashboardInbox',
            component: () => import('@/views/dashboard/inbox/index.vue'),
            meta: {
              title: '站内信',
              icon: 'lucide:inbox',
            },
          },
        ],
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
      {
        path: 'editor-demo',
        name: 'EditorDemo',
        component: () => import('~/views/_core/editor-demo/index.vue'),
        meta: {
          title: 'menu.editor_demo',
          icon: 'lucide:file-edit',
          hidden: true,
        },
      },
    ],
  },
  ...coreRoutes,
]
