import type { Router } from 'vue-router'
import type { AppContextApis } from '~/types'
import { registerAppContext, useAuthStore } from '~/stores'

/**
 * 壳层用到、但 6.0 后端还没有的接口：显式拒绝而不是回空。
 * 对应的壳层件（通知、模仿、锁屏、偏好 / 收藏的服务端同步、租户切换）缺省关着（见 stores/app/preferences.ts 的缺省值），
 * 用户在偏好中心打开后会看到这条错误，而不是一个看似工作的空件。
 */
function notAvailable(name: string): never {
  throw new Error(`${name} 尚未接入 6.0 后端`)
}

/** 时区目录：浏览器自带的 IANA 清单（后端时区目录接入后换成 metadata 操作） */
function timeZoneOptions() {
  const now = new Date()
  const ids = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : ['UTC']
  return Promise.resolve(ids.map((id) => {
    const parts = new Intl.DateTimeFormat('en-US', { timeZone: id, timeZoneName: 'longOffset' }).formatToParts(now)
    const offset = parts.find(part => part.type === 'timeZoneName')?.value ?? 'GMT'
    const match = offset.match(/([+-])(\d{2}):(\d{2})/)
    const minutes = match ? (match[1] === '-' ? -1 : 1) * (Number(match[2]) * 60 + Number(match[3])) : 0
    return { id, displayName: `(${offset}) ${id.replaceAll('_', ' ')}`, baseUtcOffsetMinutes: minutes, supportsDaylightSavingTime: false }
  }))
}

/**
 * 应用上下文（壳层 ← 应用）：packages 不直接依赖 src，由这里注入接口实现、路由实例与壳层要跳的应用路由。
 * 登录配置目前只有账号密码一种；其余入口随认证批次接入。
 */
export function registerApplicationContext(router: Router): void {
  const apis: AppContextApis = {
    getLoginConfigApi: () => Promise.resolve({ loginMethods: ['password'], oAuthProviders: [], captchaEnabled: false }),
    logoutApi: () => useAuthStore().logout(),
    getUserInfoApi: () => notAvailable('getUserInfoApi'),
    getPermissionsApi: () => notAvailable('getPermissionsApi'),
    changePasswordApi: () => notAvailable('changePasswordApi'),
    lockSessionApi: () => notAvailable('lockSessionApi'),
    unlockSessionApi: () => notAvailable('unlockSessionApi'),
    userSettingApi: {
      get: () => notAvailable('userSettingApi.get'),
      save: () => notAvailable('userSettingApi.save'),
    },
    userInboxApi: {
      banner: () => notAvailable('userInboxApi.banner'),
      confirm: () => notAvailable('userInboxApi.confirm'),
      list: () => notAvailable('userInboxApi.list'),
      mandatoryUnread: () => notAvailable('userInboxApi.mandatoryUnread'),
      markAllRead: () => notAvailable('userInboxApi.markAllRead'),
      markPopupShown: () => notAvailable('userInboxApi.markPopupShown'),
      markRead: () => notAvailable('userInboxApi.markRead'),
      popup: () => notAvailable('userInboxApi.popup'),
    },
    impersonationApi: {
      tenants: () => notAvailable('impersonationApi.tenants'),
      candidates: () => notAvailable('impersonationApi.candidates'),
      start: () => notAvailable('impersonationApi.start'),
      stop: () => notAvailable('impersonationApi.stop'),
    },
    tenantApi: {
      myAvailableTenants: () => notAvailable('tenantApi.myAvailableTenants'),
      switchTenant: () => notAvailable('tenantApi.switchTenant'),
    },
    getFilePresignedUrlApi: () => notAvailable('getFilePresignedUrlApi'),
    timeZoneApi: { options: timeZoneOptions },
    enumApi: {
      getBatch: () => notAvailable('enumApi.getBatch'),
      getByName: () => notAvailable('enumApi.getByName'),
    },
    dictApi: { options: () => notAvailable('dictApi.options') },
  }

  registerAppContext({
    apis,
    getRouter: () => Promise.resolve(router),
    // 个人中心页随 Identity 的后续批次补；改密页先顶在这个入口上，顶栏用户菜单才有去处
    shellRoutes: { profile: '/identity/profile/change-password' },
  })
}
