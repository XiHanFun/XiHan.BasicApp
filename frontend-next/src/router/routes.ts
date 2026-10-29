import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import BasicLayout from '~/layouts/basic/index.vue'
import { coreRoutes } from '~/router'

const ControlCenterPage = () => import('~/views/_core/control-center/index.vue')
const OAuthAuthorizePage = () => import('~/views/_core/oauth/authorize.vue')
const ProfilePage = () => import('~/views/_core/profile/index.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
      {
        // 个人中心（4.x 原样搬入，目前只接了账号安全页签）：顶栏用户菜单的「个人中心」指向这里，人人可进，不进菜单
        path: '/profile',
        name: 'Profile',
        component: ProfilePage,
        meta: { title: 'component.profile.title', hidden: true },
      },
    ],
  },
  {
    // 控制中心：切换租户 / 进入平台的全屏独立落点页——选择租户进入 / 平台管理入口；
    // 不挂载主布局（此时无菜单/标签栏），与认证页同级的顶层路由
    path: '/control-center',
    name: 'ControlCenter',
    component: ControlCenterPage,
    meta: {
      title: 'page.control_center.title',
      icon: 'lucide:layout-grid',
      hidden: true,
      // 独立公共页：不挂主布局、不进入标签栏（同认证页定位）
      standalone: true,
    },
  },
  {
    // OAuth2 授权同意页：后端 GET /api/oauth2/authorize 把浏览器带到 /#/oauth/authorize?<原查询串> 的全屏独立页——需已登录
    // （未登录由守卫带着完整 fullPath 跳登录再回跳）；不挂主布局、不进入标签栏。同意 / 拒绝 / 预览走 OpenPlatform 的 oauth-consents.*
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
