import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { generateTempPassword, setUserLocked, usePasswordReset, usersResource, useUserEditor } from './use-users'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function user(id: string, userName: string, extra: Record<string, unknown> = {}) {
  return { id, userName, displayName: userName.toUpperCase(), email: null, phoneNumber: null, homeTenantId: 'acme', status: 'Active', lastLoginAt: null, createdAt: '2026-10-28T00:00:00Z', createdBy: null, ...extra }
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = { method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('usersResource', () => {
  it('把 SchemaPage 的查询换成 users.list 的分页请求（关键字、邮箱模糊、状态精确），结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [user('u-1', 'alice')], total: 21, page: { index: 2, size: 20 } }))
    const result = await usersResource().page!({ page: 2, pageSize: 20, filters: { keyword: ' ali ', email: 'example.com', status: 'Locked' }, sorts: [{ field: 'createdAt', order: 'desc' }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'ali',
      filters: [{ field: 'email', operator: 'contains', value: 'example.com' }, { field: 'status', operator: 'eq', value: 'Locked' }],
      sorts: [{ field: 'createdAt', descending: true }],
    })
    expect(result.items.map(u => u.userName)).toEqual(['alice'])
    expect(result.page).toMatchObject({ totalCount: 21, pageIndex: 2, totalPages: 2, startRecord: 21, endRecord: 21, isLastPage: true })
  })

  it('后端白名单外的筛选字段不静默丢弃，直接报错', async () => {
    record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await expect(usersResource().page!({ page: 1, pageSize: 20, filters: { homeTenantId: 'acme' } })).rejects.toThrow('homeTenantId')
  })

  it('删除走 users.delete；被后端逐项拒绝（如删自己）时带着错误码抛出', async () => {
    const calls = record(() => json(200, { succeeded: [], failed: [{ id: 'u-1', code: 'IDENTITY_VALIDATION_009' }] }))
    await expect(usersResource().remove!('u-1')).rejects.toThrow('IDENTITY_VALIDATION_009')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/users?ids=u-1' })
  })
})

describe('setUserLocked', () => {
  it('锁定走 users.lock、解锁走 users.unlock，逐项失败带着错误码抛出', async () => {
    const calls = record(call => call.url.endsWith(':lock')
      ? json(200, { succeeded: ['u-1'], failed: [] })
      : json(200, { succeeded: [], failed: [{ id: 'u-1', code: 'IDENTITY_NOTFOUND_001' }] }))
    await setUserLocked('u-1', true)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users:lock', body: { ids: ['u-1'] } })
    await expect(setUserLocked('u-1', false)).rejects.toThrow('IDENTITY_NOTFOUND_001')
    expect(calls[1]).toMatchObject({ method: 'POST', url: '/api/users:unlock', body: { ids: ['u-1'] } })
  })
})

describe('generateTempPassword', () => {
  it('满足后端密码策略（至少 8 位、含字母与数字），且不含易混淆字符', () => {
    for (let i = 0; i < 20; i++) {
      const password = generateTempPassword()
      expect(password).toHaveLength(12)
      expect(password).toMatch(/[a-z]/i)
      expect(password).toMatch(/\d/)
      expect(password).not.toMatch(/[0O1lI]/)
    }
  })
})

describe('useUserEditor', () => {
  it('新建：字段去空白，空邮箱 / 手机号发 null，初始密码留空发 null；保存成功关掉弹窗', async () => {
    const calls = record(() => json(201, user('u-9', 'carol')))
    const editor = useUserEditor()
    editor.startCreate()
    expect(editor.mode.value).toBe('create')
    expect(editor.open.value).toBe(true)
    Object.assign(editor.form, { userName: ' carol ', displayName: ' Carol ', email: '  ', phoneNumber: '' })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users' })
    expect(calls[0]!.body).toEqual({ userName: 'carol', displayName: 'Carol', email: null, phoneNumber: null, password: null })

    editor.startCreate()
    Object.assign(editor.form, { userName: 'dave', displayName: 'Dave', password: 'Secret123' })
    await editor.submit()
    expect(calls[1]!.body).toMatchObject({ userName: 'dave', password: 'Secret123' })
  })

  it('编辑：按行预填，只提交显示名、邮箱与手机号（用户名不可改）', async () => {
    const calls = record(() => json(200, user('u-1', 'alice')))
    const editor = useUserEditor()
    editor.startEdit(user('u-1', 'alice', { email: 'a@example.com', phoneNumber: '123' }) as never)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({ userName: 'alice', displayName: 'ALICE', email: 'a@example.com', phoneNumber: '123', password: '' })
    editor.form.displayName = 'Alice W.'
    editor.form.phoneNumber = ''
    expect(await editor.submit()).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'PUT', url: '/api/users/u-1' })
    expect(calls[0]!.body).toEqual({ displayName: 'Alice W.', email: 'a@example.com', phoneNumber: null })
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开；重新打开清掉上次的错误', async () => {
    record(() => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { userName: ['too short'], email: ['bad email'] } }, 'application/problem+json'))
    const editor = useUserEditor()
    editor.startCreate()
    Object.assign(editor.form, { userName: 'a', displayName: 'A', email: 'nope' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('userName')).toBe('too short')
    expect(editor.fieldError('email')).toBe('bad email')
    expect(editor.submitError.value).toBeNull()

    record(() => json(409, { code: 'IDENTITY_CONFLICT_001', status: 409, detail: 'user name taken', traceId: 'x' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.fieldError('userName')).toBeUndefined()
    expect(editor.submitError.value).toBe('user name taken')

    editor.startCreate()
    expect(editor.submitError.value).toBeNull()
    expect(editor.form.userName).toBe('')
  })
})

describe('usePasswordReset', () => {
  it('打开时预填随机密码、可重新生成；提交 users.reset-password 成功后关掉弹窗并交回设下的密码', async () => {
    const calls = record(() => json(200, { userId: 'u-1', changedAt: '2026-10-28T00:00:00Z', sessionsRevoked: true }))
    const reset = usePasswordReset()
    reset.start(user('u-1', 'alice') as never)
    expect(reset.open.value).toBe(true)
    const first = reset.form.newPassword
    expect(first).toHaveLength(12)
    reset.regenerate()
    expect(reset.form.newPassword).toHaveLength(12)
    reset.form.newPassword = 'Typed4Admin'
    expect(await reset.submit()).toBe('Typed4Admin')
    expect(reset.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users/u-1:reset-password', body: { newPassword: 'Typed4Admin' } })
  })

  it('密码被拒时字段级错误挂回字段、策略错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { newPassword: ['too short'] } }, 'application/problem+json'))
    const reset = usePasswordReset()
    reset.start(user('u-1', 'alice') as never)
    reset.form.newPassword = 'short'
    expect(await reset.submit()).toBeNull()
    expect(reset.open.value).toBe(true)
    expect(reset.fieldError('newPassword')).toBe('too short')
    expect(reset.submitError.value).toBeNull()

    record(() => json(400, { code: 'IDENTITY_VALIDATION_004', status: 400, detail: 'must contain letters and digits', traceId: 'x' }, 'application/problem+json'))
    expect(await reset.submit()).toBeNull()
    expect(reset.fieldError('newPassword')).toBeUndefined()
    expect(reset.submitError.value).toBe('must contain letters and digits')
  })
})
