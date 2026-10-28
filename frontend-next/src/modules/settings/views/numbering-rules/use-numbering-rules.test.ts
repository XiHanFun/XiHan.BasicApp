import { describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { configureRequest } from '~/request'
import { datePattern, numberingRulesResource, useNumberingAllocations, useNumberingPreview, useNumberingReset, useNumberingRuleEditor } from './use-numbering-rules'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function rule(id: string, code: string, isPlatform: boolean, extra: Record<string, unknown> = {}) {
  return { id, ownerTenantId: isPlatform ? 'platform' : 'acme', isPlatform, code, name: code, prefix: 'ORD', separator: '-', dateFormat: 'None', serialLength: 4, resetCycle: 'Never', timeZoneId: 'UTC', template: 'ORD-0000', currentValue: 3, currentPeriod: 'never', hasAllocated: false, allowTenantUse: false, status: 'Active', order: 0, description: null, createdAt: '2026-10-28T00:00:00Z', modifiedAt: null, ...extra }
}

function allocation(id: string, first: string) {
  return { id, requestTenantId: 'platform', idempotencyKey: `k-${id}`, count: 1, startValue: 1, endValue: 1, periodKey: 'never', firstNumber: first, lastNumber: first, businessType: null, businessId: null, generatedAt: '2026-10-28T00:00:00Z' }
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Promise<Response> | Response): Call[] {
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

function problem(status: number, code: string, detail: string, errors?: Record<string, string[]>): Response {
  return json(status, { code, status, detail, traceId: 'x', ...(errors ? { errors } : {}) }, 'application/problem+json')
}

describe('numberingRulesResource', () => {
  it('把 SchemaPage 的查询换成 numbering-rules.list 的分页请求，结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [rule('r-1', 'order', true)], total: 1, page: { index: 1, size: 20 } }))
    const result = await numberingRulesResource().page!({ page: 1, pageSize: 20, filters: { keyword: ' ord ', owner: 'platform', status: 'Active', resetCycle: 'Never' }, sorts: [{ field: 'order', order: 'asc' }, { field: 'code', order: 'desc' }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/numbering-rules:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'ord',
      filters: [
        { field: 'owner', operator: 'eq', value: 'platform' },
        { field: 'status', operator: 'eq', value: 'Active' },
        { field: 'resetCycle', operator: 'eq', value: 'Never' },
      ],
      sorts: [{ field: 'order', descending: false }, { field: 'code', descending: true }],
    })
    expect(result.items.map(r => r.code)).toEqual(['order'])
    expect(result.page).toMatchObject({ totalCount: 1, totalPages: 1, isFirstPage: true, isLastPage: true })
  })

  it('发过号的规则删除被拒时带着错误码抛出；启停把格式原样带回整体覆盖；导入按新建弹窗的缺省值兜底', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'r-1', code: 'SETTINGS_CONFLICT_009' }] })
      if (call.method === 'GET')
        return json(200, rule('r-1', 'order', true, { hasAllocated: true, dateFormat: 'YyyyMMdd', resetCycle: 'Daily', allowTenantUse: true, order: 2 }))
      if (call.method === 'PUT')
        return json(200, rule('r-1', 'order', true, { status: 'Disabled' }))
      return json(201, rule('r-2', 'invoice', false))
    })
    const resource = numberingRulesResource()
    await expect(resource.remove!('r-1')).rejects.toThrow('SETTINGS_CONFLICT_009')
    expect(calls[0]!.url).toContain('/api/numbering-rules?ids=r-1')

    await resource.updateStatus!('r-1', false)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({ url: '/api/numbering-rules/r-1' })
    expect(calls.find(c => c.method === 'PUT')!.body).toEqual({ name: 'order', prefix: 'ORD', separator: '-', dateFormat: 'YyyyMMdd', serialLength: 4, resetCycle: 'Daily', timeZoneId: 'UTC', allowTenantUse: true, order: 2, description: null, status: 'Disabled' })

    await resource.create!({ code: ' invoice ', name: 'Invoice', prefix: 'INV', resetCycle: 'Monthly' })
    expect(calls.find(c => c.method === 'POST')!.body).toEqual({ code: 'invoice', name: 'Invoice', prefix: 'INV', separator: '-', dateFormat: 'YyyyMMdd', serialLength: 4, resetCycle: 'Monthly', timeZoneId: 'UTC', allowTenantUse: false, order: 0, description: null })
  })

  it('日期格式在编号里的样子', () => {
    expect(datePattern('YyyyMMdd')).toBe('yyyyMMdd')
    expect(datePattern('YyMM')).toBe('yyMM')
    expect(datePattern('None')).toBe('')
  })
})

