import { describe, expect, it } from 'vitest'
import { actionsConflict, enumKey, parseEnabledActions, serializeEnabledActions, tablesResource } from '@/modules/codegeneration/views/code-gen/components/use-tables'
import { configureRequest } from '~/request'

describe('包含操作', () => {
  it('空即缺省集合，恰为缺省集合时提交空', () => {
    expect(parseEnabledActions(null)).toEqual([
      'create',
      'update',
      'delete',
      'export',
      'import',
    ])
    expect(parseEnabledActions('print, CREATE ,unknown')).toEqual(['create', 'print'])
    expect(serializeEnabledActions([
      'import',
      'create',
      'update',
      'delete',
      'export',
    ])).toBeNull()
    expect(serializeEnabledActions([])).toBeNull()
    expect(serializeEnabledActions(['print', 'create'])).toBe('create,print')
  })

  it('只勾导入不勾新建是冲突', () => {
    expect(actionsConflict(['import'])).toBe(true)
    expect(actionsConflict(['import', 'create'])).toBe(false)
  })
})

describe('enumKey', () => {
  it('契约里的枚举值换成 snake_case 的文案键', () => {
    expect(enumKey('template_type', 'MasterDetail')).toBe('codegeneration.code_gen.enums.template_type.master_detail')
    expect(enumKey('query_type', 'GreaterThanOrEqual')).toBe('codegeneration.code_gen.enums.query_type.greater_than_or_equal')
    expect(enumKey('action', 'create')).toBe('codegeneration.code_gen.enums.action.create')
  })
})

describe('tablesResource', () => {
  it('生成模式与状态多选合成 in', async () => {
    const calls: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({
          url: String(url),
          body: init?.body
            ? JSON.parse(String(init.body))
            : undefined,
        })
        return new Response(JSON.stringify({
          items: [],
          total: 0,
          page: { index: 1, size: 20 },
        }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })
      }) as unknown as typeof fetch,
    })
    await tablesResource().page!({
      page: 1,
      pageSize: 20,
      filters: {},
      conditionFilters: [
        {
          field: 'templateType',
          operator: 3000,
          values: ['Tree', 'MasterDetail'],
        },
      ],
      sorts: [
        { field: 'tableName', order: 'asc' },
      ],
    } as never)
    expect(calls[0]).toMatchObject({
      url: '/api/codegen-tables:list',
      body: {
        filters: [
          { field: 'templateType', operator: 'in', value: 'Tree,MasterDetail' },
        ],
        sorts: [
          { field: 'tableName', descending: false },
        ],
      },
    })
  })
})
