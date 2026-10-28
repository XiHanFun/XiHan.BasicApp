import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { findCoveredDepartments, isDataScopeDirty, syncPickedDepartments } from './data-scope'
import { diffGrants, mergeMemberCandidates, rolesResource, toPermissionItems, useRoleDataScopeEditor, useRoleDetail, useRoleEditor, useRoleMemberEditor, useRolePermissionEditor } from './use-roles'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function role(id: string, code: string, extra: Record<string, unknown> = {}) {
  return { id, ownerTenantId: 'acme', code, name: code, description: null, order: 0, isBuiltIn: false, status: 'Active', dataScope: 'SelfOnly', createdAt: '2026-10-28T00:00:00Z', ...extra }
}

function permission(code: string, displayName: string) {
  const [pkg, resource, action] = code.split('.')
  return { code, displayName, description: null, package: pkg, resource, action, scope: 'Both', audited: false }
}

function member(userId: string, userName: string) {
  return { userId, userName, displayName: userName.toUpperCase(), status: 'Active', assignedAt: '2026-10-28T00:00:00Z' }
}

function department(id: string, name: string, children: unknown[] = []) {
  return { department: { id, name }, memberCount: 0, children }
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

describe('rolesResource', () => {
  it('把 SchemaPage 的查询换成 roles.list 的分页请求（关键字 + 状态精确匹配），结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [role('r-1', 'auditor')], total: 1, page: { index: 2, size: 10 } }))
    const result = await rolesResource().page!({ page: 2, pageSize: 10, filters: { keyword: ' aud ', status: 'Disabled' }, sorts: [{ field: 'createdAt', order: 'desc' }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/roles:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 10 },
      keyword: 'aud',
      filters: [{ field: 'status', operator: 'eq', value: 'Disabled' }],
      sorts: [{ field: 'createdAt', descending: true }],
    })
    expect(result.items.map(r => r.code)).toEqual(['auditor'])
    expect(result.page).toMatchObject({ pageIndex: 2, totalCount: 1, startRecord: 11 })
  })

  it('后端不接受的筛选（数据范围）不登记：出现即报错，不静默丢条件', async () => {
    const calls = record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await expect(rolesResource().page!({ page: 1, pageSize: 20, filters: { dataScope: 'All' } })).rejects.toThrow('dataScope')
    expect(calls).toHaveLength(0)
  })

  it('删除被后端逐项拒绝（内建角色）时带着错误码抛出；启停先取详情再整体回传，数据范围不传即保留', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'r-admin', code: 'ACCESS_CONFLICT_002' }] })
      if (call.method === 'GET')
        return json(200, { role: role('r-2', 'auditor', { name: 'Auditor', description: 'Reads logs', order: 5, dataScope: 'Custom' }), permissions: [], memberCount: 0, customDepartments: [{ departmentId: 'd-1', includeChildren: true }] })
      return json(200, role('r-2', 'auditor', { status: 'Disabled' }))
    })
    const resource = rolesResource()
    await expect(resource.remove!('r-admin')).rejects.toThrow('ACCESS_CONFLICT_002')
    expect(calls[0]!.url).toBe('/api/roles?ids=r-admin')
    await resource.updateStatus!('r-2', false)
    const put = calls.find(c => c.method === 'PUT')
    expect(put?.url).toBe('/api/roles/r-2')
    expect(put?.body).toEqual({ name: 'Auditor', description: 'Reads logs', order: 5, status: 'Disabled' })
  })
})

