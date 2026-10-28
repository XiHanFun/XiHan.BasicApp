import type { RouteRecordRaw } from 'vue-router'
import { AUTH_PATH, FORBIDDEN_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
import { resolveView } from '../views'

/**
 * 核心路由（认证 + 错误页）——唯一定义源。
 * src/router/routes.ts 直接展开使用，不再重复声明。
 * 认证子页只登记 6.0 后端已支持的（登录）；验证码登录、注册、找回密码等随 Identity 的后续批次补回。
 */
export const coreRoutes: RouteRecordRaw[] = [
  {
    path: AUTH_PATH,
    name: 'Authentication',
    component: () => import('~/views/_core/authentication/AuthLayout.vue'),
    redirect: LOGIN_PATH,
    meta: { title: 'identity.login.title', hidden: true },
    children: [
      {
        path: 'login',
        name: 'Login',
        component: resolveView('identity/login/index'),
        meta: { title: 'identity.login.title', hidden: true },
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
