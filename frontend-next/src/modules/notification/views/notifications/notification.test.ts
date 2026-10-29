import type { NotificationDetail, NotificationForm, RecipientRow } from './notification'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'
import { contentFormatLabel, deliveryChannelLabel, notificationTypeLabel, notificationTypeTone, priorityLabel, targetTypeLabel } from '../../labels'
import {
  emptyNotificationForm,
  EXPORT_LIMIT,
  findNotificationProblem,
  flattenDepartments,
  loadDepartmentOptions,
  loadRecipients,
  loadRoleOptions,
  loadUnreadForExport,
  normalizeChannels,
  NOTIFICATION_QUERY,
  notificationResource,
  problemMessage,
  readRate,
  recipientFilters,
  toCreateBody,
  toNotificationForm,
  toUpdateBody,
  unreadCsv,
} from './notification'

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

const USER_A = '0b8e6a52-3f5c-4f7e-9a1d-2c4b6e8f0a11'
const USER_B = '7d2f4c19-8a3b-4e6d-b5c1-9e0f2a4b6c22'

function form(patch: Partial<NotificationForm> = {}): NotificationForm {
  return { ...emptyNotificationForm(), title: '系统维护', ...patch }
}

function recipient(index: number): RecipientRow {
  return { userId: `u${index}`, userName: `user${index}`, displayName: '', status: 'Unread', readAt: null, confirmedAt: null, receivedAt: '2026-10-29T00:00:00Z' }
}

describe('通知公告的列表筛选', () => {
  it('类型多选合成 in，是否发布按 true / false 精确匹配，关键字单独下发，发布时间可排序', () => {
    const request = toPageRequest({
      page: 2,
      pageSize: 20,
      filters: { keyword: ' 维护 ', notificationType: ['System', 'Emergency'], isPublished: 'false' },
      conditionFilters: [{ field: 'notificationType', operator: QueryOperator.In, values: ['System', 'Emergency'] }],
      sorts: [{ field: 'publishedAt', order: 'desc' }],
    }, NOTIFICATION_QUERY)
    expect(request).toEqual({
      page: { index: 2, size: 20 },
      keyword: '维护',
      filters: [
        { field: 'notificationType', operator: 'in', value: 'System,Emergency' },
        { field: 'isPublished', operator: 'eq', value: 'false' },
      ],
      sorts: [{ field: 'publishedAt', descending: true }],
    })
  })

  it('列表走 notifications.list；删除逐项拒绝时认得的错误码翻成文案', async () => {
    const calls = stub(
      { items: [], total: 0, page: { index: 1, size: 20 } },
      { succeeded: ['n1'], failed: [] },
      { succeeded: [], failed: [{ id: 'n2', code: 'NOTIFICATION_NOTFOUND_001' }] },
    )
    const resource = notificationResource(t)
    await resource.page!({ page: 1, pageSize: 20, filters: {} })
    await resource.remove!('n1')
    await expect(resource.remove!('n2')).rejects.toThrow('notification.notifications.msg_not_found')
    expect(calls.map(call => `${call.method} ${call.url}`)).toEqual([
      'POST /api/notifications:list',
      'DELETE /api/notifications?ids=n1',
      'DELETE /api/notifications?ids=n2',
    ])
  })
})

