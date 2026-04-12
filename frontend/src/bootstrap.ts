import * as api from '@/api'
import { routes } from '@/router/routes'
import { registerAppContext } from '~/stores/app-context'

/**
 * 应用引导：在路由守卫和 store 使用前注册所有 packages 依赖的外部实现。
 * 必须在 main.ts 中尽早调用（Pinia 安装后、router.install 前）。
 */
export function setupAppContext() {
  registerAppContext({
    // ---- API 函数 ----
    apis: {
      // auth 相关
      loginApi: api.loginApi,
      logoutApi: api.logoutApi,
      phoneLoginApi: api.phoneLoginApi,
      getUserInfoApi: api.getUserInfoApi,
      getPermissionsApi: api.getPermissionsApi,
      getLoginConfigApi: api.getLoginConfigApi,
      registerApi: api.registerApi,
      sendPhoneLoginCodeApi: api.sendPhoneLoginCodeApi,
      requestPasswordResetApi: api.requestPasswordResetApi,
      // profile 相关
      getProfileApi: api.getProfileApi,
      updateProfileApi: api.updateProfileApi,
      changeUserNameApi: api.changeUserNameApi,
      confirmChangeEmailApi: api.confirmChangeEmailApi,
      confirmChangePhoneApi: api.confirmChangePhoneApi,
      sendChangeEmailCodeApi: api.sendChangeEmailCodeApi,
      sendChangePhoneCodeApi: api.sendChangePhoneCodeApi,
      sendEmailVerifyCodeApi: api.sendEmailVerifyCodeApi,
      sendPhoneVerifyCodeApi: api.sendPhoneVerifyCodeApi,
      verifyEmailApi: api.verifyEmailApi,
      verifyPhoneApi: api.verifyPhoneApi,
      // security 相关
      changePasswordApi: api.changePasswordApi,
      deactivateAccountApi: api.deactivateAccountApi,
      deleteAccountApi: api.deleteAccountApi,
      disable2FAApi: api.disable2FAApi,
      enable2FAApi: api.enable2FAApi,
      getLinkedAccountsApi: api.getLinkedAccountsApi,
      getLoginLogsApi: api.getLoginLogsApi,
      getSessionsApi: api.getSessionsApi,
      revokeOtherSessionsApi: api.revokeOtherSessionsApi,
      revokeSessionApi: api.revokeSessionApi,
      send2FASetupCodeApi: api.send2FASetupCodeApi,
      setup2FAApi: api.setup2FAApi,
      unlinkAccountApi: api.unlinkAccountApi,
      // dashboard 相关
      accessLogApi: api.accessLogApi,
      operationLogApi: api.operationLogApi,
      userApi: api.userApi,
      userSessionApi: api.userSessionApi,
      // about 相关
      serverApi: api.serverApi,
      // notification / inbox
      userInboxApi: api.userInboxApi,
      // enum
      enumApi: api.enumApi,
    },

    // ---- 路由 ----
    getRouter: async () => (await import('@/router')).router,
    getStaticRoutes: () => routes,

    // ---- 视图模块（供 dynamic.ts 解析后端菜单组件路径） ----
    viewModules: import.meta.glob('@/views/**/*.vue'),
    explicitComponentMap: {
      // workbench（inbox 在 src/views 中）
      'workbench/inbox/index': () => import('@/views/workbench/inbox/index.vue'),
      'workbench/inbox': () => import('@/views/workbench/inbox/index.vue'),
      // 日志（后端 Component 不带 /index 后缀）
      'system/log/access': () => import('@/views/system/log/access/index.vue'),
      'system/log/operation': () => import('@/views/system/log/operation/index.vue'),
      'system/log/exception': () => import('@/views/system/log/exception/index.vue'),
      'system/log/audit': () => import('@/views/system/log/audit/index.vue'),
      'system/log/login': () => import('@/views/system/log/login/index.vue'),
      'system/log/task': () => import('@/views/system/log/task/index.vue'),
      'system/log/api': () => import('@/views/system/log/api/index.vue'),
      // PascalCase → kebab-case
      'system/monitor/index': () => import('@/views/system/server/index.vue'),
      'system/cache/index': () => import('@/views/system/cache/index.vue'),
      'system/message/index': () => import('@/views/system/message/index.vue'),
      'system/constraintrule/index': () => import('@/views/system/constraint-rule/index.vue'),
      'system/constraint-rule/index': () => import('@/views/system/constraint-rule/index.vue'),
      'system/oauthapp/index': () => import('@/views/system/oauth-app/index.vue'),
      'system/o-auth-app/index': () => import('@/views/system/oauth-app/index.vue'),
      'system/usersession/index': () => import('@/views/system/user-session/index.vue'),
      'system/user-session/index': () => import('@/views/system/user-session/index.vue'),
      'system/notification/index': () => import('@/views/system/notification/index.vue'),
    },
  })
}
