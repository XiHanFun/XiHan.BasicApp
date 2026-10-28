import type { Router } from 'vue-router'
import type { ResponseOf } from '~/contracts'
import type { AppContextApis } from '~/types'
import { auth as authOps, dicts as dictOps, impersonation as impersonationOps, tenants as tenantOps, userSettings as userSettingOps } from '~/contracts'
import { registerAppContext, useAuthStore } from '~/stores'

/** 后端 `PageInfo.MaxSize`：模仿弹窗的租户范围一次取全 */
const PAGE_MAX_SIZE = 500

/**
 * 壳层用到、但 6.0 后端还没有的接口：显式拒绝而不是回空。
 * 对应的壳层件（通知、锁屏、搜索 / 表格 / 看板的服务端同步）缺省关着（见 stores/app/preferences.ts 的缺省值），
 * 用户在偏好中心打开后会看到这条错误，而不是一个看似工作的空件。
 */
function notAvailable(name: string): never {
  throw new Error(`${name} 尚未接入 6.0 后端`)
}

/** 壳层的用户设置场景是 4.x 的枚举数值（UserSettingScene），契约是字面量 */
function sceneOf(scene: number): 'Page' | 'Preference' {
  if (scene === 0)
    return 'Preference'
  if (scene === 1)
    return 'Page'
  throw new Error(`未知的用户设置场景 ${scene}`)
}

function toShellSetting(setting: ResponseOf<'user-settings.get'>) {
  return { scene: setting.scene === 'Page' ? 1 : 0, settingKey: setting.key, settingValue: setting.value ?? null }
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
    // 用户设置（Identity 的 user-settings，按 用户 × 场景 × 键 整体覆盖）：偏好与收藏的跨端同步；clientId / origin 是实时回推用的，随 Realtime 批次接入
    userSettingApi: {
      get: async input => toShellSetting(await userSettingOps.get({ scene: sceneOf(input.scene), key: input.settingKey })),
      save: async input => toShellSetting(await userSettingOps.update({ scene: sceneOf(input.scene), key: input.settingKey, value: input.settingValue ?? null })),
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
    // 模仿登录（方案 5.8）：平台主体先选范围（平台账号或某个租户的成员），租户主体钉在本租户；候选、发起与结束都是 Identity 的操作
    impersonationApi: {
      // 可选范围：全部正常状态的租户（只有平台主体会调到；tenants.list 是平台专属操作）
      tenants: async () => (await tenantOps.list({ page: { index: 1, size: PAGE_MAX_SIZE }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }] })).items.map(tenant => ({ tenantId: tenant.tenantId, tenantName: tenant.name })),
      candidates: async input => (await impersonationOps.candidates({ tenantId: input.tenantId ?? null, keyword: input.keyword ?? null })).items.map(candidate => ({ basicId: candidate.id, userName: candidate.userName, nickName: candidate.displayName })),
      start: input => impersonationOps.start({ targetUserId: input.targetUserId, tenantId: input.tenantId ?? null, reason: input.reason ?? null }),
      stop: () => impersonationOps.stop({}),
    },
    // 控制中心：我能进入的租户（注册地 + 成员）与能否进入平台；切换走 auth.switch-tenant，在目标上下文续接当前会话
    tenantApi: {
      myAvailableTenants: async () => (await tenantOps.mine({})).tenants.map(tenant => ({ tenantId: tenant.tenantId, tenantName: tenant.name, isHome: tenant.isHome, memberType: tenant.memberType ?? null, isCurrent: tenant.isCurrent })),
      switchTenant: input => authOps.switchTenant({ tenantId: input.tenantId ?? null }),
    },
    getFilePresignedUrlApi: () => notAvailable('getFilePresignedUrlApi'),
    timeZoneApi: { options: timeZoneOptions },
    enumApi: {
      getBatch: () => notAvailable('enumApi.getBatch'),
      getByName: () => notAvailable('enumApi.getByName'),
    },
    // 字典选项（Settings 的 dicts.options，只要求已登录）：树形字典已按深度优先展平
    dictApi: { options: async code => (await dictOps.options({ code })).items },
  }

  registerAppContext({
    apis,
    getRouter: () => Promise.resolve(router),
    // 个人中心页随 Identity 的后续批次补；改密页先顶在这个入口上，顶栏用户菜单才有去处。
    // 控制中心视图在 packages，路径由这里登记（顶栏的上下文按钮据此显示并跳转）
    shellRoutes: { profile: '/identity/profile/change-password', controlCenter: '/control-center' },
  })
}