describe('通知公告的表单', () => {
  it('提交前找出第一处问题：标题空白、目标没选对象、手填的用户 ID 不是 GUID', () => {
    expect(findNotificationProblem(form())).toBeNull()
    expect(findNotificationProblem(form({ title: '  ' }))).toBe('title')
    expect(findNotificationProblem(form({ targetType: 'User', targetIds: [] }))).toBe('user_required')
    expect(findNotificationProblem(form({ targetType: 'User', targetIds: [' '] }))).toBe('user_required')
    expect(findNotificationProblem(form({ targetType: 'Role', targetIds: [] }))).toBe('role_required')
    expect(findNotificationProblem(form({ targetType: 'Department', targetIds: [] }))).toBe('department_required')
    // 4.x 的正整数 ID 在 6.0 不再合法
    expect(findNotificationProblem(form({ targetType: 'User', targetIds: ['10086'] }))).toBe('user_id_invalid')
    expect(findNotificationProblem(form({ targetType: 'User', targetIds: [USER_A, ` ${USER_B.toUpperCase()} `] }))).toBeNull()
    // 角色、部门是选出来的，不核对格式
    expect(findNotificationProblem(form({ targetType: 'Role', targetIds: ['r1'] }))).toBeNull()
    expect(problemMessage(t, 'user_id_invalid')).toBe('notification.notifications.msg_user_id_invalid')
    expect(problemMessage(t, 'department_required')).toBe('notification.notifications.msg_department_required')
  })

  it('新建只存草稿：去空白写 null、目标 ID 去重、时间转 ISO，全员时不带目标', () => {
    const startsAt = Date.parse('2026-11-01T00:00:00Z')
    expect(toCreateBody(form({ title: ' 系统维护 ', content: ' ', icon: ' lucide:megaphone ', targetType: 'User', targetIds: [USER_A, ` ${USER_A} `, USER_B], startsAt }))).toEqual({
      title: '系统维护',
      content: null,
      contentFormat: 'Markdown',
      notificationType: 'System',
      priority: 'Normal',
      targetType: 'User',
      targetIds: [USER_A, USER_B],
      deliveryChannels: ['InApp'],
      icon: 'lucide:megaphone',
      link: null,
      businessType: null,
      businessId: null,
      needConfirm: false,
      isMandatory: false,
      isBanner: false,
      isPopup: false,
      startsAt: '2026-11-01T00:00:00.000Z',
      expiresAt: null,
      remark: null,
      publishImmediately: false,
    })
    expect(toCreateBody(form({ targetType: 'All', targetIds: [USER_A] })).targetIds).toEqual([])
  })

  it('草稿回填表单带上原目标与时间；修改原样带回表单不暴露的业务字段、不带立即发布', () => {
    const detail: NotificationDetail = {
      id: 'n1',
      title: '演练',
      content: '# 标题',
      contentFormat: 'Markdown',
      notificationType: 'Emergency',
      priority: 'Urgent',
      icon: null,
      link: '/workbench',
      businessType: 'drill',
      businessId: 'd-1',
      deliveryChannels: ['Sms', 'InApp'],
      targetType: 'Role',
      targetIds: ['r1', 'r2'],
      needConfirm: true,
      isMandatory: true,
      isBanner: false,
      isPopup: true,
      startsAt: null,
      expiresAt: '2026-12-31T16:00:00Z',
      isPublished: false,
      publishedAt: null,
      publisherId: null,
      recipientCount: 0,
      remark: '季度演练',
      createdAt: '2026-10-29T00:00:00Z',
      modifiedAt: null,
    }
    const filled = toNotificationForm(detail)
    expect(filled).toMatchObject({ id: 'n1', targetType: 'Role', targetIds: ['r1', 'r2'], deliveryChannels: ['InApp', 'Sms'], icon: '', link: '/workbench', startsAt: null, expiresAt: Date.parse('2026-12-31T16:00:00Z') })
    const body = toUpdateBody({ ...filled, id: 'n1' })
    expect(body).toMatchObject({ id: 'n1', targetIds: ['r1', 'r2'], deliveryChannels: ['InApp', 'Sms'], businessType: 'drill', businessId: 'd-1', remark: '季度演练', expiresAt: '2026-12-31T16:00:00.000Z' })
    expect(body).not.toHaveProperty('publishImmediately')
  })
})

describe('通知公告的投递渠道', () => {
  it('站内信总在内、去重并按契约顺序排；缺省只有站内信', () => {
    expect(emptyNotificationForm().deliveryChannels).toEqual(['InApp'])
    expect(normalizeChannels(['Sms', 'Email', 'Sms'])).toEqual(['InApp', 'Email', 'Sms'])
    expect(normalizeChannels([])).toEqual(['InApp'])
    expect(normalizeChannels(null)).toEqual(['InApp'])
    // 不认得的值不带上去
    expect(normalizeChannels(['Bot'])).toEqual(['InApp'])
    // 表单里被取消的站内信在提交时补回（后端拒绝不含站内信的 NOTIFICATION_VALIDATION_010）
    expect(toCreateBody(form({ deliveryChannels: ['Email'] })).deliveryChannels).toEqual(['InApp', 'Email'])
    expect(deliveryChannelLabel(t, 'Sms')).toBe('notification.notifications.channel_sms')
    expect(deliveryChannelLabel(t, 'InApp')).toBe('notification.notifications.channel_in_app')
  })
})

