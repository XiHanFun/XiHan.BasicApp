import type { DepartmentRow } from './use-departments'
import { describe, expect, it, vi } from 'vitest'
import { ref } from 'vue'
import { configureRequest } from '~/request'
import { departmentsResource, findDepartmentRow, removeMembership, setPrimaryMembership, toParentOptions, useDepartmentDetail, useDepartmentEditor, useMembershipEditor } from './use-departments'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

function department(id: string, code: string, name: string, parentId: string | null, extra: Record<string, unknown> = {}) {
  return {
    id,
    code,
    name,
    departmentType: 'Department',
    parentId,
    leaderUserId: null,
    status: 'Active',
    order: 0,
    phone: null,
    email: null,
    address: null,
    description: null,
    createdAt: '2026-10-28T00:00:00Z',
    ...extra,
  }
}

function membership(id: string, userName: string, extra: Record<string, unknown> = {}) {
  return {
    id,
    userId: `u-${id}`,
    userName,
    displayName: userName.toUpperCase(),
    departmentId: 'd-2',
    departmentName: 'R&D',
    positionId: null,
    positionName: null,
    isPrimary: false,
    employeeNumber: null,
    jobGrade: null,
    hiredAt: null,
    order: 0,
    createdAt: '2026-10-28T00:00:00Z',
    remark: null,
    ...extra,
  }
}

const hq = department('d-1', 'hq', 'Headquarters', null)
const rd = department('d-2', 'rd', 'R&D', 'd-1', {
  departmentType: 'Center',
  leaderUserId: 'u-9',
  order: 10,
  phone: '010-1000',
  email: 'rd@example.com',
  address: 'Building 7',
  description: 'Research',
})
const lab = department('d-4', 'lab', 'Lab', 'd-2')
const mkt = department('d-3', 'mkt', 'Marketing', 'd-1', { order: 20 })

const treeBody = {
  items: [
    {
      department: hq,
      memberCount: 1,
      children: [
        {
          department: rd,
          memberCount: 2,
          children: [
            { department: lab, memberCount: 0, children: [] },
          ],
        },
        { department: mkt, memberCount: 0, children: [] },
      ],
    },
  ],
  total: 4,
}

function row(record: ReturnType<typeof department>, children: DepartmentRow[] = [], memberCount = 0): DepartmentRow {
  return { ...record, memberCount, children } as DepartmentRow
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

const notFound = (call: Call) => json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: call.url }, 'application/problem+json')

