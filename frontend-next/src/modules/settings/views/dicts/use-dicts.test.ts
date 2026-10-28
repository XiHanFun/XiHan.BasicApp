import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useDicts } from './use-dicts'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

const region = { id: 'd-1', ownerTenantId: 'acme', isPlatform: false, code: 'region', name: 'Region', category: null, description: null, isBuiltIn: false, status: 'Active', order: 0, createdAt: '2026-10-28T00:00:00Z' }

function item(id: string, code: string, parentId: null | string, depth: number) {
  return { id, dictId: 'd-1', parentId, code, name: code, value: null, description: null, metadata: null, isDefault: false, status: 'Active', order: 0, depth, createdAt: '2026-10-28T00:00:00Z' }
}

describe('useDicts', () => {
  it('loads the items of the selected dictionary and offers only valid parents', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method: init?.method ?? 'GET', url: path, body })
        if (path.endsWith('/api/dicts:list'))
          return json(200, { items: [region], total: 1, page: { index: 1, size: 20 } })
        if (path.endsWith('/api/dict-items?dictId=d-1'))
          return json(200, { items: [item('i-1', 'east', null, 0), item('i-2', 'shanghai', 'i-1', 1), item('i-3', 'pudong', 'i-2', 2), item('i-4', 'north', null, 0)] })
        if (path.endsWith('/api/dict-items') && init?.method === 'POST')
          return json(201, item('i-5', body.code, body.parentId, 1))
        if (path.endsWith('/api/dict-items/i-2') && init?.method === 'PUT')
          return json(200, item('i-2', 'shanghai', body.parentId, 1))
        if (path.includes('/api/dict-items?ids='))
          return json(200, { succeeded: [], failed: [{ id: 'i-1', code: 'SETTINGS_CONFLICT_005' }] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useDicts(() => false, 20)
    await page.load()
    await page.select('d-1')
    expect(page.editable.value).toBe(true)
    expect(page.dictItems.value.map(i => i.code)).toEqual(['east', 'shanghai', 'pudong', 'north'])

    // 选中上海：可选上级去掉自己与下级（浦东）
    page.selectItem('i-2')
    expect(page.parentChoices.value.map(i => i.code)).toEqual(['east', 'north'])
    page.itemEdit.parentId = 'i-4'
    expect(await page.saveItem()).toBe(true)
    expect(calls.find(c => c.method === 'PUT')?.body).toMatchObject({ parentId: 'i-4', name: 'shanghai', isDefault: false, status: 'Active' })
    expect(page.notice.value).toBe('item_saved')

    page.itemForm.code = 'hangzhou'
    page.itemForm.name = 'Hangzhou'
    page.itemForm.parentId = 'i-1'
    expect(await page.submitItem()).toBe(true)
    expect(calls.find(c => c.method === 'POST' && c.url.endsWith('/api/dict-items'))?.body).toMatchObject({ dictId: 'd-1', code: 'hangzhou', parentId: 'i-1', metadata: null })
    expect(page.itemForm.code).toBe('')

    page.selectItem('i-1')
    expect(await page.removeItem()).toBe(false)
    expect(page.detailError.value).toBe('SETTINGS_CONFLICT_005')
  })

  it('keeps a platform dictionary read-only for a tenant', async () => {
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL) => {
        if (String(url).endsWith('/api/dicts:list'))
          return json(200, { items: [{ ...region, isPlatform: true, ownerTenantId: 'platform' }], total: 1, page: { index: 1, size: 20 } })
        return json(200, { items: [] })
      }) as unknown as typeof fetch,
    })
    const page = useDicts(() => false, 20)
    await page.load()
    await page.select('d-1')
    expect(page.editable.value).toBe(false)
  })
})
