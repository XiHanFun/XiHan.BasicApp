import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { configsResource, useConfigEditor } from './use-configs'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function config(id: string, key: string, isPlatform: boolean, extra: Record<string, unknown> = {}) {
  return { id, ownerTenantId: isPlatform ? 'platform' : 'acme', isPlatform, key, name: key, group: null, dataType: 'Text', value: 'v', defaultValue: null, hasValue: true, isEncrypted: false, isBuiltIn: false, allowTenantOverride: isPlatform, status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z', modifiedAt: null, ...extra }
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = { method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('configsResource', () => {
  it('把 SchemaPage 的查询换成 configs.list 的分页请求，结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [config('c-1', 'demo.greeting', true)], total: 1, page: { index: 1, size: 20 } }))
    const result = await configsResource().page!({ page: 1, pageSize: 20, filters: { keyword: ' demo ', status: 'Active', owner: 'platform' }, sorts: [{ field: 'name', order: 'asc' }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/configs:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'demo',
      filters: [{ field: 'status', operator: 'eq', value: 'Active' }, { field: 'owner', operator: 'eq', value: 'platform' }],
      sorts: [{ field: 'name', descending: false }],
    })
    expect(result.items.map(c => c.key)).toEqual(['demo.greeting'])
    expect(result.page.totalCount).toBe(1)
  })

  it('删除被后端逐项拒绝时带着错误码抛出；启停整体覆盖更新，加密参数的值传空即保留', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'c-2', code: 'SETTINGS_CONFLICT_006' }] })
      if (call.method === 'GET')
        return json(200, config('c-2', 'demo.secret', false, { isEncrypted: true, value: null }))
      return json(200, config('c-2', 'demo.secret', false, { status: 'Disabled' }))
    })
    const resource = configsResource()
    await expect(resource.remove!('c-2')).rejects.toThrow('SETTINGS_CONFLICT_006')
    await resource.updateStatus!('c-2', false)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({ url: '/api/configs/c-2', body: { name: 'demo.secret', value: null, isEncrypted: true, status: 'Disabled' } })
  })
})

describe('useConfigEditor', () => {
  it('另一侧的参数只读；租户可按允许覆盖的平台参数预填新建表单', () => {
    const tenant = useConfigEditor(() => false)
    const platformRow = config('c-1', 'demo.greeting', true) as never
    expect(tenant.isOwn(platformRow)).toBe(false)
    expect(tenant.canOverride(platformRow)).toBe(true)
    tenant.startOverride(platformRow)
    expect(tenant.mode.value).toBe('override')
    expect(tenant.form).toMatchObject({ key: 'demo.greeting', name: 'demo.greeting', allowTenantOverride: false })
    expect(useConfigEditor(() => true).canOverride(platformRow)).toBe(false)
  })

  it('编辑加密参数时值留空（发 null 即保留原值），保存成功关掉弹窗', async () => {
    const calls = record(() => json(200, config('c-2', 'demo.secret', false, { isEncrypted: true, value: null })))
    const editor = useConfigEditor(() => false)
    editor.startEdit(config('c-2', 'demo.secret', false, { isEncrypted: true, value: null }) as never)
    expect(editor.form.value).toBe('')
    editor.form.name = 'Secret v2'
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]!.body).toEqual({ name: 'Secret v2', group: null, dataType: 'Text', value: null, defaultValue: null, isEncrypted: true, allowTenantOverride: false, order: 0, description: null, status: 'Active' })
  })

  it('新建的字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, { code: 'SETTINGS_VALIDATION_002', status: 400, detail: 'type mismatch', traceId: 'x', errors: { value: ['not a number'] } }, 'application/problem+json'))
    const editor = useConfigEditor(() => true)
    editor.startCreate()
    Object.assign(editor.form, { key: 'demo.limit', name: 'Limit', dataType: 'Number', value: 'many' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('value')).toBe('not a number')

    record(() => json(409, { code: 'SETTINGS_CONFLICT_001', status: 409, detail: 'key taken', traceId: 'x' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('key taken')
  })
})
