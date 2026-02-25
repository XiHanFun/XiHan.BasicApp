import type { RouteRecordRaw } from 'vue-router'
import { LOGIN_PATH } from '~/constants'

export const coreRoutes: RouteRecordRaw[] = [
  {
    path: '/auth',
    name: 'Authentication',
    component: () => import('~/views/_core/authentication/AuthLayout.vue'),
    redirect: LOGIN_PATH,
    meta: { title: 'page.login.title', hidden: true },
    children: [
      {
        path: 'login',
        name: 'Login',
        component: () => import('~/views/_core/authentication/login.vue'),
        meta: { title: 'page.login.title' },
      },
      {
        path: 'code-login',
        name: 'CodeLogin',
        component: () => import('~/views/_core/authentication/code-login.vue'),
        meta: { title: 'page.auth.mobile_login' },
      },
      {
        path: 'qrcode-login',
        name: 'QrCodeLogin',
        component: () => import('~/views/_core/authentication/qrcode-login.vue'),
        meta: { title: 'page.auth.qrcode_login' },
      },
      {
        path: 'forget-password',
        name: 'ForgetPassword',
        component: () => import('~/views/_core/authentication/forget-password.vue'),
        meta: { title: 'page.auth.forget_password_title' },
      },
      {
        path: 'register',
        name: 'Register',
        component: () => import('~/views/_core/authentication/register.vue'),
        meta: { title: 'page.auth.register_btn' },
      },
    ],
  },
  {
    path: '/403',
    name: 'Forbidden',
    component: () => import('~/views/_core/fallback/forbidden.vue'),
    meta: {
      title: 'error.forbidden',
      hidden: true,
    },
  },
  {
    path: '/500',
    name: 'ServerError',
    component: () => import('~/views/_core/fallback/server-error.vue'),
    meta: {
      title: 'error.server_error',
      hidden: true,
    },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('~/views/_core/fallback/not-found.vue'),
    meta: {
      title: 'error.not_found',
      hidden: true,
    },
  },
]
