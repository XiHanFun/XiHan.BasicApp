import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import BasicLayout from '~/layouts/basic/index.vue'
import { coreRoutes } from '~/router/routes/core'

const AboutPage = () => import('~/views/_core/about/index.vue')
const EditorDemoPage = () => import('~/views/_core/editor-demo/index.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
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
