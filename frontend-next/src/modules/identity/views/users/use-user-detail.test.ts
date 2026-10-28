import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useUserDetail } from './use-user-detail'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const bob = { id: 'u-1', userName: 'bob', displayName: 'Bob', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

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
  it('账号资料直接用列表行；按区块另取角色（roles.by-user）与部门归属（memberships.list 按用户筛）', async () => {
    const calls = record((url) => {
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
    expect(detail.roles.value?.map(r => r.code)).toEqual(['viewer'])
    expect(detail.memberships.value?.map(m => m.departmentName)).toEqual(['HQ'])
    expect(detail.loading.value).toBe(false)
    expect(calls.find(c => c.url.endsWith('/api/memberships:list'))?.body).toMatchObject({ filters: [{ field: 'userId', operator: 'eq', value: 'u-1' }] })
    expect(calls.some(c => c.url.includes('/api/users/'))).toBe(false)
  })

  it('区块都没开时不发请求，对应区块保持 null', async () => {
    const calls = record(() => json(500, {}))
    const detail = useUserDetail()
    await detail.show(bob as never, { roles: false, departments: false })
    expect(calls).toHaveLength(0)
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
    expect(detail.roles.value).toBeNull()
    expect(detail.loading.value).toBe(false)
  })
})