describe('useNumberingRuleEditor', () => {
  it('另一侧的规则只读；租户新建的规则不供租户使用，编辑发过号的规则格式冻结并带状态', async () => {
    const calls = record(call => json(call.method === 'POST' ? 201 : 200, rule('r-2', 'invoice', false)))
    const tenant = useNumberingRuleEditor(() => false)
    expect(tenant.isOwn(rule('r-1', 'order', true) as never)).toBe(false)
    expect(tenant.isOwn(rule('r-2', 'invoice', false) as never)).toBe(true)

    tenant.startCreate()
    expect(tenant.formatFrozen.value).toBe(false)
    Object.assign(tenant.form, { code: ' invoice ', name: 'Invoice', prefix: ' ', timeZoneId: ' ', allowTenantUse: true, serialLength: null, order: null })
    expect(await tenant.submit()).toBe(true)
    expect(tenant.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/numbering-rules' })
    expect(calls[0]!.body).toEqual({ code: 'invoice', name: 'Invoice', prefix: null, separator: '-', dateFormat: 'YyyyMMdd', serialLength: 4, resetCycle: 'Daily', timeZoneId: 'UTC', allowTenantUse: false, order: 0, description: null })

    tenant.startEdit(rule('r-2', 'invoice', false, { hasAllocated: true, description: 'Invoices' }) as never)
    expect(tenant.isEdit.value).toBe(true)
    expect(tenant.formatFrozen.value).toBe(true)
    expect(tenant.form).toMatchObject({ code: 'invoice', prefix: 'ORD', description: 'Invoices', status: 'Active' })
    tenant.form.status = 'Disabled'
    expect(await tenant.submit()).toBe(true)
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/numbering-rules/r-2' })
    expect(calls[1]!.body).toMatchObject({ name: 'invoice', prefix: 'ORD', dateFormat: 'None', status: 'Disabled', description: 'Invoices', allowTenantUse: false })
  })

  it('平台规则可以供租户使用；按表单里的格式预览一个号（GET，不发号）', async () => {
    const calls = record(call => call.url.includes(':preview')
      ? json(200, { numbers: ['ORD-20261028-0001'], periodKey: '2026-10-28', dateText: '20261028' })
      : json(201, rule('r-3', 'order', true)))
    const platform = useNumberingRuleEditor(() => true)
    platform.startCreate()
    Object.assign(platform.form, { code: 'order', name: 'Order', prefix: 'ORD', allowTenantUse: true })
    await platform.runPreview()
    expect(platform.previewNumber.value).toBe('ORD-20261028-0001')
    const preview = calls[0]!
    expect(preview.method).toBe('GET')
    expect(preview.url).toContain('/api/numbering-rules:preview?')
    expect(preview.url).toContain('prefix=ORD')
    expect(preview.url).toContain('dateFormat=YyyyMMdd')
    expect(preview.url).toContain('startValue=1')
    expect(preview.url).toContain('count=1')

    expect(await platform.submit()).toBe(true)
    expect(calls[1]!.body).toMatchObject({ code: 'order', allowTenantUse: true })
    platform.startCreate()
    expect(platform.previewNumber.value).toBe('')
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => problem(400, 'KERNEL_VALIDATION_000', 'invalid', { timeZoneId: ['unknown zone'] }))
    const editor = useNumberingRuleEditor(() => true)
    editor.startCreate()
    Object.assign(editor.form, { code: 'bad', name: 'Bad', timeZoneId: 'Mars/Base' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('timeZoneId')).toBe('unknown zone')

    record(() => problem(400, 'SETTINGS_VALIDATION_005', 'unsafe'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('unsafe')
  })
})

describe('useNumberingReset', () => {
  it('预填当前流水 + 1；平台规则要回填编码，租户规则不带确认码', async () => {
    const calls = record(() => json(200, rule('r-1', 'order', true, { currentValue: 99 })))
    const reset = useNumberingReset()
    reset.start(rule('r-1', 'order', true, { currentValue: 41 }) as never)
    expect(reset.open.value).toBe(true)
    expect(reset.needsConfirm.value).toBe(true)
    expect(reset.form.nextValue).toBe('42')
    Object.assign(reset.form, { nextValue: ' 100 ', reason: ' migration ', confirmCode: ' order ' })
    expect(await reset.submit()).toBe(true)
    expect(reset.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/numbering-rules/r-1:reset', body: { nextValue: '100', reason: 'migration', confirmCode: 'order' } })

    reset.start(rule('r-2', 'invoice', false, { currentValue: 0 }) as never)
    expect(reset.needsConfirm.value).toBe(false)
    expect(reset.form.nextValue).toBe('1')
    Object.assign(reset.form, { reason: 'jump', confirmCode: 'ignored' })
    expect(await reset.submit()).toBe(true)
    expect(calls[1]!.body).toEqual({ nextValue: '1', reason: 'jump', confirmCode: null })
  })

  it('越界与回退的错误挂回字段或给整条提示', async () => {
    record(() => problem(400, 'KERNEL_VALIDATION_000', 'invalid', { nextValue: ['too low'] }))
    const reset = useNumberingReset()
    reset.start(rule('r-1', 'order', true) as never)
    expect(await reset.submit()).toBe(false)
    expect(reset.fieldError('nextValue')).toBe('too low')
    expect(reset.open.value).toBe(true)

    record(() => problem(400, 'SETTINGS_VALIDATION_008', 'type the rule code'))
    expect(await reset.submit()).toBe(false)
    expect(reset.submitError.value).toBe('type the rule code')
  })
})

describe('useNumberingPreview', () => {
  it('单个预览只拼一个号，批量按个数拼出连续区间；起始流水与个数先在本地校验', async () => {
    const calls = record((call) => {
      const count = Number(new URL(call.url, 'http://x').searchParams.get('count'))
      return json(200, { numbers: Array.from({ length: count }, (_, i) => `ORD-${String(i + 8).padStart(4, '0')}`), periodKey: 'never', dateText: null })
    })
    const preview = useNumberingPreview()
    preview.load(rule('r-1', 'order', true) as never)

    preview.startValue.value = '08'
    expect(await preview.execute()).toBe('invalid_start')
    preview.startValue.value = '8'
    expect(await preview.execute()).toBe('done')
    expect(preview.rows.value).toEqual([{ sequence: 1, number: 'ORD-0008' }])
    expect(preview.metadata.value).toEqual({ periodKey: 'never', dateText: null, startValue: '8', endValue: '8' })
    expect(calls[0]!.url).toContain('count=1')

    preview.mode.value = 'batch'
    await nextTick()
    // 条件一变旧结果即清空
    expect(preview.metadata.value).toBeNull()
    preview.count.value = 51
    expect(await preview.execute()).toBe('invalid_count')
    preview.count.value = 3
    expect(await preview.execute()).toBe('done')
    expect(preview.rows.value.map(r => r.number)).toEqual(['ORD-0008', 'ORD-0009', 'ORD-0010'])
    expect(preview.metadata.value).toMatchObject({ startValue: '8', endValue: '10' })
    expect(calls).toHaveLength(2)
    expect(calls[1]!.url).toContain('startValue=8')
    expect(calls[1]!.url).toContain('prefix=ORD')
  })

  it('期间换了规则：旧响应作废，不回写结果', async () => {
    const pending: Array<(response: Response) => void> = []
    record(() => new Promise<Response>((resolve) => {
      pending.push(resolve)
    }))
    const preview = useNumberingPreview()
    preview.load(rule('r-1', 'order', true) as never)
    const running = preview.execute()
    await vi.waitFor(() => expect(pending).toHaveLength(1))
    preview.load(rule('r-2', 'invoice', false) as never)
    pending[0]!(json(200, { numbers: ['ORD-0001'], periodKey: 'never', dateText: null }))
    expect(await running).toBe('stale')
    expect(preview.rows.value).toEqual([])
    expect(preview.rule.value?.code).toBe('invoice')
  })
})

describe('useNumberingAllocations', () => {
  it('打开即取第 1 页，关键字与翻页都下发后端；失败原样抛出', async () => {
    const calls = record((call) => {
      const body = call.body as { page: { index: number }, keyword: null | string }
      if (body.keyword === 'boom')
        return problem(404, 'SETTINGS_NOTFOUND_004', 'gone')
      return json(200, { items: [allocation(`a-${body.page.index}`, `ORD-000${body.page.index}`)], total: 12, page: { index: body.page.index, size: 10 } })
    })
    const allocations = useNumberingAllocations()
    await allocations.open(rule('r-1', 'order', true) as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/numbering-rules/r-1:allocations', body: { page: { index: 1, size: 10 }, keyword: null } })
    expect(allocations.items.value.map(a => a.firstNumber)).toEqual(['ORD-0001'])
    expect(allocations.total.value).toBe(12)

    await allocations.changePage(2)
    expect(calls[1]!.body).toEqual({ page: { index: 2, size: 10 }, keyword: null })

    allocations.keyword.value = ' k-a '
    await allocations.search()
    expect(calls[2]!.body).toEqual({ page: { index: 1, size: 10 }, keyword: 'k-a' })
    expect(allocations.loading.value).toBe(false)

    allocations.keyword.value = 'boom'
    await expect(allocations.search()).rejects.toThrow('gone')
    expect(allocations.loading.value).toBe(false)
  })

  it('快速切换规则时旧规则的响应不覆盖新结果', async () => {
    const pending: Array<(response: Response) => void> = []
    record(() => new Promise<Response>((resolve) => {
      pending.push(resolve)
    }))
    const allocations = useNumberingAllocations()
    const first = allocations.open(rule('r-1', 'order', true) as never)
    const second = allocations.open(rule('r-2', 'invoice', false) as never)
    await vi.waitFor(() => expect(pending).toHaveLength(2))
    pending[1]!(json(200, { items: [allocation('a-2', 'INV-0001')], total: 1, page: { index: 1, size: 10 } }))
    await second
    pending[0]!(json(200, { items: [allocation('a-1', 'ORD-0001')], total: 1, page: { index: 1, size: 10 } }))
    await first
    expect(allocations.items.value.map(a => a.firstNumber)).toEqual(['INV-0001'])
  })
})
