import type { RouteRecordRaw } from 'vue-router'
import { HOME_PATH } from '~/constants'
import BasicLayout from '~/layouts/basic/index.vue'
import { coreRoutes, resolveView } from '~/router'

const ControlCenterPage = () => import('~/views/_core/control-center/index.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
      {
        // 改密页：个人中心页随 Identity 的后续批次补，顶栏用户菜单的「个人中心」暂指向这里
        path: '/identity/profile/change-password',
        name: 'identity.profile.change-password',
        component: resolveView('identity/profile/change-password/index'),
        meta: { title: 'identity.profile.change_password.title' },
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
  ...coreRoutes,
]
