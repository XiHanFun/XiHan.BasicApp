import type { Router } from 'vue-router'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureRequest } from '~/request'
import { useAppContext, useAuthStore, useUserStore } from '~/stores'
import { registerApplicationContext } from './context'

function json(status: number, body: object): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

interface Call { method: string, url: string, body: unknown }

function record(calls: Call[], respond: (url: string) => Response) {
  configureRequest({
    baseUrl: '',
    getToken: () => 'a-1',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({ method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : null })
      return respond(String(url))
    }) as unknown as typeof fetch,
  })
}

const DAY = 24 * 60 * 60 * 1000

/** 会话与资料里的用户（后端 UserResponse） */
function userOf(patch: object = {}) {
  return { id: 'u-1', userName: 'alice', displayName: 'Alice', email: 'alice@example.com', phoneNumber: null, homeTenantId: 'platform', status: 'Active', lastLoginAt: null, createdAt: '2026-10-01T00:00:00Z', createdBy: null, avatarFileId: 'f-1', ...patch }
}

/** 本人资料（后端 ProfileResponse）：没改过用户名 */
function profileOf(userPatch: object = {}) {
  return { user: userOf(userPatch), twoFactorEnabled: false, twoFactorMethods: [], passwordChangedAt: null, mustChangePassword: false, failedAccessCount: 0, lockoutEndAt: null, realName: '张三', gender: 'Female', birthday: '1990-05-04T00:00:00Z', country: '中国', remark: null, userNameChangedAt: null, userNameChangeableAt: null }
}

function sessionOf() {
  return { accessToken: 'a-1', accessExpiresAt: '2026-10-30T02:00:00Z', refreshToken: 'r-1', refreshExpiresAt: '2026-11-06T00:00:00Z', sessionId: 's-1', tenantId: null, tenantName: null, impersonatorUserName: null, mustChangePassword: false, user: userOf() }
}