describe('useRoleEditor', () => {
  it('新建只送角色码、名称、说明与排序；编辑角色码只读、送名称 / 说明 / 排序 / 状态，保存成功关掉弹窗', async () => {
    const calls = record(call => json(call.method === 'POST' ? 201 : 200, role('r-9', 'support')))
    const editor = useRoleEditor()
    editor.startCreate()
    expect(editor.mode.value).toBe('create')
    Object.assign(editor.form, { code: ' support ', name: ' Support ', description: '  ', order: 3 })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/roles', body: { code: 'support', name: 'Support', description: null, order: 3 } })

    editor.startEdit(role('r-9', 'support', { name: 'Support', description: 'Helpdesk', order: 3 }) as never)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({ code: 'support', name: 'Support', description: 'Helpdesk', order: 3, status: 'Active' })
    editor.form.status = 'Disabled'
    expect(await editor.submit()).toBe(true)
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/roles/r-9', body: { name: 'Support', description: 'Helpdesk', order: 3, status: 'Disabled' } })
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, { code: 'KERNEL_VALIDATION_003', status: 400, detail: 'invalid', traceId: 'x', errors: { code: ['pattern'] } }, 'application/problem+json'))
    const editor = useRoleEditor()
    editor.startCreate()
    Object.assign(editor.form, { code: 'Bad Code', name: 'x' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('code')).toBe('pattern')
    expect(editor.submitError.value).toBeNull()

    record(() => json(409, { code: 'ACCESS_CONFLICT_001', status: 409, detail: 'code taken', traceId: 'x' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('code taken')
  })
})

describe('useRolePermissionEditor', () => {
  it('目录按「包.资源」分段，目录外的已授权限补进条目；保存按差量授予 / 撤销，逐项失败带权限码返回，目录只取一次', async () => {
    let granted = ['identity.users.read', 'tenancy.tenants.read']
    const calls = record((call) => {
      if (call.url === '/api/permissions')
        return json(200, { items: [permission('identity.users.read', 'Read users'), permission('identity.users.create', 'Create users'), permission('access.roles.read', 'Read roles')] })
      if (call.url === '/api/roles/r-1')
        return json(200, { role: role('r-1', 'auditor'), permissions: granted, memberCount: 0, customDepartments: [] })
      if (call.url.endsWith(':grant')) {
        granted = [...granted, 'identity.users.create']
        return json(200, { succeeded: ['identity.users.create'], failed: [{ permission: 'access.roles.read', code: 'ACCESS_VALIDATION_002' }] })
      }
      if (call.url.endsWith(':revoke')) {
        granted = granted.filter(code => code !== 'identity.users.read')
        return json(200, { succeeded: ['identity.users.read'], failed: [] })
      }
      return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: call.url }, 'application/problem+json')
    })
    const editor = useRolePermissionEditor()
    await editor.start(role('r-1', 'auditor') as never)
    expect(editor.open.value).toBe(true)
    expect(editor.items.value.map(item => [item.basicId, item.groupCode])).toEqual([
      ['identity.users.read', 'identity.users'],
      ['identity.users.create', 'identity.users'],
      ['access.roles.read', 'access.roles'],
      ['tenancy.tenants.read', 'tenancy.tenants'],
    ])
    expect(editor.items.value[3]!.permissionName).toBe('Read tenants')
    expect(editor.dirty.value).toBe(false)
    expect(await editor.save()).toBeNull()

    editor.draft.value = ['tenancy.tenants.read', 'identity.users.create', 'access.roles.read']
    expect(editor.dirty.value).toBe(true)
    const outcome = await editor.save()
    expect(calls.find(c => c.url.endsWith(':grant'))?.body).toEqual({ permissions: ['identity.users.create', 'access.roles.read'] })
    expect(calls.find(c => c.url.endsWith(':revoke'))?.body).toEqual({ permissions: ['identity.users.read'] })
    expect(outcome).toEqual({ added: 1, removed: 1, failures: ['access.roles.read: ACCESS_VALIDATION_002'] })
    expect(editor.draft.value).toEqual(['tenancy.tenants.read', 'identity.users.create'])

    await editor.start(role('r-1', 'auditor') as never)
    expect(calls.filter(c => c.url === '/api/permissions')).toHaveLength(1)
  })

  it('差量只比集合：顺序不同不算改动', () => {
    expect(diffGrants(['b', 'a'], ['a', 'b'])).toEqual({ grant: [], revoke: [] })
    expect(toPermissionItems([], ['custom.thing'])[0]).toMatchObject({ basicId: 'custom.thing', permissionName: 'custom.thing', groupCode: 'custom.thing' })
  })
})

