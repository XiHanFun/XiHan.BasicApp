import { describe, expect, it } from 'vitest'
import { MessageSendStatus, toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { canCancel, cancelRecord, canDelete, canResend, emailRecordResource, formatAttempts, prettyJson, RECORD_QUERY, resendRecord, smsRecordResource } from './messages'

const t = (key: string) => key

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

describe('发送记录的列表筛选', () => {
  it('状态多选合成 in，写入时间区间拆成 ge / le，模板编码与业务类型精确匹配，发送时间可排序', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' a@b.com ', status: ['Failed', 'Cancelled'], templateCode: ' auth-email-login-code ', businessType: 'notification', createdAt: ['2026-10-01T00:00:00Z', '2026-10-31T23:59:59Z'] },
      conditionFilters: [
        { field: 'status', operator: QueryOperator.In, values: ['Failed', 'Cancelled'] },
        { field: 'createdAt', operator: QueryOperator.Between, values: ['2026-10-01T00:00:00Z', '2026-10-31T23:59:59Z'] },
      ],
      sorts: [{ field: 'sentAt', order: 'desc' }],
    }, RECORD_QUERY)
    expect(request).toEqual({
      page: { index: 1, size: 20 },
      keyword: 'a@b.com',
      filters: [
        { field: 'status', operator: 'in', value: 'Failed,Cancelled' },
        { field: 'createdAt', operator: 'ge', value: '2026-10-01T00:00:00Z' },
        { field: 'createdAt', operator: 'le', value: '2026-10-31T23:59:59Z' },
        { field: 'templateCode', operator: 'eq', value: 'auth-email-login-code' },
        { field: 'businessType', operator: 'eq', value: 'notification' },
      ],
      sorts: [{ field: 'sentAt', descending: true }],
    })
  })

  it('邮件走 emails.list、短信走 sms.list；删除逐项拒绝时翻成文案', async () => {
    const calls = stub(
      { items: [], total: 0, page: { index: 1, size: 20 } },
      { items: [], total: 0, page: { index: 1, size: 20 } },
      { succeeded: ['e1'], failed: [] },
      { succeeded: [], failed: [{ id: 's1', code: 'NOTIFICATION_CONFLICT_006' }] },
    )
    await emailRecordResource(t).page!({ page: 1, pageSize: 20, filters: {} })
    await smsRecordResource(t).page!({ page: 1, pageSize: 20, filters: {} })
    await emailRecordResource(t).remove!('e1')
    await expect(smsRecordResource(t).remove!('s1')).rejects.toThrow('notification.messaging.failures.record_busy')
    expect(calls.map(call => `${call.method} ${call.url}`)).toEqual([
      'POST /api/emails:list',
      'POST /api/sms:list',
      'DELETE /api/emails?ids=e1',
      'DELETE /api/sms?ids=s1',
    ])
  })
})

describe('发送记录的行操作', () => {
  it('重发不收排队中与发送中的，取消只收排队中的，删除不收发送中的', () => {
    expect(MessageSendStatus.filter(canResend)).toEqual(['Success', 'Failed', 'Cancelled'])
    expect(MessageSendStatus.filter(canCancel)).toEqual(['Pending'])
    expect(MessageSendStatus.filter(canDelete)).toEqual(['Pending', 'Success', 'Failed', 'Cancelled'])
    expect(formatAttempts({ attempts: 2, maxAttempts: 5 })).toBe('2/5')
  })

  it('重发与取消按种类走各自的批量接口，逐项拒绝即抛错', async () => {
    const calls = stub(
      { succeeded: ['e1'], failed: [] },
      { succeeded: ['s1'], failed: [] },
      { succeeded: [], failed: [{ id: 'e2', code: 'NOTIFICATION_CONFLICT_007' }] },
    )
    await resendRecord(t, 'email', 'e1')
    await cancelRecord(t, 'sms', 's1')
    await expect(cancelRecord(t, 'email', 'e2')).rejects.toThrow('notification.messaging.failures.record_not_pending')
    expect(calls).toEqual([
      { method: 'POST', url: '/api/emails:resend', body: { ids: ['e1'] } },
      { method: 'POST', url: '/api/sms:cancel', body: { ids: ['s1'] } },
      { method: 'POST', url: '/api/emails:cancel', body: { ids: ['e2'] } },
    ])
  })

  it('模板参数排版成多行 JSON，不合法的原样返回', () => {
    expect(prettyJson('{"code":"1234"}')).toBe('{\n  "code": "1234"\n}')
    expect(prettyJson('not json')).toBe('not json')
    expect(prettyJson(null)).toBe('')
  })
})
