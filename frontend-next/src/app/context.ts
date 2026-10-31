import type { Router } from 'vue-router'
import type { components, LoginEvent, ResponseOf, TwoFactorMethod, UserGender } from '~/contracts'
import type { ApiCallSettings, ApiCredentialItem, ApiCredentialSecret, AppContextApis, AppUserInboxDisplayItem, AppUserInboxItem, ExternalLoginItem, LoginAuditResult, LoginLogItem, MyOAuthAppItem, MyOAuthAppSecret, NotificationPreference, OAuthConsentItem, UserActivity, UserActivityPeriod, UserProfile, UserSessionItem } from '~/types'
import type { NotificationContentFormat, NotificationPriority, NotificationStatus, NotificationType } from '~/types/enums'
import { auth as authOps, dicts as dictOps, exportTasks as exportTaskOps, files as fileOps, impersonation as impersonationOps, importHistories as importHistoryOps, inbox as inboxOps, myActivity as myActivityOps, myApiCredentials as myApiCredentialOps, myLoginLogs as myLoginLogOps, myOauthApps as myOAuthAppOps, oauthConsents as oauthConsentOps, profile as profileOps, tenants as tenantOps, userSettings as userSettingOps } from '~/contracts'
import { endpointUrl, uploadForm } from '~/request'
import { registerAppContext, useAuthStore } from '~/stores'
import { getDisplayTimeZone } from '~/utils'

/** 壳层导出菜单的范围编号（4.x：0 当前页、1 搜索结果、2 全部）对应的契约名称 */
const EXPORT_SCOPES: Record<number, 'All' | 'CurrentPage' | 'SearchResult'> = { 0: 'CurrentPage', 1: 'SearchResult', 2: 'All' }

/** 顶栏通知中心一次取的条数（4.x 同至多 100 条） */
const INBOX_PANEL_SIZE = 100

type InboxItem = ResponseOf<'inbox.list'>['items'][number]

/** 6.0 的收件箱条目 → 壳层的通知条目：时间字段换成 4.x 的名字，枚举名称与壳层的字符串枚举同值 */
function toShellInboxItem(item: InboxItem): AppUserInboxItem {
  return {
    basicId: item.id,
    title: item.title,
    content: item.content ?? null,
    contentFormat: item.contentFormat as NotificationContentFormat,
    notificationType: item.notificationType as NotificationType,
    notificationStatus: item.status as NotificationStatus,
    sendTime: item.publishedAt,
    readTime: item.readAt ?? null,
    confirmTime: item.confirmedAt ?? null,
    isGlobal: item.isGlobal,
    needConfirm: item.needConfirm,
    icon: item.icon ?? null,
    link: item.link ?? null,
  }
}

function toShellDisplayItem(item: InboxItem): AppUserInboxDisplayItem {
  return {
    ...toShellInboxItem(item),
    notificationId: item.notificationId,
    priority: item.priority as NotificationPriority,
    contentFormat: item.contentFormat as NotificationContentFormat,
    isMandatory: item.isMandatory,
    isBanner: item.isBanner,
    isPopup: item.isPopup,
  }
}

/** 后端 `PageInfo.MaxSize`：模仿弹窗的租户范围一次取全 */
const PAGE_MAX_SIZE = 500

/**
 * 壳层用到、但 6.0 后端还没有的接口：显式拒绝而不是回空。
 * 对应的壳层件（如通知、锁屏）缺省关着（见 stores/app/preferences.ts 的缺省值），
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

/** 性别：壳层（4.x 个人中心）按数字传，0 未设置、1 男、2 女，契约是字面量 */
const GENDER_CODES: Record<UserGender, number> = { Unknown: 0, Male: 1, Female: 2 }

function genderOf(code: number): UserGender {
  const found = (Object.keys(GENDER_CODES) as UserGender[]).find(gender => GENDER_CODES[gender] === code)
  if (!found)
    throw new Error(`未知的性别 ${code}`)
  return found
}

