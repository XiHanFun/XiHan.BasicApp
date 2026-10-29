import type { EditionOption } from './use-tenants'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { inheritedEdition, seatUsage, storageUsage, tenantsResource, useAdminInitializer, useEditionOptions, useMemberEditor, useQuotaAudit, useTenantDetail, useTenantEditor, useTenantQuotas } from './use-tenants'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function problem(status: number, code: string, detail: string, errors?: Record<string, string[]>): Response {
  return json(status, { code, status, detail, traceId: 'x', ...(errors ? { errors } : {}) }, 'application/problem+json')
}

function tenant(extra: Record<string, unknown> = {}) {
  return { id: 't-1', tenantId: 'contoso', name: 'Contoso', description: null, status: 'Active', expiresAt: null, adminUserId: null, editionId: 'e-1', order: 10, createdAt: '2026-10-28T00:00:00Z', userLimit: null, storageLimitMb: null, ...extra }
}

function quota(tenantId: string, extra: Record<string, unknown> = {}) {
  return { tenantId, name: tenantId, editionCode: 'free', editionName: 'Free', userLimit: 10, usedSeats: 2, storageLimitMb: 100, usedStorageBytes: 0, isOverSeats: false, isOverStorage: false, ...extra }
}

function edition(id: string, extra: Partial<EditionOption> = {}): EditionOption {
  return { id, code: id, name: id, description: null, isDefault: false, order: 10, permissionCount: 0, tenantCount: 0, createdAt: '2026-10-28T00:00:00Z', userLimit: null, storageLimitMb: null, price: null, billingPeriodMonths: null, isFree: false, ...extra }
}

const MB = 1024 * 1024

function member(id: string, userName: string, extra: Record<string, unknown> = {}) {
  return { id, tenantId: 'contoso', userId: `u-${id}`, userName, displayName: userName, homeTenantId: 'platform', memberType: 'Member', createdAt: '2026-10-28T00:00:00Z', ...extra }
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

describe('tenantsResource', () => {
  it('把 SchemaPage 的查询换成 tenants.list 的分页请求（关键字、状态精确匹配、排序），结果换回 SchemaPage 的分页结构', async () => {
    const calls = record(() => json(200, { items: [tenant()], total: 21, page: { index: 2, size: 20 } }))
    const result = await tenantsResource().page!({ page: 2, pageSize: 20, filters: { keyword: ' con ', status: 'Suspended' }, sorts: [{ field: 'expiresAt', order: 'desc' }] })
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenants:list' })
    expect(calls[0]!.body).toEqual({
      page: { index: 2, size: 20 },
      keyword: 'con',
      filters: [{ field: 'status', operator: 'eq', value: 'Suspended' }],
      sorts: [{ field: 'expiresAt', descending: true }],
    })
    expect(result.items.map(r => r.tenantId)).toEqual(['contoso'])
    expect(result.page).toMatchObject({ totalCount: 21, pageIndex: 2, totalPages: 2, startRecord: 21, isLastPage: true })
  })

  it('删除与启停都是集合操作：逐项拒绝的以错误码抛出；启用走 resume、停用走 suspend', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 't-1', code: 'TENANCY_CONFLICT_003' }] })
      if (call.url.endsWith(':resume'))
        return json(200, { succeeded: [], failed: [{ id: 't-1', code: 'TENANCY_NOTFOUND_001' }] })
      return json(200, { succeeded: ['t-1'], failed: [] })
    })
    const resource = tenantsResource()
    await expect(resource.remove!('t-1')).rejects.toThrow('TENANCY_CONFLICT_003')
    expect(calls[0]!.url).toBe('/api/tenants?ids=t-1')
    await resource.updateStatus!('t-1', false)
    expect(calls[1]).toMatchObject({ method: 'POST', url: '/api/tenants:suspend', body: { ids: ['t-1'] } })
    await expect(resource.updateStatus!('t-1', true)).rejects.toThrow('TENANCY_NOTFOUND_001')
    expect(calls[2]).toMatchObject({ method: 'POST', url: '/api/tenants:resume', body: { ids: ['t-1'] } })
  })

  it('给了用量：每次取页并行取一次全部租户的配额，按租户 ID 对上；配额取不到只报原因、列表照常返回', async () => {
    let failQuotas = false
    const calls = record((call) => {
      if (call.url === '/api/tenants:quotas')
        return failQuotas ? problem(500, 'KERNEL_UNEXPECTED_000', 'quota store down') : json(200, { items: [quota('contoso', { usedSeats: 9 })] })
      return json(200, { items: [tenant()], total: 1, page: { index: 1, size: 20 } })
    })
    const reasons: string[] = []
    const quotas = useTenantQuotas(reason => reasons.push(reason))
    const resource = tenantsResource(quotas)
    await resource.page!({ page: 1, pageSize: 20, filters: {} })
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['POST /api/tenants:list', 'GET /api/tenants:quotas'])
    expect(quotas.of('contoso')?.usedSeats).toBe(9)
    expect(quotas.of('fabrikam')).toBeUndefined()

    failQuotas = true
    const result = await resource.page!({ page: 1, pageSize: 20, filters: {} })
    expect(result.items).toHaveLength(1)
    expect(calls.filter(c => c.url === '/api/tenants:quotas')).toHaveLength(2)
    expect(quotas.of('contoso')).toBeUndefined()
    expect(quotas.error.value).toBe('quota store down')
    expect(reasons).toEqual(['quota store down'])
  })
})

