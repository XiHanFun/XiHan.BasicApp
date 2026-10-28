import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { buildDictItemTree, dictItemParentOptions, dictItemsResource, dictsResource, findDictItem, useDictEditor, useDictItemEditor } from './use-dicts'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function dict(id: string, code: string, isPlatform: boolean, extra: Record<string, unknown> = {}) {
  return { id, ownerTenantId: isPlatform ? 'platform' : 'acme', isPlatform, code, name: code, category: null, description: null, isBuiltIn: false, status: 'Active', order: 0, createdAt: '2026-10-28T00:00:00Z', ...extra }
}

function item(id: string, code: string, parentId: null | string, depth: number, extra: Record<string, unknown> = {}) {
  return { id, dictId: 'd-1', parentId, code, name: code, value: null, description: null, metadata: null, isDefault: false, status: 'Active', order: 0, depth, createdAt: '2026-10-28T00:00:00Z', ...extra }
}

// 深度优先平铺：华东 › 上海 › 浦东，华北
const flatItems = [item('i-1', 'east', null, 0), item('i-2', 'shanghai', 'i-1', 1), item('i-3', 'pudong', 'i-2', 2), item('i-4', 'north', null, 0)]

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

describe('dictsResource', () => {
  it('把 SchemaPage 的查询换成 dicts.list 的分页请求，结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [dict('d-1', 'region', false)], total: 21, page: { index: 2, size: 20 } }))
    const result = await dictsResource().page!({
      page: 2,
      pageSize: 20,
      filters: { keyword: ' reg ', owner: 'tenant', status: 'Active', isBuiltIn: 'false', category: 'geo' },
      sorts: [{ field: 'code', order: 'desc' }],
    })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/dicts:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'reg',
      filters: [
        { field: 'owner', operator: 'eq', value: 'tenant' },
        { field: 'status', operator: 'eq', value: 'Active' },
        { field: 'isBuiltIn', operator: 'eq', value: 'false' },
        { field: 'category', operator: 'eq', value: 'geo' },
      ],
      sorts: [{ field: 'code', descending: true }],
    })
    expect(result.items.map(d => d.code)).toEqual(['region'])
    expect(result.page).toMatchObject({ totalCount: 21, pageIndex: 2, totalPages: 2, startRecord: 21, endRecord: 21, isLastPage: true })
  })

  it('删除被后端逐项拒绝时带着错误码抛出；启停整体覆盖更新；导入把记录换成新建请求', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'd-1', code: 'SETTINGS_CONFLICT_004' }] })
      if (call.method === 'GET')
        return json(200, dict('d-1', 'region', false, { category: 'geo', description: 'Regions', order: 3 }))
      if (call.method === 'PUT')
        return json(200, dict('d-1', 'region', false, { status: 'Disabled' }))
      return json(201, dict('d-2', 'color', false))
    })
    const resource = dictsResource()
    await expect(resource.remove!('d-1')).rejects.toThrow('SETTINGS_CONFLICT_004')
    expect(calls[0]!.url).toContain('/api/dicts?ids=d-1')

    await resource.updateStatus!('d-1', false)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({ url: '/api/dicts/d-1', body: { name: 'region', category: 'geo', description: 'Regions', order: 3, status: 'Disabled' } })

    await resource.create!({ code: ' color ', name: 'Color', category: '', order: 5 })
    expect(calls.find(c => c.method === 'POST')!.body).toEqual({ code: 'color', name: 'Color', category: null, description: null, order: 5 })
  })
})

describe('dictItemsResource', () => {
  it('按 parentId 把深度优先的平铺项挂成树，可维护时带删除、启停与导入', async () => {
    const calls = record((call) => {
      if (call.method === 'GET' && call.url.includes('dictId=d-1'))
        return json(200, { items: flatItems })
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'i-1', code: 'SETTINGS_CONFLICT_005' }] })
      if (call.method === 'GET')
        return json(200, item('i-2', 'shanghai', 'i-1', 1, { metadata: '{"short":"SH"}', isDefault: true }))
      if (call.method === 'PUT')
        return json(200, item('i-2', 'shanghai', 'i-1', 1, { status: 'Disabled' }))
      return json(201, item('i-5', 'west', null, 0))
    })
    const resource = dictItemsResource('d-1', true)
    const tree = await resource.tree!({ page: 1, pageSize: 20, filters: {} })
    expect(tree.map(n => n.code)).toEqual(['east', 'north'])
    expect(tree[0]!.children!.map(n => n.code)).toEqual(['shanghai'])
    expect(tree[0]!.children![0]!.children!.map(n => n.code)).toEqual(['pudong'])
    expect(tree[1]!.children).toBeUndefined()

    await expect(resource.remove!('i-1')).rejects.toThrow('SETTINGS_CONFLICT_005')

    await resource.updateStatus!('i-2', false)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({ url: '/api/dict-items/i-2', body: { name: 'shanghai', parentId: 'i-1', metadata: '{"short":"SH"}', isDefault: true, status: 'Disabled' } })

    await resource.create!({ code: 'west', name: 'West', value: '', isDefault: true, order: 2 })
    expect(calls.find(c => c.method === 'POST')).toMatchObject({ url: '/api/dict-items', body: { dictId: 'd-1', code: 'west', name: 'West', value: null, description: null, isDefault: true, order: 2 } })
  })

  it('另一侧的字典只带取数：没有删除、启停与导入入口', () => {
    const resource = dictItemsResource('d-1', false)
    expect(resource.tree).toBeTypeOf('function')
    expect(resource.remove).toBeUndefined()
    expect(resource.updateStatus).toBeUndefined()
    expect(resource.create).toBeUndefined()
  })

  it('上级只能选不是自己、也不是自己下级的项；树里按主键能找回节点', () => {
    const tree = buildDictItemTree(flatItems as never)
    const options = dictItemParentOptions(tree, 'i-2')
    expect(options.map(o => o.value)).toEqual(['i-1', 'i-4'])
    expect(options[0]!.children).toBeUndefined()
    expect(dictItemParentOptions(tree)[0]!.children![0]!.children![0]).toMatchObject({ value: 'i-3', label: 'pudong · pudong' })
    expect(findDictItem(tree, 'i-3')?.code).toBe('pudong')
    expect(findDictItem(tree, null)).toBeUndefined()
  })
})

