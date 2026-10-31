import type { RouteRecordRaw } from 'vue-router'
import { AUTH_PATH, FORBIDDEN_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'

/**
 * 核心路由（认证 + 错误页）——唯一定义源。
 * src/router/routes.ts 直接展开使用，不再重复声明。
 * 认证子页照 4.x：登录、验证码登录、找回密码、注册与第三方登录回调（扫码登录 6.0 不做）；找回密码 6.0 发验证码，没有 4.x 的重置链接页。
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
        path: 'forget-password',
        name: 'ForgetPassword',
        component: () => import('~/views/_core/authentication/forget-password.vue'),
        meta: { title: 'page.auth.forget_password_title', hidden: true },
      },
      {
        // 注册入口由登录配置的 registrationEnabled 决定显不显示，路由常在：没开放时提交即被后端拒绝
        path: 'register',
        name: 'Register',
        component: () => import('~/views/_core/authentication/register.vue'),
        meta: { title: 'page.auth.create_account_title', hidden: true },
      },
      {
        // 后端回调把浏览器带到这里（OAUTH_CALLBACK_PATH）：登录凭票据换会话，绑定由已登录的人在这里确认；已登录也放行（见守卫）
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
