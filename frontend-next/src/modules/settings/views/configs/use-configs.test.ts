import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useConfigs } from './use-configs'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function config(id: string, key: string, isPlatform: boolean, extra: Record<string, unknown> = {}) {
  return { id, ownerTenantId: isPlatform ? 'platform' : 'acme', isPlatform, key, name: key, group: null, dataType: 'Text', value: 'v', defaultValue: null, hasValue: true, isEncrypted: false, isBuiltIn: false, allowTenantOverride: isPlatform, status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z', modifiedAt: null, ...extra }
}

describe('useConfigs', () => {
  it('treats the other side read-only, saves its own entries and prefills an override of a platform entry', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        if (path.endsWith('/api/configs:list'))
          return json(200, { items: [config('c-1', 'demo.greeting', true), config('c-2', 'demo.secret', false, { isEncrypted: true, value: null })], total: 2, page: { index: 1, size: 20 } })
        if (path.endsWith('/api/configs/c-2') && init?.method === 'PUT')
          return json(200, config('c-2', 'demo.secret', false, { isEncrypted: true, value: null, name: body.name }))
        if (path.includes('/api/configs?ids='))
          return json(200, { succeeded: [], failed: [{ id: 'c-2', code: 'SETTINGS_CONFLICT_006' }] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    // 租户视角：平台参数只读
    const page = useConfigs(() => false, 20)
    await page.load()
    expect(page.items.value.map(c => c.key)).toEqual(['demo.greeting', 'demo.secret'])
    page.select('c-1')
    expect(page.editable.value).toBe(false)
    page.overrideFrom(page.selected.value!)
    expect(page.form.key).toBe('demo.greeting')
    expect(page.form.allowTenantOverride).toBe(false)

    // 自己的加密参数：值留空即保留原值（发 null）
    page.select('c-2')
    expect(page.editable.value).toBe(true)
    expect(page.edit.value).toBe('')
    page.edit.name = 'Secret v2'
    expect(await page.save()).toBe(true)
    expect(calls.find(c => c.method === 'PUT')?.body).toEqual({ name: 'Secret v2', group: null, dataType: 'Text', value: null, defaultValue: null, isEncrypted: true, allowTenantOverride: false, order: 0, description: null, status: 'Active' })
    expect(page.notice.value).toBe('saved')

    expect(await page.remove()).toBe(false)
    expect(page.detailError.value).toBe('SETTINGS_CONFLICT_006')
  })

  it('hands creation validation errors to the fields and sends the keyword with the list', async () => {
    const calls: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({ url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
        if (String(url).endsWith('/api/configs:list'))
          return json(200, { items: [], total: 0, page: { index: 1, size: 20 } })
        return json(400, { code: 'SETTINGS_VALIDATION_002', status: 400, detail: 'type mismatch', traceId: 'x' }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })
    const page = useConfigs(() => true, 20)
    page.keyword.value = ' demo '
    await page.load(1)
    expect(calls[0]!.body).toMatchObject({ keyword: 'demo' })
    page.form.key = 'demo.limit'
    page.form.name = 'Limit'
    page.form.dataType = 'Number'
    page.form.value = 'many'
    expect(await page.submit()).toBe(false)
    expect(page.submitError.value).toBe('type mismatch')
    expect(calls[1]!.body).toMatchObject({ key: 'demo.limit', dataType: 'Number', value: 'many', allowTenantOverride: true })
  })
})
