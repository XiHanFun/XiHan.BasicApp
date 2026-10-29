import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { canRevoke, delegationName, delegationsResource, delegationTarget, toCreateRequest, useDelegationEditor } from './use-delegations'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = { method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

function delegation(extra: Record<string, unknown> = {}) {
  return { id: 'd-1', delegatorId: 'u-1', delegatorName: 'Alice (@alice)', delegateeId: 'u-2', delegateeName: 'Bob (@bob)', permission: null, roleId: 'r-1', roleName: 'Approver', effectiveAt: null, expiresAt: '2026-11-01T00:00:00Z', reason: null, status: 'Active', revokedAt: null, createdAt: '2026-10-29T00:00:00Z', ...extra }
}

describe('delegationsResource', () => {
  it('状态多选合成 in，删除逐项失败即报错', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'd-9', code: 'ACCESS_NOTFOUND_006' }] })
      return json(200, { items: [delegation()], total: 1, page: { index: 1, size: 20 } })
    })
    const result = await delegationsResource().page!({ page: 1, pageSize: 20, filters: { keyword: ' vac ' }, conditionFilters: [{ field: 'status', operator: QueryOperator.In, values: ['Active', 'Pending'] }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/delegations:list' })
    expect(calls[0]!.body).toMatchObject({ keyword: 'vac', filters: [{ field: 'status', operator: 'in', value: 'Active,Pending' }] })
    expect(result.items.map(r => r.id)).toEqual(['d-1'])
    await expect(delegationsResource().remove!('d-9')).rejects.toThrow('ACCESS_NOTFOUND_006')
  })
})

describe('委托的显示与请求', () => {
  it('委托对象取角色名或权限显示名；只有待生效与生效中的能撤销', () => {
    expect(delegationTarget(delegation())).toBe('Approver')
    expect(delegationTarget(delegation({ permission: 'identity.users.read', roleId: null, roleName: null }))).toBe('Read users')
    expect(delegationTarget(delegation({ permission: 'custom.thing', roleId: null, roleName: null }))).toBe('custom.thing')
    expect(delegationName(delegation() as never)).toBe('Alice (@alice) → Bob (@bob) · Approver')
    expect(canRevoke({ status: 'Pending' })).toBe(true)
    expect(canRevoke({ status: 'Active' })).toBe(true)
    expect(canRevoke({ status: 'Expired' })).toBe(false)
    expect(canRevoke({ status: 'Revoked' })).toBe(false)
  })

  it('只带当前委托类型的对象，时间换成 ISO，空原因不传', () => {
    const at = Date.UTC(2026, 10, 1)
    const form = { delegatorId: 'u-1', delegateeId: 'u-2', targetKind: 'permission' as const, roleId: 'r-1', permission: 'identity.users.read', effectiveAt: null, expiresAt: at, reason: '  ' }
    expect(toCreateRequest(form)).toEqual({ delegatorId: 'u-1', delegateeId: 'u-2', roleId: null, permission: 'identity.users.read', effectiveAt: null, expiresAt: '2026-11-01T00:00:00.000Z', reason: null })
    expect(toCreateRequest({ ...form, targetKind: 'role', effectiveAt: at })).toMatchObject({ roleId: 'r-1', permission: null, effectiveAt: '2026-11-01T00:00:00.000Z' })
  })
})

describe('useDelegationEditor', () => {
  it('打开即取用户、启用的非内置角色与权限目录；提交失败把错误留在弹窗里', async () => {
    const calls = record((call) => {
      if (call.url === '/api/users:list')
        return json(200, { items: [{ id: 'u-1', userName: 'alice', displayName: 'Alice', homeTenantId: 'platform', status: 'Active', createdAt: '2026-10-28T00:00:00Z' }], total: 1, page: { index: 1, size: 50 } })
      if (call.url === '/api/roles:list')
        return json(200, { items: [{ id: 'r-1', code: 'approver', name: 'Approver', isBuiltIn: false, status: 'Active' }, { id: 'r-0', code: 'admin', name: 'Admin', isBuiltIn: true, status: 'Active' }], total: 2, page: { index: 1, size: 500 } })
      if (call.url === '/api/permissions')
        return json(200, { items: [{ code: 'identity.users.read', displayName: 'Read users', description: null, package: 'identity', resource: 'users', action: 'read', scope: 'Both', audited: false }] })
      return json(400, { code: 'ACCESS_VALIDATION_030', status: 400, detail: 'not holding', traceId: 'x' }, 'application/problem+json')
    })
    const editor = useDelegationEditor()
    await editor.start()
    expect(editor.open.value).toBe(true)
    expect(editor.userOptions.value).toEqual([{ value: 'u-1', label: 'Alice', description: '@alice' }])
    expect(editor.roleOptions.value.map(o => o.value)).toEqual(['r-1'])
    expect(editor.permissionOptions.value).toEqual([{ value: 'identity.users.read', label: 'Read users', description: 'identity.users.read' }])
    expect(calls.find(c => c.url === '/api/roles:list')?.body).toMatchObject({ filters: [{ field: 'status', operator: 'eq', value: 'Active' }] })

    Object.assign(editor.form, { delegatorId: 'u-1', delegateeId: 'u-2', roleId: 'r-1', expiresAt: Date.UTC(2026, 10, 1) })
    expect(await editor.submit()).toBe(false)
    expect(calls.at(-1)).toMatchObject({ method: 'POST', url: '/api/delegations' })
    expect(editor.submitError.value).toBe('not holding')
    expect(editor.open.value).toBe(true)
  })
})
