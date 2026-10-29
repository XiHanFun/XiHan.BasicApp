import type { Router } from 'vue-router'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useAppContext } from '~/stores'
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

  it('我的租户：tenants.mine 投影成切换器条目（注册地没有成员类型）', async () => {
    const calls: Call[] = []
    record(calls, () => json(200, {
      canAccessPlatform: true,
      platformIsCurrent: true,
      tenants: [
        { tenantId: 'demo', name: 'Demo Tenant', isHome: false, memberType: 'Owner', isCurrent: false },
        { tenantId: 'acme', name: 'Acme', isHome: true, memberType: null, isCurrent: true },
      ],
    }))
    expect(await useAppContext().apis.tenantApi.myAvailableTenants()).toEqual([
      { tenantId: 'demo', tenantName: 'Demo Tenant', isHome: false, memberType: 'Owner', isCurrent: false },
      { tenantId: 'acme', tenantName: 'Acme', isHome: true, memberType: null, isCurrent: true },
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
