import type { RouteRecordRaw } from 'vue-router'
import { AUTH_PATH, FORBIDDEN_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'

/**
 * 核心路由（认证 + 错误页）——唯一定义源。
 * src/router/routes.ts 直接展开使用，不再重复声明。
 */
export const coreRoutes: RouteRecordRaw[] = [
  {
    path: AUTH_PATH,
    name: 'Authentication',
    component: () => import('~/views/_core/authentication/AuthLayout.vue'),
    redirect: LOGIN_PATH,
    meta: { title: 'page.login.title', hidden: true },
    children: [
      {
        path: 'login',
        name: 'Login',
        component: () => import('~/views/_core/authentication/login.vue'),
        meta: { title: 'page.login.title', hidden: true },
      },
      {
        path: 'code-login',
        name: 'CodeLogin',
        component: () => import('~/views/_core/authentication/code-login.vue'),
        meta: { title: 'page.auth.mobile_login', hidden: true },
      },
      {
        path: 'email-login',
        name: 'EmailLogin',
        component: () => import('~/views/_core/authentication/email-login.vue'),
        meta: { title: 'page.auth.email_login', hidden: true },
      },
      {
        path: 'qrcode-login',
        name: 'QrCodeLogin',
        component: () => import('~/views/_core/authentication/qrcode-login.vue'),
        meta: { title: 'page.auth.qrcode_login', hidden: true },
      },
      {
        path: 'forget-password',
        name: 'ForgetPassword',
        component: () => import('~/views/_core/authentication/forget-password.vue'),
        meta: { title: 'page.auth.forget_password_title', hidden: true },
      },
      {
        path: 'register',
        name: 'Register',
        component: () => import('~/views/_core/authentication/register.vue'),
        meta: { title: 'page.auth.create_account_title', hidden: true },
      },
      {
        path: 'oauth-callback',
        name: 'OAuthCallback',
        component: () => import('~/views/_core/authentication/oauth-callback.vue'),
        meta: { title: 'page.auth.oauth_callback_title', hidden: true },
      },
    ],
  },
  {
    path: FORBIDDEN_PATH,
    name: 'Forbidden',
    component: () => import('~/views/_core/fallback/forbidden.vue'),
    meta: { title: 'error.forbidden', hidden: true },
  },
  {
    path: SERVER_ERROR_PATH,
    name: 'ServerError',
    component: () => import('~/views/_core/fallback/server-error.vue'),
    meta: { title: 'error.server_error', hidden: true },
  },
  {
    path: NOT_FOUND_PATH,
    name: 'NotFound',
    component: () => import('~/views/_core/fallback/not-found.vue'),
    meta: { title: 'error.not_found', hidden: true },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFoundCatchAll',
    component: () => import('~/views/_core/fallback/not-found.vue'),
    meta: { title: 'error.not_found', hidden: true },
  },
]

/** 递归提取路由树中的所有 name */
function collectRouteNames(routes: RouteRecordRaw[]): string[] {
  const names: string[] = []
  for (const route of routes) {
    if (route.name)
      names.push(String(route.name))
    if (route.children)
      names.push(...collectRouteNames(route.children))
  }
  return names
}

/** 核心路由名称集合，logout 时保留这些路由不被移除 */
export const CORE_ROUTE_NAMES = new Set(collectRouteNames(coreRoutes))
