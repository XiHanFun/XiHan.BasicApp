import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { promptsResource, usePromptEditor } from './use-prompts'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function detail(extra: Record<string, unknown> = {}) {
  return { id: 'p-1', code: 'rag-answer', name: 'RAG', category: null, version: null, content: 'You are helpful.', status: 'Active', order: 100, remark: null, concurrencyStamp: 's1', createdAt: '2026-11-04T00:00:00Z', modifiedAt: null, ...extra }
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

describe('promptsResource', () => {
  it('状态多选合成 in，关键字原样下发', async () => {
    const calls = record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await promptsResource().page!({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' rag ' },
      conditionFilters: [{ field: 'status', operator: 3000, values: ['Disabled'] }],
      sorts: [],
    } as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/ai-prompts:list' })
    expect(calls[0]!.body).toMatchObject({ keyword: 'rag', filters: [{ field: 'status', operator: 'in', value: 'Disabled' }] })
  })

  it('批量删除逐项调用，逐项失败抛出码', async () => {
    const calls = record(() => json(200, { succeeded: [], failed: [{ id: 'x-1', code: 'AI_NOTFOUND', message: 'AI_NOTFOUND 的原因' }] }))
    await expect(promptsResource().remove!('x-1')).rejects.toThrow('AI_NOTFOUND 的原因')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/ai-prompts?ids=x-1' })
    record(() => json(200, { succeeded: ['x-2'], failed: [] }))
    await expect(promptsResource().remove!('x-2')).resolves.toBeUndefined()
  })
})

describe('usePromptEditor', () => {
  it('编辑先取详情（列表不带正文）；编码与状态不发，带上读到的并发戳，正文原样不去空白', async () => {
    const calls = record(call => json(200, call.method === 'GET' ? detail() : detail({ concurrencyStamp: 's2' })))
    const editor = usePromptEditor()
    await editor.startEdit('p-1')
    expect(editor.form).toMatchObject({ code: 'rag-answer', content: 'You are helpful.', category: '' })
    editor.form.content = '  line one\n  line two\n'
    expect(await editor.submit()).toBe(true)
    const put = calls.find(c => c.method === 'PUT')!
    expect(put).toMatchObject({ url: '/api/ai-prompts/p-1', body: { concurrencyStamp: 's1', content: '  line one\n  line two\n', category: null, version: null } })
    expect(put.body).not.toHaveProperty('code')
    expect(put.body).not.toHaveProperty('status')
  })

  it('新建带编码与初始状态；字段级错误挂回字段，弹窗保持打开', async () => {
    const calls = record(() => json(400, { code: 'KERNEL_VALIDATION_001', status: 400, detail: 'invalid', traceId: 'x', errors: { content: ['required'] } }, 'application/problem+json'))
    const editor = usePromptEditor()
    editor.startCreate()
    editor.form.code = ' rag-answer '
    expect(await editor.submit()).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/ai-prompts', body: { code: 'rag-answer', status: 'Active' } })
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('content')).toBe('required')
  })
})
