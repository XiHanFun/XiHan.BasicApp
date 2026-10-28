import { describe, expect, it } from 'vitest'
import { QueryOperator } from '~/types/contracts'
import { toPageRequest, toSchemaPageResult } from './paging'

describe('schemaPage ↔ PageRequest', () => {
  it('关键字、登记过的筛选与多列排序按后端契约下发，空值不下发', () => {
    const request = toPageRequest(
      { page: 2, pageSize: 20, filters: { keyword: ' demo ', group: 'mail', status: 'Active', name: '' }, sorts: [{ field: 'name', order: 'asc' }, { field: 'createdAt', order: 'desc' }] },
      { filters: { group: 'eq', status: 'eq', name: 'contains' } },
    )
    expect(request).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'demo',
      filters: [{ field: 'group', operator: 'eq', value: 'mail' }, { field: 'status', operator: 'eq', value: 'Active' }],
      sorts: [{ field: 'name', descending: false }, { field: 'createdAt', descending: true }],
    })
  })

  it('没登记运算符的搜索字段、区间与多选条件直接报错，不静默丢掉', () => {
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { owner: 'platform' } })).toThrow(/owner/)
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: {}, conditionFilters: [{ field: 'status', operator: QueryOperator.In, values: ['Active'] }] })).toThrow(/searchMultiple/)
  })

  it('分页结果换成 SchemaPage 读的 PageResult：总数、页码与首末记录', () => {
    const result = toSchemaPageResult({ items: ['a', 'b'], total: '22', page: { index: 2, size: 20 } }, { page: 2, pageSize: 20, filters: {} })
    expect(result.items).toEqual(['a', 'b'])
    expect(result.page).toMatchObject({ totalCount: 22, pageIndex: 2, pageSize: 20, totalPages: 2, startRecord: 21, endRecord: 22, hasNext: false, hasPrevious: true, isLastPage: true })
    expect(toSchemaPageResult({ items: [], total: 0, page: {} }, { page: 1, pageSize: 10, filters: {} }).page).toMatchObject({ totalCount: 0, startRecord: 0, endRecord: 0, totalPages: 1 })
  })
})
