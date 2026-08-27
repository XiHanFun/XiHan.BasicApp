/**
 * 查询条件构造工具（src/api/helpers.ts）单元测试。
 *
 * 职责边界：只覆盖 conditions / page 请求体的纯构造逻辑——默认值、空值裁剪、
 * 排序优先级映射与对象压缩，不涉及任何网络行为。
 */
import type { QueryConditions } from './types'
import { describe, expect, it } from 'vitest'
import {
  compactRecord,
  createDefaultQueryConditions,
  createPageRequest,
  queryFilter,
  queryKeyword,
  querySort,
  querySortsFromSchema,
} from './helpers'
import { QueryOperator, SortDirection } from './types'

describe('createDefaultQueryConditions 的默认条件', () => {
  it('不传参时给出「空过滤 + 无关键字 + 空排序」的完整三件套', () => {
    expect(createDefaultQueryConditions()).toEqual({ filters: [], keyword: null, sorts: [] })
  })

  it('每次调用返回全新的数组实例，避免多个查询共享同一份 filters 相互污染', () => {
    const first = createDefaultQueryConditions()
    const second = createDefaultQueryConditions()
    first.filters.push(queryFilter('status', 'Enabled', QueryOperator.Equal))

    expect(second.filters).toHaveLength(0)
  })

  it('传入的字段整体覆盖默认值，未传的字段保留默认', () => {
    const filters = [queryFilter('roleType', 1, QueryOperator.Equal)]
    const conditions = createDefaultQueryConditions({ filters })

    expect(conditions.filters).toBe(filters)
    expect(conditions.keyword).toBeNull()
    expect(conditions.sorts).toEqual([])
  })

  it('显式传 keyword: undefined 会把默认的 null 覆盖成 undefined——两者对后端语义不同，调用方须自行区分', () => {
    const conditions = createDefaultQueryConditions({ keyword: undefined })

    expect(conditions.keyword).toBeUndefined()
    expect('keyword' in conditions).toBe(true)
  })
})

describe('createPageRequest 的分页默认值', () => {
  it('不传参时给出第 1 页、每页 20 条', () => {
    expect(createPageRequest().page).toEqual({ pageIndex: 1, pageSize: 20 })
  })

  it('页码 0 是显式取值不是缺省，必须原样保留而不是回落到 1', () => {
    expect(createPageRequest({ page: { pageIndex: 0, pageSize: 0 } }).page)
      .toEqual({ pageIndex: 0, pageSize: 0 })
  })

  it('负数与超大页长同样原样透传，边界校验由后端负责', () => {
    expect(createPageRequest({ page: { pageIndex: -3, pageSize: 100000 } }).page)
      .toEqual({ pageIndex: -3, pageSize: 100000 })
  })

  it('只给页码时页长仍取默认 20，反之亦然', () => {
    expect(createPageRequest({ page: { pageIndex: 5 } }).page).toEqual({ pageIndex: 5, pageSize: 20 })
    expect(createPageRequest({ page: { pageSize: 50 } }).page).toEqual({ pageIndex: 1, pageSize: 50 })
  })

  it('conditions 缺省时补齐为默认三件套，传入时按字段合并', () => {
    expect(createPageRequest().conditions).toEqual({ filters: [], keyword: null, sorts: [] })

    const sorts = [querySort('createdTime', SortDirection.Descending)]
    expect(createPageRequest({ conditions: { sorts } }).conditions.sorts).toBe(sorts)
  })
})

describe('queryKeyword 的关键字裁剪', () => {
  it('关键字两端空白被裁掉后才下发', () => {
    expect(queryKeyword('  张三  ', ['userName'])).toEqual({ fields: ['userName'], value: '张三' })
  })

  it('undefined / 空串 / 纯空白一律返回 null，让后端走「无关键字」分支', () => {
    expect(queryKeyword(undefined, ['userName'])).toBeNull()
    expect(queryKeyword('', ['userName'])).toBeNull()
    expect(queryKeyword('   \t\n ', ['userName'])).toBeNull()
  })

  it('空白字段名被剔除，其余字段保持原样（不对字段名做裁剪）', () => {
    expect(queryKeyword('abc', ['userName', '', '   ', ' realName '])).toEqual({
      fields: ['userName', ' realName '],
      value: 'abc',
    })
  })

  it('字段列表为空时仍返回关键字对象，fields 为空数组——是否报错由后端裁决', () => {
    expect(queryKeyword('abc', [])).toEqual({ fields: [], value: 'abc' })
  })

  it('中文、emoji 与特殊字符不做转义，仅裁剪两端空白', () => {
    expect(queryKeyword('  %_张三🙂  ', ['userName'])?.value).toBe('%_张三🙂')
  })
})

