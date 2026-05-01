import type { RouteRecordRaw } from 'vue-router'
import { AUTH_PATH, FORBIDDEN_PATH, HOME_PATH, LOGIN_PATH, NOT_FOUND_PATH } from '~/constants'
import AuthLayout from '~/layouts/auth/index.vue'
import BasicLayout from '~/layouts/basic/index.vue'

const DashboardPage = () => import('~/views/_core/dashboard/index.vue')
const AboutPage = () => import('~/views/_core/about/index.vue')
const ProfilePage = () => import('~/views/_core/profile/index.vue')
const EditorDemoPage = () => import('~/views/_core/editor-demo/index.vue')
const LoginPage = () => import('~/views/_core/authentication/login.vue')
const CodeLoginPage = () => import('~/views/_core/authentication/code-login.vue')
const QrCodeLoginPage = () => import('~/views/_core/authentication/qrcode-login.vue')
const ForgetPasswordPage = () => import('~/views/_core/authentication/forget-password.vue')
const RegisterPage = () => import('~/views/_core/authentication/register.vue')
const OAuthCallbackPage = () => import('~/views/_core/authentication/oauth-callback.vue')
const ForbiddenPage = () => import('~/views/_core/fallback/forbidden.vue')
const ServerErrorPage = () => import('~/views/_core/fallback/server-error.vue')
const NotFoundPage = () => import('~/views/_core/fallback/not-found.vue')

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'RootLayout',
    component: BasicLayout,
    redirect: HOME_PATH,
    children: [
      {
        path: HOME_PATH,
        name: 'Dashboard',
        component: DashboardPage,
        meta: {
          title: 'menu.dashboard',
          icon: 'lucide:layout-dashboard',
          affixTab: true,
        },
      },
      {
        path: '/about',
        name: 'About',
        component: AboutPage,
        meta: {
          title: 'menu.about',
          icon: 'lucide:info',
        },
      },
      {
        path: '/profile',
        name: 'Profile',
        component: ProfilePage,
        meta: {
          title: 'header.user.profile',
          icon: 'lucide:user',
        },
      },
      {
        path: '/editor-demo',
        name: 'EditorDemo',
        component: EditorDemoPage,
        meta: {
          title: 'Editor Demo',
          icon: 'lucide:file-pen-line',
        },
      },
    ],
  },
  {
    path: AUTH_PATH,
    name: 'Authentication',
    component: AuthLayout,
    redirect: LOGIN_PATH,
    meta: {
      title: 'page.auth.login',
      hidden: true,
    },
    children: [
      {
        path: 'login',
        name: 'Login',
        component: LoginPage,
        meta: { title: 'page.auth.login', hidden: true },
      },
      {
        path: 'code-login',
        name: 'CodeLogin',
        component: CodeLoginPage,
        meta: { title: 'page.auth.mobile_login', hidden: true },
      },
      {
        path: 'qrcode-login',
        name: 'QrCodeLogin',
        component: QrCodeLoginPage,
        meta: { title: 'page.auth.qrcode_login', hidden: true },
      },
      {
        path: 'forget-password',
        name: 'ForgetPassword',
        component: ForgetPasswordPage,
        meta: { title: 'page.auth.forget_password', hidden: true },
      },
      {
        path: 'register',
        name: 'Register',
        component: RegisterPage,
        meta: { title: 'page.auth.register', hidden: true },
      },
      {
        path: 'oauth-callback',
        name: 'OAuthCallback',
        component: OAuthCallbackPage,
        meta: { title: 'OAuth Callback', hidden: true },
      },
    ],
  },
  {
    path: FORBIDDEN_PATH,
    name: 'Forbidden',
    component: ForbiddenPage,
    meta: { title: 'error.403', hidden: true },
  },
  {
    path: '/500',
    name: 'ServerError',
    component: ServerErrorPage,
    meta: { title: 'error.500', hidden: true },
  },
  {
    path: NOT_FOUND_PATH,
    name: 'NotFound',
    component: NotFoundPage,
    meta: { title: 'error.404', hidden: true },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFoundCatchAll',
    redirect: NOT_FOUND_PATH,
  },
]