/** 本人资料（`profile.get` / `update` / `change-username` / `remove-avatar` 与头像上传端点回的都是这一份） */
type ProfileResponse = components['schemas']['ProfileResponse']

/**
 * 本人资料 → 4.x 个人中心的 UserProfile：显示名沿用 nickName，头像是用户的头像文件主键，性别换成壳层的数字，没有的字段不给；
 * 上次改用户名的时间沿用 *Time 命名，现在能不能改看后端给的下次可改时间（没改过即为空、现在就能改）。
 * 6.0 没有系统账号（isSystemAccount 恒为否），邮箱 / 手机还没有验证（发码随邮件短信能力接入），按「未验证」给、页签不显示；
 * 锁定取账号状态与登录失败锁定期。
 */
function toShellProfile(profile: ProfileResponse): UserProfile {
  const lockedUntil = profile.lockoutEndAt && Date.parse(profile.lockoutEndAt) > Date.now() ? profile.lockoutEndAt : null
  return {
    userId: profile.user.id,
    userName: profile.user.userName,
    ...(profile.realName ? { realName: profile.realName } : {}),
    nickName: profile.user.displayName,
    ...(profile.user.avatarFileId ? { avatar: profile.user.avatarFileId } : {}),
    ...(profile.user.email ? { email: profile.user.email } : {}),
    ...(profile.user.phoneNumber ? { phone: profile.user.phoneNumber } : {}),
    gender: GENDER_CODES[profile.gender],
    ...(profile.birthday ? { birthday: profile.birthday } : {}),
    ...(profile.country ? { country: profile.country } : {}),
    ...(profile.remark ? { remark: profile.remark } : {}),
    tenantId: profile.user.homeTenantId === 'platform' ? null : profile.user.homeTenantId,
    ...(profile.user.lastLoginAt ? { lastLoginTime: profile.user.lastLoginAt } : {}),
    isSystemAccount: false,
    twoFactorEnabled: profile.twoFactorEnabled,
    twoFactorMethod: profile.twoFactorMethods.reduce((bits, method) => bits | TWO_FACTOR_FLAGS[method], 0),
    emailVerified: profile.emailVerified,
    phoneVerified: profile.phoneVerified,
    ...(profile.passwordChangedAt ? { lastPasswordChangeTime: profile.passwordChangedAt } : {}),
    ...(profile.userNameChangedAt ? { lastUserNameChangeTime: profile.userNameChangedAt } : {}),
    canChangeUserName: !profile.userNameChangeableAt || Date.parse(profile.userNameChangeableAt) <= Date.now(),
    isLocked: profile.user.status === 'Locked' || lockedUntil !== null,
    ...(lockedUntil ? { lockoutEndTime: lockedUntil } : {}),
    failedLoginAttempts: profile.failedAccessCount,
  }
}

/** 本人改了资料（显示名、用户名、头像）：会话里的用户换成回来的那份（顶栏、用户菜单与个人中心侧栏的名字和头像随之刷新），再投影成壳层的资料 */
function applyProfile(profile: ProfileResponse): UserProfile {
  useAuthStore().applyUser(profile.user)
  return toShellProfile(profile)
}

/** `profile.external-logins` / `link-external` / `unlink-external` → 4.x 个人中心「账号绑定」的条目：第三方账号的显示名沿用 providerDisplayName，最近登录换成 *Time 命名，没有的字段不给 */
function toShellExternalLogin(login: ResponseOf<'profile.link-external'>): ExternalLoginItem {
  return {
    provider: login.provider,
    ...(login.displayName ? { providerDisplayName: login.displayName } : {}),
    ...(login.email ? { email: login.email } : {}),
    ...(login.avatarUrl ? { avatarUrl: login.avatarUrl } : {}),
    ...(login.lastLoginAt ? { lastLoginTime: login.lastLoginAt } : {}),
  }
}

