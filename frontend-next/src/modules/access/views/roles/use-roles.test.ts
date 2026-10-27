import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useRoles } from './use-roles'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const role = { id: 'r-1', ownerTenantId: 'platform', code: 'auditor', name: 'Auditor', description: null, order: 0, isBuiltIn: false, status: 'Active', createdAt: '2026-10-28T00:00:00Z' }

describe('useRoles', () => {
  it('loads the list, selects a role with its catalog and members, and saves permission changes as a diff', async () => {
    const calls: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ url: path, body })
        if (path.endsWith('/api/roles:list'))
          return json(200, { items: [role], total: 1, page: { index: 1, size: 20 } })
        if (path.endsWith('/api/roles/r-1'))
          return json(200, { role, permissions: ['identity.users.read'], memberCount: 1 })
        if (path.endsWith('/api/permissions')) {
          return json(200, { items: [
            { code: 'identity.users.read', displayName: 'Read users', description: null, package: 'identity', resource: 'users', action: 'read', scope: 'Both', audited: false },
            { code: 'identity.users.create', displayName: 'Create users', description: null, package: 'identity', resource: 'users', action: 'create', scope: 'Both', audited: true },
            { code: 'access.roles.read', displayName: 'Read roles', description: null, package: 'access', resource: 'roles', action: 'read', scope: 'Both', audited: false },
          ] })
        }
        if (path.endsWith('/api/roles/r-1:members'))
          return json(200, { items: [{ userId: 'u-1', userName: 'bob', displayName: 'Bob', status: 'Active', assignedAt: '2026-10-28T00:00:00Z' }], total: 1 })
        if (path.endsWith('/api/roles/r-1:grant'))
          return json(200, { succeeded: body.permissions, failed: [] })
        if (path.endsWith('/api/roles/r-1:revoke'))
          return json(200, { succeeded: body.permissions, failed: [] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useRoles(20)
    await page.load()
    expect(page.items.value.map(r => r.code)).toEqual(['auditor'])

    await page.select('r-1')
    expect(page.selected.value?.permissions).toEqual(['identity.users.read'])
    expect(page.groups.value.map(g => g.package)).toEqual(['identity', 'access'])
    expect(page.members.value.map(m => m.userName)).toEqual(['bob'])
    expect(page.dirty.value).toBe(false)

    page.togglePermission('identity.users.create')
    page.togglePermission('identity.users.read')
    expect(page.dirty.value).toBe(true)
    expect(await page.savePermissions()).toBe(true)
    const grant = calls.find(c => c.url.endsWith(':grant'))
    const revoke = calls.find(c => c.url.endsWith(':revoke'))
    expect(grant?.body).toEqual({ permissions: ['identity.users.create'] })
    expect(revoke?.body).toEqual({ permissions: ['identity.users.read'] })
    expect(page.notice.value).toBe('saved')
  })

  it('hands creation validation errors to the fields', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['pattern'] } }, 'application/problem+json')) as unknown as typeof fetch,
    })
    const page = useRoles(20)
    page.form.code = 'Bad Code'
    page.form.name = 'x'
    expect(await page.submit()).toBe(false)
    expect(page.fieldError('code')).toBe('pattern')
    expect(page.submitError.value).toBeNull()
  })
})
