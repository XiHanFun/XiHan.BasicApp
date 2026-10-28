import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { positionsResource, usePositionEditor } from './use-positions'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function position(id: string, code: string, extra: Record<string, unknown> = {}) {
  return { id, code, name: code.toUpperCase(), status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z', ...extra }
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

describe('positionsResource', () => {
  it('把 SchemaPage 的查询换成 positions.list 的分页请求，结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [position('p-1', 'manager')], total: 21, page: { index: 2, size: 20 } }))
    const result = await positionsResource().page!({ page: 2, pageSize: 20, filters: { keyword: ' man ', status: 'Disabled' }, sorts: [{ field: 'code', order: 'desc' }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/positions:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'man',
      filters: [{ field: 'status', operator: 'eq', value: 'Disabled' }],
      sorts: [{ field: 'code', descending: true }],
    })
    expect(result.items.map(p => p.code)).toEqual(['manager'])
    expect(result.page).toMatchObject({ pageIndex: 2, totalCount: 21, totalPages: 2, startRecord: 21, endRecord: 21, hasPrevious: true, hasNext: false })
  })

  it('只登记了状态筛选：别的搜索字段出现即报错，不发请求', async () => {
    const calls = record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await expect(positionsResource().page!({ page: 1, pageSize: 20, filters: { code: 'mgr' } })).rejects.toThrow('code')
    expect(calls).toHaveLength(0)
  })

  it('删除被后端逐项拒绝时带着错误码抛出；启停取当前值整体替换，只改状态', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'p-1', code: 'ORGANIZATION_CONFLICT_004', detail: 'in use' }] })
      if (call.method === 'GET')
        return json(200, position('p-1', 'manager', { order: 5, description: 'Leads a team' }))
      return json(200, position('p-1', 'manager', { status: 'Disabled' }))
    })
    const resource = positionsResource()
    await expect(resource.remove!('p-1')).rejects.toThrow('ORGANIZATION_CONFLICT_004')
    expect(calls[0]!.url).toBe('/api/positions?ids=p-1')
    await resource.updateStatus!('p-1', false)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({ url: '/api/positions/p-1', body: { name: 'MANAGER', order: 5, description: 'Leads a team', status: 'Disabled' } })
  })

  it('导入的记录换成 positions.create：缺省排序为 0、空说明为 null', async () => {
    const calls = record(() => json(201, position('p-2', 'lead')))
    await positionsResource().create!({ code: ' lead ', name: ' Lead ', description: '  ' })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/positions', body: { code: 'lead', name: 'Lead', order: 0, description: null } })
  })
})

describe('usePositionEditor', () => {
  it('新建不带状态；编辑按行预填，编码不改、状态随表单整体更新', async () => {
    const calls = record(call => json(call.method === 'POST' ? 201 : 200, position('p-1', 'manager')))
    const editor = usePositionEditor()
    editor.startCreate()
    expect(editor.isEdit.value).toBe(false)
    Object.assign(editor.form, { code: ' manager ', name: ' Manager ', order: 3, description: ' Leads ' })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/positions', body: { code: 'manager', name: 'Manager', order: 3, description: 'Leads' } })

    editor.startEdit(position('p-1', 'manager', { order: 3, description: 'Leads' }) as never)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({ code: 'manager', name: 'MANAGER', order: 3, description: 'Leads', status: 'Active' })
    editor.form.name = 'Team Manager'
    editor.form.status = 'Disabled'
    expect(await editor.submit()).toBe(true)
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/positions/p-1' })
    expect(calls[1]!.body).toEqual({ name: 'Team Manager', order: 3, description: 'Leads', status: 'Disabled' })
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['pattern'] } }, 'application/problem+json'))
    const editor = usePositionEditor()
    editor.startCreate()
    Object.assign(editor.form, { code: 'bad code!', name: 'x' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('code')).toBe('pattern')
    expect(editor.submitError.value).toBeNull()

    record(() => json(409, { code: 'ORGANIZATION_CONFLICT_003', status: 409, detail: 'code taken', traceId: 'x' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('code taken')
  })
})
