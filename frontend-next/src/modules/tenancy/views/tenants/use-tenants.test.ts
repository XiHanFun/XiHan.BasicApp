import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useTenants } from './use-tenants'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const tenant = { id: 't-1', tenantId: 'contoso', name: 'Contoso', description: null, status: 'Active', expiresAt: null, adminUserId: null, editionId: 'e-1', order: 10, createdAt: '2026-10-28T00:00:00Z' }
const editionRows = [
  { id: 'e-1', code: 'free', name: 'Free', description: null, isDefault: true, order: 10, permissionCount: 6, tenantCount: 1, createdAt: '2026-10-28T00:00:00Z' },
  { id: 'e-4', code: 'enterprise', name: 'Enterprise', description: null, isDefault: false, order: 40, permissionCount: 26, tenantCount: 0, createdAt: '2026-10-28T00:00:00Z' },
]

describe('useTenants', () => {
  it('loads the list, selects a tenant, initializes its administrator, suspends it and reports a refused delete', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    let adminUserId: string | null = null
    let status = 'Active'
    let editionId: string | null = 'e-1'
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        const current = { ...tenant, adminUserId, status, editionId }
        if (path.endsWith('/api/editions'))
          return json(200, { items: editionRows })
        if (path.endsWith('/api/tenants/t-1:change-edition')) {
          editionId = body.editionId
          return json(200, { ...current, editionId })
        }
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
    await Promise.all([page.load(), page.loadEditions()])
    expect(page.items.value.map(r => r.tenantId)).toEqual(['contoso'])
    expect(page.editionName('e-1')).toBe('Free')
    expect(page.editionName(null)).toBeNull()

    await page.select('t-1')
    expect(page.selected.value?.adminUserName).toBeNull()
    expect(page.edit.name).toBe('Contoso')
    expect(page.edit.order).toBe('10')
    expect(page.editionDraft.value).toBe('e-1')

    // 换套餐：选企业版；再清空即不受套餐限制（请求里是 null）
    page.editionDraft.value = 'e-4'
    expect(await page.changeEdition()).toBe(true)
    expect(calls.filter(c => c.url.endsWith(':change-edition')).at(-1)?.body).toEqual({ editionId: 'e-4' })
    expect(page.selected.value?.tenant.editionId).toBe('e-4')
    expect(page.notice.value).toBe('edition_changed')
    page.editionDraft.value = ''
    expect(await page.changeEdition()).toBe(true)
    expect(calls.filter(c => c.url.endsWith(':change-edition')).at(-1)?.body).toEqual({ editionId: null })
    expect(page.selected.value?.tenant.editionId).toBeNull()

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
