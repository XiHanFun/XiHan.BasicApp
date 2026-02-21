import type { RouteRecordRaw } from 'vue-router'
import { LOGIN_PATH } from '~/constants'

export const coreRoutes: RouteRecordRaw[] = [
  {
    path: LOGIN_PATH,
    name: 'Login',
    component: () => import('@/views/_core/authentication/login.vue'),
    meta: {
      title: '登录',
      hidden: true,
    },
  },
  {
    path: '/403',
    name: 'Forbidden',
    component: () => import('@/views/_core/fallback/forbidden.vue'),
    meta: {
      title: '无权限',
      hidden: true,
    },
  },
  {
    path: '/500',
    name: 'ServerError',
    component: () => import('@/views/_core/fallback/server-error.vue'),
    meta: {
      title: '服务器错误',
      hidden: true,
    },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/_core/fallback/not-found.vue'),
    meta: {
      title: '页面不存在',
      hidden: true,
    },
  },
]
