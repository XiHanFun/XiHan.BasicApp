import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { QueryOperator } from '~/types/contracts'
import {
  ACCESS_LOG_QUERY,
  accessLogsResource,
  conditionHitsResource,
  DIFF_LOG_QUERY,
  diffLogsResource,
  EXCEPTION_LOG_QUERY,
  exceptionLogsResource,
  LOGIN_LOG_QUERY,
  loginLogsResource,
  OPEN_API_LOG_QUERY,
  openApiLogsResource,
  OPERATION_LOG_QUERY,
  operationLogsResource,
  permissionChangeLogsResource,
} from './log-resources'

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

  it('4.x 有服务端导出的六种日志提交到导出中心：业务类型是各自的列表操作，查询快照按列表同一份登记换算', () => {
    const params = {
      page: 2,
      pageSize: 50,
      filters: { keyword: 'x', traceId: 'abc', occurredAt: [1, 2] },
      conditionFilters: [{ field: 'occurredAt', operator: QueryOperator.Between, values: ['2026-10-01T00:00:00.000Z', '2026-10-02T00:00:00.000Z'] }],
      sorts: [{ field: 'occurredAt', order: 'desc' as const }],
    }
    const cases = [
      [operationLogsResource(), 'operation-logs.list', OPERATION_LOG_QUERY],
      [accessLogsResource(), 'access-logs.list', ACCESS_LOG_QUERY],
      [loginLogsResource(), 'login-logs.list', LOGIN_LOG_QUERY],
      [exceptionLogsResource(), 'exception-logs.list', EXCEPTION_LOG_QUERY],
      [diffLogsResource(), 'diff-logs.list', DIFF_LOG_QUERY],
      [openApiLogsResource(), 'open-api-logs.list', OPEN_API_LOG_QUERY],
    ] as const
    for (const [resource, businessType, query] of cases) {
      expect(resource.export?.businessType).toBe(businessType)
      expect(resource.export?.buildQuery?.(params)).toEqual(toPageRequest(params, query))
    }
    // 操作日志的客户端地址记在细节里，只显示、不能筛选：快照与列表一样拒绝没登记的筛选
    expect(() => operationLogsResource().export?.buildQuery?.({ page: 1, pageSize: 20, filters: { clientIp: '127.0.0.1' } })).toThrow(/clientIp/)
  })

  it('权限变更日志与条件命中只留本地导出', () => {
    expect(permissionChangeLogsResource().export).toBeUndefined()
    expect(conditionHitsResource().export).toBeUndefined()
  })

  it('操作日志的会话与地址不能筛选：这两项不在登记里，出现即报错', () => {
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { clientIp: '127.0.0.1' } }, OPERATION_LOG_QUERY)).toThrow(/clientIp/)
    expect(() => toPageRequest({ page: 1, pageSize: 20, filters: { sessionId: 'x' } }, OPERATION_LOG_QUERY)).toThrow(/sessionId/)
  })
})