/** 4.x 个人中心「登录设备」图标表的设备类型编号：2 手机、3 桌面、4 平板；机器人与没识别出来的是 1（地球） */
const SESSION_DEVICE_TYPES: Record<string, number> = { Mobile: 2, Desktop: 3, Tablet: 4 }

/** `profile.sessions` / `revoke-session` / `revoke-other-sessions` → 4.x 个人中心「登录设备」的条目：设备类别换成图标表的编号，系统与时间换成 4.x 的字段名，没有的字段不给（6.0 不给设备名） */
function toShellSession(session: ResponseOf<'profile.sessions'>['items'][number]): UserSessionItem {
  return {
    sessionId: session.id,
    deviceType: SESSION_DEVICE_TYPES[session.device ?? ''] ?? 1,
    ...(session.browser ? { browser: session.browser } : {}),
    ...(session.os ? { operatingSystem: session.os } : {}),
    ...(session.ipAddress ? { ipAddress: session.ipAddress } : {}),
    ...(session.location ? { location: session.location } : {}),
    loginTime: session.loginAt,
    lastActivityTime: session.lastActiveAt,
    isCurrent: session.isCurrent,
  }
}

/** 登录以外的认证事件 → 4.x 的登录结果（成功时）；这些事件失败一律是 Failed */
const LOGIN_EVENT_RESULTS: Record<Exclude<LoginEvent, 'Login'>, LoginAuditResult> = {
  Refresh: 'TokenRefreshed',
  Logout: 'Logout',
  SwitchTenant: 'TenantSwitched',
  ImpersonationStart: 'ImpersonationStarted',
  ImpersonationStop: 'ImpersonationEnded',
  ChangePassword: 'PasswordChanged',
  TwoFactorChallenge: 'RequiresTwoFactor',
  TwoFactorEnable: 'MfaBound',
  TwoFactorDisable: 'MfaUnbound',
  TwoFactorReset: 'MfaUnbound',
  RoleActivation: 'RolesActivated',
}

/** 登录失败按错误码细分成 4.x 的结果：账号或密码不对、双因素验证码不对，其余是 Failed */
const LOGIN_FAILURE_RESULTS: Record<string, LoginAuditResult> = {
  IDENTITY_DENIED_003: 'InvalidCredentials',
  IDENTITY_DENIED_009: 'TwoFactorFailed',
}

type MyLoginLog = ResponseOf<'my-login-logs.list'>['items'][number]

function loginResultOf(log: MyLoginLog): LoginAuditResult {
  if (log.event === 'Login')
    return log.succeeded ? 'Success' : LOGIN_FAILURE_RESULTS[log.errorCode ?? ''] ?? 'Failed'
  return log.succeeded ? LOGIN_EVENT_RESULTS[log.event] : 'Failed'
}

/** `my-login-logs.list` → 4.x 个人中心「登录日志」的条目：结果由事件、成败与错误码推出，时间与 IP 换成 4.x 的字段名，失败原因进 message，没有的字段不给（6.0 不记登录地点） */
function toShellLoginLog(log: MyLoginLog): LoginLogItem {
  return {
    loginTime: log.occurredAt,
    ...(log.clientIp ? { loginIp: log.clientIp } : {}),
    ...(log.browser ? { browser: log.browser } : {}),
    ...(log.os ? { os: log.os } : {}),
    loginResult: loginResultOf(log),
    ...(log.errorMessage ? { message: log.errorMessage } : {}),
  }
}

/** `profile.notification-preference` / `update-notification-preference` → 4.x 个人中心「通知」的偏好：六个开关与 4.x 同名，逐项给（4.x 的推送、机器人、审批、营销 6.0 没有） */
function toShellNotificationPreference(preference: ResponseOf<'profile.notification-preference'>): NotificationPreference {
  return {
    channelInApp: preference.channelInApp,
    channelEmail: preference.channelEmail,
    channelSms: preference.channelSms,
    typeAnnouncement: preference.typeAnnouncement,
    typeTask: preference.typeTask,
    typeSecurity: preference.typeSecurity,
  }
}