describe('通知公告的运营数据', () => {
  it('已读率取整，没有接收人时为 0；要求确认的才有「未确认」一档', () => {
    expect(readRate({ recipientCount: 3, readCount: 2 })).toBe(67)
    expect(readRate({ recipientCount: 0, readCount: 0 })).toBe(0)
    expect(recipientFilters(false)).toEqual(['Unread', 'Read', 'All'])
    expect(recipientFilters(true)).toEqual(['Unread', 'Read', 'Unconfirmed', 'All'])
  })

  it('接收人按筛选与页码分页取', async () => {
    const calls = stub({ items: [recipient(1)], total: 1, page: { index: 2, size: 10 } })
    const page = await loadRecipients('n1', 'Unconfirmed', 2)
    expect(page.items).toHaveLength(1)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/notifications/n1:recipients', body: { filter: 'Unconfirmed', page: { index: 2, size: 10 } } })
  })

  it('导出未读逐页取到上限为止，超出即标记截断', async () => {
    const pageOf = (start: number) => Array.from({ length: 500 }, (_, i) => recipient(start + i))
    const calls = stub(
      { items: pageOf(0), total: 1200, page: { index: 1, size: 500 } },
      { items: pageOf(500), total: 1200, page: { index: 2, size: 500 } },
    )
    const result = await loadUnreadForExport('n1')
    expect(result.rows).toHaveLength(EXPORT_LIMIT)
    expect(result.truncated).toBe(true)
    expect(calls.map(call => (call.body as { filter: string, page: { index: number } }).page.index)).toEqual([1, 2])
    expect(calls.every(call => (call.body as { filter: string }).filter === 'Unread')).toBe(true)
  })

  it('人数不足一页时只取一次、不截断', async () => {
    const calls = stub({ items: [recipient(1), recipient(2)], total: 2, page: { index: 1, size: 500 } })
    expect(await loadUnreadForExport('n1')).toMatchObject({ truncated: false })
    expect(calls).toHaveLength(1)
  })

  it('未读 CSV 带 BOM、CRLF，每格加引号并转义，显示名为空写 -', () => {
    const csv = unreadCsv(['用户名', '显示名', '收到时间'], [{ ...recipient(1), userName: 'a"b', displayName: '' }], value => value.slice(0, 10))
    expect(csv.charCodeAt(0)).toBe(0xFEFF)
    expect(csv.slice(1)).toBe('"用户名","显示名","收到时间"\r\n"a""b","-","2026-10-29"')
  })
})

describe('通知公告的目标候选', () => {
  it('角色取本侧启用的；部门树展平，停用的不列出但下级照常列', async () => {
    const node = (id: string, status: 'Active' | 'Disabled', children: unknown[] = []) => ({ department: { id, name: `D-${id}`, status }, children })
    const calls = stub(
      { items: [{ id: 'r1', name: '管理员', code: 'admin' }], total: 1, page: { index: 1, size: 500 } },
      { items: [node('a', 'Active', [node('b', 'Disabled', [node('c', 'Active')])])] },
    )
    expect(await loadRoleOptions()).toEqual([{ label: '管理员', value: 'r1' }])
    expect(await loadDepartmentOptions()).toEqual([{ label: 'D-a', value: 'a' }, { label: 'D-c', value: 'c' }])
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/roles:list', body: { filters: [{ field: 'status', operator: 'eq', value: 'Active' }] } })
    expect(calls[1]).toMatchObject({ method: 'GET', url: '/api/departments:tree' })
    expect(flattenDepartments([])).toEqual([])
  })
})

describe('通知包的枚举文案', () => {
  it('每个契约字面量有自己的文案键与语气', () => {
    expect(notificationTypeLabel(t, 'Emergency')).toBe('notification.notifications.type_emergency')
    expect(notificationTypeTone('Emergency')).toBe('danger')
    expect(notificationTypeTone('Todo')).toBe('neutral')
    expect(targetTypeLabel(t, 'Department')).toBe('notification.notifications.target_department')
    expect(priorityLabel(t, 'Urgent')).toBe('notification.notifications.priority_urgent')
    expect(contentFormatLabel(t, 'Html')).toBe('notification.notifications.format_html')
  })
})
