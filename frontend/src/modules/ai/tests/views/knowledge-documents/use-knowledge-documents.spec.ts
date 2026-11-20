import { describe, expect, it } from 'vitest'
import { citationTitle, knowledgeDocumentsResource, splitByHit, useKnowledgeDetail, useKnowledgeIngest, useKnowledgePlayground } from '@/modules/ai/views/knowledge-documents/use-knowledge-documents'
import { configureRequest } from '~/request'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

function detail(extra: Record<string, unknown> = {}) {
  return {
    id: 'd-1',
    title: 'Handbook',
    sourceType: 'PasteText',
    source: null,
    rawContent: 'text',
    chunkCount: 1,
    embeddingProviderCode: null,
    status: 'Indexed',
    errorCode: null,
    errorMessage: null,
    order: 0,
    remark: null,
    createdAt: '2026-11-04T00:00:00Z',
    modifiedAt: null,
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

describe('knowledgeDocumentsResource', () => {
  it('来源类型与状态多选合成 in，关键字原样下发', async () => {
    const calls = record(() => json(200, {
      items: [],
      total: 0,
      page: { index: 1, size: 20 },
    }))
    await knowledgeDocumentsResource().page!({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' hand ' },
      conditionFilters: [
        {
          field: 'status',
          operator: 3000,
          values: ['Indexed', 'Failed'],
        },
        {
          field: 'sourceType',
          operator: 3000,
          values: ['UploadFile'],
        },
      ],
      sorts: [],
    } as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/knowledge-documents:list' })
    expect(calls[0]!.body).toMatchObject({
      keyword: 'hand',
      filters: [
        { field: 'status', operator: 'in', value: 'Indexed,Failed' },
        { field: 'sourceType', operator: 'in', value: 'UploadFile' },
      ],
    })
  })

  it('批量删除逐项调用，逐项失败抛出码', async () => {
    const calls = record(() => json(200, {
      succeeded: [],
      failed: [
        { id: 'd-1', code: 'AI_UNAVAILABLE_002', message: 'AI_UNAVAILABLE_002 的原因' },
      ],
    }))
    await expect(knowledgeDocumentsResource().remove!('d-1')).rejects.toThrow('AI_UNAVAILABLE_002 的原因')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/knowledge-documents?ids=d-1' })
  })
})

describe('useKnowledgeIngest', () => {
  it('读到文件后来源记文件名，标题为空即取文件名，已填的标题不动', () => {
    const ingest = useKnowledgeIngest()
    ingest.startCreate()
    ingest.applyFile('handbook.md', '# Handbook')
    expect(ingest.form).toMatchObject({
      title: 'handbook.md',
      source: 'handbook.md',
      sourceType: 'UploadFile',
      text: '# Handbook',
    })

    ingest.form.title = 'Mine'
    ingest.applyFile('other.md', 'other')
    expect(ingest.form).toMatchObject({ title: 'Mine', source: 'other.md', text: 'other' })
  })

  it('提交：标题去空白、文本原样、空的可选项发 null；成功关弹窗并返回文档', async () => {
    const calls = record(() => json(200, detail()))
    const ingest = useKnowledgeIngest()
    ingest.startCreate()
    ingest.form.title = '  Handbook '
    ingest.form.text = '  line one\n'
    ingest.form.embeddingProviderCode = '   '
    const document = await ingest.submit()
    expect(document?.id).toBe('d-1')
    expect(ingest.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/knowledge-documents:ingest' })
    expect(calls[0]!.body).toMatchObject({
      title: 'Handbook',
      text: '  line one\n',
      sourceType: 'PasteText',
      source: null,
      embeddingProviderCode: null,
      remark: null,
    })
  })

  it('字段错误挂回字段，弹窗留着', async () => {
    record(() => json(400, {
      type: 'x',
      title: 'x',
      status: 400,
      code: 'KERNEL_VALIDATION_003',
      detail: 'bad',
      errors: {
        title: ['required'],
      },
    }, 'application/problem+json'))
    const ingest = useKnowledgeIngest()
    ingest.startCreate()
    expect(await ingest.submit()).toBeNull()
    expect(ingest.open.value).toBe(true)
    expect(ingest.fieldError('title')).toBe('required')
    expect(ingest.submitError.value).toBeNull()
  })

  it('索引失败的文档照样返回，由页面按状态提示', async () => {
    record(() => json(200, detail({
      status: 'Failed',
      chunkCount: 0,
      errorCode: 'AI_UNAVAILABLE_002',
      errorMessage: 'no store',
    })))
    const ingest = useKnowledgeIngest()
    ingest.startCreate()
    ingest.form.title = 't'
    ingest.form.text = 'x'
    const document = await ingest.submit()
    expect(document).toMatchObject({ status: 'Failed', errorMessage: 'no store' })
    expect(ingest.open.value).toBe(false)
  })
})

describe('useKnowledgePlayground', () => {
  it('没填问题不发请求', async () => {
    const calls = record(() => json(200, { answer: null, citations: [] }))
    const playground = useKnowledgePlayground()
    playground.question.value = '   '
    expect(await playground.run()).toBe(false)
    expect(calls).toHaveLength(0)
    expect(playground.searched.value).toBe(false)
  })

  it('问题走查询串，空的提供商不发；答案与片段写回', async () => {
    const calls = record(() => json(200, {
      answer: 'Thirty days [1].',
      citations: [
        {
          documentId: 'd-1',
          index: 0,
          title: 'Handbook',
          source: null,
          score: 0.9,
          text: 'text',
        },
      ],
    }))
    const playground = useKnowledgePlayground()
    playground.question.value = ' refund? '
    playground.top.value = 3
    expect(await playground.run()).toBe(true)
    expect(calls[0]!.method).toBe('GET')
    expect(calls[0]!.url).toContain('/api/knowledge-documents:query?')
    expect(calls[0]!.url).toContain('question=refund%3F')
    expect(calls[0]!.url).toContain('top=3')
    expect(calls[0]!.url).toContain('answer=true')
    expect(calls[0]!.url).not.toContain('provider=')
    expect(playground.answer.value).toBe('Thirty days [1].')
    expect(playground.citations.value).toHaveLength(1)
    expect(playground.searched.value).toBe(true)
    expect(playground.loading.value).toBe(false)
  })

  it('不要答案时只检索：走 search，不带 answer，答案清空', async () => {
    const calls = record(() => json(200, {
      citations: [
        {
          documentId: 'd-2',
          index: 1,
          title: null,
          source: 'faq.md',
          score: 0.7,
          text: 'chunk',
        },
      ],
    }))
    const playground = useKnowledgePlayground()
    playground.answer.value = 'earlier answer'
    playground.question.value = 'refund?'
    playground.wantAnswer.value = false
    expect(await playground.run()).toBe(true)
    expect(calls[0]!.method).toBe('GET')
    expect(calls[0]!.url).toContain('/api/knowledge-documents:search?')
    expect(calls[0]!.url).toContain('question=refund%3F')
    expect(calls[0]!.url).not.toContain('answer=')
    expect(playground.answer.value).toBeNull()
    expect(playground.citations.value.map(c => c.documentId)).toEqual(['d-2'])
  })

  it('请求失败抛出，加载状态收回', async () => {
    record(() => json(503, {
      type: 'x',
      title: 'x',
      status: 503,
      code: 'AI_UNAVAILABLE_002',
      detail: 'no store',
    }, 'application/problem+json'))
    const playground = useKnowledgePlayground()
    playground.question.value = 'a'
    await expect(playground.run()).rejects.toThrow()
    expect(playground.loading.value).toBe(false)
    expect(playground.searched.value).toBe(false)
  })
})

describe('useKnowledgeDetail', () => {
  it('按 ID 取详情，带上命中片段；加载完收起加载状态', async () => {
    const calls = record(() => json(200, detail({ rawContent: 'a b c' })))
    const view = useKnowledgeDetail()
    await view.show('d-1', 'b')
    expect(calls[0]!.url).toBe('/api/knowledge-documents/d-1')
    expect(view.open.value).toBe(true)
    expect(view.loading.value).toBe(false)
    expect(view.document.value?.rawContent).toBe('a b c')
    expect(view.hit.value).toBe('b')
  })

  it('连着打开两次只认后一次', async () => {
    const pending: Array<(response: Response) => void> = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (() => new Promise<Response>((resolve) => {
        pending.push(resolve)
      })) as unknown as typeof fetch,
    })
    const view = useKnowledgeDetail()
    const first = view.show('d-1')
    const second = view.show('d-2')
    await Promise.resolve()
    pending[1]!(json(200, detail({ id: 'd-2' })))
    await second
    pending[0]!(json(200, detail({ id: 'd-1' })))
    await first
    expect(view.document.value?.id).toBe('d-2')
    expect(view.loading.value).toBe(false)
  })

  it('加载失败关掉抽屉并抛出', async () => {
    record(() => json(404, {
      type: 'x',
      title: 'x',
      status: 404,
      code: 'AI_NOTFOUND_003',
      detail: 'gone',
    }, 'application/problem+json'))
    const view = useKnowledgeDetail()
    await expect(view.show('d-1')).rejects.toThrow('gone')
    expect(view.open.value).toBe(false)
    expect(view.loading.value).toBe(false)
  })
})

describe('splitByHit', () => {
  it('原文按命中片段切三段，片段两端的空白不算', () => {
    expect(splitByHit('one two three', ' two ')).toEqual({ before: 'one ', hit: 'two', after: ' three' })
  })

  it('没有片段或找不到时整段在 before', () => {
    expect(splitByHit('one two', null)).toEqual({ before: 'one two', hit: '', after: '' })
    expect(splitByHit('one two', '  ')).toEqual({ before: 'one two', hit: '', after: '' })
    expect(splitByHit('one two', 'four')).toEqual({ before: 'one two', hit: '', after: '' })
  })
})

describe('citationTitle', () => {
  it('标题，没有即来源，再没有即文档 ID', () => {
    const base = {
      documentId: 'd-1',
      index: 0,
      score: 0.5,
      text: 't',
    }
    expect(citationTitle({ ...base, title: 'T', source: 'S' })).toBe('T')
    expect(citationTitle({ ...base, title: null, source: 'S' })).toBe('S')
    expect(citationTitle({ ...base, title: null, source: null })).toBe('d-1')
  })
})