describe('departmentsResource', () => {
  it('把 departments.tree 的节点换成树表行（部门字段平铺、成员数、children），并把树交给页面', async () => {
    const calls = record(() => json(200, treeBody))
    let handed: DepartmentRow[] = []
    const rows = await departmentsResource((tree) => {
      handed = tree
    }).tree!({
      page: 1,
      pageSize: 20,
      filters: {},
      sorts: [],
    })
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/departments:tree' })
    expect(rows.map(r => `${r.code}:${r.memberCount}`)).toEqual(['hq:1'])
    expect(rows[0]!.children.map(r => `${r.code}:${r.memberCount}:${r.children.length}`)).toEqual(['rd:2:1', 'mkt:0:0'])
    expect(rows[0]!.children[0]).toMatchObject({
      id: 'd-2',
      name: 'R&D',
      parentId: 'd-1',
      leaderUserId: 'u-9',
      description: 'Research',
    })
    expect(handed).toBe(rows)
  })

  it('部门树只接受关键字：别的筛选与排序出现即报错，不发请求', async () => {
    const calls = record(() => json(200, treeBody))
    const resource = departmentsResource()
    await expect(resource.tree!({
      page: 1,
      pageSize: 20,
      filters: { status: 'Active' },
    })).rejects.toThrow('只接受关键字')
    await expect(resource.tree!({
      page: 1,
      pageSize: 20,
      filters: {},
      sorts: [
        { field: 'name', order: 'asc' },
      ],
    })).rejects.toThrow('只接受关键字')
    expect(calls).toHaveLength(0)
    // 重置后筛选值是 null，全是空白也一样，都不算条件
    await resource.tree!({
      page: 1,
      pageSize: 20,
      filters: { keyword: null },
    })
    await resource.tree!({
      page: 1,
      pageSize: 20,
      filters: { keyword: '   ' },
    })
    expect(calls.map(c => c.url)).toEqual(['/api/departments:tree', '/api/departments:tree'])
  })

  it('带关键字时表格只显示命中的部门，交给页面的仍是整棵树', async () => {
    const matched = {
      items: [
        { department: lab, memberCount: 0, children: [] },
      ],
      total: 1,
    }
    const calls = record(call => json(200, call.url.includes('keyword=')
      ? matched
      : treeBody))
    let handed: DepartmentRow[] = []
    const rows = await departmentsResource((tree) => {
      handed = tree
    }).tree!({
      page: 1,
      pageSize: 20,
      filters: { keyword: ' lab ' },
    })
    expect(calls.map(c => c.url).sort()).toEqual(['/api/departments:tree', '/api/departments:tree?keyword=lab'])
    expect(rows.map(r => r.code)).toEqual(['lab'])
    expect(handed.map(r => r.code)).toEqual(['hq'])
    expect(handed[0]!.children.map(r => r.code)).toEqual(['rd', 'mkt'])
    // 整棵树里按主键能找到被搜出来的行，子部门是全的
    expect(findDepartmentRow(handed, 'd-2')?.children.map(r => r.code)).toEqual(['lab'])
    expect(findDepartmentRow(handed, 'no-such')).toBeUndefined()

    // 页面不要树时不多发那一次
    const single = record(() => json(200, matched))
    await departmentsResource().tree!({
      page: 1,
      pageSize: 20,
      filters: { keyword: 'lab' },
    })
    expect(single).toHaveLength(1)
  })

  it('删除被后端逐项拒绝时带着原因抛出；启停取当前值整体替换，类型、负责人与联系方式原样带回', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE') {
        return json(200, {
          succeeded: [],
          failed: [
            { id: 'd-2', code: 'ORGANIZATION_CONFLICT_002', message: 'ORGANIZATION_CONFLICT_002 的原因' },
          ],
        })
      }
      if (call.method === 'GET') {
        return json(200, {
          department: rd,
          ancestors: [hq],
          leaderUserName: 'leader',
          childCount: 1,
          memberCount: 2,
        })
      }
      return json(200, { ...rd, status: 'Disabled' })
    })
    const resource = departmentsResource()
    await expect(resource.remove!('d-2')).rejects.toThrow('ORGANIZATION_CONFLICT_002 的原因')
    expect(calls[0]!.url).toBe('/api/departments?ids=d-2')
    await resource.updateStatus!('d-2', false)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({
      url: '/api/departments/d-2',
      body: {
        name: 'R&D',
        departmentType: 'Center',
        leaderUserId: 'u-9',
        order: 10,
        phone: '010-1000',
        email: 'rd@example.com',
        address: 'Building 7',
        description: 'Research',
        status: 'Disabled',
      },
    })
  })
})

describe('toParentOptions', () => {
  it('上级候选是整棵树；编辑时去掉部门自己连同子树', () => {
    const tree = [
      row(hq, [
        row(rd, [
          row(lab),
        ]),
        row(mkt),
      ]),
    ]
    expect(toParentOptions(tree)).toEqual([
      {
        label: 'Headquarters',
        value: 'd-1',
        children: [
          {
            label: 'R&D',
            value: 'd-2',
            children: [
              { label: 'Lab', value: 'd-4' },
            ],
          },
          { label: 'Marketing', value: 'd-3' },
        ],
      },
    ])
    expect(toParentOptions(tree, 'd-2')).toEqual([
      {
        label: 'Headquarters',
        value: 'd-1',
        children: [
          { label: 'Marketing', value: 'd-3' },
        ],
      },
    ])
  })
})

