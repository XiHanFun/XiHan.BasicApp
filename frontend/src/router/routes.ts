import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import BasicLayout from '~/layouts/basic/index.vue'
import { coreRoutes } from '~/router/routes/core'

const DashboardPage = () => import('~/views/_core/dashboard/index.vue')
const AboutPage = () => import('~/views/_core/about/index.vue')
const ProfilePage = () => import('~/views/_core/profile/index.vue')
const EditorDemoPage = () => import('~/views/_core/editor-demo/index.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
      {
        path: HOME_PATH,
        name: 'Dashboard',
        component: DashboardPage,
        meta: {
          title: 'menu.dashboard',
          icon: 'lucide:layout-dashboard',
          affixTab: true,
        },
      },
      {
        path: '/about',
        name: 'About',
        component: AboutPage,
        meta: {
          title: 'menu.about',
          icon: 'lucide:info',
        },
      },
      {
        path: '/profile',
        name: 'Profile',
        component: ProfilePage,
        meta: {
          title: 'header.user.profile',
          icon: 'lucide:user',
        },
      },
      {
        path: '/editor-demo',
        name: 'EditorDemo',
        component: EditorDemoPage,
        meta: {
          title: 'Editor Demo',
          icon: 'lucide:file-pen-line',
        },
      },
    ],
  },
  ...coreRoutes,
]
