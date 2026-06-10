import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import BasicLayout from '~/layouts/basic/index.vue'
import { coreRoutes } from '~/router/routes/core'

const AboutPage = () => import('~/views/_core/about/index.vue')
const ControlCenterPage = () => import('~/views/_core/control-center/index.vue')
const EditorDemoPage = () => import('~/views/_core/editor-demo/index.vue')
const ProfilePage = () => import('~/views/_core/profile/index.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
      {
        // 个人中心：经 Header 头像下拉进入，不在侧边栏菜单展示（hidden），故作为静态隐藏路由
        path: '/workbench/profile',
        name: 'Profile',
        component: ProfilePage,
        meta: {
          title: 'menu.profile',
          icon: 'lucide:user',
          hidden: true,
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
        path: '/editor-demo',
        name: 'EditorDemo',
        component: EditorDemoPage,
        meta: {
          title: 'menu.editor_demo',
          icon: 'lucide:file-pen-line',
        },
      },
    ],
  },
  {
    // 控制中心：登录后未进入租户（平台态）的全屏独立落点页——选择租户进入 / 平台管理入口；
    // 不挂载主布局（此时无菜单/标签栏），与认证页同级的顶层路由
    path: '/control-center',
    name: 'ControlCenter',
    component: ControlCenterPage,
    meta: {
      title: 'menu.control_center',
      icon: 'lucide:layout-grid',
      hidden: true,
    },
  },
  ...coreRoutes,
]