describe('配额用量', () => {
  it('席位与存储按有效上限算水位：到 80% 吃紧、到上限超限，不限照常出文本；存储上限 MB 换成字节再比', () => {
    expect(seatUsage({ usedSeats: 3, userLimit: 10 }, '∞')).toEqual({ text: '3 / 10', tone: 'muted' })
    expect(seatUsage({ usedSeats: 8, userLimit: 10 }, '∞')).toEqual({ text: '8 / 10', tone: 'warning' })
    expect(seatUsage({ usedSeats: 11, userLimit: 10 }, '∞')).toEqual({ text: '11 / 10', tone: 'danger' })
    expect(seatUsage({ usedSeats: 11, userLimit: null }, '∞')).toEqual({ text: '11 / ∞', tone: 'muted' })
    expect(storageUsage({ usedStorageBytes: 90 * MB, storageLimitMb: 100 }, '∞')).toEqual({ text: '90 MB / 100 MB', tone: 'warning' })
    expect(storageUsage({ usedStorageBytes: 0, storageLimitMb: null }, '∞')).toEqual({ text: '0 B / ∞', tone: 'muted' })
  })

  it('上限留空沿用的套餐：选了即所选；开通没选即缺省套餐，编辑没选即没有套餐；说不出来为 undefined', () => {
    const list = [edition('e-1', { userLimit: 5 }), edition('e-2', { isDefault: true, storageLimitMb: 512 })]
    expect(inheritedEdition(list, 'e-1', false)?.userLimit).toBe(5)
    expect(inheritedEdition(list, null, true)?.id).toBe('e-2')
    expect(inheritedEdition(list, null, false)).toBeNull()
    expect(inheritedEdition(list, 'e-9', true)).toBeUndefined()
    expect(inheritedEdition([edition('e-1')], null, true)).toBeUndefined()
  })

  it('配额核对只取超额的租户（overOnly=true）；取不到给出原因并清空', async () => {
    let calls = record(() => json(200, { items: [quota('contoso', { usedSeats: 12, isOverSeats: true })] }))
    const audit = useQuotaAudit()
    await audit.start()
    expect(audit.open.value).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/tenants:quotas?overOnly=true' })
    expect(audit.items.value.map(q => q.tenantId)).toEqual(['contoso'])
    expect(audit.loading.value).toBe(false)

    calls = record(() => problem(403, 'SECURITY_DENIED_002', 'denied'))
    await audit.load()
    expect(calls).toHaveLength(1)
    expect(audit.items.value).toEqual([])
    expect(audit.error.value).toBe('denied')
  })
})

describe('useEditionOptions', () => {
  it('取全部套餐作下拉；没绑定即不受限，绑定的不在选项里就显示主键，读失败给出原因', async () => {
    record(() => json(200, { items: [{ id: 'e-1', code: 'free', name: 'Free', description: null, isDefault: true, order: 10, permissionCount: 6, tenantCount: 1, createdAt: '2026-10-28T00:00:00Z' }] }))
    const options = useEditionOptions()
    await options.load()
    expect(options.editionName('e-1')).toBe('Free')
    expect(options.editionName(null)).toBeNull()
    expect(options.editionName('e-9')).toBe('e-9')

    record(() => problem(403, 'SECURITY_DENIED_002', 'denied'))
    await options.load()
    expect(options.editions.value).toEqual([])
    expect(options.error.value).toBe('denied')
  })
})

