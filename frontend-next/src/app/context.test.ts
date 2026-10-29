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
