import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import BasicLayout from '~/layouts/basic/index.vue'
import { coreRoutes } from '~/router/routes/core'

const AboutPage = () => import('~/views/_core/about/index.vue')
const ControlCenterPage = () => import('~/views/_core/control-center/index.vue')
const EditorDemoPage = () => import('~/views/_core/editor-demo/index.vue')
const OAuthAuthorizePage = () => import('@/views/oauth/authorize.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
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
  {
    // OAuth2 授权同意页：第三方经 /connect/authorize 跳入的全屏独立页——需已登录（未登录由守卫跳登录再回跳）；
    // 不挂主布局、不进入标签栏。后端令牌/授权码流程见 OAuthConnectEndpoints / OAuthConsentAppService
    path: '/oauth/authorize',
    name: 'OAuthAuthorize',
    component: OAuthAuthorizePage,
    meta: {
      title: 'page.oauth.consent_title',
      hidden: true,
      standalone: true,
    },
  },
  ...coreRoutes,
]