describe('useDictEditor', () => {
  it('另一侧的字典只读；新建发编码，编辑整体覆盖并带状态，保存成功关掉弹窗', async () => {
    const calls = record(call => json(call.method === 'POST' ? 201 : 200, dict('d-1', 'region', false)))
    const tenant = useDictEditor(() => false)
    expect(tenant.isOwn(dict('d-0', 'gender', true) as never)).toBe(false)
    expect(tenant.isOwn(dict('d-1', 'region', false) as never)).toBe(true)
    expect(tenant.isOwn(null)).toBe(false)

    tenant.startCreate()
    expect(tenant.mode.value).toBe('create')
    Object.assign(tenant.form, { code: ' region ', name: 'Region', category: ' ', order: null })
    expect(await tenant.submit()).toBe(true)
    expect(tenant.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/dicts', body: { code: 'region', name: 'Region', category: null, description: null, order: 0 } })

    tenant.startEdit(dict('d-1', 'region', false, { category: 'geo', order: 4 }) as never)
    expect(tenant.isEdit.value).toBe(true)
    expect(tenant.form).toMatchObject({ code: 'region', category: 'geo', order: 4, status: 'Active' })
    tenant.form.status = 'Disabled'
    expect(await tenant.submit()).toBe(true)
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/dicts/d-1' })
    expect(calls[1]!.body).toEqual({ name: 'region', category: 'geo', description: null, order: 4, status: 'Disabled' })
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, { code: 'KERNEL_VALIDATION_000', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['code taken'] } }, 'application/problem+json'))
    const editor = useDictEditor(() => true)
    editor.startCreate()
    Object.assign(editor.form, { code: 'gender', name: 'Gender' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('code')).toBe('code taken')

    record(() => json(409, { code: 'SETTINGS_CONFLICT_002', status: 409, detail: 'code taken', traceId: 'x' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('code taken')
  })
})

describe('useDictItemEditor', () => {
  it('新建挂到选中的上级，编辑时上级下拉去掉自己与下级，元数据的字段错误挂回字段', async () => {
    const calls = record((call) => {
      if (call.method === 'PUT')
        return json(400, { code: 'SETTINGS_VALIDATION_003', status: 400, detail: 'not json', traceId: 'x', errors: { metadata: ['not json'] } }, 'application/problem+json')
      return json(201, item('i-5', 'hangzhou', 'i-1', 1))
    })
    const tree = buildDictItemTree(flatItems as never)
    const editor = useDictItemEditor()

    editor.startCreate(dict('d-1', 'region', false) as never, tree)
    expect(editor.parentOptions.value.map(o => o.value)).toEqual(['i-1', 'i-4'])
    Object.assign(editor.form, { code: 'hangzhou', name: 'Hangzhou', parentId: 'i-1', order: 2 })
    expect(await editor.submit()).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/dict-items' })
    expect(calls[0]!.body).toEqual({ dictId: 'd-1', code: 'hangzhou', name: 'Hangzhou', parentId: 'i-1', value: null, description: null, metadata: null, isDefault: false, order: 2 })

    editor.startEdit(findDictItem(tree, 'i-2')!, tree)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({ code: 'shanghai', parentId: 'i-1', metadata: '' })
    expect(editor.parentOptions.value.map(o => o.value)).toEqual(['i-1', 'i-4'])
    expect(editor.parentOptions.value[0]!.children).toBeUndefined()
    Object.assign(editor.form, { parentId: 'i-4', metadata: '{bad' })
    expect(await editor.submit()).toBe(false)
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/dict-items/i-2', body: { parentId: 'i-4', metadata: '{bad', status: 'Active' } })
    expect(editor.fieldError('metadata')).toBe('not json')
    expect(editor.open.value).toBe(true)
  })
})
