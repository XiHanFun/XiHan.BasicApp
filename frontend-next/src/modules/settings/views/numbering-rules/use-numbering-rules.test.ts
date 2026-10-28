import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useNumberingRules } from './use-numbering-rules'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const order = { id: 'r-1', ownerTenantId: 'platform', isPlatform: true, code: 'order', name: 'Order', prefix: 'ORD', separator: '-', dateFormat: 'None', serialLength: 4, resetCycle: 'Never', timeZoneId: 'UTC', template: 'ORD-0000', currentValue: 3, currentPeriod: 'never', hasAllocated: true, allowTenantUse: true, status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z', modifiedAt: null }

describe('useNumberingRules', () => {
  it('loads allocations on select, freezes the format of an issued rule, previews from the edit form and resets', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        if (path.endsWith('/api/numbering-rules:list'))
          return json(200, { items: [order], total: 1, page: { index: 1, size: 20 } })
        if (path.endsWith('/api/numbering-rules/r-1:allocations'))
          return json(200, { items: [{ id: 'a-1', requestTenantId: 'platform', idempotencyKey: 'k1', count: 3, startValue: 1, endValue: 3, periodKey: 'never', firstNumber: 'ORD-0001', lastNumber: 'ORD-0003', businessType: null, businessId: null, generatedAt: '2026-10-28T00:00:00Z' }], total: 1, page: { index: 1, size: 10 } })
        if (path.includes('/api/numbering-rules:preview'))
          return json(200, { numbers: ['ORD-0008', 'ORD-0009'], periodKey: 'never', dateText: null })
        if (path.endsWith('/api/numbering-rules/r-1:reset'))
          return json(200, { ...order, currentValue: 99 })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useNumberingRules(() => true, 20)
    await page.load()
    await page.select('r-1')
    expect(page.editable.value).toBe(true)
    expect(page.formatFrozen.value).toBe(true)
    expect(page.allocations.value.map(a => a.firstNumber)).toEqual(['ORD-0001'])
    expect(page.edit.prefix).toBe('ORD')

    page.preview.startValue = '8'
    page.preview.count = '2'
    expect(await page.runPreview()).toBe(true)
    expect(page.previewNumbers.value).toEqual(['ORD-0008', 'ORD-0009'])
    const preview = calls.find(c => c.url.includes(':preview'))!
    expect(preview.method).toBe('GET')
    expect(preview.url).toContain('startValue=8')
    expect(preview.url).toContain('count=2')
    expect(preview.url).toContain('prefix=ORD')

    page.reset.nextValue = '100'
    page.reset.reason = 'migration'
    page.reset.confirmCode = 'order'
    expect(await page.runReset()).toBe(true)
    expect(calls.find(c => c.url.endsWith(':reset'))?.body).toEqual({ nextValue: '100', reason: 'migration', confirmCode: 'order' })
    expect(page.notice.value).toBe('reset')
  })

  it('keeps a shared platform rule read-only for a tenant and surfaces creation errors', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL) => {
        const path = String(url)
        if (path.endsWith('/api/numbering-rules:list'))
          return json(200, { items: [order], total: 1, page: { index: 1, size: 20 } })
        if (path.endsWith(':allocations'))
          return json(200, { items: [], total: 0, page: { index: 1, size: 10 } })
        return json(400, { code: 'SETTINGS_VALIDATION_005', status: 400, detail: 'unsafe', traceId: 'x' }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })
    const page = useNumberingRules(() => false, 20)
    await page.load()
    await page.select('r-1')
    expect(page.editable.value).toBe(false)
    page.form.code = 'bad'
    page.form.name = 'Bad'
    page.form.dateFormat = 'YyyyMM'
    expect(await page.submit()).toBe(false)
    expect(page.submitError.value).toBe('unsafe')
  })
})
