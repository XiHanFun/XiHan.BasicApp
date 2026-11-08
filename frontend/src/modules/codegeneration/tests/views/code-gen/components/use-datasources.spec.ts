import { describe, expect, it } from 'vitest'
import { datasourcesResource, useDatasourceEditor } from '@/modules/codegeneration/views/code-gen/components/use-datasources'
import { configureRequest } from '~/request'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

function detail(extra: Record<string, unknown> = {}) {
  return {
    id: 'ds-1',
    sourceName: 'erp',
    sourceDescription: null,
    databaseType: 'PostgreSql',
    host: '127.0.0.1',
    port: 5432,
    databaseName: 'erp',
    userName: 'reader',
    hasConnectionString: true,
    extraParams: null,
    connectionTimeout: 30,
    isDefault: false,
    lastTestAt: null,
    lastTestResult: false,
    lastTestMessage: null,
    status: 'Active',
    order: 0,
    remark: null,
    concurrencyStamp: 's1',
    createdAt: '2026-11-03T00:00:00Z',
    createdBy: null,
    modifiedAt: null,
    modifiedBy: null,
    ...extra,
  }
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('datasourcesResource', () => {
  it('数据库类型与状态多选合成 in，关键字原样下发', async () => {
    const calls = record(() => json(200, {
      items: [],
      total: 0,
      page: { index: 1, size: 20 },
    }))
    await datasourcesResource().page!({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' erp ' },
      conditionFilters: [
        {
          field: 'databaseType',
          operator: 3000,
          values: ['MySql', 'Sqlite'],
        },
      ],
      sorts: [],
    } as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/codegen-datasources:list' })
    expect(calls[0]!.body).toMatchObject({
      keyword: 'erp',
      filters: [
        { field: 'databaseType', operator: 'in', value: 'MySql,Sqlite' },
      ],
    })
  })
})

describe('useDatasourceEditor', () => {
  it('编辑时口令与连接串留空即发 null（不改），状态变了另调启停并带上刚写入的并发戳', async () => {
    const calls = record((call) => {
      if (call.method === 'GET')
        return json(200, detail())
      if (call.method === 'PUT')
        return json(200, detail({ concurrencyStamp: 's2' }))
      return json(200, detail({ concurrencyStamp: 's3', status: 'Disabled' }))
    })
    const editor = useDatasourceEditor()
    await editor.startEdit('ds-1')
    expect(editor.form).toMatchObject({ sourceName: 'erp', password: '', connectionString: '' })
    editor.form.status = 'Disabled'
    expect(await editor.submit('remark')).toBe(true)
    const put = calls.find(c => c.method === 'PUT')!
    expect(put).toMatchObject({
      url: '/api/codegen-datasources/ds-1',
      body: { concurrencyStamp: 's1', password: null, connectionString: null },
    })
    expect(calls.at(-1)).toMatchObject({
      method: 'POST',
      url: '/api/codegen-datasources/ds-1:set-status',
      body: { concurrencyStamp: 's2', status: 'Disabled', remark: 'remark' },
    })
  })

  it('新建的字段级错误挂回字段，弹窗保持打开', async () => {
    record(() => json(400, {
      code: 'KERNEL_VALIDATION_001',
      status: 400,
      detail: 'invalid',
      traceId: 'x',
      errors: {
        host: ['required'],
      },
    }, 'application/problem+json'))
    const editor = useDatasourceEditor()
    editor.startCreate()
    expect(await editor.submit('remark')).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('host')).toBe('required')
  })
})