describe('useDepartmentEditor', () => {
  const tree = ref([
    row(hq, [
      row(rd, [
        row(lab),
      ]),
      row(mkt),
    ]),
  ])

  it('新增子部门预填上级，提交 departments.create', async () => {
    const calls = record(() => json(201, department('d-9', 'ops', 'Ops', 'd-1')))
    const editor = useDepartmentEditor(tree)
    editor.startCreate('d-1')
    expect(editor.mode.value).toBe('create')
    expect(editor.form.parentId).toBe('d-1')
    // 类型缺省是部门
    expect(editor.form.departmentType).toBe('Department')
    Object.assign(editor.form, {
      code: ' ops ',
      name: ' Ops ',
      description: '  ',
      departmentType: 'Team',
      phone: ' 010-2000 ',
      email: '  ',
      address: 'Floor 3',
    })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/departments',
      body: {
        code: 'ops',
        name: 'Ops',
        departmentType: 'Team',
        parentId: 'd-1',
        leaderUserId: null,
        order: 0,
        phone: '010-2000',
        email: null,
        address: 'Floor 3',
        description: null,
      },
    })
  })

  it('负责人按关键字检索用户（只认最后一次检索），新建时一并提交', async () => {
    const calls = record(call => call.url.startsWith('/api/users')
      ? json(200, {
          items: [
            { id: 'u-7', userName: 'alice', displayName: 'Alice' },
          ],
          total: 1,
          page: { index: 1, size: 100 },
        })
      : json(201, department('d-9', 'ops', 'Ops', null)))
    const editor = useDepartmentEditor(tree)
    editor.startCreate()
    await editor.searchLeaders(' ali ')
    expect(calls[0]).toMatchObject({
      method: 'POST',
      url: '/api/users:list',
      body: {
        keyword: 'ali',
        filters: [
          { field: 'isSupportMember', operator: 'eq', value: 'false' },
        ],
      },
    })
    expect(editor.leaderOptions.value).toEqual([
      { label: 'Alice', value: 'u-7', description: 'alice' },
    ])
    Object.assign(editor.form, { code: 'ops', name: 'Ops', leaderUserId: 'u-7' })
    expect(await editor.submit()).toBe(true)
    expect(calls[1]).toMatchObject({
      method: 'POST',
      url: '/api/departments',
      body: { code: 'ops', leaderUserId: 'u-7' },
    })
  })

  it('编辑时上级候选去掉自己的子树；上级变了先整体更新再 departments.move，没变不移动', async () => {
    const calls = record(call => call.method === 'GET'
      ? json(200, { id: 'u-9', userName: 'leader', displayName: 'Leader' })
      : json(200, call.url.endsWith(':move')
          ? { ...rd, parentId: null }
          : rd))
    const editor = useDepartmentEditor(tree)
    editor.startEdit(row(rd, [
      row(lab),
    ], 2), true)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({
      code: 'rd',
      name: 'R&D',
      departmentType: 'Center',
      parentId: 'd-1',
      leaderUserId: 'u-9',
      order: 10,
      phone: '010-1000',
      email: 'rd@example.com',
      address: 'Building 7',
      description: 'Research',
      status: 'Active',
    })
    // 现任负责人按用户详情放进候选，与检索结果同样显示显示名与登录名
    await vi.waitFor(() => expect(editor.leaderOptions.value).toEqual([
      { label: 'Leader', value: 'u-9', description: 'leader' },
    ]))
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/users/u-9' })
    calls.length = 0
    expect(editor.parentOptions.value.map(o => o.value)).toEqual(['d-1'])
    expect(editor.parentOptions.value[0]!.children?.map(o => o.value)).toEqual(['d-3'])

    editor.form.name = 'Research & Development'
    editor.form.parentId = null
    expect(await editor.submit()).toBe(true)
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/departments/d-2', 'POST /api/departments/d-2:move'])
    expect(calls[0]!.body).toEqual({
      name: 'Research & Development',
      departmentType: 'Center',
      leaderUserId: 'u-9',
      order: 10,
      phone: '010-1000',
      email: 'rd@example.com',
      address: 'Building 7',
      description: 'Research',
      status: 'Active',
    })
    expect(calls[1]!.body).toEqual({ parentId: null })

    calls.length = 0
    editor.startEdit(row(rd, [
      row(lab),
    ], 2))
    expect(await editor.submit()).toBe(true)
    // 看不了用户时不取负责人，只有一次整体更新
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/departments/d-2'])
  })

  it('字段级错误挂回字段，其余错误（如移动成环）给整条提示，弹窗保持打开', async () => {
    record(() => json(400, {
      code: 'KERNEL_VALIDATION_003',
      status: 400,
      detail: 'invalid',
      traceId: 'x',
      errors: {
        code: ['pattern'],
      },
    }, 'application/problem+json'))
    const editor = useDepartmentEditor(tree)
    editor.startCreate()
    Object.assign(editor.form, { code: 'bad code!', name: 'x' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('code')).toBe('pattern')
    expect(editor.submitError.value).toBeNull()

    record(call => call.url.endsWith(':move')
      ? json(400, {
          code: 'ORGANIZATION_VALIDATION_002',
          status: 400,
          detail: 'cycle',
          traceId: 'x',
        }, 'application/problem+json')
      : json(200, rd))
    editor.startEdit(row(rd))
    editor.form.parentId = 'd-4'
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('cycle')
  })
})

