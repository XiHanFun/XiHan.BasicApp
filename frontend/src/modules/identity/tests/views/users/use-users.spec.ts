import { describe, expect, it, vi } from 'vitest'
import { forceSignOut, generateTempPassword, setUserDisabled, setUserLocked, usePasswordReset, usersResource, useUserEditor } from '@/modules/identity/views/users/use-users'
import { configureRequest } from '~/request'
import { QueryOperator } from '~/types/contracts'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

function user(id: string, userName: string, extra: Record<string, unknown> = {}) {
  return {
    id,
    userName,
    displayName: userName.toUpperCase(),
    email: null,
    phoneNumber: null,
    homeTenantId: 'acme',
    status: 'Active',
    lastLoginAt: null,
    createdAt: '2026-10-28T00:00:00Z',
    createdBy: null,
    realName: null,
    gender: 'Unknown',
    birthday: null,
    country: null,
    remark: null,
    lastLoginIp: null,
    isExternalMember: false,
    roleNames: [],
    departmentName: null,
    isLocked: false,
    twoFactorEnabled: false,
    ...extra,
  }
}

interface Call { method: string, url: string, body: unknown }

function record(respond: (call: Call) => Response): Call[] {
  const calls: Call[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      const call = {
        method: init?.method ?? 'GET',
        url: String(url),
        body: init?.body
          ? JSON.parse(String(init.body))
          : undefined,
      }
      calls.push(call)
      return respond(call)
    }) as unknown as typeof fetch,
  })
  return calls
}

describe('usersResource', () => {
  it('把 SchemaPage 的查询换成 users.list 的分页请求（关键字、邮箱与手机号模糊、角色精确、状态多选、创建时间区间），结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, {
      items: [
        user('u-1', 'alice'),
      ],
      total: 21,
      page: { index: 2, size: 20 },
    }))
    const result = await usersResource().page!({
      page: 2,
      pageSize: 20,
      filters: {
        keyword: ' ali ',
        email: 'example.com',
        phoneNumber: '138',
        roleId: 'r-1',
        status: ['Locked', 'Disabled'],
      },
      conditionFilters: [
        {
          field: 'status',
          operator: QueryOperator.In,
          values: ['Locked', 'Disabled'],
        },
        {
          field: 'createdAt',
          operator: QueryOperator.Between,
          values: ['2026-10-01T00:00:00Z', '2026-10-31T23:59:59Z'],
        },
      ],
      sorts: [
        { field: 'createdAt', order: 'desc' },
      ],
    })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'ali',
      filters: [
        { field: 'status', operator: 'in', value: 'Locked,Disabled' },
        { field: 'createdAt', operator: 'ge', value: '2026-10-01T00:00:00Z' },
        { field: 'createdAt', operator: 'le', value: '2026-10-31T23:59:59Z' },
        { field: 'email', operator: 'contains', value: 'example.com' },
        { field: 'phoneNumber', operator: 'contains', value: '138' },
        { field: 'roleId', operator: 'eq', value: 'r-1' },
      ],
      sorts: [
        { field: 'createdAt', descending: true },
      ],
    })
    expect(result.items.map(u => u.userName)).toEqual(['alice'])
    expect(result.page).toMatchObject({
      totalCount: 21,
      pageIndex: 2,
      totalPages: 2,
      startRecord: 21,
      endRecord: 21,
      isLastPage: true,
    })
  })

  it('提交到导出中心：业务类型是 users.list，查询快照与列表发出的分页请求同一份', async () => {
    const calls = record(() => json(200, {
      items: [],
      total: 0,
      page: { index: 3, size: 50 },
    }))
    const resource = usersResource()
    const params = {
      page: 3,
      pageSize: 50,
      filters: { keyword: 'bob', email: '@acme' },
      sorts: [
        { field: 'userName', order: 'asc' as const },
      ],
    }
    await resource.page!(params)
    expect(resource.export?.businessType).toBe('users.list')
    expect(resource.export?.buildQuery?.(params)).toEqual(calls[0]!.body)
  })

  it('后端白名单外的筛选字段不静默丢弃，直接报错', async () => {
    record(() => json(200, {
      items: [],
      total: 0,
      page: { index: 1, size: 20 },
    }))
    await expect(usersResource().page!({
      page: 1,
      pageSize: 20,
      filters: { homeTenantId: 'acme' },
    })).rejects.toThrow('homeTenantId')
  })

  it('删除走 users.delete；被后端逐项拒绝（如删自己）时带着原因抛出', async () => {
    const calls = record(() => json(200, {
      succeeded: [],
      failed: [
        { id: 'u-1', code: 'IDENTITY_VALIDATION_009', message: 'IDENTITY_VALIDATION_009 的原因' },
      ],
    }))
    await expect(usersResource().remove!('u-1')).rejects.toThrow('IDENTITY_VALIDATION_009 的原因')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/users?ids=u-1' })
  })
})

