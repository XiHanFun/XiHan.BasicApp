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
        // 个人中心：前端静态路由（后端菜单表按约定不登记静态路由）。
        // 菜单项由 use-layout-menu-domain 注入到「工作台」末位展示；也可经 Header 头像下拉进入。
        path: '/workbench/profile',
        name: 'Profile',
        component: ProfilePage,
        meta: {
          title: 'menu.profile',
          icon: 'lucide:user',
        },
      },
      {
        path: '/about/project',
        name: 'AboutProject',
        component: AboutPage,
        meta: {
          title: 'menu.about_project',
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
      // 独立公共页：不挂主布局、不进入标签栏（同认证页定位）
      standalone: true,
    },
  },
  ...coreRoutes,
]
