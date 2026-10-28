import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { QueryOperator } from '~/types/contracts'
import { ACCESS_LOG_QUERY, LOGIN_LOG_QUERY, OPERATION_LOG_QUERY } from './log-resources'

describe('日志列表的后端筛选登记', () => {
  it('发生时间区间拆成 ge / le，访问结果多选合成 in，公共列精确匹配', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: 'roles', clientIp: '127.0.0.1', traceId: 'abc', occurredAt: [1, 2], result: ['Success', 'Forbidden'] },
      conditionFilters: [
        { field: 'occurredAt', operator: QueryOperator.Between, values: ['2026-10-01T00:00:00.000Z', '2026-10-02T00:00:00.000Z'] },
        { field: 'result', operator: QueryOperator.In, values: ['Success', 'Forbidden'] },
      ],
      sorts: [{ field: 'occurredAt', order: 'desc' }],
    }, ACCESS_LOG_QUERY)

    expect(request.keyword).toBe('roles')
    expect(request.filters).toEqual([
      { field: 'occurredAt', operator: 'ge', value: '2026-10-01T00:00:00.000Z' },
      { field: 'occurredAt', operator: 'le', value: '2026-10-02T00:00:00.000Z' },
      { field: 'result', operator: 'in', value: 'Success,Forbidden' },
      { field: 'clientIp', operator: 'eq', value: '127.0.0.1' },
      { field: 'traceId', operator: 'eq', value: 'abc' },
    ])
    expect(request.sorts).toEqual([{ field: 'occurredAt', descending: true }])
  })

  it('登录日志的事件多选、成败按 true / false 精确匹配', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { succeeded: 'false', event: ['Login'] },
      conditionFilters: [{ field: 'event', operator: QueryOperator.In, values: ['Login'] }],
    }, LOGIN_LOG_QUERY)

    expect(request.filters).toEqual([
      { field: 'event', operator: 'in', value: 'Login' },
      { field: 'succeeded', operator: 'eq', value: 'false' },
    ])
  })

  it('操作日志没有会话与地址列：这两项不在登记里，出现即报错', () => {
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { clientIp: '127.0.0.1' } }, OPERATION_LOG_QUERY)).toThrow(/clientIp/)
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { sessionId: 'x' } }, OPERATION_LOG_QUERY)).toThrow(/sessionId/)
  })
})
