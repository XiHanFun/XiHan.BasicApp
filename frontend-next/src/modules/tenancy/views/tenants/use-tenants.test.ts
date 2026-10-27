import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useTenants } from './use-tenants'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const tenant = { id: 't-1', tenantId: 'contoso', name: 'Contoso', description: null, status: 'Active', expiresAt: null, adminUserId: null, order: 10, createdAt: '2026-10-28T00:00:00Z' }

describe('useTenants', () => {
  it('loads the list, selects a tenant, initializes its administrator, suspends it and reports a refused delete', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    let adminUserId: string | null = null
    let status = 'Active'
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        const current = { ...tenant, adminUserId, status }
        if (path.endsWith('/api/tenants:list'))
          return json(200, { items: [current], total: 1, page: { index: 1, size: 20 } })
        if (path.endsWith('/api/tenants/t-1'))
          return json(200, { tenant: current, adminUserName: adminUserId ? 'admin' : null, userCount: adminUserId ? 1 : 0 })
        if (path.endsWith('/api/tenants/t-1:initialize-admin')) {
          adminUserId = 'u-1'
          return json(200, { id: 'u-1', userName: body.userName, displayName: body.displayName ?? body.userName, homeTenantId: 'contoso', status: 'Active', createdAt: '2026-10-28T00:00:00Z' })
        }
        if (path.endsWith('/api/tenants:suspend')) {
          status = 'Suspended'
          return json(200, { succeeded: body.ids, failed: [] })
        }
        if (path.includes('/api/tenants?ids='))
          return json(200, { succeeded: [], failed: [{ id: 't-1', code: 'TENANCY_CONFLICT_003', detail: 'has users' }] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useTenants(20)
    await page.load()
    expect(page.items.value.map(r => r.tenantId)).toEqual(['contoso'])

    await page.select('t-1')
    expect(page.selected.value?.adminUserName).toBeNull()
    expect(page.edit.name).toBe('Contoso')
    expect(page.edit.order).toBe('10')

    page.adminForm.userName = 'admin'
    page.adminForm.password = 'Admin@123456'
    expect(await page.initializeAdmin()).toBe(true)
    const initialize = calls.find(c => c.url.endsWith(':initialize-admin'))
    expect(initialize?.body).toEqual({ userName: 'admin', password: 'Admin@123456', displayName: null })
    expect(page.selected.value?.adminUserName).toBe('admin')
    expect(page.notice.value).toBe('admin_created')

    expect(await page.suspend()).toBe(true)
    expect(calls.find(c => c.url.endsWith(':suspend'))?.body).toEqual({ ids: ['t-1'] })
    expect(page.selected.value?.tenant.status).toBe('Suspended')

    expect(await page.remove()).toBe(false)
    expect(calls.find(c => c.method === 'DELETE')?.url).toContain('/api/tenants?ids=t-1')
    expect(page.detailError.value).toBe('TENANCY_CONFLICT_003')
    expect(page.selected.value?.tenant.id).toBe('t-1')
  })

  it('hands creation validation errors to the fields', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { tenantId: ['pattern'] } }, 'application/problem+json')) as unknown as typeof fetch,
    })
    const page = useTenants(20)
    page.form.tenantId = 'bad id!'
    page.form.name = 'x'
    expect(await page.submit()).toBe(false)
    expect(page.fieldError('tenantId')).toBe('pattern')
    expect(page.submitError.value).toBeNull()
  })
})