describe('queryFilter 与 querySort 的结构', () => {
  it('过滤条件按「字段 / 操作符 / 值」三元组组装，值原样保留', () => {
    expect(queryFilter('status', null, QueryOperator.IsNull))
      .toEqual({ field: 'status', operator: QueryOperator.IsNull, value: null })
    expect(queryFilter('age', 0, QueryOperator.GreaterThanOrEqual))
      .toEqual({ field: 'age', operator: QueryOperator.GreaterThanOrEqual, value: 0 })
    expect(queryFilter('enabled', false, QueryOperator.Equal).value).toBe(false)
  })

  it('排序默认优先级为 0，即主排序', () => {
    expect(querySort('createdTime', SortDirection.Descending))
      .toEqual({ field: 'createdTime', direction: SortDirection.Descending, priority: 0 })
  })

  it('显式优先级原样保留，用于多字段排序', () => {
    expect(querySort('sortOrder', SortDirection.Ascending, 3).priority).toBe(3)
  })
})

describe('querySortsFromSchema 的多字段排序映射', () => {
  it('未传或空数组时返回空排序，让后端回退各自默认排序', () => {
    expect(querySortsFromSchema()).toEqual([])
    expect(querySortsFromSchema([])).toEqual([])
  })

  it('数组下标即优先级：0 为主排序，依次递增', () => {
    expect(querySortsFromSchema([
      { field: 'status', order: 'asc' },
      { field: 'createdTime', order: 'desc' },
      { field: 'sortOrder', order: 'asc' },
    ])).toEqual([
      { field: 'status', direction: SortDirection.Ascending, priority: 0 },
      { field: 'createdTime', direction: SortDirection.Descending, priority: 1 },
      { field: 'sortOrder', direction: SortDirection.Ascending, priority: 2 },
    ])
  })

  it('只有 desc 映射为降序，其余一律降级为升序', () => {
    expect(querySortsFromSchema([{ field: 'a', order: 'desc' }])[0]?.direction)
      .toBe(SortDirection.Descending)
    expect(querySortsFromSchema([{ field: 'a', order: 'asc' }])[0]?.direction)
      .toBe(SortDirection.Ascending)
  })

  it('不修改传入的数组本身，返回的是新数组', () => {
    const input = [{ field: 'a', order: 'asc' as const }]
    const result = querySortsFromSchema(input)

    expect(result).not.toBe(input)
    expect(input).toEqual([{ field: 'a', order: 'asc' }])
  })
})

describe('compactRecord 的空值压缩', () => {
  it('undefined / null / 空串被剔除，其余键保留', () => {
    expect(compactRecord({ a: 1, b: undefined, c: null, d: '', e: 'x' }))
      .toEqual({ a: 1, e: 'x' })
  })

  it('0 / false / NaN / 空数组 / 空对象都是有效取值，不得被当成空值丢掉', () => {
    const result = compactRecord({ zero: 0, no: false, nan: Number.NaN, list: [], obj: {} })

    expect(Object.keys(result).sort()).toEqual(['list', 'nan', 'no', 'obj', 'zero'])
    expect(result.zero).toBe(0)
    expect(result.no).toBe(false)
  })

  it('纯空格字符串不算空值，会被保留（只有严格空串才剔除）', () => {
    expect(compactRecord({ keyword: ' ' })).toEqual({ keyword: ' ' })
  })

  it('全部为空值时返回空对象，且不改动原对象', () => {
    const input = { a: undefined, b: null, c: '' }
    expect(compactRecord(input)).toEqual({})
    expect(Object.keys(input)).toEqual(['a', 'b', 'c'])
  })

  it('只压缩自有可枚举属性，原型链上的属性不会被带出来', () => {
    const base = { inherited: 'yes' }
    const input: Record<string, unknown> = Object.create(base) as Record<string, unknown>
    input.own = 'ok'

    expect(compactRecord(input)).toEqual({ own: 'ok' })
  })
})

describe('分页请求组合出的完整契约形状', () => {
  it('关键字 + 过滤 + 排序可以组装成后端期望的 PageRequest', () => {
    const conditions: QueryConditions = {
      filters: [queryFilter('status', 'Enabled', QueryOperator.Equal)],
      keyword: queryKeyword(' admin ', ['userName', 'realName']),
      sorts: querySortsFromSchema([{ field: 'createdTime', order: 'desc' }]),
    }
    const request = createPageRequest({ conditions, page: { pageIndex: 2 } })

    expect(request).toEqual({
      conditions: {
        filters: [{ field: 'status', operator: QueryOperator.Equal, value: 'Enabled' }],
        keyword: { fields: ['userName', 'realName'], value: 'admin' },
        sorts: [{ field: 'createdTime', direction: SortDirection.Descending, priority: 0 }],
      },
      page: { pageIndex: 2, pageSize: 20 },
    })
  })
})