describe('useTenantEditor', () => {
  it('开通：不选套餐即缺省套餐（发 null），到期时间按 ISO 发出，返回建好的租户并关掉弹窗', async () => {
    const calls = record(() => json(201, tenant({ id: 't-2', tenantId: 'fabrikam' })))
    const editor = useTenantEditor()
    editor.startCreate()
    expect(editor.isEdit.value).toBe(false)
    Object.assign(editor.form, { tenantId: ' fabrikam ', name: ' Fabrikam ', expiresAt: Date.UTC(2027, 0, 1), userLimit: 30 })
    const saved = await editor.submit()
    expect(saved?.id).toBe('t-2')
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenants' })
    expect(calls[0]!.body).toEqual({ tenantId: 'fabrikam', name: 'Fabrikam', description: null, expiresAt: '2027-01-01T00:00:00.000Z', order: 0, editionId: null, userLimit: 30, storageLimitMb: null })
  })

  it('编辑：资料与上限走 tenants.update（整体覆盖，上限清空即沿用套餐）；套餐没变不换，清空即换成不受套餐限制（change-edition 发 null）', async () => {
    const row = tenant({ description: 'Main', expiresAt: '2027-01-01T00:00:00Z', userLimit: 50, storageLimitMb: 2048 }) as never
    let calls = record(call => json(200, tenant({ editionId: call.url.endsWith(':change-edition') ? null : 'e-1' })))
    const editor = useTenantEditor()
    editor.startEdit(row)
    expect(editor.form).toMatchObject({ tenantId: 'contoso', name: 'Contoso', description: 'Main', order: 10, editionId: 'e-1', expiresAt: Date.UTC(2027, 0, 1), userLimit: 50, storageLimitMb: 2048 })
    editor.form.name = 'Contoso Ltd'
    editor.form.userLimit = null
    expect(await editor.submit()).not.toBeNull()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/tenants/t-1'])
    expect(calls[0]!.body).toEqual({ name: 'Contoso Ltd', description: 'Main', expiresAt: '2027-01-01T00:00:00.000Z', order: 10, userLimit: null, storageLimitMb: 2048 })

    calls = record(call => json(200, tenant({ editionId: call.url.endsWith(':change-edition') ? null : 'e-1' })))
    editor.startEdit(row)
    editor.form.editionId = null
    const saved = await editor.submit()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/tenants/t-1', 'POST /api/tenants/t-1:change-edition'])
    expect(calls[1]!.body).toEqual({ editionId: null })
    expect(saved?.editionId).toBeNull()
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => problem(400, 'KERNEL_VALIDATION_003', 'invalid', { tenantId: ['pattern'] }))
    const editor = useTenantEditor()
    editor.startCreate()
    Object.assign(editor.form, { tenantId: 'bad id!', name: 'x' })
    expect(await editor.submit()).toBeNull()
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('tenantId')).toBe('pattern')
    expect(editor.submitError.value).toBeNull()

    record(() => problem(409, 'TENANCY_CONFLICT_001', 'tenant id taken'))
    expect(await editor.submit()).toBeNull()
    expect(editor.fieldError('tenantId')).toBeUndefined()
    expect(editor.submitError.value).toBe('tenant id taken')
  })
})

