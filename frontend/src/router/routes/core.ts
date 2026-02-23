import type { RouteRecordRaw } from 'vue-router'
import { LOGIN_PATH } from '~/constants'

export const coreRoutes: RouteRecordRaw[] = [
  {
    path: LOGIN_PATH,
    name: 'Login',
    component: () => import('@/views/_core/authentication/login.vue'),
    meta: {
      title: 'page.login.title',
      hidden: true,
    },
  },
  {
    path: '/403',
    name: 'Forbidden',
    component: () => import('@/views/_core/fallback/forbidden.vue'),
    meta: {
      title: 'error.forbidden',
      hidden: true,
    },
  },
  {
    path: '/500',
    name: 'ServerError',
    component: () => import('@/views/_core/fallback/server-error.vue'),
    meta: {
      title: 'error.server_error',
      hidden: true,
    },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/_core/fallback/not-found.vue'),
    meta: {
      title: 'error.not_found',
      hidden: true,
    },
  },
]
