import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { assistantsResource, useAssistantEditor } from './use-assistants'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function detail(extra: Record<string, unknown> = {}) {
  return { id: 'a-1', code: 'doc-helper', name: 'Docs', avatar: null, description: null, greeting: null, promptCode: null, providerCode: null, enableKnowledge: true, knowledgeProviderCode: null, knowledgeTopK: 5, historyRounds: 10, tools: ['users_list'], isDefault: false, status: 'Active', order: 100, remark: null, concurrencyStamp: 's1', createdAt: '2026-11-04T00:00:00Z', modifiedAt: null, ...extra }
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

describe('assistantsResource', () => {
  it('状态多选合成 in，关键字原样下发', async () => {
    const calls = record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await assistantsResource().page!({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' doc ' },
      conditionFilters: [{ field: 'status', operator: 3000, values: ['Disabled'] }],
      sorts: [],
    } as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/ai-assistants:list' })
    expect(calls[0]!.body).toMatchObject({ keyword: 'doc', filters: [{ field: 'status', operator: 'in', value: 'Disabled' }] })
  })

  it('批量删除逐项调用，逐项失败抛出码', async () => {
    const calls = record(() => json(200, { succeeded: [], failed: [{ id: 'x-1', code: 'AI_NOTFOUND_104' }] }))
    await expect(assistantsResource().remove!('x-1')).rejects.toThrow('AI_NOTFOUND_104')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/ai-assistants?ids=x-1' })
    record(() => json(200, { succeeded: ['x-2'], failed: [] }))
    await expect(assistantsResource().remove!('x-2')).resolves.toBeUndefined()
  })
})

describe('useAssistantEditor', () => {
  it('新建带编码与状态，空白的可选项发 null，缺省值照 4.x', async () => {
    const calls = record(() => json(201, detail()))
    const editor = useAssistantEditor()
    editor.startCreate()
    expect(editor.form).toMatchObject({ enableKnowledge: true, knowledgeTopK: 5, historyRounds: 10, order: 100, status: 'Active' })
    editor.form.code = ' doc-helper '
    editor.form.name = ' Docs '
    editor.form.providerCode = '  '
    expect(await editor.submit()).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/ai-assistants' })
    expect(calls[0]!.body).toMatchObject({ code: 'doc-helper', name: 'Docs', status: 'Active', providerCode: null, tools: [] })
    expect(editor.open.value).toBe(false)
  })

  it('编辑先取详情；编码与状态不发，带上读到的并发戳', async () => {
    const calls = record(call => json(200, call.method === 'GET' ? detail() : detail({ concurrencyStamp: 's2' })))
    const editor = useAssistantEditor()
    await editor.startEdit('a-1')
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({ code: 'doc-helper', tools: ['users_list'] })
    editor.form.name = 'Renamed'
    expect(await editor.submit()).toBe(true)
    const put = calls.find(c => c.method === 'PUT')!
    expect(put.url).toBe('/api/ai-assistants/a-1')
    expect(put.body).toMatchObject({ name: 'Renamed', concurrencyStamp: 's1', tools: ['users_list'] })
    expect(put.body).not.toHaveProperty('code')
    expect(put.body).not.toHaveProperty('status')
  })

  it('工具目录给出可选项与知识库能否使用；已勾选而目录里没有的工具留着可见', async () => {
    record(call => call.url.endsWith(':tools')
      ? json(200, { items: [{ name: 'users_list', description: 'List users' }, { name: 'bare', description: null }], knowledgeAvailable: false })
      : json(200, detail({ tools: ['users_list', 'gone_tool'] })))
    const editor = useAssistantEditor()
    await editor.loadTools()
    await editor.startEdit('a-1')
    expect(editor.knowledgeAvailable.value).toBe(false)
    expect(editor.toolOptions.value).toEqual([
      { value: 'users_list', label: 'users_list - List users' },
      { value: 'bare', label: 'bare' },
      { value: 'gone_tool', label: 'gone_tool' },
    ])
  })

  it('字段错误挂回字段，别的错误进提交错误', async () => {
    record(() => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'bad', errors: { name: ['blank'] } }, 'application/problem+json'))
    const editor = useAssistantEditor()
    editor.startCreate()
    expect(await editor.submit()).toBe(false)
    expect(editor.fieldError('name')).toBe('blank')
    expect(editor.open.value).toBe(true)

    record(() => json(409, { code: 'AI_CONFLICT_105', status: 409, detail: 'taken' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('taken')
  })
})