type MyActivity = ResponseOf<'my-activity.summary'>

function toShellActivityPeriod(counts: MyActivity['today']): UserActivityPeriod {
  return { loginCount: counts.logins, accessCount: counts.accesses, operationCount: counts.operations, onlineTime: counts.onlineSeconds }
}

/** `my-activity.summary` → 4.x 个人中心「统计」的活跃度：次数、在线时长与时间换成 4.x 的 *Count / onlineTime / *Time 字段名，没有的最近时间不给 */
function toShellActivity(activity: MyActivity): UserActivity {
  return {
    today: toShellActivityPeriod(activity.today),
    thisWeek: toShellActivityPeriod(activity.thisWeek),
    thisMonth: toShellActivityPeriod(activity.thisMonth),
    ...(activity.lastLoginAt ? { lastLoginTime: activity.lastLoginAt } : {}),
    ...(activity.lastAccessAt ? { lastAccessTime: activity.lastAccessAt } : {}),
    ...(activity.lastOperationAt ? { lastOperationTime: activity.lastOperationAt } : {}),
    trend: activity.trend.map(day => ({ date: day.date, accessCount: day.accesses, operationCount: day.operations, onlineMinutes: day.onlineMinutes })),
  }
}

/** `my-api-credentials.*` → 4.x 个人中心「开放接口」的凭证条目（状态是启停字面量，主键沿用 basicId） */
function toShellCredential(credential: ResponseOf<'my-api-credentials.update'>): ApiCredentialItem {
  return {
    basicId: credential.id,
    credentialName: credential.name,
    appKey: credential.appKey,
    status: credential.isEnabled ? 'Enabled' : 'Disabled',
    lastUsedTime: credential.lastUsedAt ?? null,
    createdTime: credential.createdAt,
  }
}

function toShellSecret(secret: ResponseOf<'my-api-credentials.create'>): ApiCredentialSecret {
  return { basicId: secret.id, appKey: secret.appKey, appSecret: secret.secret }
}

function toShellCallSettings(settings: ResponseOf<'my-api-credentials.settings'>): ApiCallSettings {
  return { signAlgorithm: settings.signatureAlgorithm, ipWhitelist: [...settings.ipAllowList], algorithms: [...settings.algorithms] }
}

/** `my-oauth-apps.*` → 4.x 个人中心「OAuth 应用」的条目：主键沿用 basicId，启停换成状态字面量，授权类型与范围用逗号、回调地址按行拼成 4.x 的字符串 */
function toShellOAuthApp(app: ResponseOf<'my-oauth-apps.get'>): MyOAuthAppItem {
  return {
    basicId: app.id,
    clientId: app.clientId,
    appName: app.appName,
    appDescription: app.appDescription ?? null,
    homepage: app.homepage ?? null,
    logo: app.logo ?? null,
    redirectUris: app.redirectUris.join('\n'),
    clientType: app.clientType,
    grantTypes: app.grantTypes.join(','),
    scopes: app.scopes.join(','),
    status: app.isEnabled ? 'Enabled' : 'Disabled',
    createdTime: app.createdAt,
  }
}

/** 一次性客户端密钥：公开客户端没有密钥（后端给 null），壳层按空串判断不显示 */
function toShellOAuthSecret(secret: ResponseOf<'my-oauth-apps.create'>): MyOAuthAppSecret {
  return { basicId: secret.id, clientId: secret.clientId, clientType: secret.clientType, clientSecret: secret.clientSecret ?? '' }
}

