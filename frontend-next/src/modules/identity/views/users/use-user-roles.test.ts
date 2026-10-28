import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useUserRoles } from './use-user-roles'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const bob = { id: 'u-1', userName: 'bob', displayName: 'Bob', email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null }

function role(id: string, code: string, status = 'Active') {
  return { id, ownerTenantId: 'acme', code, name: code.toUpperCase(), description: null, order: 0, isBuiltIn: false, status, dataScope: 'SelfOnly', createdAt: '2026-10-28T00:00:00Z' }
}

interface Call { method: string, url: string, body: { userIds?: string[] } | undefined }

/** 带状态的假后端：角色目录固定，指派按请求增删 */
function backend(initial: string[], reject: Record<string, string> = {}) {
  const mine = new Set(initial)
  const catalog = [role('r-1', 'viewer'), role('r-2', 'lead'), role('r-3', 'legacy', 'Disabled')]
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const path = String(url)
      const body = init?.body ? JSON.parse(String(init.body)) : undefined
      calls.push({ method: init?.method ?? 'GET', url: path, body })
      if (path.endsWith('/api/roles:list'))
        return json(200, { items: catalog, total: catalog.length, page: { index: 1, size: 500 } })
      if (path.endsWith('/api/roles:by-user?userId=u-1'))
        return json(200, { items: [...catalog.filter(r => mine.has(r.id)), ...[...mine].filter(id => id === 'r-9').map(id => role(id, 'outside'))] })
      const match = /\/api\/roles\/([^:]+):(assign|unassign)$/.exec(path)
      if (match) {
        const [, id, verb] = match
        if (reject[id!])
          return json(200, { succeeded: [], failed: [{ id: 'u-1', code: reject[id!] }] })
        if (verb === 'assign')
          mine.add(id!)
        else mine.delete(id!)
        return json(200, { succeeded: body.userIds, failed: [] })
      }
      return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('useUserRoles', () => {
  it('打开时装载当前侧的角色目录与该用户已有的角色；已有却不在目录里的补进条目，停用的角色带标记', async () => {
    const calls = backend(['r-1', 'r-9'])
    const roles = useUserRoles()
    await roles.start(bob as never)
    expect(roles.open.value).toBe(true)
    expect(roles.loading.value).toBe(false)
    expect(roles.items.value.map(item => item.basicId)).toEqual(['r-1', 'r-2', 'r-3', 'r-9'])
    expect(roles.items.value.find(item => item.basicId === 'r-3')?.disabled).toBe(true)
    expect(roles.draft.value).toEqual(['r-1', 'r-9'])
    expect(roles.dirty.value).toBe(false)
    expect(calls.find(c => c.url.endsWith('/api/roles:list'))?.body).toMatchObject({ page: { index: 1, size: 500 } })
  })

  it('保存按差异逐个角色 assign / unassign，随后按服务端重取', async () => {
    const calls = backend(['r-1'])
    const roles = useUserRoles()
    await roles.start(bob as never)
    roles.change(['r-2'])
    expect(roles.dirty.value).toBe(true)
    expect(await roles.save()).toEqual({ assigned: 1, unassigned: 1 })
    expect(calls.find(c => c.url.endsWith('/api/roles/r-2:assign'))).toMatchObject({ method: 'POST', body: { userIds: ['u-1'] } })
    expect(calls.find(c => c.url.endsWith('/api/roles/r-1:unassign'))).toMatchObject({ method: 'POST', body: { userIds: ['u-1'] } })
    expect(roles.assigned.value).toEqual(['r-2'])
    expect(roles.dirty.value).toBe(false)
  })

  it('没有改动时不发写请求', async () => {
    const calls = backend(['r-1'])
    const roles = useUserRoles()
    await roles.start(bob as never)
    const before = calls.length
    expect(await roles.save()).toEqual({ assigned: 0, unassigned: 0 })
    expect(calls).toHaveLength(before)
  })

  it('逐项被拒时带着错误码抛出，已落地的部分照样重取', async () => {
    backend([], { 'r-2': 'ACCESS_VALIDATION_005' })
    const roles = useUserRoles()
    await roles.start(bob as never)
    roles.change(['r-1', 'r-2'])
    await expect(roles.save()).rejects.toThrow('ACCESS_VALIDATION_005')
    expect(roles.assigned.value).toEqual(['r-1'])
    expect(roles.draft.value).toEqual(['r-1'])
    expect(roles.loading.value).toBe(false)
  })

  it('装载失败时抛出、抽屉保持打开且条目为空', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => json(403, { code: 'SECURITY_DENIED_002', status: 403, detail: 'not allowed', traceId: 'x' }, 'application/problem+json')) as unknown as typeof fetch,
    })
    const roles = useUserRoles()
    await expect(roles.start(bob as never)).rejects.toThrow('not allowed')
    expect(roles.open.value).toBe(true)
    expect(roles.items.value).toEqual([])
    expect(roles.loading.value).toBe(false)
  })
})
