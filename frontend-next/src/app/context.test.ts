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

  it('登记了控制中心与个人中心的路径', () => {
    expect(useAppContext().shellRoutes).toMatchObject({ controlCenter: '/control-center', profile: '/identity/profile/change-password' })
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

  it('用户设置：壳层的场景数值与契约字面量互转，整体覆盖写入（clientId 不上行）', async () => {
    const calls: Call[] = []
    record(calls, url => url.startsWith('/api/user-settings?')
      ? json(200, { scene: 'Preference', key: 'global', value: '{"a":1}', modifiedAt: '2026-10-28T00:00:00Z' })
      : json(200, { scene: 'Page', key: 'identity.users', value: null, modifiedAt: null }))
    const { userSettingApi } = useAppContext().apis
    expect(await userSettingApi.get({ scene: 0, settingKey: 'global' })).toEqual({ scene: 0, settingKey: 'global', settingValue: '{"a":1}' })
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/user-settings?scene=Preference&key=global', body: null })
    expect(await userSettingApi.save({ scene: 1, settingKey: 'identity.users', settingValue: null, clientId: 'c-1' })).toEqual({ scene: 1, settingKey: 'identity.users', settingValue: null })
    expect(calls[1]).toEqual({ method: 'PUT', url: '/api/user-settings', body: { scene: 'Page', key: 'identity.users', value: null } })
  })

  it('字典选项：取 dicts.options 的 items（已深度优先展平）', async () => {
    const calls: Call[] = []
    record(calls, () => json(200, { items: [{ value: 'vip', label: 'VIP', parentValue: null, isDefault: false, disabled: false }] }))
    expect(await useAppContext().apis.dictApi.options('customer_level')).toEqual([{ value: 'vip', label: 'VIP', parentValue: null, isDefault: false, disabled: false }])
    expect(calls[0]).toEqual({ method: 'GET', url: '/api/dicts:options?code=customer_level', body: null })
  })
})