describe('setUserLocked', () => {
  it('锁定走 users.lock、解锁走 users.unlock，逐项失败带着原因抛出', async () => {
    const calls = record(call => call.url.endsWith(':lock')
      ? json(200, {
          succeeded: ['u-1'],
          failed: [],
        })
      : json(200, {
          succeeded: [],
          failed: [
            { id: 'u-1', code: 'IDENTITY_NOTFOUND_001', message: 'IDENTITY_NOTFOUND_001 的原因' },
          ],
        }))
    await setUserLocked('u-1', true)
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/users:lock',
      body: {
        ids: ['u-1'],
      },
    })
    await expect(setUserLocked('u-1', false)).rejects.toThrow('IDENTITY_NOTFOUND_001 的原因')
    expect(calls[1]).toMatchObject({
      method: 'POST',
      url: '/api/users:unlock',
      body: {
        ids: ['u-1'],
      },
    })
  })
})

describe('setUserDisabled 与批量启停', () => {
  it('停用走 users.disable、启用走 users.enable；SchemaPage 的批量启停按行调同一对操作', async () => {
    const calls = record(() => json(200, {
      succeeded: ['u-1'],
      failed: [],
    }))
    await setUserDisabled('u-1', true)
    await usersResource().updateStatus!('u-2', true)
    expect(calls.map(c => [c.url, c.body])).toEqual([
      [
        '/api/users:disable',
        {
          ids: ['u-1'],
        },
      ],
      [
        '/api/users:enable',
        {
          ids: ['u-2'],
        },
      ],
    ])
  })

  it('锁定中的用户启用被拒：带着原因抛出', async () => {
    record(() => json(200, {
      succeeded: [],
      failed: [
        { id: 'u-1', code: 'IDENTITY_CONFLICT_012', message: 'IDENTITY_CONFLICT_012 的原因' },
      ],
    }))
    await expect(setUserDisabled('u-1', false)).rejects.toThrow('IDENTITY_CONFLICT_012 的原因')
  })
})

