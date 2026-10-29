import type { Router } from 'vue-router'
import type { ResponseOf, TwoFactorMethod } from '~/contracts'
import type { AppContextApis, UserProfile } from '~/types'
import { auth as authOps, dicts as dictOps, exportTasks as exportTaskOps, files as fileOps, impersonation as impersonationOps, importHistories as importHistoryOps, profile as profileOps, tenants as tenantOps, userSettings as userSettingOps } from '~/contracts'
import { endpointUrl } from '~/request'
import { registerAppContext, useAuthStore } from '~/stores'

/** 壳层导出菜单的范围编号（4.x：0 当前页、1 搜索结果、2 全部）对应的契约名称 */
const EXPORT_SCOPES: Record<number, 'All' | 'CurrentPage' | 'SearchResult'> = { 0: 'CurrentPage', 1: 'SearchResult', 2: 'All' }

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

/** 双因素方式：壳层（4.x 个人中心）按位标志 1 = 认证器、2 = 邮箱、4 = 手机传，契约是字面量 */
const TWO_FACTOR_FLAGS: Record<TwoFactorMethod, number> = { Totp: 1, Email: 2, Phone: 4 }

function twoFactorMethodOf(flag: number | string): TwoFactorMethod {
  const found = (Object.keys(TWO_FACTOR_FLAGS) as TwoFactorMethod[]).find(method => TWO_FACTOR_FLAGS[method] === Number(flag))
  if (!found)
    throw new Error(`未知的双因素方式 ${flag}`)
  return found
}

/**
 * `profile.get` → 4.x 个人中心的 UserProfile：6.0 还没有的资料字段（性别、生日、头像、联系方式验证、改用户名）按「没有」给，
 * 这些随个人中心批次补全；锁定取账号状态与登录失败锁定期。
 */
function toShellProfile(profile: ResponseOf<'profile.get'>): UserProfile {
  const lockedUntil = profile.lockoutEndAt && Date.parse(profile.lockoutEndAt) > Date.now() ? profile.lockoutEndAt : null
  return {
    userId: profile.user.id,
    userName: profile.user.userName,
    nickName: profile.user.displayName,
    ...(profile.user.email ? { email: profile.user.email } : {}),
    ...(profile.user.phoneNumber ? { phone: profile.user.phoneNumber } : {}),
    gender: 0,
    tenantId: profile.user.homeTenantId === 'platform' ? null : profile.user.homeTenantId,
    ...(profile.user.lastLoginAt ? { lastLoginTime: profile.user.lastLoginAt } : {}),
    isSystemAccount: false,
    twoFactorEnabled: profile.twoFactorEnabled,
    twoFactorMethod: profile.twoFactorMethods.reduce((bits, method) => bits | TWO_FACTOR_FLAGS[method], 0),
    emailVerified: false,
    phoneVerified: false,
    ...(profile.passwordChangedAt ? { lastPasswordChangeTime: profile.passwordChangedAt } : {}),
    canChangeUserName: false,
    isLocked: profile.user.status === 'Locked' || lockedUntil !== null,
    ...(lockedUntil ? { lockoutEndTime: lockedUntil } : {}),
    failedLoginAttempts: profile.failedAccessCount,
  }
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
 * 登录配置来自 `auth.login-config`（目前只有账号密码一种，是否要图形验证码由后端配置）；验证码登录、第三方登录随后续批次接入。
 */
export function registerApplicationContext(router: Router): void {
  const apis: AppContextApis = {
    getLoginConfigApi: async () => {
      const config = await authOps.loginConfig({}, { anonymous: true })
      return { loginMethods: [...config.loginMethods], oAuthProviders: config.oAuthProviders.map(provider => ({ name: provider.name, displayName: provider.displayName })), captchaEnabled: config.captchaEnabled }
    },
    getCaptchaApi: () => authOps.captcha({}, { anonymous: true }),
    logoutApi: () => useAuthStore().logout(),
    getUserInfoApi: () => notAvailable('getUserInfoApi'),
    getPermissionsApi: () => notAvailable('getPermissionsApi'),
    // 本人改密（profile.change-password）：本会话保留、其他会话注销，强制改密标记随之清除
    changePasswordApi: async (input) => {
      const changed = await profileOps.changePassword({ currentPassword: input.oldPassword, newPassword: input.newPassword })
      useAuthStore().markPasswordChanged()
      return changed
    },
    // 个人中心：资料与账号安全概况、认证器双因素；邮箱 / 短信双因素要等 Notification 发码，停用与注销账号随个人中心批次
    getProfileApi: async () => toShellProfile(await profileOps.get({})),
    setup2FAApi: async () => {
      const setup = await profileOps.setupTotp({})
      return { sharedKey: setup.sharedKey, authenticatorUri: setup.authenticatorUri }
    },
    enable2FAApi: (method, code) => profileOps.enableTwoFactor({ method: twoFactorMethodOf(method), code }),
    disable2FAApi: (method, code) => profileOps.disableTwoFactor({ method: twoFactorMethodOf(method), code }),
    send2FASetupCodeApi: () => notAvailable('send2FASetupCodeApi'),
    deactivateAccountApi: () => notAvailable('deactivateAccountApi'),
    deleteAccountApi: () => notAvailable('deleteAccountApi'),
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
    // 切换角色：auth.session-roles 列出持有与激活，auth.activate-roles 换激活集并续接会话（Access 的动态职责分离在服务端判定）
    sessionRoleApi: {
      list: async () => (await authOps.sessionRoles({})).items.map(role => ({ roleId: role.roleId, code: role.code, name: role.name, isActive: role.isActive })),
      activate: roleIds => authOps.activateRoles({ roleIds }),
    },
    // 文件引用（头像、表格里的文件列）：files.presign 签发的签名地址补上接口前缀，图片标签不带令牌也能取
    getFilePresignedUrlApi: async fileId => endpointUrl((await fileOps.presign({ id: fileId })).url),
    timeZoneApi: { options: timeZoneOptions },
    // 导入历史：import-histories.create / recent（只看自己的）；壳层沿用 4.x 的 basicId / createdTime 字段名
    importHistoryApi: {
      create: input => importHistoryOps.create({ ...input, resourceCode: input.resourceCode ?? null, errorSummary: input.errorSummary ?? null }),
      recent: async (pageCode, count) => (await importHistoryOps.recent({ pageCode, count: count ?? 5 })).items.map(item => ({ basicId: item.id, pageCode: item.pageCode, resourceCode: item.resourceCode, fileName: item.fileName, totalCount: item.totalCount, successCount: item.successCount, failCount: item.failCount, errorSummary: item.errorSummary, createdTime: item.createdAt })),
    },
    // 导出中心：export-tasks.submit，后台以发起人的身份逐页调页面声明的列表操作；壳层的范围与格式是 4.x 的数字，换成契约的名称
    exportTaskApi: {
      submit: input => exportTaskOps.submit({
        businessType: input.businessType,
        taskName: input.taskName ?? null,
        scope: EXPORT_SCOPES[input.scope] ?? 'SearchResult',
        format: input.format === 1 ? 'Xlsx' : 'Csv',
        querySnapshot: input.querySnapshot ?? null,
        columns: input.columns.map(column => ({ key: column.key, title: column.title, valueMap: column.valueMap ?? null })),
      }),
    },
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
    // 个人中心与控制中心的视图都在 packages（4.x 原样搬入），路径由这里登记（顶栏的用户菜单与上下文按钮据此显示并跳转）
    shellRoutes: { profile: '/profile', controlCenter: '/control-center' },
  })
}
