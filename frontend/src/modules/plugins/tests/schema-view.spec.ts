import type { PluginSchemaField, PluginSchemaView } from '@/modules/plugins/schema-view'
import { describe, expect, it } from 'vitest'
import { columnDataType, formRequest, initialForm, pageRows, rowKeyOf } from '@/modules/plugins/schema-view'

const view: PluginSchemaView = {
  list: 'crm-customers.list',
  columns: [
    { key: 'name', titleKey: 'crm.customers.name' },
    { key: 'email', titleKey: 'crm.customers.email' },
  ],
}

const fields: PluginSchemaField[] = [
  { key: 'name', labelKey: 'crm.customers.name', required: true },
  { key: 'email', labelKey: 'crm.customers.email' },
  { key: 'level', labelKey: 'crm.customers.level', type: 'number' },
  { key: 'vip', labelKey: 'crm.customers.vip', type: 'switch' },
]

describe('声明式页面的列表', () => {
  const rows = [
    { id: '1', name: 'Ada', email: 'ada@contoso.test' },
    { id: '2', name: 'Grace', email: null },
    { id: '3', name: 'Linus', email: 'linus@contoso.test' },
  ]

  it('关键字在各列里找，不分大小写', () => {
    const result = pageRows(rows, view, {
      page: 1,
      pageSize: 10,
      filters: { keyword: 'CONTOSO' },
    })
    expect(result.items.map(row => row.name)).toEqual(['Ada', 'Linus'])
    expect(result.page.totalCount).toBe(2)
  })

  it('按页切片，总数是筛完的条数', () => {
    const result = pageRows(rows, view, { page: 2, pageSize: 2, filters: {} })
    expect(result.items.map(row => row.name)).toEqual(['Linus'])
    expect(result.page.totalCount).toBe(3)
    expect(result.page.pageIndex).toBe(2)
  })

  it('行键缺省是 id，列种类映射成 SchemaPage 的数据类型', () => {
    expect(rowKeyOf(view)).toBe('id')
    expect(rowKeyOf({ ...view, rowKey: 'code' })).toBe('code')
    expect([
      columnDataType(undefined),
      columnDataType('number'),
      columnDataType('boolean'),
      columnDataType('datetime'),
    ]).toEqual([
      'string',
      'number',
      'boolean',
      'datetime',
    ])
  })
})

describe('声明式页面的表单', () => {
  it('新建按控件给空值，编辑取行里的值', () => {
    expect(initialForm(fields)).toEqual({
      name: '',
      email: '',
      level: null,
      vip: false,
    })
    expect(initialForm(fields, {
      name: 'Ada',
      email: null,
      level: 3,
      vip: true,
    })).toEqual({
      name: 'Ada',
      email: '',
      level: 3,
      vip: true,
    })
  })

  it('文本去掉首尾空白，选填的空文本交 null，修改时带上行键', () => {
    const form = {
      name: '  Ada  ',
      email: '   ',
      level: 3,
      vip: true,
    }
    expect(formRequest(fields, form)).toEqual({
      name: 'Ada',
      email: null,
      level: 3,
      vip: true,
    })
    expect(formRequest(fields, form, { name: 'id', value: '1' })).toEqual({
      id: '1',
      name: 'Ada',
      email: null,
      level: 3,
      vip: true,
    })
  })

  it('必填的空文本照样交空串，由插件报字段错误', () => {
    expect(formRequest(fields, {
      name: ' ',
      email: '',
      level: null,
      vip: false,
    }).name).toBe('')
  })
})