describe('useDepartmentDetail', () => {
  it('打开即取详情与本部门成员（按部门筛、按排序与创建时间）；路径由远到近', async () => {
    const calls = record((call) => {
      if (call.url === '/api/departments/d-4') {
        return json(200, {
          department: lab,
          ancestors: [rd, hq],
          leaderUserName: null,
          childCount: 0,
          memberCount: 1,
        })
      }
      if (call.url === '/api/memberships:list') {
        return json(200, {
          items: [
            membership('m-1', 'bob'),
          ],
          total: 1,
          page: { index: 1, size: 100 },
        })
      }
      return notFound(call)
    })
    const detail = useDepartmentDetail(() => true)
    await detail.show(row(lab))
    expect(detail.open.value).toBe(true)
    expect(detail.loading.value).toBe(false)
    expect(detail.path.value).toBe('Headquarters / R&D / Lab')
    expect(detail.members.value.map(m => m.userName)).toEqual(['bob'])
    expect(calls.find(c => c.url === '/api/memberships:list')!.body).toEqual({
      page: { index: 1, size: 100 },
      filters: [
        { field: 'departmentId', operator: 'eq', value: 'd-4' },
      ],
      sorts: [
        { field: 'order' },
        { field: 'createdAt' },
      ],
    })
  })

  it('不能读归属的主体不取成员；取不到详情时留下错误', async () => {
    const calls = record(call => call.url === '/api/departments/d-4'
      ? json(200, {
          department: lab,
          ancestors: [],
          leaderUserName: null,
          childCount: 0,
          memberCount: 0,
        })
      : notFound(call))
    const detail = useDepartmentDetail(() => false)
    await detail.show(row(lab))
    expect(calls.map(c => c.url)).toEqual(['/api/departments/d-4'])
    expect(detail.members.value).toEqual([])

    record(() => json(404, {
      code: 'ORGANIZATION_NOTFOUND_001',
      status: 404,
      detail: 'gone',
      traceId: 'x',
    }, 'application/problem+json'))
    await detail.show(row(mkt))
    expect(detail.detail.value).toBeNull()
    expect(detail.error.value).toBe('gone')
  })
})