/** `oauth-consents.list` → 个人中心「已授权的应用」条目：授权时间换成壳层的 *Time 命名，范围保持列表（按标签显示） */
function toShellOAuthConsent(consent: ResponseOf<'oauth-consents.list'>['items'][number]): OAuthConsentItem {
  return {
    clientId: consent.clientId,
    appName: consent.appName ?? null,
    logo: consent.logo ?? null,
    homepage: consent.homepage ?? null,
    scopes: [...consent.scopes],
    grantedTime: consent.grantedAt,
  }
}

/** 4.x 的回调地址是一段文本（换行或逗号分隔）：拆成 6.0 的地址列表，去掉空项 */
function splitRedirectUris(value: string): string[] {
  return value.split(/[\n,]/).map(uri => uri.trim()).filter(Boolean)
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
 * 登录配置来自 `auth.login-config`（账号密码与后端配置启用的第三方登录提供方，是否要图形验证码由后端配置）；验证码登录随后续批次接入。
 */
export function registerApplicationContext(router: Router): void {
  const apis: AppContextApis = {
    getLoginConfigApi: async () => {
      const config = await authOps.loginConfig({}, { anonymous: true })
      return { loginMethods: [...config.loginMethods], oAuthProviders: config.oAuthProviders.map(provider => ({ name: provider.name, displayName: provider.displayName, type: provider.type })), captchaEnabled: config.captchaEnabled }
    },
    getCaptchaApi: () => authOps.captcha({}, { anonymous: true }),
    // 验证码登录发码（auth.send-login-code，匿名）：租户留空即平台；账号不存在、不唯一也回答已发送（不发），不返回调试码
    sendEmailLoginCodeApi: (email, tenant) => authOps.sendLoginCode({ channel: 'Email', target: email, tenant: tenant?.trim() || null }, { anonymous: true }),
    sendPhoneLoginCodeApi: (phone, tenant) => authOps.sendLoginCode({ channel: 'Phone', target: phone, tenant: tenant?.trim() || null }, { anonymous: true }),
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
    // 个人资料（Identity 的 profile.update / change-username / remove-avatar 与宿主的头像上传端点，只要求已登录）：都回整份资料，会话里的用户随之换掉。
    // 资料整体覆盖、空白当作没填，不收头像；改用户名凭当前密码、90 天一次（模仿会话被拒）
    updateProfileApi: async input => applyProfile(await profileOps.update({
      displayName: input.nickName,
      realName: input.realName ?? null,
      gender: genderOf(input.gender),
      birthday: input.birthday ?? null,
      country: input.country ?? null,
      remark: input.remark ?? null,
    })),
    // 头像：普通端点 POST /api/profile/avatar（multipart 字段 file，带上传进度），后端按文件头只认 JPEG / PNG、不超过 2 MB，上传即换成本人的头像并回收旧的
    uploadAvatarApi: async (file, onProgress) => {
      const form = new FormData()
      form.append('file', file, file.name)
      const profile = await uploadForm<ProfileResponse>('/api/profile/avatar', form, { onProgress })
      applyProfile(profile)
      if (!profile.user.avatarFileId)
        throw new Error('头像上传后的资料里没有头像')
      return { fileId: profile.user.avatarFileId }
    },
    removeAvatarApi: async () => applyProfile(await profileOps.removeAvatar({})),
    changeUserNameApi: async input => applyProfile(await profileOps.changeUsername({ userName: input.userName, password: input.password })),
    // 邮箱 / 手机的验证与换绑（profile.send-verification-code / verify-contact / send-change-contact-code / change-contact）：码经 Notification 的发件箱发出，
    // 租户没有自己启用的缺省邮件 / 短信配置时由平台代发；换绑凭当前密码、码发到新的，模仿会话被拒
    sendEmailVerifyCodeApi: () => profileOps.sendVerificationCode({ channel: 'Email' }),
    sendPhoneVerifyCodeApi: () => profileOps.sendVerificationCode({ channel: 'Phone' }),
    verifyEmailApi: async code => applyProfile(await profileOps.verifyContact({ channel: 'Email', code })),
    verifyPhoneApi: async code => applyProfile(await profileOps.verifyContact({ channel: 'Phone', code })),
    sendChangeEmailCodeApi: input => profileOps.sendChangeContactCode({ channel: 'Email', newValue: input.newEmail, password: input.password }),
    sendChangePhoneCodeApi: input => profileOps.sendChangeContactCode({ channel: 'Phone', newValue: input.newPhone, password: input.password }),
    confirmChangeEmailApi: async code => applyProfile(await profileOps.changeContact({ channel: 'Email', code })),
    confirmChangePhoneApi: async code => applyProfile(await profileOps.changeContact({ channel: 'Phone', code })),
    setup2FAApi: async () => {
      const setup = await profileOps.setupTotp({})
      return { sharedKey: setup.sharedKey, authenticatorUri: setup.authenticatorUri }
    },
    enable2FAApi: (method, code) => profileOps.enableTwoFactor({ method: twoFactorMethodOf(method), code }),
    disable2FAApi: (method, code) => profileOps.disableTwoFactor({ method: twoFactorMethodOf(method), code }),
    // 邮箱 / 短信双因素的开关凭发到本人已验证联系方式的码（profile.send-two-factor-code），认证器不发码
    send2FASetupCodeApi: method => profileOps.sendTwoFactorCode({ method: twoFactorMethodOf(method) }),
    deactivateAccountApi: () => notAvailable('deactivateAccountApi'),
    deleteAccountApi: () => notAvailable('deleteAccountApi'),
    // 第三方账号绑定（Identity 的 profile.external-logins / begin-external-bind / link-external / unlink-external，只要求已登录，模仿会话发起、确认与解绑被拒）：
    // 发起先领本人的绑定意图（4.x 先换绑定票据），整页跳去它给的地址（auth store 的 startOAuthBind），回调页凭票据确认——票据只认发起的本人
    getLinkedAccountsApi: async () => (await profileOps.externalLogins({})).items.map(toShellExternalLogin),
    beginOAuthBindApi: async provider => (await profileOps.beginExternalBind({ provider })).url,
    linkAccountApi: async ticket => toShellExternalLogin(await profileOps.linkExternal({ ticket })),
    unlinkAccountApi: async provider => (await profileOps.unlinkExternal({ provider })).items.map(toShellExternalLogin),
    // 登录设备（Identity 的 profile.sessions / revoke-session / revoke-other-sessions，只要求已登录）：本人全部在线的会话、当前的在最前，IP 不遮掩；
    // 下线当前会话是 IDENTITY_CONFLICT_005、不是本人的会话是 IDENTITY_NOTFOUND_002；下线后页签重新拉列表
    getSessionsApi: async () => (await profileOps.sessions({})).items.map(toShellSession),
    revokeSessionApi: sessionId => profileOps.revokeSession({ sessionId }),
    revokeOtherSessionsApi: () => profileOps.revokeOtherSessions({}),
    // 登录日志（Auditing 的 my-login-logs.list，只要求已登录、只看本人的认证事件，pageSize 1–50）：页码与条数照 4.x 回请求的那一页
    getLoginLogsApi: async (page, pageSize) => {
      const result = await myLoginLogOps.list({ page, pageSize })
      return { items: result.items.map(toShellLoginLog), total: result.total, page, pageSize }
    },
    // 通知偏好（Identity 的 profile.notification-preference / update-notification-preference，只要求已登录）：整体覆盖，没保存过即全收；强制阅读与紧急通知不受偏好影响
    getNotificationPreferenceApi: async () => toShellNotificationPreference(await profileOps.notificationPreference({})),
    updateNotificationPreferenceApi: async input => toShellNotificationPreference(await profileOps.updateNotificationPreference({
      channelInApp: input.channelInApp,
      channelEmail: input.channelEmail,
      channelSms: input.channelSms,
      typeAnnouncement: input.typeAnnouncement,
      typeTask: input.typeTask,
      typeSecurity: input.typeSecurity,
    })),
    // 活跃度（Auditing 的 my-activity.summary，只要求已登录、只按本人）：按应用时区分天（与界面上的时间一致；4.x 按 UTC），本周从周一算
    getActivityApi: async () => toShellActivity(await myActivityOps.summary({ timeZone: getDisplayTimeZone() })),
    // 锁屏（服务端强制）：auth.lock 记本会话一次性的锁屏密码，锁定中除解锁与注销外的请求都是 IDENTITY_DENIED_002（请求层 onLocked 拉起锁屏）
    lockSessionApi: input => authOps.lock({ password: input.password }),
    unlockSessionApi: input => authOps.unlock({ password: input.password }),
    // 用户设置（Identity 的 user-settings，按 用户 × 场景 × 键 整体覆盖）：偏好与收藏的跨端同步；clientId / origin 随实时回推（UserSettingChanged）原样转给其他端
    userSettingApi: {
      get: async input => toShellSetting(await userSettingOps.get({ scene: sceneOf(input.scene), key: input.settingKey })),
      save: async input => toShellSetting(await userSettingOps.update({ scene: sceneOf(input.scene), key: input.settingKey, value: input.settingValue ?? null, clientId: input.clientId ?? null, origin: input.origin ?? null })),
    },
    // 收件箱（Notification 的 inbox.*，只看本人在当前一侧的）：壳层传的用户与租户参数是 4.x 的遗留，后端按令牌认人与侧
    userInboxApi: {
      banner: async () => (await inboxOps.banners({})).items.map(toShellDisplayItem),
      confirm: id => inboxOps.confirm({ id }),
      list: async (_userId, unreadOnly) => (await inboxOps.list({
        page: { index: 1, size: INBOX_PANEL_SIZE },
        filters: unreadOnly ? [{ field: 'pending', operator: 'eq', value: 'true' }] : [],
        sorts: [],
      })).items.map(toShellInboxItem),
      mandatoryUnread: async () => (await inboxOps.mandatory({})).items.map(toShellDisplayItem),
      markAllRead: () => inboxOps.markAllRead({}),
      markPopupShown: id => inboxOps.markPopupShown({ id }),
      markRead: id => inboxOps.markRead({ ids: [id] }),
      popup: async () => (await inboxOps.popups({})).items.map(toShellDisplayItem),
    },
    // 模仿登录（方案 5.8）：平台主体先选范围（平台账号或某个租户的成员），租户主体钉在本租户；候选、发起与结束都是 Identity 的操作
    impersonationApi: {
      // 可选范围：全部正常状态的租户（只有平台主体会调到；tenants.list 是平台专属操作）
      tenants: async () => (await tenantOps.list({ page: { index: 1, size: PAGE_MAX_SIZE }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }] })).items.map(tenant => ({ tenantId: tenant.tenantId, tenantName: tenant.name })),
      candidates: async input => (await impersonationOps.candidates({ tenantId: input.tenantId ?? null, keyword: input.keyword ?? null })).items.map(candidate => ({ basicId: candidate.id, userName: candidate.userName, nickName: candidate.displayName })),
      start: input => impersonationOps.start({ targetUserId: input.targetUserId, tenantId: input.tenantId ?? null, reason: input.reason ?? null }),
      stop: () => impersonationOps.stop({}),
    },
    // 控制中心与个人中心「我的租户」：我能进入的租户（注册地 + 成员）与能否进入平台；切换走 auth.switch-tenant，在目标上下文续接当前会话
    tenantApi: {
      myAvailableTenants: async () => (await tenantOps.mine({})).tenants.map(tenant => ({ tenantId: tenant.tenantId, tenantName: tenant.name, isHome: tenant.isHome, memberType: tenant.memberType ?? null, isOwner: tenant.isOwner, isCurrent: tenant.isCurrent, joinedTime: tenant.joinedAt })),
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
    getApiCredentialsApi: async () => (await myApiCredentialOps.list({})).items.map(toShellCredential),
    createApiCredentialApi: async credentialName => toShellSecret(await myApiCredentialOps.create({ name: credentialName ?? null })),
    rotateApiCredentialSecretApi: async id => toShellSecret(await myApiCredentialOps.rotateSecret({ id: String(id) })),
    updateApiCredentialStatusApi: async (id, status) => toShellCredential(await myApiCredentialOps.update({ id: String(id), isEnabled: status === 'Enabled' })),
    // 删除按批量结果判定：被拒的（不存在或不是本人的）抛错带错误码，不当成删除成功
    deleteApiCredentialApi: async (id) => {
      const result = await myApiCredentialOps.delete({ ids: [String(id)] })
      if (result.failed.length > 0)
        throw new Error(result.failed.map(failure => failure.code).join('; '))
      return result
    },
    getApiCallSettingsApi: async () => toShellCallSettings(await myApiCredentialOps.settings({})),
    saveApiCallSettingsApi: async input => toShellCallSettings(await myApiCredentialOps.updateSettings({ signatureAlgorithm: input.signAlgorithm, ipAllowList: input.ipWhitelist })),
    // 我的 OAuth 应用（OpenPlatform 的 my-oauth-apps.*，只要求已登录）：修改是整体替换（含启停），壳层的修改不带状态、启停只带状态，都先取最新一份再改
    getMyOAuthAppsApi: async () => (await myOAuthAppOps.list({})).items.map(toShellOAuthApp),
    createMyOAuthAppApi: async input => toShellOAuthSecret(await myOAuthAppOps.create({
      appName: input.appName,
      clientType: input.clientType,
      homepage: input.homepage ?? null,
      appDescription: input.appDescription ?? null,
      redirectUris: splitRedirectUris(input.redirectUris),
      logo: input.logo ?? null,
    })),
    updateMyOAuthAppApi: async (input) => {
      const current = await myOAuthAppOps.get({ id: String(input.basicId) })
      return toShellOAuthApp(await myOAuthAppOps.update({
        id: current.id,
        appName: input.appName,
        homepage: input.homepage ?? null,
        appDescription: input.appDescription ?? null,
        redirectUris: splitRedirectUris(input.redirectUris),
        logo: input.logo ?? null,
        isEnabled: current.isEnabled,
      }))
    },
    regenerateMyOAuthAppSecretApi: async id => toShellOAuthSecret(await myOAuthAppOps.resetSecret({ id: String(id) })),
    updateMyOAuthAppStatusApi: async (id, status) => {
      const current = await myOAuthAppOps.get({ id: String(id) })
      return toShellOAuthApp(await myOAuthAppOps.update({
        id: current.id,
        appName: current.appName,
        homepage: current.homepage ?? null,
        appDescription: current.appDescription ?? null,
        redirectUris: current.redirectUris,
        logo: current.logo ?? null,
        isEnabled: status === 'Enabled',
      }))
    },
    // 删除按批量结果判定：被拒的（不存在或不是本人的）抛错带错误码，不当成删除成功
    deleteMyOAuthAppApi: async (id) => {
      const result = await myOAuthAppOps.delete({ ids: [String(id)] })
      if (result.failed.length > 0)
        throw new Error(result.failed.map(failure => failure.code).join('; '))
      return result
    },
    // 已授权的应用（OpenPlatform 的 oauth-consents.*，只要求已登录，模仿会话撤销被拒）：撤销按 ClientId，同时吊销该应用拿到的令牌
    getOAuthConsentsApi: async () => (await oauthConsentOps.list({})).items.map(toShellOAuthConsent),
    revokeOAuthConsentApi: clientId => oauthConsentOps.revoke({ clientId }),
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
    shellRoutes: { profile: '/profile', controlCenter: '/control-center', inbox: '/notification/inbox' },
  })
}
