import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useEditions } from './use-editions'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const edition = { id: 'e-1', code: 'free', name: 'Free', description: null, isDefault: true, order: 10, permissionCount: 1, tenantCount: 2, createdAt: '2026-10-28T00:00:00Z' }

function permission(code: string) {
  const [pkg, resource, action] = code.split('.')
  return { code, displayName: code, description: null, package: pkg, resource, action, scope: 'Both', audited: false }
}

const catalog = ['access.roles.read', 'identity.users.create', 'identity.users.read'].map(permission)

describe('useEditions', () => {
  it('loads editions with the tenant-side catalog, edits the whitelist by package and reports a refused delete', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    let whitelist = ['identity.users.read']
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        const detail = () => ({ edition: { ...edition, permissionCount: whitelist.length }, permissions: whitelist })
        if (path.endsWith('/api/editions') && (init?.method ?? 'GET') === 'GET')
          return json(200, { items: [{ ...edition, permissionCount: whitelist.length }] })
        if (path.endsWith('/api/permissions?side=tenant'))
          return json(200, { items: catalog })
        if (path.endsWith('/api/editions/e-1') && (init?.method ?? 'GET') === 'GET')
          return json(200, detail())
        if (path.endsWith('/api/editions/e-1:set-permissions')) {
          whitelist = body.permissions
          return json(200, detail())
        }
        if (path.includes('/api/editions?ids='))
          return json(200, { succeeded: [], failed: [{ id: 'e-1', code: 'TENANCY_CONFLICT_007', detail: 'in use' }] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useEditions()
    await page.load()
    expect(page.items.value.map(e => e.code)).toEqual(['free'])
    expect(page.groups.value.map(g => [g.package, g.items.length])).toEqual([['access', 1], ['identity', 2]])

    await page.select('e-1')
    expect(page.edit.name).toBe('Free')
    expect(page.edit.isDefault).toBe(true)
    expect([...page.draft.value]).toEqual(['identity.users.read'])
    expect(page.dirty.value).toBe(false)

    page.setGroup('identity', true)
    page.togglePermission('access.roles.read')
    page.togglePermission('identity.users.read')
    expect(page.dirty.value).toBe(true)
    expect(await page.savePermissions()).toBe(true)
    expect(calls.find(c => c.url.endsWith(':set-permissions'))?.body).toEqual({ permissions: ['access.roles.read', 'identity.users.create'] })
    expect(page.notice.value).toBe('permissions_saved')
    expect(page.dirty.value).toBe(false)
    expect(page.items.value[0]?.permissionCount).toBe(2)

    expect(await page.remove()).toBe(false)
    expect(calls.find(c => c.method === 'DELETE')?.url).toContain('/api/editions?ids=e-1')
    expect(page.detailError.value).toBe('TENANCY_CONFLICT_007')
    expect(page.selected.value?.edition.id).toBe('e-1')
  })

  it('creates with an empty whitelist and hands validation errors to the fields', async () => {
    const bodies: unknown[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (_url: RequestInfo | URL, init?: RequestInit) => {
        bodies.push(init?.body ? JSON.parse(String(init.body)) : undefined)
        return json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['pattern'] } }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })
    const page = useEditions()
    page.form.code = 'Bad Code'
    page.form.name = 'Bad'
    expect(await page.submit()).toBe(false)
    expect(bodies[0]).toEqual({ code: 'Bad Code', name: 'Bad', description: null, isDefault: false, order: 0, permissions: [] })
    expect(page.fieldError('code')).toBe('pattern')
    expect(page.submitError.value).toBeNull()
  })
})