describe('useMembershipEditor', () => {
  it('添加成员：打开即取用户与启用中的岗位，提交 memberships.create（入职日期按 ISO 发）', async () => {
    const calls = record((call) => {
      if (call.url === '/api/users:list') {
        return json(200, {
          items: [
            { id: 'u-2', userName: 'carol', displayName: 'Carol' },
          ],
          total: 1,
          page: { index: 1, size: 100 },
        })
      }
      if (call.url === '/api/positions:list') {
        return json(200, {
          items: [
            {
              id: 'p-1',
              code: 'mgr',
              name: 'Manager',
              status: 'Active',
              order: 0,
              description: null,
              createdAt: '2026-10-28T00:00:00Z',
            },
          ],
          total: 1,
          page: { index: 1, size: 100 },
        })
      }
      if (call.url === '/api/memberships' && call.method === 'POST')
        return json(201, membership('m-2', 'carol'))
      return notFound(call)
    })
    const editor = useMembershipEditor()
    await editor.startCreate('d-2', { users: true, positions: true })
    expect(editor.userOptions.value).toEqual([
      { label: 'Carol', value: 'u-2', description: 'carol' },
    ])
    expect(calls.find(c => c.url === '/api/users:list')!.body).toMatchObject({
      filters: [
        { field: 'isSupportMember', operator: 'eq', value: 'false' },
      ],
    })
    expect(editor.positionOptions.value).toEqual([
      { label: 'Manager', value: 'p-1' },
    ])
    expect(calls.find(c => c.url === '/api/positions:list')!.body).toMatchObject({
      filters: [
        { field: 'status', operator: 'eq', value: 'Active' },
      ],
    })

    const hiredAt = Date.UTC(2026, 9, 1)
    Object.assign(editor.form, {
      userId: 'u-2',
      positionId: 'p-1',
      employeeNumber: ' 0002 ',
      hiredAt,
      isPrimary: true,
      remark: ' On loan ',
    })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls.find(c => c.method === 'POST' && c.url === '/api/memberships')!.body).toEqual({
      userId: 'u-2',
      departmentId: 'd-2',
      positionId: 'p-1',
      isPrimary: true,
      employeeNumber: '0002',
      jobGrade: null,
      hiredAt: '2026-10-01T00:00:00.000Z',
      order: 0,
      remark: 'On loan',
    })
  })

  it('没有读用户 / 读岗位权限时不发对应请求', async () => {
    const calls = record(notFound)
    const editor = useMembershipEditor()
    await editor.startCreate('d-2', { users: false, positions: false })
    expect(calls).toHaveLength(0)
  })

  it('编辑任职：不取用户、原岗位已停用也补进候选，主部门原样带回；字段级错误挂回字段', async () => {
    const member = membership('m-1', 'bob', {
      positionId: 'p-old',
      positionName: 'Retired',
      isPrimary: true,
      jobGrade: 'P5',
      hiredAt: '2026-01-02T00:00:00Z',
      order: 3,
      remark: 'Transferred',
    })
    const calls = record((call) => {
      if (call.url === '/api/positions:list') {
        return json(200, {
          items: [],
          total: 0,
          page: { index: 1, size: 100 },
        })
      }
      return json(200, member)
    })
    const editor = useMembershipEditor()
    await editor.startEdit(member as never, { users: true, positions: true })
    expect(calls.map(c => c.url)).toEqual(['/api/positions:list'])
    expect(editor.positionOptions.value).toEqual([
      { label: 'Retired', value: 'p-old' },
    ])
    expect(editor.form).toMatchObject({
      userId: 'u-m-1',
      positionId: 'p-old',
      jobGrade: 'P5',
      hiredAt: Date.UTC(2026, 0, 2),
      order: 3,
      isPrimary: true,
      remark: 'Transferred',
    })

    editor.form.jobGrade = 'P6'
    editor.form.remark = '  '
    expect(await editor.submit()).toBe(true)
    expect(calls.find(c => c.method === 'PUT')).toMatchObject({
      url: '/api/memberships/m-1',
      body: {
        positionId: 'p-old',
        isPrimary: true,
        employeeNumber: null,
        jobGrade: 'P6',
        hiredAt: '2026-01-02T00:00:00.000Z',
        order: 3,
        remark: null,
      },
    })

    record(() => json(400, {
      code: 'KERNEL_VALIDATION_003',
      status: 400,
      detail: 'invalid',
      traceId: 'x',
      errors: {
        employeeNumber: ['too long'],
      },
    }, 'application/problem+json'))
    await editor.startEdit(member as never, { users: false, positions: false })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('employeeNumber')).toBe('too long')
  })
})

describe('成员行操作', () => {
  it('设为主部门整体替换其余字段原样；移出被拒绝时带着原因抛出', async () => {
    const member = membership('m-3', 'dave', {
      positionId: 'p-1',
      employeeNumber: '0003',
      jobGrade: 'P3',
      hiredAt: '2026-03-01T00:00:00Z',
      order: 2,
      remark: 'On loan',
    })
    const calls = record(call => call.method === 'DELETE'
      ? json(200, {
          succeeded: [],
          failed: [
            { id: 'm-3', code: 'ORGANIZATION_NOTFOUND_003', message: 'ORGANIZATION_NOTFOUND_003 的原因' },
          ],
        })
      : json(200, { ...member, isPrimary: true }))
    await setPrimaryMembership(member as never)
    expect(calls[0]).toMatchObject({
      method: 'PUT',
      url: '/api/memberships/m-3',
      body: {
        positionId: 'p-1',
        isPrimary: true,
        employeeNumber: '0003',
        jobGrade: 'P3',
        hiredAt: '2026-03-01T00:00:00Z',
        order: 2,
        remark: 'On loan',
      },
    })
    await expect(removeMembership('m-3')).rejects.toThrow('ORGANIZATION_NOTFOUND_003 的原因')
    expect(calls[1]!.url).toBe('/api/memberships?ids=m-3')
  })
})