describe('useRoleMemberEditor', () => {
  it('候选是本侧用户并上现有成员；挪好后一次提交加入与移出（都按用户主键）', async () => {
    let current = [member('u-1', 'bob'), member('u-2', 'carol')]
    const calls = record((call) => {
      if (call.url === '/api/users:list')
        return json(200, { items: [{ id: 'u-1', userName: 'bob', displayName: 'Bob', homeTenantId: 'acme', status: 'Active', createdAt: '2026-10-28T00:00:00Z' }, { id: 'u-3', userName: 'dave', displayName: 'Dave', homeTenantId: 'acme', status: 'Active', createdAt: '2026-10-28T00:00:00Z' }], total: 2, page: { index: 1, size: 500 } })
      if (call.url === '/api/roles/r-1:members')
        return json(200, { items: current, total: current.length })
      if (call.url.endsWith(':assign')) {
        current = [...current, member('u-3', 'dave')]
        return json(200, { succeeded: ['u-3'], failed: [] })
      }
      if (call.url.endsWith(':unassign')) {
        current = current.filter(m => m.userId !== 'u-2')
        return json(200, { succeeded: ['u-2'], failed: [] })
      }
      return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: call.url }, 'application/problem+json')
    })
    const editor = useRoleMemberEditor()
    await editor.start(role('r-1', 'auditor') as never, true)
    expect(calls.find(c => c.url === '/api/users:list')?.body).toMatchObject({ page: { index: 1, size: 500 } })
    expect(editor.items.value.map(item => item.basicId)).toEqual(['u-1', 'u-3', 'u-2'])
    expect(editor.draft.value).toEqual(['u-1', 'u-2'])

    editor.draft.value = ['u-1', 'u-3']
    const outcome = await editor.save()
    expect(calls.find(c => c.url.endsWith(':assign'))?.body).toEqual({ userIds: ['u-3'] })
    expect(calls.find(c => c.url.endsWith(':unassign'))?.body).toEqual({ userIds: ['u-2'] })
    expect(outcome).toEqual({ added: 1, removed: 1, failures: [] })
    expect(editor.dirty.value).toBe(false)
  })

  it('读不到用户目录时不取候选，现有成员仍在条目里（只能移出）', async () => {
    const calls = record(() => json(200, { items: [member('u-2', 'carol')], total: 1 }))
    const editor = useRoleMemberEditor()
    await editor.start(role('r-1', 'auditor') as never, false)
    expect(calls.map(c => c.url)).toEqual(['/api/roles/r-1:members'])
    expect(editor.items.value).toEqual([{ basicId: 'u-2', displayName: 'CAROL', userName: 'carol' }])
    expect(mergeMemberCandidates([], [])).toEqual([])
  })
})