describe('forceSignOut', () => {
  it('列出此人的有效会话，一次交给 sessions.revoke，返回下线数', async () => {
    const calls = record(call => call.url.endsWith(':list')
      ? json(200, {
          items: [
            { id: 's-1' },
            { id: 's-2' },
          ],
          total: 2,
          page: { index: 1, size: 100 },
        })
      : json(200, {
          succeeded: ['s-1', 's-2'],
          failed: [],
        }))
    await expect(forceSignOut('u-1', '维护')).resolves.toBe(2)
    expect(calls[0]).toMatchObject({
      url: '/api/sessions:list',
      body: {
        filters: [
          { field: 'userId', operator: 'eq', value: 'u-1' },
        ],
      },
    })
    expect(calls[1]).toMatchObject({
      url: '/api/sessions:revoke',
      body: {
        ids: ['s-1', 's-2'],
        reason: '维护',
      },
    })
  })

  it('没有会话不调吊销，返回 0', async () => {
    const calls = record(() => json(200, {
      items: [],
      total: 0,
      page: { index: 1, size: 100 },
    }))
    await expect(forceSignOut('u-1', '维护')).resolves.toBe(0)
    expect(calls).toHaveLength(1)
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
  it('新建：字段去空白，空值发 null，未设置的性别不传；保存成功关掉弹窗', async () => {
    const calls = record(() => json(201, user('u-9', 'carol')))
    const editor = useUserEditor()
    editor.startCreate()
    expect(editor.mode.value).toBe('create')
    expect(editor.open.value).toBe(true)
    Object.assign(editor.form, {
      userName: ' carol ',
      displayName: ' Carol ',
      email: '  ',
      phoneNumber: '',
    })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users' })
    expect(calls[0]!.body).toEqual({
      userName: 'carol',
      displayName: 'Carol',
      email: null,
      phoneNumber: null,
      password: null,
      realName: null,
      gender: null,
      birthday: null,
      country: null,
      remark: null,
    })

    editor.startCreate()
    Object.assign(editor.form, {
      userName: 'dave',
      displayName: 'Dave',
      password: 'Secret123',
      realName: ' 戴夫 ',
      gender: 'Male',
      birthday: Date.UTC(1990, 4, 1),
      country: ' CN ',
      remark: ' 财务 ',
    })
    await editor.submit()
    expect(calls[1]!.body).toMatchObject({
      userName: 'dave',
      password: 'Secret123',
      realName: '戴夫',
      gender: 'Male',
      birthday: '1990-05-01T00:00:00.000Z',
      country: 'CN',
      remark: '财务',
    })
  })

  it('编辑：按行预填，提交资料（用户名不可改）；性别照发，生日换回 ISO 串', async () => {
    const calls = record(() => json(200, user('u-1', 'alice')))
    const editor = useUserEditor()
    editor.startEdit(user('u-1', 'alice', {
      email: 'a@example.com',
      phoneNumber: '123',
      realName: '爱丽丝',
      gender: 'Female',
      birthday: '1990-05-01T00:00:00+00:00',
      country: 'CN',
      remark: '备注',
    }) as never)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.readonly.value).toBe(false)
    expect(editor.form).toMatchObject({
      userName: 'alice',
      displayName: 'ALICE',
      email: 'a@example.com',
      phoneNumber: '123',
      password: '',
      realName: '爱丽丝',
      gender: 'Female',
      birthday: Date.UTC(1990, 4, 1),
      country: 'CN',
      remark: '备注',
    })
    editor.form.displayName = 'Alice W.'
    editor.form.phoneNumber = ''
    editor.form.remark = ''
    expect(await editor.submit()).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'PUT', url: '/api/users/u-1' })
    expect(calls[0]!.body).toEqual({
      displayName: 'Alice W.',
      email: 'a@example.com',
      phoneNumber: null,
      realName: '爱丽丝',
      gender: 'Female',
      birthday: '1990-05-01T00:00:00.000Z',
      country: 'CN',
      remark: null,
    })
  })

  it('外部成员：打开即只读，提交不发请求，也不读登录策略', async () => {
    const calls = record(() => json(200, user('u-1', 'alice')))
    const editor = useUserEditor(() => true)
    editor.startEdit(user('u-1', 'alice', { homeTenantId: 'fabrikam', isExternalMember: true }) as never)
    expect(editor.readonly.value).toBe(true)
    expect(editor.loadedPolicy.value).toBeNull()
    expect(await editor.submit()).toBe(false)
    expect(calls).toHaveLength(0)

    // 新建与本租户的用户不受影响
    editor.startCreate()
    expect(editor.readonly.value).toBe(false)
  })

  it('编辑：有设置权限才读登录策略，改了才提交', async () => {
    const calls = record((call) => {
      if (call.url.endsWith(':login-policy'))
        return json(200, { userId: 'u-1', allowMultiLogin: true, maxLoginDevices: 0 })
      if (call.url.endsWith(':set-login-policy'))
        return json(200, { userId: 'u-1', allowMultiLogin: false, maxLoginDevices: 0 })
      return json(200, user('u-1', 'alice'))
    })
    const editor = useUserEditor(() => true)
    editor.startEdit(user('u-1', 'alice') as never)
    await vi.waitFor(() => expect(editor.loadedPolicy.value).toEqual({ allowMultiLogin: true, maxLoginDevices: 0 }))
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/users/u-1:login-policy' })

    // 没改：只提交资料
    await editor.submit()
    expect(calls.map(c => c.url)).toEqual(['/api/users/u-1:login-policy', '/api/users/u-1'])

    // 改了：另提交登录策略
    editor.startEdit(user('u-1', 'alice') as never)
    await vi.waitFor(() => expect(editor.loadedPolicy.value).not.toBeNull())
    editor.loginPolicy.allowMultiLogin = false
    await editor.submit()
    expect(calls.at(-1)).toMatchObject({ method: 'POST', url: '/api/users/u-1:set-login-policy' })
    expect(calls.at(-1)!.body).toEqual({ allowMultiLogin: false, maxLoginDevices: 0 })

    // 没权限：不读也不写
    calls.length = 0
    const plain = useUserEditor()
    plain.startEdit(user('u-1', 'alice') as never)
    expect(plain.loadedPolicy.value).toBeNull()
    await plain.submit()
    expect(calls.map(c => c.url)).toEqual(['/api/users/u-1'])
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开；重新打开清掉上次的错误', async () => {
    record(() => json(400, {
      code: 'KERNEL_VALIDATION_003',
      status: 400,
      detail: 'invalid',
      traceId: 'x',
      errors: {
        userName: ['too short'],
        email: ['bad email'],
      },
    }, 'application/problem+json'))
    const editor = useUserEditor()
    editor.startCreate()
    Object.assign(editor.form, { userName: 'a', displayName: 'A', email: 'nope' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('userName')).toBe('too short')
    expect(editor.fieldError('email')).toBe('bad email')
    expect(editor.submitError.value).toBeNull()

    record(() => json(409, {
      code: 'IDENTITY_CONFLICT_001',
      status: 409,
      detail: 'user name taken',
      traceId: 'x',
    }, 'application/problem+json'))
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
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/users/u-1:reset-password',
      body: { newPassword: 'Typed4Admin' },
    })
  })

  it('密码被拒时字段级错误挂回字段、策略错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, {
      code: 'KERNEL_VALIDATION_003',
      status: 400,
      detail: 'invalid',
      traceId: 'x',
      errors: {
        newPassword: ['too short'],
      },
    }, 'application/problem+json'))
    const reset = usePasswordReset()
    reset.start(user('u-1', 'alice') as never)
    reset.form.newPassword = 'short'
    expect(await reset.submit()).toBeNull()
    expect(reset.open.value).toBe(true)
    expect(reset.fieldError('newPassword')).toBe('too short')
    expect(reset.submitError.value).toBeNull()

    record(() => json(400, {
      code: 'IDENTITY_VALIDATION_004',
      status: 400,
      detail: 'must contain letters and digits',
      traceId: 'x',
    }, 'application/problem+json'))
    expect(await reset.submit()).toBeNull()
    expect(reset.fieldError('newPassword')).toBeUndefined()
    expect(reset.submitError.value).toBe('must contain letters and digits')
  })
})
