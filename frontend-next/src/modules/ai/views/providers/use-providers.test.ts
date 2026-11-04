import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { probeLines, providersResource, useProviderEditor, vendorOptions } from './use-providers'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function detail(extra: Record<string, unknown> = {}) {
  return { id: 'p-1', code: 'openai-main', name: 'Main', vendor: 'OpenAI', chatModel: 'gpt-4o-mini', embeddingModel: null, endpoint: null, hasApiKey: true, maxOutputTokens: null, temperature: 0.7, timeoutSeconds: null, isDefault: false, status: 'Active', order: 100, remark: null, concurrencyStamp: 's1', createdAt: '2026-11-04T00:00:00Z', modifiedAt: null, ...extra }
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

describe('providersResource', () => {
  it('状态多选合成 in，关键字原样下发', async () => {
    const calls = record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await providersResource().page!({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' main ' },
      conditionFilters: [{ field: 'status', operator: 3000, values: ['Active', 'Disabled'] }],
      sorts: [],
    } as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/ai-providers:list' })
    expect(calls[0]!.body).toMatchObject({ keyword: 'main', filters: [{ field: 'status', operator: 'in', value: 'Active,Disabled' }] })
  })

  it('批量删除逐项调用，逐项失败抛出码', async () => {
    const calls = record(() => json(200, { succeeded: [], failed: [{ id: 'x-1', code: 'AI_NOTFOUND', message: 'AI_NOTFOUND 的原因' }] }))
    await expect(providersResource().remove!('x-1')).rejects.toThrow('AI_NOTFOUND 的原因')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/ai-providers?ids=x-1' })
    record(() => json(200, { succeeded: ['x-2'], failed: [] }))
    await expect(providersResource().remove!('x-2')).resolves.toBeUndefined()
  })
})

describe('vendorOptions', () => {
  it('品牌厂商用原名，自建与自定义按语言取', () => {
    const options = vendorOptions(key => `t:${key}`)
    expect(options.map(o => o.value)).toEqual(['OpenAI', 'DeepSeek', 'Azure', 'Moonshot', 'SelfHosted', 'Custom'])
    expect(options.find(o => o.value === 'Azure')!.label).toBe('Azure OpenAI')
    expect(options.find(o => o.value === 'SelfHosted')!.label).toBe('t:ai.providers.vendor_self_hosted')
    expect(options.find(o => o.value === 'Custom')!.label).toBe('t:ai.providers.vendor_custom')
  })
})

describe('probeLines', () => {
  it('会话一行、嵌入一行；没配嵌入模型时说明不能用于知识库', () => {
    expect(probeLines({ success: true, chat: { success: true, message: 'stop', elapsedMilliseconds: 12, model: 'm', dimensions: null }, embedding: { success: true, message: null, elapsedMilliseconds: 8, model: 'e', dimensions: 1536 } }))
      .toEqual({ embeddingAbsent: false, lines: [{ kind: 'chat', success: true, elapsedMilliseconds: 12, message: 'stop', dimensions: null }, { kind: 'embedding', success: true, elapsedMilliseconds: 8, message: null, dimensions: 1536 }] })
    const chatOnly = probeLines({ success: false, chat: { success: false, message: 'boom', elapsedMilliseconds: 3, model: 'm', dimensions: null }, embedding: null })
    expect(chatOnly.embeddingAbsent).toBe(true)
    expect(chatOnly.lines).toEqual([{ kind: 'chat', success: false, elapsedMilliseconds: 3, message: 'boom', dimensions: null }])
  })
})

describe('useProviderEditor', () => {
  it('编辑时编码不发、API 密钥留空即发 null（保留原密钥），带上读到的并发戳', async () => {
    const calls = record(call => json(200, call.method === 'GET' ? detail() : detail({ concurrencyStamp: 's2' })))
    const editor = useProviderEditor()
    await editor.startEdit('p-1')
    expect(editor.form).toMatchObject({ code: 'openai-main', apiKey: '', temperature: 0.7, embeddingModel: '' })
    editor.form.chatModel = ' gpt-4o '
    expect(await editor.submit()).toBe(true)
    const put = calls.find(c => c.method === 'PUT')!
    expect(put).toMatchObject({ url: '/api/ai-providers/p-1', body: { concurrencyStamp: 's1', chatModel: 'gpt-4o', apiKey: null, embeddingModel: null } })
    expect(put.body).not.toHaveProperty('code')
    expect(put.body).not.toHaveProperty('status')
  })

  it('新建带编码与初始状态；字段级错误挂回字段，弹窗保持打开', async () => {
    const calls = record(() => json(400, { code: 'KERNEL_VALIDATION_001', status: 400, detail: 'invalid', traceId: 'x', errors: { endpoint: ['bad endpoint'] } }, 'application/problem+json'))
    const editor = useProviderEditor()
    editor.startCreate()
    editor.form.code = ' deepseek '
    editor.form.endpoint = '/v1'
    expect(await editor.submit()).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/ai-providers', body: { code: 'deepseek', status: 'Active', endpoint: '/v1', vendor: 'OpenAI' } })
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('endpoint')).toBe('bad endpoint')
  })
})
