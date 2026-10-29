import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { formatDuration, loadSessionSummary, onlineSeconds, onlineSessionsResource, revokeSession, sessionUserName } from './use-online-users'

/** 假翻译：键原样返回，带参数时拼上参数 */
function t(key: string, values?: Record<string, unknown>): string {
  return values ? `${key}${JSON.stringify(values)}` : key
}

function json(status: number, body: object): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

/** 记下请求（方法、地址、请求体），按顺序回给定的响应 */
function stub(...responses: object[]): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({ method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
      return json(200, responses[calls.length - 1] ?? {})
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('在线用户', () => {
  it('在线时长按登录时间至今算秒，跨度分别落到秒、分、时分、天时', () => {
    const now = Date.parse('2026-10-29T08:00:00Z')
    expect(onlineSeconds({ loginAt: '2026-10-29T07:59:30Z' }, now)).toBe(30)
    expect(onlineSeconds({ loginAt: '2026-10-29T08:00:05Z' }, now)).toBe(0)
    expect(formatDuration(30, t)).toBe('identity.online_users.duration_seconds{"n":30}')
    expect(formatDuration(150, t)).toBe('identity.online_users.duration_minutes{"n":2}')
    expect(formatDuration(3 * 3600 + 120, t)).toBe('identity.online_users.duration_hours{"h":3,"m":2}')
    expect(formatDuration(2 * 86400 + 5 * 3600, t)).toBe('identity.online_users.duration_days{"d":2,"h":5}')
  })

  it('展示名优先显示名，其次登录名', () => {
    expect(sessionUserName({ displayName: 'Ann', userName: 'ann', userId: 'u1' })).toBe('Ann')
    expect(sessionUserName({ displayName: '', userName: 'ann', userId: 'u1' })).toBe('ann')
    expect(sessionUserName({ displayName: '', userName: '', userId: 'u1' })).toBe('#u1')
  })

  it('列表走 sessions.list：设备多选发 in，浏览器精确匹配，关键字与排序原样带上', async () => {
    const calls = stub({ items: [], total: 0, page: { index: 1, size: 20 } })
    await onlineSessionsResource().page!({ page: 1, pageSize: 20, filters: { keyword: 'ann', device: ['Desktop', 'Mobile'], browser: 'Chrome' }, conditionFilters: [{ field: 'device', operator: QueryOperator.In, values: ['Desktop', 'Mobile'] }], sorts: [{ field: 'loginAt', order: 'desc' }] })
    expect(calls[0]?.method).toBe('POST')
    expect(calls[0]?.url).toBe('/api/sessions:list')
    expect(calls[0]?.body).toMatchObject({
      keyword: 'ann',
      filters: expect.arrayContaining([{ field: 'device', operator: 'in', value: 'Desktop,Mobile' }, { field: 'browser', operator: 'eq', value: 'Chrome' }]),
      sorts: [{ field: 'loginAt', descending: true }],
    })
  })

  it('概况走 GET sessions:summary；强制下线带原因，逐项拒绝按错误码报错', async () => {
    const calls = stub({ activeSessions: 3, activeUsers: 2, realtimeOnlineUsers: 1 }, { succeeded: ['s1'], failed: [] }, { succeeded: [], failed: [{ id: 's2', code: 'IDENTITY_CONFLICT_005' }] })
    expect(await loadSessionSummary()).toEqual({ activeSessions: 3, activeUsers: 2, realtimeOnlineUsers: 1 })
    await revokeSession('s1', 'maintenance')
    await expect(revokeSession('s2', 'maintenance')).rejects.toThrow('IDENTITY_CONFLICT_005')
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['GET /api/sessions:summary', 'POST /api/sessions:revoke', 'POST /api/sessions:revoke'])
    expect(calls[1]?.body).toEqual({ ids: ['s1'], reason: 'maintenance' })
  })
})