describe('useRoleDataScopeEditor', () => {
  it('打开时取详情与部门树，自定义部门进草稿；保存整体回传名称等字段并替换自定义部门', async () => {
    const calls = record((call) => {
      if (call.url === '/api/roles/r-1' && call.method === 'GET')
        return json(200, { role: role('r-1', 'auditor', { name: 'Auditor', order: 2, dataScope: 'Custom' }), permissions: [], memberCount: 0, customDepartments: [{ departmentId: 'd-1', includeChildren: true }] })
      if (call.url === '/api/departments:tree')
        return json(200, { items: [department('d-1', 'HQ', [department('d-2', 'Sales')])] })
      return json(200, role('r-1', 'auditor'))
    })
    const editor = useRoleDataScopeEditor()
    await editor.start(role('r-1', 'auditor', { dataScope: 'Custom' }) as never, true)
    expect(editor.tree.value).toHaveLength(1)
    expect(editor.draft.value).toEqual({ dataScope: 'Custom', departments: [{ departmentId: 'd-1', includeChildren: true }] })
    expect(editor.dirty.value).toBe(false)

    editor.draft.value = { dataScope: 'Custom', departments: [] }
    expect(editor.complete.value).toBe(false)
    expect(await editor.save()).toBe(false)
    expect(calls.some(c => c.method === 'PUT')).toBe(false)

    editor.draft.value = { dataScope: 'Custom', departments: [{ departmentId: 'd-2', includeChildren: false }] }
    expect(await editor.save()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls.find(c => c.method === 'PUT')?.body).toEqual({ name: 'Auditor', description: null, order: 2, status: 'Active', dataScope: 'Custom', customDepartments: [{ departmentId: 'd-2', includeChildren: false }] })
  })

  it('换到非自定义档位时清空自定义部门；读不到部门时不取部门树', async () => {
    const calls = record((call) => {
      if (call.method === 'GET')
        return json(200, { role: role('r-1', 'auditor'), permissions: [], memberCount: 0, customDepartments: [] })
      return json(200, role('r-1', 'auditor'))
    })
    const editor = useRoleDataScopeEditor()
    await editor.start(role('r-1', 'auditor') as never, false)
    expect(calls.some(c => c.url === '/api/departments:tree')).toBe(false)
    editor.draft.value = { dataScope: 'DepartmentAndChildren', departments: [{ departmentId: 'd-1', includeChildren: true }] }
    expect(await editor.save()).toBe(true)
    expect(calls.find(c => c.method === 'PUT')?.body).toMatchObject({ dataScope: 'DepartmentAndChildren', customDepartments: [] })
  })

  it('部门勾选：新勾的缺省含下级，已有的保留设置；被上级含下级覆盖的标出来；脏态与顺序无关', () => {
    expect(syncPickedDepartments([{ departmentId: 'd-1', includeChildren: false }], ['d-1', 'd-2'])).toEqual([{ departmentId: 'd-1', includeChildren: false }, { departmentId: 'd-2', includeChildren: true }])
    const parents = new Map<string, null | string>([['d-1', null], ['d-2', 'd-1'], ['d-3', 'd-2']])
    expect(findCoveredDepartments([{ departmentId: 'd-1', includeChildren: true }, { departmentId: 'd-3', includeChildren: false }], parents)).toEqual(new Map([['d-3', 'd-1']]))
    const a = { dataScope: 'Custom' as const, departments: [{ departmentId: 'd-1', includeChildren: true }, { departmentId: 'd-2', includeChildren: false }] }
    expect(isDataScopeDirty({ ...a, departments: [...a.departments].reverse() }, a)).toBe(false)
    expect(isDataScopeDirty({ dataScope: 'All', departments: a.departments }, { dataScope: 'All', departments: [] })).toBe(false)
  })
})

describe('useRoleDetail', () => {
  it('详情带成员；已授权限按权限定义表取显示名；自定义部门按部门树取名称', async () => {
    record((call) => {
      if (call.url === '/api/roles/r-1')
        return json(200, { role: role('r-1', 'auditor', { dataScope: 'Custom' }), permissions: ['identity.users.read', 'custom.unknown'], memberCount: 1, customDepartments: [{ departmentId: 'd-2', includeChildren: false }] })
      if (call.url === '/api/roles/r-1:members')
        return json(200, { items: [member('u-1', 'bob')], total: 1 })
      return json(200, { items: [department('d-1', 'HQ', [department('d-2', 'Sales')])] })
    })
    const detail = useRoleDetail()
    await detail.start(role('r-1', 'auditor', { dataScope: 'Custom' }) as never, true)
    expect(detail.open.value).toBe(true)
    expect(detail.permissions.value.map(p => p.displayName)).toEqual(['Read users', 'custom.unknown'])
    expect(detail.members.value.map(m => m.userName)).toEqual(['bob'])
    expect(detail.departmentName('d-2')).toBe('Sales')
    expect(detail.departmentName('d-9')).toBe('d-9')
  })
})
