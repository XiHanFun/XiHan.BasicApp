import type { InboxItem } from './inbox'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import {
  clampPage,
  confirmItem,
  deleteItem,
  INBOX_PAGE_SIZE,
  inboxCounts,
  inboxRequest,
  loadInbox,
  loadPending,
  markAllRead,
  markRead,
  needsAttention,
  PENDING_SAMPLE_SIZE,
  resolveLink,
  toHeaderItem,
  typeColor,
} from './inbox'

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

function item(patch: Partial<InboxItem> = {}): InboxItem {
  return {
    id: 'i1',
    notificationId: 'n1',
    title: '系统维护',
    content: '今晚停机',
    contentFormat: 'Text',
    notificationType: 'System',
    priority: 'Normal',
    status: 'Unread',
    publishedAt: '2026-10-29T08:00:00Z',
    readAt: null,
    confirmedAt: null,
    isGlobal: true,
    needConfirm: false,
    isMandatory: false,
    isBanner: false,
    isPopup: false,
    icon: null,
    link: null,
    expiresAt: null,
    ...patch,
  }
}

describe('我的消息的查询', () => {
  it('类型精确匹配、待处理走 pending eq true，按发布时间倒序（后端缺省，不传排序）', () => {
    expect(inboxRequest({ type: null, scope: 'all', page: 1 })).toEqual({ page: { index: 1, size: INBOX_PAGE_SIZE }, filters: [], sorts: [], keyword: null })
    expect(inboxRequest({ type: 'Security', scope: 'pending', page: 3 }).filters).toEqual([
      { field: 'notificationType', operator: 'eq', value: 'Security' },
      { field: 'pending', operator: 'eq', value: 'true' },
    ])
  })

  it('列表与待处理样本都走 inbox.list；样本与顶栏同取最近 100 条待处理的', async () => {
    const calls = stub(
      { items: [item()], total: 1, page: { index: 2, size: 20 } },
      { items: [item()], total: 1, page: { index: 1, size: 100 } },
    )
    expect((await loadInbox({ type: 'Todo', scope: 'all', page: 2 })).items).toHaveLength(1)
    expect(await loadPending()).toHaveLength(1)
    expect(calls.map(call => `${call.method} ${call.url}`)).toEqual(['POST /api/inbox:list', 'POST /api/inbox:list'])
    expect(calls[0]?.body).toMatchObject({ page: { index: 2, size: INBOX_PAGE_SIZE }, filters: [{ field: 'notificationType', operator: 'eq', value: 'Todo' }] })
    expect(calls[1]?.body).toMatchObject({ page: { index: 1, size: PENDING_SAMPLE_SIZE }, filters: [{ field: 'pending', operator: 'eq', value: 'true' }] })
  })

  it('删掉末页最后一条后页码退回最后一页', () => {
    expect(clampPage(3, 40)).toBe(2)
    expect(clampPage(2, 41)).toBe(2)
    expect(clampPage(2, 0)).toBe(1)
  })
})

describe('我的消息的计数与顶栏同步', () => {
  it('未读与待确认分别计数；需要关注 = 未读或需要确认而未确认', () => {
    const items = [
      item({ id: 'a' }),
      item({ id: 'b', status: 'Read', needConfirm: true }),
      item({ id: 'c', needConfirm: true }),
      item({ id: 'd', status: 'Read', needConfirm: true, confirmedAt: '2026-10-29T09:00:00Z' }),
    ]
    expect(inboxCounts(items)).toEqual({ unread: 2, confirm: 2 })
    expect(items.map(needsAttention)).toEqual([true, true, true, false])
  })

  it('收件箱条目换成顶栏通知中心的条目（4.x 字段名，空值去掉）', () => {
    expect(toHeaderItem(item({ status: 'Read', readAt: '2026-10-29T09:00:00Z', link: '/workbench', contentFormat: 'Markdown' }))).toEqual({
      basicId: 'i1',
      title: '系统维护',
      content: '今晚停机',
      contentFormat: 'Markdown',
      notificationType: 'System',
      notificationStatus: 'Read',
      sendTime: '2026-10-29T08:00:00Z',
      readTime: '2026-10-29T09:00:00Z',
      confirmTime: undefined,
      isGlobal: true,
      needConfirm: false,
      icon: undefined,
      link: '/workbench',
    })
  })
})

describe('我的消息的操作', () => {
  it('标为已读、确认、全部已读与删除各走自己的操作；条目已不在收件箱时报给定文案', async () => {
    const calls = stub(
      { succeeded: ['i1'], failed: [] },
      { count: 1 },
      { count: 3 },
      { succeeded: ['i1'], failed: [] },
      { succeeded: [], failed: [{ id: 'i2', code: 'NOTIFICATION_NOTFOUND_002' }] },
      { succeeded: [], failed: [{ id: 'i3', code: 'KERNEL_UNEXPECTED_000' }] },
    )
    await markRead('i1', 'gone')
    await confirmItem('i1')
    expect(await markAllRead()).toBe(3)
    await deleteItem('i1', 'gone')
    await expect(deleteItem('i2', 'gone')).rejects.toThrow('gone')
    await expect(markRead('i3', 'gone')).rejects.toThrow('KERNEL_UNEXPECTED_000')
    expect(calls.map(call => `${call.method} ${call.url}`)).toEqual([
      'POST /api/inbox:mark-read',
      'POST /api/inbox/i1:confirm',
      'POST /api/inbox:mark-all-read',
      'DELETE /api/inbox?ids=i1',
      'DELETE /api/inbox?ids=i2',
      'POST /api/inbox:mark-read',
    ])
    expect(calls[0]?.body).toEqual({ ids: ['i1'] })
  })

  it('链接：站内路由走跳转、http(s) 新窗口打开，其它协议不当链接', () => {
    expect(resolveLink('/workbench?tab=1')).toEqual({ kind: 'route', path: '/workbench?tab=1' })
    expect(resolveLink(' https://example.com/a ')).toEqual({ kind: 'external', url: 'https://example.com/a' })
    expect(resolveLink('javascript:alert(1)')).toBeNull()
    expect(resolveLink('//evil.example')).toBeNull()
    expect(resolveLink('')).toBeNull()
    expect(resolveLink(null)).toBeNull()
  })

  it('图标按类型着色，取组件库的语气色阶', () => {
    expect(typeColor('Emergency')).toBe('var(--xh-color-danger-600)')
    expect(typeColor('Todo')).toBe('var(--xh-color-purple-500)')
    expect(typeColor('System')).toBe('var(--xh-color-info-600)')
  })
})