describe('应用上下文：壳层接口接到 6.0 操作', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    registerApplicationContext({} as Router)
  })

  it('登记了控制中心、个人中心与我的消息的路径', () => {
    expect(useAppContext().shellRoutes).toMatchObject({ controlCenter: '/control-center', profile: '/profile', inbox: '/notification/inbox' })
  })

  it('收件箱：只看未读走 pending 筛选，条目换成壳层的字段名；标已读发 ids，横幅取 items', async () => {
    const calls: Call[] = []
    const item = { id: 'i-1', notificationId: 'n-1', title: 'T', content: '**x**', contentFormat: 'Markdown', notificationType: 'Security', priority: 'High', status: 'Unread', publishedAt: '2026-10-29T00:00:00Z', readAt: null, confirmedAt: null, isGlobal: true, needConfirm: true, isMandatory: false, isBanner: true, isPopup: false, icon: null, link: '/x', expiresAt: null }
    record(calls, url => url.includes(':list')
      ? json(200, { items: [item], total: 1, page: { index: 1, size: 100 } })
      : url.includes(':banners') ? json(200, { items: [item] }) : json(200, { succeeded: ['i-1'], failed: [] }))
    const { userInboxApi } = useAppContext().apis
    expect(await userInboxApi.list('u-1', true, null)).toEqual([{ basicId: 'i-1', title: 'T', content: '**x**', contentFormat: 'Markdown', notificationType: 'Security', notificationStatus: 'Unread', sendTime: '2026-10-29T00:00:00Z', readTime: null, confirmTime: null, isGlobal: true, needConfirm: true, icon: null, link: '/x' }])
    expect(calls[0]).toEqual({ method: 'POST', url: '/api/inbox:list', body: { page: { index: 1, size: 100 }, filters: [{ field: 'pending', operator: 'eq', value: 'true' }], sorts: [] } })
    await userInboxApi.markRead('i-1')
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/inbox:mark-read', body: { ids: ['i-1'] } })
    const [banner] = await userInboxApi.banner()
    expect(banner).toMatchObject({ basicId: 'i-1', notificationId: 'n-1', priority: 'High', isBanner: true, isMandatory: false })
    expect(calls[2]).toMatchObject({ method: 'GET', url: '/api/inbox:banners' })
  })

  it('我的租户：tenants.mine 投影成切换器条目（注册地没有成员类型，加入时间换成 joinedTime）', async () => {
    const calls: Call[] = []
    record(calls, () => json(200, {
      canAccessPlatform: true,
      platformIsCurrent: true,
      tenants: [
        { tenantId: 'demo', name: 'Demo Tenant', isHome: false, memberType: 'Owner', isCurrent: false, joinedAt: '2026-10-01T00:00:00Z' },
        { tenantId: 'acme', name: 'Acme', isHome: true, memberType: null, isCurrent: true, joinedAt: '2026-01-01T00:00:00Z' },
      ],
    }))
    expect(await useAppContext().apis.tenantApi.myAvailableTenants()).toEqual([
      { tenantId: 'demo', tenantName: 'Demo Tenant', isHome: false, memberType: 'Owner', isCurrent: false, joinedTime: '2026-10-01T00:00:00Z' },
      { tenantId: 'acme', tenantName: 'Acme', isHome: true, memberType: null, isCurrent: true, joinedTime: '2026-01-01T00:00:00Z' },
    ])
    expect(calls).toEqual([{ method: 'GET', url: '/api/tenants:mine', body: null }])
  })

  it('切换租户：进入租户带 tenantId，回平台传 null', async () => {
    const calls: Call[] = []
    record(calls, () => json(200, { accessToken: 'a-2' }))
    await useAppContext().apis.tenantApi.switchTenant({ tenantId: 'demo' })
    await useAppContext().apis.tenantApi.switchTenant({})
    expect(calls.map(call => call.body)).toEqual([{ tenantId: 'demo' }, { tenantId: null }])
    expect(new Set(calls.map(call => call.url))).toEqual(new Set(['/api/auth:switch-tenant']))
  })

  it('锁屏：锁定与解锁把密码交给 auth.lock / auth.unlock', async () => {
    const calls: Call[] = []
    record(calls, url => json(200, url.endsWith(':lock') ? { reason: 'ScreenLock', lockedAt: '2026-10-29T00:00:00Z' } : { unlocked: true }))
    await useAppContext().apis.lockSessionApi({ password: 'open-sesame' })
    await useAppContext().apis.unlockSessionApi({ password: 'open-sesame' })
    expect(calls).toEqual([
      { method: 'POST', url: '/api/auth:lock', body: { password: 'open-sesame' } },
      { method: 'POST', url: '/api/auth:unlock', body: { password: 'open-sesame' } },
    ])
  })

  it('开放接口凭证：my-api-credentials.* 投影成 4.x 的凭证条目与一次性密钥；启停换成 isEnabled，删除传 ids，调用安全设置换字段名', async () => {
    const calls: Call[] = []
    const credential = { id: 'c-1', name: '默认凭证', appKey: 'ak_x', isEnabled: true, lastUsedAt: null, createdAt: '2026-10-30T00:00:00Z' }
    const settings = { signatureAlgorithm: 'HmacSHA512', ipAllowList: ['10.0.0.0/24'], algorithms: ['HmacSHA256', 'HmacSHA512'] }
    record(calls, (url) => {
      if (url.endsWith('/api/my-api-credentials') || url.includes(':rotate-secret'))
        return json(url.includes(':rotate-secret') ? 200 : 201, { id: 'c-1', appKey: 'ak_x', secret: 'sk_y' })
      if (url.includes(':settings') || url.includes(':update-settings'))
        return json(200, settings)
      if (url.includes('?ids='))
        return json(200, { succeeded: ['c-1'], failed: [] })
      return json(200, { ...credential, isEnabled: false })
    })
    const apis = useAppContext().apis
    expect(await apis.createApiCredentialApi()).toEqual({ basicId: 'c-1', appKey: 'ak_x', appSecret: 'sk_y' })
    expect(calls[0]).toEqual({ method: 'POST', url: '/api/my-api-credentials', body: { name: null } })
    expect(await apis.updateApiCredentialStatusApi('c-1', 'Disabled')).toEqual({ basicId: 'c-1', credentialName: '默认凭证', appKey: 'ak_x', status: 'Disabled', lastUsedTime: null, createdTime: '2026-10-30T00:00:00Z' })
    expect(calls[1]).toEqual({ method: 'PUT', url: '/api/my-api-credentials/c-1', body: { isEnabled: false } })
    await apis.rotateApiCredentialSecretApi('c-1')
    expect(calls[2]).toMatchObject({ method: 'POST', url: '/api/my-api-credentials/c-1:rotate-secret' })
    await apis.deleteApiCredentialApi('c-1')
    expect(calls[3]).toEqual({ method: 'DELETE', url: '/api/my-api-credentials?ids=c-1', body: null })
    expect(await apis.saveApiCallSettingsApi({ signAlgorithm: 'HmacSHA512', ipWhitelist: ['10.0.0.0/24'] })).toEqual({ signAlgorithm: 'HmacSHA512', ipWhitelist: ['10.0.0.0/24'], algorithms: ['HmacSHA256', 'HmacSHA512'] })
    expect(calls[4]).toEqual({ method: 'POST', url: '/api/my-api-credentials:update-settings', body: { signatureAlgorithm: 'HmacSHA512', ipAllowList: ['10.0.0.0/24'] } })
  })

  it('我的 OAuth 应用：my-oauth-apps.* 投影成 4.x 的条目与一次性密钥；回调地址文本拆成列表，修改与启停取最新一份整体替换，删除被拒即报错', async () => {
    const calls: Call[] = []
    const app = { id: 'o-1', appName: '我的集成', appDescription: null, clientId: 'cid_x', clientType: 'Confidential', appType: 'Web', grantTypes: ['authorization_code', 'refresh_token'], redirectUris: ['https://a.example/cb', 'https://b.example/cb'], scopes: ['openid', 'profile'], accessTokenLifetime: 3600, refreshTokenLifetime: 2592000, authorizationCodeLifetime: 300, logo: null, homepage: 'https://a.example', skipConsent: false, isEnabled: true, ownerUserId: 'u-1', ownerUserName: 'alice', remark: null, createdAt: '2026-10-30T00:00:00Z', modifiedAt: null }
    let deleteFails = false
    record(calls, (url) => {
      if (url.endsWith('/api/my-oauth-apps') && calls.at(-1)?.method === 'GET')
        return json(200, { items: [app] })
      if (url.endsWith('/api/my-oauth-apps'))
        return json(201, { id: 'o-2', clientId: 'cid_p', clientType: 'Public', clientSecret: null })
      if (url.includes(':reset-secret'))
        return json(200, { id: 'o-1', clientId: 'cid_x', clientType: 'Confidential', clientSecret: 'cs_y' })
      if (url.includes('?ids='))
        return json(200, deleteFails ? { succeeded: [], failed: [{ id: 'o-1', code: 'OPENPLATFORM_NOTFOUND_001' }] } : { succeeded: ['o-1'], failed: [] })
      return json(200, calls.at(-1)?.method === 'PUT' ? { ...app, ...(calls.at(-1)?.body as object) } : app)
    })
    const apis = useAppContext().apis
    expect(await apis.getMyOAuthAppsApi()).toEqual([{ basicId: 'o-1', clientId: 'cid_x', appName: '我的集成', appDescription: null, homepage: 'https://a.example', logo: null, redirectUris: 'https://a.example/cb\nhttps://b.example/cb', clientType: 'Confidential', grantTypes: 'authorization_code,refresh_token', scopes: 'openid,profile', status: 'Enabled', createdTime: '2026-10-30T00:00:00Z' }])
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/my-oauth-apps', body: null })
    expect(await apis.createMyOAuthAppApi({ appName: 'SPA', clientType: 'Public', redirectUris: ' https://c.example/cb \n\nhttps://d.example/cb, ' })).toEqual({ basicId: 'o-2', clientId: 'cid_p', clientType: 'Public', clientSecret: '' })
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/my-oauth-apps', body: { appName: 'SPA', clientType: 'Public', homepage: null, appDescription: null, redirectUris: ['https://c.example/cb', 'https://d.example/cb'], logo: null } })
    expect(await apis.updateMyOAuthAppApi({ basicId: 'o-1', appName: '改名', redirectUris: 'https://a.example/cb' })).toMatchObject({ appName: '改名', status: 'Enabled', redirectUris: 'https://a.example/cb' })
    expect(calls[2]).toEqual({ method: 'GET', url: '/api/my-oauth-apps/o-1', body: null })
    expect(calls[3]).toEqual({ method: 'PUT', url: '/api/my-oauth-apps/o-1', body: { appName: '改名', homepage: null, appDescription: null, redirectUris: ['https://a.example/cb'], logo: null, isEnabled: true } })
    expect(await apis.updateMyOAuthAppStatusApi('o-1', 'Disabled')).toMatchObject({ basicId: 'o-1', status: 'Disabled' })
    expect(calls[5]).toEqual({ method: 'PUT', url: '/api/my-oauth-apps/o-1', body: { appName: '我的集成', homepage: 'https://a.example', appDescription: null, redirectUris: ['https://a.example/cb', 'https://b.example/cb'], logo: null, isEnabled: false } })
    expect(await apis.regenerateMyOAuthAppSecretApi('o-1')).toEqual({ basicId: 'o-1', clientId: 'cid_x', clientType: 'Confidential', clientSecret: 'cs_y' })
    expect(calls[6]).toMatchObject({ method: 'POST', url: '/api/my-oauth-apps/o-1:reset-secret' })
    await apis.deleteMyOAuthAppApi('o-1')
    expect(calls[7]).toEqual({ method: 'DELETE', url: '/api/my-oauth-apps?ids=o-1', body: null })
    deleteFails = true
    await expect(apis.deleteMyOAuthAppApi('o-1')).rejects.toThrow('OPENPLATFORM_NOTFOUND_001')
  })

  it('已授权的应用：oauth-consents.list 投影成壳层条目（授权时间换成 grantedTime），撤销按 ClientId 交给 oauth-consents.revoke', async () => {
    const calls: Call[] = []
    record(calls, url => url.endsWith(':revoke')
      ? json(200, { revoked: true })
      : json(200, { items: [{ clientId: 'cid_x', appName: null, logo: 'https://a.example/logo.png', homepage: null, scopes: ['openid', 'profile'], grantedAt: '2026-10-30T00:00:00Z' }] }))
    const apis = useAppContext().apis
    expect(await apis.getOAuthConsentsApi()).toEqual([{ clientId: 'cid_x', appName: null, logo: 'https://a.example/logo.png', homepage: null, scopes: ['openid', 'profile'], grantedTime: '2026-10-30T00:00:00Z' }])
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/oauth-consents', body: null })
    expect(await apis.revokeOAuthConsentApi('cid_x')).toEqual({ revoked: true })
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/oauth-consents:revoke', body: { clientId: 'cid_x' } })
  })

  it('登录配置：第三方登录提供方原样给登录页（名称、显示名与类型）', async () => {
    const calls: Call[] = []
    record(calls, () => json(200, { loginMethods: ['password'], oAuthProviders: [{ name: 'github', displayName: 'GitHub', type: 'github' }, { name: 'corp', displayName: '企业微信', type: 'wecom' }], captchaEnabled: true }))
    expect(await useAppContext().apis.getLoginConfigApi()).toEqual({ loginMethods: ['password'], oAuthProviders: [{ name: 'github', displayName: 'GitHub', type: 'github' }, { name: 'corp', displayName: '企业微信', type: 'wecom' }], captchaEnabled: true })
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/auth:login-config', body: null })
  })

  it('账号绑定：发起返回 profile.begin-external-bind 给的地址；profile.external-logins / link-external / unlink-external 投影成 4.x 的条目（账号显示名进 providerDisplayName、最近登录换成 lastLoginTime，没有的字段不给）', async () => {
    const calls: Call[] = []
    const github = { provider: 'github', displayName: 'octocat', email: 'octo@example.com', avatarUrl: 'https://a.example/o.png', boundAt: '2026-10-30T00:00:00Z', lastLoginAt: '2026-10-30T01:00:00Z' }
    const wecom = { provider: 'wecom', displayName: null, email: null, avatarUrl: null, boundAt: '2026-10-30T02:00:00Z', lastLoginAt: null }
    record(calls, (url) => {
      if (url.endsWith(':begin-external-bind'))
        return json(200, { url: '/api/oauth/wecom/challenge?mode=bind&intent=i-1' })
      if (url.endsWith(':link-external'))
        return json(200, wecom)
      if (url.endsWith(':unlink-external'))
        return json(200, { items: [wecom] })
      return json(200, { items: [github, wecom] })
    })
    const apis = useAppContext().apis
    expect(await apis.getLinkedAccountsApi()).toEqual([
      { provider: 'github', providerDisplayName: 'octocat', email: 'octo@example.com', avatarUrl: 'https://a.example/o.png', lastLoginTime: '2026-10-30T01:00:00Z' },
      { provider: 'wecom' },
    ])
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/profile:external-logins', body: null })
    expect(await apis.linkAccountApi('x-1')).toEqual({ provider: 'wecom' })
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/profile:link-external', body: { ticket: 'x-1' } })
    expect(await apis.unlinkAccountApi('github')).toEqual([{ provider: 'wecom' }])
    expect(calls[2]).toEqual({ method: 'POST', url: '/api/profile:unlink-external', body: { provider: 'github' } })
    expect(await apis.beginOAuthBindApi('wecom')).toBe('/api/oauth/wecom/challenge?mode=bind&intent=i-1')
    expect(calls[3]).toEqual({ method: 'POST', url: '/api/profile:begin-external-bind', body: { provider: 'wecom' } })
  })

  it('登录设备：profile.sessions 投影成 4.x 的条目（设备类别换成图标表编号，机器人与没识别的是 1，系统与时间换字段名，没有的字段不给）；下线一个传 sessionId、下线其他传空', async () => {
    const calls: Call[] = []
    const session = (id: string, device: null | string, extra: object = {}) => ({ id, device, browser: null, os: null, ipAddress: null, location: null, loginAt: `2026-10-30T00:00:0${id.at(-1)}Z`, lastActiveAt: '2026-10-30T01:00:00Z', isCurrent: false, ...extra })
    const current = session('s-1', 'Desktop', { browser: 'Chrome', os: 'Windows', ipAddress: '10.0.0.1', location: '上海', isCurrent: true })
    record(calls, url => json(200, { items: url.endsWith(':sessions') ? [current, session('s-2', 'Mobile'), session('s-3', 'Tablet'), session('s-4', 'Bot'), session('s-5', null)] : [current] }))
    const apis = useAppContext().apis
    expect(await apis.getSessionsApi()).toEqual([
      { sessionId: 's-1', deviceType: 3, browser: 'Chrome', operatingSystem: 'Windows', ipAddress: '10.0.0.1', location: '上海', loginTime: '2026-10-30T00:00:01Z', lastActivityTime: '2026-10-30T01:00:00Z', isCurrent: true },
      { sessionId: 's-2', deviceType: 2, loginTime: '2026-10-30T00:00:02Z', lastActivityTime: '2026-10-30T01:00:00Z', isCurrent: false },
      { sessionId: 's-3', deviceType: 4, loginTime: '2026-10-30T00:00:03Z', lastActivityTime: '2026-10-30T01:00:00Z', isCurrent: false },
      { sessionId: 's-4', deviceType: 1, loginTime: '2026-10-30T00:00:04Z', lastActivityTime: '2026-10-30T01:00:00Z', isCurrent: false },
      { sessionId: 's-5', deviceType: 1, loginTime: '2026-10-30T00:00:05Z', lastActivityTime: '2026-10-30T01:00:00Z', isCurrent: false },
    ])
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/profile:sessions', body: null })
    await apis.revokeSessionApi('s-2')
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/profile:revoke-session', body: { sessionId: 's-2' } })
    await apis.revokeOtherSessionsApi()
    expect(calls[2]).toEqual({ method: 'POST', url: '/api/profile:revoke-other-sessions', body: {} })
  })

  it('登录日志：my-login-logs.list 按页码与条数取（回请求的那一页），事件、成败与错误码推成 4.x 的结果，时间与 IP 换字段名、失败原因进 message，没有的字段不给', async () => {
    const calls: Call[] = []
    const events: Array<[string, boolean, null | string, string]> = [
      ['Login', true, null, 'Success'],
      ['Login', false, 'IDENTITY_DENIED_003', 'InvalidCredentials'],
      ['Login', false, 'IDENTITY_DENIED_009', 'TwoFactorFailed'],
      ['Login', false, 'IDENTITY_DENIED_001', 'Failed'],
      ['Refresh', true, null, 'TokenRefreshed'],
      ['Refresh', false, 'IDENTITY_DENIED_004', 'Failed'],
      ['Logout', true, null, 'Logout'],
      ['SwitchTenant', true, null, 'TenantSwitched'],
      ['ImpersonationStart', true, null, 'ImpersonationStarted'],
      ['ImpersonationStop', true, null, 'ImpersonationEnded'],
      ['ChangePassword', true, null, 'PasswordChanged'],
      ['ChangePassword', false, 'IDENTITY_DENIED_003', 'Failed'],
      ['TwoFactorChallenge', true, null, 'RequiresTwoFactor'],
      ['TwoFactorEnable', true, null, 'MfaBound'],
      ['TwoFactorDisable', true, null, 'MfaUnbound'],
      ['TwoFactorReset', true, null, 'MfaUnbound'],
      ['RoleActivation', true, null, 'RolesActivated'],
    ]
    const log = { id: 'l-0', occurredAt: '2026-10-30T00:00:00Z', event: 'Login', succeeded: false, errorCode: 'IDENTITY_DENIED_003', errorMessage: '用户名或密码错误。', clientIp: '10.0.0.1', browser: 'Chrome', os: 'Windows', device: 'Desktop', impersonated: false }
    record(calls, () => json(200, {
      items: [log, ...events.map(([event, succeeded, errorCode], index) => ({ id: `l-${index + 1}`, occurredAt: '2026-10-30T00:00:00Z', event, succeeded, errorCode, errorMessage: null, clientIp: null, browser: null, os: null, device: null, impersonated: false }))],
      total: 42,
      page: { index: 2, size: 20 },
    }))
    const result = await useAppContext().apis.getLoginLogsApi(2, 20)
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/my-login-logs?page=2&pageSize=20', body: null })
    expect(result).toMatchObject({ total: 42, page: 2, pageSize: 20 })
    expect(result.items[0]).toEqual({ loginTime: '2026-10-30T00:00:00Z', loginIp: '10.0.0.1', browser: 'Chrome', os: 'Windows', loginResult: 'InvalidCredentials', message: '用户名或密码错误。' })
    expect(result.items[1]).toEqual({ loginTime: '2026-10-30T00:00:00Z', loginResult: 'Success' })
    expect(result.items.slice(1).map(item => item.loginResult)).toEqual(events.map(([, , , expected]) => expected))
  })

  it('通知偏好：profile.notification-preference 给六个同名开关；保存逐项交给 update-notification-preference（4.x 的推送、机器人、审批、营销不上送），回保存后的偏好', async () => {
    const calls: Call[] = []
    const preference = { channelInApp: true, channelEmail: false, channelSms: true, typeAnnouncement: true, typeTask: false, typeSecurity: true }
    record(calls, () => json(200, preference))
    const apis = useAppContext().apis
    expect(await apis.getNotificationPreferenceApi()).toEqual(preference)
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/profile:notification-preference', body: null })
    const legacy = { ...preference, channelPush: true, channelBot: false, typeApproval: true, typeMarketing: false }
    expect(await apis.updateNotificationPreferenceApi(legacy)).toEqual(preference)
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/profile:update-notification-preference', body: preference })
  })

  it('活跃度：my-activity.summary 带浏览器的 IANA 时区；次数与时间换成 4.x 的 *Count / *Time，没有的最近时间不给，趋势换字段名（没有在线时长）', async () => {
    const calls: Call[] = []
    const resolvedOptions = Intl.DateTimeFormat.prototype.resolvedOptions
    vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockImplementation(function (this: Intl.DateTimeFormat) {
      return { ...resolvedOptions.call(this), timeZone: 'Asia/Shanghai' }
    })
    record(calls, () => json(200, {
      today: { logins: 1, accesses: 20, operations: 3 },
      thisWeek: { logins: 4, accesses: 120, operations: 9 },
      thisMonth: { logins: 12, accesses: 480, operations: 30 },
      lastLoginAt: '2026-10-30T01:00:00Z',
      lastAccessAt: null,
      lastOperationAt: '2026-10-30T02:30:00Z',
      trend: [{ date: '2026-10-29', operations: 5, accesses: 40 }, { date: '2026-10-30', operations: 0, accesses: 20 }],
    }))
    expect(await useAppContext().apis.getActivityApi()).toEqual({
      today: { loginCount: 1, accessCount: 20, operationCount: 3 },
      thisWeek: { loginCount: 4, accessCount: 120, operationCount: 9 },
      thisMonth: { loginCount: 12, accessCount: 480, operationCount: 30 },
      lastLoginTime: '2026-10-30T01:00:00Z',
      lastOperationTime: '2026-10-30T02:30:00Z',
      trend: [{ date: '2026-10-29', accessCount: 40, operationCount: 5 }, { date: '2026-10-30', accessCount: 20, operationCount: 0 }],
    })
    expect(calls).toEqual([{ method: 'GET', url: '/api/my-activity:summary?timeZone=Asia%2FShanghai', body: null }])
  })

  it('个人资料：profile.get 投影成 4.x 的资料——显示名进 nickName、头像是文件主键、性别换成数字、上次改名换成 *Time，下次可改时间没到即不能改，没有的字段不给', async () => {
    const calls: Call[] = []
    const later = new Date(Date.now() + 10 * DAY).toISOString()
    let response: object = { ...profileOf(), userNameChangedAt: '2026-10-01T00:00:00Z', userNameChangeableAt: later }
    record(calls, () => json(200, response))
    expect(await useAppContext().apis.getProfileApi()).toEqual({
      userId: 'u-1',
      userName: 'alice',
      realName: '张三',
      nickName: 'Alice',
      avatar: 'f-1',
      email: 'alice@example.com',
      gender: 2,
      birthday: '1990-05-04T00:00:00Z',
      country: '中国',
      tenantId: null,
      isSystemAccount: false,
      twoFactorEnabled: false,
      twoFactorMethod: 0,
      emailVerified: false,
      phoneVerified: false,
      lastUserNameChangeTime: '2026-10-01T00:00:00Z',
      canChangeUserName: false,
      isLocked: false,
      failedLoginAttempts: 0,
    })
    expect(calls).toEqual([{ method: 'GET', url: '/api/profile', body: null }])

    // 冷却期已过（或从没改过）即能改；性别未设置是 0
    response = { ...profileOf({ avatarFileId: null }), realName: null, gender: 'Unknown', birthday: null, country: null, remark: '自我介绍', userNameChangedAt: '2026-07-01T00:00:00Z', userNameChangeableAt: new Date(Date.now() - DAY).toISOString() }
    const bare = await useAppContext().apis.getProfileApi()
    expect(bare).toMatchObject({ gender: 0, remark: '自我介绍', lastUserNameChangeTime: '2026-07-01T00:00:00Z', canChangeUserName: true })
    for (const key of ['avatar', 'realName', 'birthday', 'country'])
      expect(bare).not.toHaveProperty(key)
  })

  it('保存资料：nickName 换成 displayName、性别数字换回字面量、没给的为 null，整体交给 profile.update（PUT）；回来的用户换掉会话里的那份，认不得的性别不发', async () => {
    const calls: Call[] = []
    localStorage.setItem('xihan.next.session', JSON.stringify(sessionOf()))
    record(calls, () => json(200, profileOf({ displayName: 'Alice Z' })))
    const apis = useAppContext().apis
    expect(await apis.updateProfileApi({ nickName: 'Alice Z', realName: '', gender: 1, birthday: '1990-05-04T00:00:00.000Z', country: '中国' })).toMatchObject({ nickName: 'Alice Z' })
    expect(calls).toEqual([{ method: 'PUT', url: '/api/profile', body: { displayName: 'Alice Z', realName: '', gender: 'Male', birthday: '1990-05-04T00:00:00.000Z', country: '中国', remark: null } }])
    expect(useUserStore().nickname).toBe('Alice Z')
    expect(useAuthStore().accessToken).toBe('a-1')

    await expect(apis.updateProfileApi({ nickName: 'Alice', gender: 9 })).rejects.toThrow('未知的性别 9')
    expect(calls).toHaveLength(1)
  })

  it('去掉头像与改用户名：交给 profile.remove-avatar / change-username，回来的用户换掉会话里的那份（顶栏头像与用户名随之换）', async () => {
    const calls: Call[] = []
    localStorage.setItem('xihan.next.session', JSON.stringify(sessionOf()))
    record(calls, url => json(200, url.endsWith(':remove-avatar') ? profileOf({ avatarFileId: null }) : profileOf({ avatarFileId: null, userName: 'bob' })))
    const apis = useAppContext().apis
    expect(useUserStore().avatar).toBe('f-1')

    expect(await apis.removeAvatarApi()).not.toHaveProperty('avatar')
    expect(calls[0]).toEqual({ method: 'POST', url: '/api/profile:remove-avatar', body: {} })
    expect(useUserStore().avatar).toBe('')

    await apis.changeUserNameApi({ userName: 'bob', password: 'pw' })
    expect(calls[1]).toEqual({ method: 'POST', url: '/api/profile:change-username', body: { userName: 'bob', password: 'pw' } })
    expect(useUserStore().username).toBe('bob')
  })

  it('上传头像：multipart 字段 file 带令牌发给 POST /api/profile/avatar 并回报进度，返回新头像的文件主键，会话里的用户随之换掉', async () => {
    const requests: Array<{ method: string, url: string, headers: Record<string, string>, body: FormData }> = []
    // 上传走 XMLHttpRequest（要进度）：换成只记请求、立即回资料的替身
    class FakeXhr {
      upload: { onprogress: ((event: { lengthComputable: boolean, loaded: number, total: number }) => void) | null } = { onprogress: null }
      onload: (() => void) | null = null
      onerror: (() => void) | null = null
      onabort: (() => void) | null = null
      status = 0
      responseText = ''
      private request = { method: '', url: '', headers: {} as Record<string, string> }

      open(method: string, url: string) {
        this.request.method = method
        this.request.url = url
      }

      setRequestHeader(name: string, value: string) {
        this.request.headers[name] = value
      }

      getResponseHeader() {
        return 'application/json'
      }

      abort() {}

      send(body: FormData) {
        requests.push({ ...this.request, body })
        this.upload.onprogress?.({ lengthComputable: true, loaded: 1, total: 2 })
        this.status = 200
        this.responseText = JSON.stringify(profileOf({ avatarFileId: 'f-2' }))
        this.onload?.()
      }
    }
    vi.stubGlobal('XMLHttpRequest', FakeXhr)
    record([], () => json(500, {}))
    localStorage.setItem('xihan.next.session', JSON.stringify(sessionOf()))
    const progress: number[] = []
    const avatar = new File([new Uint8Array(16)], 'me.png', { type: 'image/png' })

    expect(await useAppContext().apis.uploadAvatarApi(avatar, percent => progress.push(percent))).toEqual({ fileId: 'f-2' })
    expect(requests).toHaveLength(1)
    expect(requests[0]).toMatchObject({ method: 'POST', url: '/api/profile/avatar', headers: { Authorization: 'Bearer a-1' } })
    expect((requests[0]!.body.get('file') as File).name).toBe('me.png')
    expect(progress).toEqual([50])
    expect(useUserStore().avatar).toBe('f-2')
  })

  it('模仿：范围只取正常状态的租户；候选与发起把范围原样传给操作', async () => {
    const calls: Call[] = []
    record(calls, (url) => {
      if (url.endsWith('/api/tenants:list'))
        return json(200, { items: [{ id: 't-1', tenantId: 'demo', name: 'Demo Tenant', status: 'Active', order: 0, createdAt: '2026-10-28T00:00:00Z' }], total: 1, page: { index: 1, size: 500 } })
      if (url.includes('/api/impersonation:candidates'))
        return json(200, { items: [{ id: 'u-2', userName: 'bob', displayName: 'Bob', homeTenantId: 'demo' }] })
      return json(200, { accessToken: 'a-9' })
    })
    const { impersonationApi } = useAppContext().apis
    expect(await impersonationApi.tenants()).toEqual([{ tenantId: 'demo', tenantName: 'Demo Tenant' }])
    expect(calls[0]).toEqual({ method: 'POST', url: '/api/tenants:list', body: { page: { index: 1, size: 500 }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }] } })

    expect(await impersonationApi.candidates({ keyword: 'bo', tenantId: 'demo' })).toEqual([{ basicId: 'u-2', userName: 'bob', nickName: 'Bob' }])
    expect(calls[1]).toEqual({ method: 'GET', url: '/api/impersonation:candidates?tenantId=demo&keyword=bo', body: null })
    await impersonationApi.candidates({})
    // 不填即当前上下文：空值不进查询串
    expect(calls[2]).toEqual({ method: 'GET', url: '/api/impersonation:candidates', body: null })

    await impersonationApi.start({ targetUserId: 'u-2', tenantId: 'demo', reason: 'ticket 42' })
    expect(calls[3]).toMatchObject({ url: '/api/impersonation:start', body: { targetUserId: 'u-2', tenantId: 'demo', reason: 'ticket 42' } })
    await impersonationApi.stop()
    expect(calls[4]).toMatchObject({ url: '/api/impersonation:stop', body: {} })
  })

  it('用户设置：壳层的场景数值与契约字面量互转，整体覆盖写入；clientId / origin 上行供实时回推过滤回显', async () => {
    const calls: Call[] = []
    record(calls, url => url.startsWith('/api/user-settings?')
      ? json(200, { scene: 'Preference', key: 'global', value: '{"a":1}', modifiedAt: '2026-10-28T00:00:00Z' })
      : json(200, { scene: 'Page', key: 'identity.users', value: null, modifiedAt: null }))
    const { userSettingApi } = useAppContext().apis
    expect(await userSettingApi.get({ scene: 0, settingKey: 'global' })).toEqual({ scene: 0, settingKey: 'global', settingValue: '{"a":1}' })
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/user-settings?scene=Preference&key=global', body: null })
    expect(await userSettingApi.save({ scene: 1, settingKey: 'identity.users', settingValue: null, clientId: 'c-1', origin: '40,60' })).toEqual({ scene: 1, settingKey: 'identity.users', settingValue: null })
    expect(calls[1]).toEqual({ method: 'PUT', url: '/api/user-settings', body: { scene: 'Page', key: 'identity.users', value: null, clientId: 'c-1', origin: '40,60' } })
  })

  it('字典选项：取 dicts.options 的 items（已深度优先展平）', async () => {
    const calls: Call[] = []
    record(calls, () => json(200, { items: [{ value: 'vip', label: 'VIP', parentValue: null, isDefault: false, disabled: false }] }))
    expect(await useAppContext().apis.dictApi.options('customer_level')).toEqual([{ value: 'vip', label: 'VIP', parentValue: null, isDefault: false, disabled: false }])
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/dicts:options?code=customer_level', body: null })
  })
})
