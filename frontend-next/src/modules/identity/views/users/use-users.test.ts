import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useUsers } from './use-users'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

describe('useUsers', () => {
  it('loads a page and hands validation errors to the form by field', async () => {
    const calls: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ url: String(url), body })
        if (String(url).endsWith('/api/users:list'))
          return json(200, { items: [{ id: '1', userName: 'alice', displayName: 'Alice', email: null, homeTenantId: 'acme', status: 'Active', createdAt: '2026-10-28T00:00:00Z', createdBy: 'root' }], total: 1, page: { index: 1, size: 10 } })
        return json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { userName: ['too short'] } }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useUsers(10)
    await page.load()
    expect(page.items.value).toHaveLength(1)
    expect(page.total.value).toBe(1)
    expect(calls[0]?.body).toEqual({ page: { index: 1, size: 10 }, filters: [], sorts: [{ field: 'createdAt', descending: true }] })

    page.form.userName = 'a'
    page.form.displayName = 'A'
    expect(await page.submit()).toBe(false)
    expect(page.fieldError('userName')).toBe('too short')
    expect(page.submitError.value).toBeNull()
  })
})
