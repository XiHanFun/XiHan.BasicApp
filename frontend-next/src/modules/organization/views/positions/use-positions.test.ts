import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { usePositions } from './use-positions'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const manager = { id: 'p-1', code: 'manager', name: 'Manager', status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z' }

describe('usePositions', () => {
  it('loads the list, selects a position into the edit form, saves it and reports a refused delete', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    let name = manager.name
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        if (path.endsWith('/api/positions:list'))
          return json(200, { items: [{ ...manager, name }], total: 1, page: { index: 1, size: 20 } })
        if (path.endsWith('/api/positions/p-1') && init?.method === 'PUT') {
          name = body.name
          return json(200, { ...manager, name })
        }
        if (path.includes('/api/positions?ids='))
          return json(200, { succeeded: [], failed: [{ id: 'p-1', code: 'ORGANIZATION_CONFLICT_004', detail: 'in use' }] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = usePositions(20)
    await page.load()
    expect(page.items.value.map(p => p.code)).toEqual(['manager'])

    page.select('p-1')
    expect(page.edit.name).toBe('Manager')
    expect(page.edit.status).toBe('Active')

    page.edit.name = 'Team Manager'
    page.edit.status = 'Disabled'
    expect(await page.save()).toBe(true)
    expect(calls.find(c => c.method === 'PUT')?.body).toEqual({ name: 'Team Manager', description: null, order: 0, status: 'Disabled' })
    expect(page.notice.value).toBe('saved')
    expect(page.selected.value?.name).toBe('Team Manager')

    expect(await page.remove()).toBe(false)
    expect(page.detailError.value).toBe('ORGANIZATION_CONFLICT_004')
    expect(page.selected.value?.id).toBe('p-1')
  })

  it('hands creation validation errors to the fields', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['pattern'] } }, 'application/problem+json')) as unknown as typeof fetch,
    })
    const page = usePositions(20)
    page.form.code = 'bad code!'
    page.form.name = 'x'
    expect(await page.submit()).toBe(false)
    expect(page.fieldError('code')).toBe('pattern')
    expect(page.submitError.value).toBeNull()
  })
})
