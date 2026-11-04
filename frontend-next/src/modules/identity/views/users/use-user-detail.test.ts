import { afterEach, describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { setDisplayTimeZone } from '~/utils'
import { useUserDetail } from './use-user-detail'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const bob = { id: 'u-1', userName: 'bob', displayName: 'Bob', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null, isExternalMember: false, roleNames: [], departmentName: null, isLocked: false, twoFactorEnabled: false }

const overview = {
  user: bob,
  security: { emailVerified: true, phoneVerified: false, twoFactorEnabled: true, twoFactorMethods: ['Totp'], isLocked: false, lockoutEndAt: null, failedAccessCount: 2 },
  today: { logins: 1, accesses: 12, onlineSeconds: 600 },
  currentSession: { device: 'Desktop', browser: 'Chrome', os: 'Windows', ipAddressMasked: '10.0.0.*', location: null, loginAt: '2026-10-28T00:00:00Z', lastActiveAt: '2026-10-28T01:00:00Z' },
}

function role(id: string, code: string) {
  return { id, ownerTenantId: 'acme', code, name: code, description: null, order: 0, isBuiltIn: false, status: 'Active', dataScope: 'SelfOnly', createdAt: '2026-10-28T00:00:00Z' }
}

const membership = { id: 'm-1', userId: 'u-1', userName: 'bob', displayName: 'Bob', departmentId: 'd-1', departmentName: 'HQ', positionId: null, positionName: null, isPrimary: true, employeeNumber: null, jobGrade: null, hiredAt: null, order: 0, createdAt: '2026-10-28T00:00:00Z' }

function record(respond: (url: string, body: unknown) => Response): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const body = init?.body ? JSON.parse(String(init.body)) : undefined
      calls.push({ method: init?.method ?? 'GET', url: String(url), body })
      return respond(String(url), body)
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('useUserDetail', () => {
  afterEach(() => setDisplayTimeZone(''))

  it('账号安全、今日统计与当前会话取 users.detail（按显示时区分天）；按区块另取角色（roles.by-user）与部门归属（memberships.list 按用户筛）', async () => {
    setDisplayTimeZone('Asia/Shanghai')
    const calls = record((url) => {
      if (url.includes('/api/users/u-1:detail'))
        return json(200, overview)
      if (url.endsWith('/api/roles:by-user?userId=u-1'))
        return json(200, { items: [role('r-1', 'viewer')] })
      if (url.endsWith('/api/memberships:list'))
        return json(200, { items: [membership], total: 1, page: { index: 1, size: 100 } })
      return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: url }, 'application/problem+json')
    })
    const detail = useUserDetail()
    await detail.show(bob as never, { roles: true, departments: true })
    expect(detail.open.value).toBe(true)
    expect(detail.user.value?.userName).toBe('bob')
    expect(detail.sections.value).toEqual({ roles: true, departments: true })
    expect(detail.detail.value?.security?.failedAccessCount).toBe(2)
    expect(detail.detail.value?.today?.accesses).toBe(12)
    expect(detail.detail.value?.currentSession?.ipAddressMasked).toBe('10.0.0.*')
    expect(detail.roles.value?.map(r => r.code)).toEqual(['viewer'])
    expect(detail.memberships.value?.map(m => m.departmentName)).toEqual(['HQ'])
    expect(detail.loading.value).toBe(false)
    expect(calls.find(c => c.url.endsWith('/api/memberships:list'))?.body).toMatchObject({ filters: [{ field: 'userId', operator: 'eq', value: 'u-1' }] })
    expect(calls.find(c => c.url.includes(':detail'))?.url).toBe(`/api/users/u-1:detail?timeZone=${encodeURIComponent('Asia/Shanghai')}`)
  })

  it('区块都没开时只取 users.detail，对应区块保持 null', async () => {
    const calls = record(() => json(200, { ...overview, security: null, today: null, currentSession: null }))
    const detail = useUserDetail()
    await detail.show(bob as never, { roles: false, departments: false })
    expect(calls).toHaveLength(1)
    expect(calls[0]!.url).toContain('/api/users/u-1:detail')
    expect(detail.detail.value?.security).toBeNull()
    expect(detail.roles.value).toBeNull()
    expect(detail.memberships.value).toBeNull()
    expect(detail.loading.value).toBe(false)
  })

  it('取数失败把后端文案留在 error 上，弹窗照常打开', async () => {
    record(() => json(403, { code: 'SECURITY_DENIED_002', status: 403, detail: 'not allowed', traceId: 'x' }, 'application/problem+json'))
    const detail = useUserDetail()
    await detail.show(bob as never, { roles: true, departments: false })
    expect(detail.open.value).toBe(true)
    expect(detail.error.value).toBe('not allowed')
    expect(detail.detail.value).toBeNull()
    expect(detail.roles.value).toBeNull()
    expect(detail.loading.value).toBe(false)
  })

  it('连点两行只认最后一次：慢响应回来不覆盖新选中的用户', async () => {
    let release: (() => void) | undefined
    const gate = new Promise<void>(resolve => (release = resolve))
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL) => {
        if (String(url).includes('/api/users/u-1:detail'))
          await gate
        return json(200, { ...overview, user: { ...bob, id: String(url).includes('u-1') ? 'u-1' : 'u-2' } })
      }) as unknown as typeof fetch,
    })
    const detail = useUserDetail()
    const slow = detail.show(bob as never, { roles: false, departments: false })
    await detail.show({ ...bob, id: 'u-2', userName: 'carol' } as never, { roles: false, departments: false })
    release?.()
    await slow
    expect(detail.user.value?.userName).toBe('carol')
    expect(detail.detail.value?.user.id).toBe('u-2')
    expect(detail.loading.value).toBe(false)
  })
})
