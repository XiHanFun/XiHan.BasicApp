import type { RouteRecordRaw } from 'vue-router'
import { coreRoutes } from '~/router/routes/core'

const BasicLayout = () => import('~/layouts/basic/index.vue')

// 静态路由只包含不依赖后端菜单的核心页面
// Dashboard/About 等业务菜单由后端动态菜单驱动注入
export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    children: [
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