describe('useAdminInitializer', () => {
  it('按行初始化管理员：显示名与邮箱留空发 null，字段级错误挂回字段，成功关掉弹窗', async () => {
    let calls = record(() => problem(400, 'KERNEL_VALIDATION_003', 'invalid', { password: ['too short'] }))
    const admin = useAdminInitializer()
    admin.start(tenant() as never)
    Object.assign(admin.form, { userName: ' admin ', password: 'short' })
    expect(await admin.submit()).toBe(false)
    expect(admin.fieldError('password')).toBe('too short')
    expect(admin.open.value).toBe(true)

    calls = record(() => json(200, { id: 'u-1', userName: 'admin', displayName: 'admin', homeTenantId: 'contoso', status: 'Active', createdAt: '2026-10-28T00:00:00Z' }))
    admin.form.password = 'Admin@123456'
    admin.form.email = 'admin@contoso.test'
    expect(await admin.submit()).toBe(true)
    expect(admin.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenants/t-1:initialize-admin' })
    expect(calls[0]!.body).toEqual({ userName: 'admin', password: 'Admin@123456', displayName: null, email: 'admin@contoso.test' })
  })
})

describe('useTenantDetail', () => {
  it('取详情后按目录键取成员；移除被逐项拒绝时以错误码抛出，成功后重取成员', async () => {
    let roster = [member('m-1', 'alice'), member('m-2', 'bob', { memberType: 'Owner' })]
    const calls = record((call) => {
      if (call.url === '/api/tenants/t-1')
        return json(200, { tenant: tenant({ adminUserId: 'u-0' }), adminUserName: 'admin', userCount: 3, quota: quota('contoso', { usedSeats: 4 }) })
      if (call.url.startsWith('/api/tenant-members?tenantId='))
        return json(200, { items: roster })
      if (call.url === '/api/tenant-members?ids=m-9')
        return json(200, { succeeded: [], failed: [{ id: 'm-9', code: 'TENANCY_NOTFOUND_002' }] })
      roster = roster.filter(m => m.id !== 'm-1')
      return json(200, { succeeded: ['m-1'], failed: [] })
    })
    const detail = useTenantDetail(() => true)
    await detail.show(tenant() as never)
    expect(detail.open.value).toBe(true)
    expect(detail.detail.value).toMatchObject({ adminUserName: 'admin', userCount: 3, quota: { usedSeats: 4, userLimit: 10 } })
    expect(calls[1]).toMatchObject({ method: 'GET', url: '/api/tenant-members?tenantId=contoso' })
    expect(detail.members.value.map(m => m.userName)).toEqual(['alice', 'bob'])

    await expect(detail.removeMember(member('m-9', 'ghost') as never)).rejects.toThrow('TENANCY_NOTFOUND_002')
    await detail.removeMember(detail.members.value[0]!)
    expect(calls.find(c => c.method === 'DELETE' && c.url.endsWith('m-1'))?.url).toBe('/api/tenant-members?ids=m-1')
    expect(detail.members.value.map(m => m.userName)).toEqual(['bob'])
  })

  it('没有读成员的权限就不取成员；详情读失败给出原因', async () => {
    let calls = record(() => json(200, { tenant: tenant(), adminUserName: null, userCount: 0, quota: quota('contoso') }))
    const detail = useTenantDetail(() => false)
    await detail.show(tenant() as never)
    expect(calls.map(c => c.url)).toEqual(['/api/tenants/t-1'])
    expect(detail.members.value).toEqual([])

    calls = record(() => problem(404, 'TENANCY_NOTFOUND_001', 'tenant gone'))
    await detail.show(tenant() as never)
    expect(calls).toHaveLength(1)
    expect(detail.detail.value).toBeNull()
    expect(detail.error.value).toBe('tenant gone')
  })
})

describe('useMemberEditor', () => {
  it('打开即按空关键字检索用户作候选，提交按目录键加成员；冲突给整条提示', async () => {
    let calls = record(() => json(200, { items: [{ id: 'u-7', userName: 'carol', displayName: 'Carol', homeTenantId: 'platform', status: 'Active', createdAt: '2026-10-28T00:00:00Z' }], total: 1, page: { index: 1, size: 20 } }))
    const editor = useMemberEditor()
    editor.start()
    await editor.searchUsers('')
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users:list', body: { page: { index: 1, size: 20 }, filters: [], sorts: [], keyword: null } })
    expect(editor.userOptions.value).toEqual([{ label: 'carol', value: 'u-7', description: 'Carol · platform' }])
    expect(await editor.submit('contoso')).toBe(false)

    calls = record(() => json(201, member('m-3', 'carol', { memberType: 'Owner' })))
    editor.form.userId = 'u-7'
    editor.form.memberType = 'Owner'
    expect(await editor.submit('contoso')).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenant-members', body: { tenantId: 'contoso', userId: 'u-7', memberType: 'Owner' } })

    record(() => problem(409, 'TENANCY_CONFLICT_005', 'already a member'))
    editor.open.value = true
    expect(await editor.submit('contoso')).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.submitError.value).toBe('already a member')
  })
})
