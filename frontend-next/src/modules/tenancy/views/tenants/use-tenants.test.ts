import type { EditionOption } from './use-tenants'
import { flushPromises } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { canInitializeAdmin, canInitializeDatabase, databaseConfiguration, inheritedEdition, initializeTenantDatabase, isMemberExpired, isMemberInEffect, isOwnerCandidate, ownerCandidates, seatUsage, storageUsage, tenantsResource, useAdminInitializer, useEditionOptions, useMemberEditor, useOwnerTransfer, useQuotaAudit, useTenantDetail, useTenantEditor, useTenantQuotas } from './use-tenants'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function problem(status: number, code: string, detail: string, errors?: Record<string, string[]>): Response {
  return json(status, { code, status, detail, traceId: 'x', ...(errors ? { errors } : {}) }, 'application/problem+json')
}

function tenant(extra: Record<string, unknown> = {}) {
  return { id: 't-1', tenantId: 'contoso', name: 'Contoso', description: null, status: 'Active', expiresAt: null, adminUserId: null, editionId: 'e-1', order: 10, createdAt: '2026-10-28T00:00:00Z', userLimit: null, storageLimitMb: null, isolationMode: 'Field', databaseProvider: null, databaseStatus: 'NotRequired', databaseError: null, ...extra }
}

/** 独立库租户：缺省待初始化、库类型 PostgreSQL */
function dedicated(extra: Record<string, unknown> = {}) {
  return tenant({ isolationMode: 'Database', databaseProvider: 'postgresql', databaseStatus: 'Pending', ...extra })
}

function quota(tenantId: string, extra: Record<string, unknown> = {}) {
  return { tenantId, name: tenantId, editionCode: 'free', editionName: 'Free', userLimit: 10, usedSeats: 2, storageLimitMb: 100, usedStorageBytes: 0, isOverSeats: false, isOverStorage: false, ...extra }
}

function edition(id: string, extra: Partial<EditionOption> = {}): EditionOption {
  return { id, code: id, name: id, description: null, isDefault: false, order: 10, permissionCount: 0, tenantCount: 0, createdAt: '2026-10-28T00:00:00Z', userLimit: null, storageLimitMb: null, price: null, billingPeriodMonths: null, isFree: false, ...extra }
}

const MB = 1024 * 1024

function member(id: string, userName: string, extra: Record<string, unknown> = {}) {
  return { id, tenantId: 'contoso', userId: `u-${id}`, userName, displayName: userName, homeTenantId: 'fabrikam', memberType: 'Member', effectiveAt: null, expiresAt: null, createdAt: '2026-10-28T00:00:00Z', ...extra }
}

/** users.list 的一项：显示名取登录名首字母大写 */
function user(id: string, userName: string, homeTenantId: string) {
  return { id, userName, displayName: userName.charAt(0).toUpperCase() + userName.slice(1), homeTenantId, status: 'Active', createdAt: '2026-10-28T00:00:00Z' }
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
    const editor = useTenantEditor(() => true)
    editor.startCreate()
    expect(editor.isEdit.value).toBe(false)
    Object.assign(editor.form, { tenantId: ' fabrikam ', name: ' Fabrikam ', expiresAt: Date.UTC(2027, 0, 1), userLimit: 30 })
    const saved = await editor.submit()
    expect(saved?.id).toBe('t-2')
    expect(editor.open.value).toBe(false)
    expect(calls).toHaveLength(1)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenants' })
    expect(calls[0]!.body).toEqual({ tenantId: 'fabrikam', name: 'Fabrikam', description: null, expiresAt: '2027-01-01T00:00:00.000Z', order: 0, editionId: null, userLimit: 30, storageLimitMb: null })
  })

  it('编辑：资料与上限走 tenants.update（整体覆盖，上限清空即沿用套餐）；套餐没变不换，清空即换成不受套餐限制（change-edition 发 null）', async () => {
    const row = tenant({ description: 'Main', expiresAt: '2027-01-01T00:00:00Z', userLimit: 50, storageLimitMb: 2048 }) as never
    let calls = record(call => json(200, tenant({ editionId: call.url.endsWith(':change-edition') ? null : 'e-1' })))
    const editor = useTenantEditor(() => true)
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
    const editor = useTenantEditor(() => true)
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

describe('独立库设置', () => {
  it('相对租户现状才发：共库不变不发、改回共库只发隔离方式；独立库换方式、填了连接串或换了库类型才发，连接串留空发 null；已就绪一律不发', () => {
    const form = (extra: Record<string, unknown> = {}) => ({ isolationMode: 'Database' as const, databaseProvider: null, connectionString: '', ...extra })
    expect(databaseConfiguration(form({ isolationMode: 'Field' }), tenant() as never)).toBeNull()
    expect(databaseConfiguration(form({ isolationMode: 'Field' }), dedicated() as never)).toEqual({ isolationMode: 'Field' })
    expect(databaseConfiguration(form({ connectionString: ' Host=db;Database=contoso ', databaseProvider: 'mysql' }), tenant() as never))
      .toEqual({ isolationMode: 'Database', connectionString: 'Host=db;Database=contoso', databaseProvider: 'mysql' })
    // 从共库改过来没填连接串也照发（null），由后端拒绝 TENANCY_VALIDATION_008
    expect(databaseConfiguration(form(), tenant() as never)).toEqual({ isolationMode: 'Database', connectionString: null, databaseProvider: null })
    expect(databaseConfiguration(form({ databaseProvider: 'postgresql' }), dedicated() as never)).toBeNull()
    expect(databaseConfiguration(form({ databaseProvider: 'postgresql', connectionString: '   ' }), dedicated({ databaseStatus: 'Failed' }) as never)).toBeNull()
    expect(databaseConfiguration(form({ databaseProvider: null }), dedicated() as never)).toEqual({ isolationMode: 'Database', connectionString: null, databaseProvider: null })
    expect(databaseConfiguration(form({ databaseProvider: 'postgresql', connectionString: 'Host=db2' }), dedicated({ databaseStatus: 'Failed' }) as never))
      .toEqual({ isolationMode: 'Database', connectionString: 'Host=db2', databaseProvider: 'postgresql' })
    expect(databaseConfiguration(form({ isolationMode: 'Field' }), dedicated({ databaseStatus: 'Ready' }) as never)).toBeNull()
    expect(databaseConfiguration(form({ connectionString: 'Host=db3' }), dedicated({ databaseStatus: 'Ready' }) as never)).toBeNull()
  })

  it('开通独立库租户：先 tenants.create（不带隔离设置）再 configure-database，返回配好的租户；选共库只开通', async () => {
    let calls = record(call => (call.url.endsWith(':configure-database')
      ? json(200, dedicated({ id: 't-2', tenantId: 'fabrikam' }))
      : json(201, tenant({ id: 't-2', tenantId: 'fabrikam' }))))
    const editor = useTenantEditor(() => true)
    editor.startCreate()
    expect(editor.form).toMatchObject({ isolationMode: 'Field', databaseProvider: null, connectionString: '' })
    expect(editor.hasConnection.value).toBe(false)
    Object.assign(editor.form, { tenantId: 'fabrikam', name: 'Fabrikam', isolationMode: 'Database', databaseProvider: 'postgresql', connectionString: 'Host=db;Database=fabrikam' })
    const saved = await editor.submit()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['POST /api/tenants', 'POST /api/tenants/t-2:configure-database'])
    expect(calls[0]!.body).not.toHaveProperty('isolationMode')
    expect(calls[0]!.body).not.toHaveProperty('connectionString')
    expect(calls[1]!.body).toEqual({ isolationMode: 'Database', connectionString: 'Host=db;Database=fabrikam', databaseProvider: 'postgresql' })
    expect(saved).toMatchObject({ id: 't-2', isolationMode: 'Database', databaseStatus: 'Pending' })
    expect(editor.open.value).toBe(false)

    calls = record(() => json(201, tenant({ id: 't-3', tenantId: 'litware' })))
    editor.startCreate()
    Object.assign(editor.form, { tenantId: 'litware', name: 'Litware', databaseProvider: 'mysql', connectionString: 'ignored' })
    expect((await editor.submit())?.isolationMode).toBe('Field')
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['POST /api/tenants'])
  })

  it('开通成功而配库失败：弹窗转为编辑这个已开通的租户（套餐按建好的回填），再保存走更新与配库、不会重复开通', async () => {
    let calls = record(call => (call.url.endsWith(':configure-database')
      ? problem(400, 'TENANCY_VALIDATION_008', 'connection string required')
      : json(201, tenant({ id: 't-2', tenantId: 'fabrikam', editionId: 'e-default' }))))
    const editor = useTenantEditor(() => true)
    editor.startCreate()
    Object.assign(editor.form, { tenantId: ' fabrikam ', name: 'Fabrikam', isolationMode: 'Database' })
    expect(await editor.submit()).toBeNull()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['POST /api/tenants', 'POST /api/tenants/t-2:configure-database'])
    expect(calls[1]!.body).toEqual({ isolationMode: 'Database', connectionString: null, databaseProvider: null })
    expect(editor.open.value).toBe(true)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.editing.value?.id).toBe('t-2')
    expect(editor.form).toMatchObject({ tenantId: 'fabrikam', editionId: 'e-default', isolationMode: 'Database' })
    expect(editor.submitError.value).toBe('connection string required')

    calls = record(call => (call.url.endsWith(':configure-database') ? json(200, dedicated({ id: 't-2' })) : json(200, tenant({ id: 't-2', editionId: 'e-default' }))))
    editor.form.connectionString = 'Host=db;Database=fabrikam'
    const saved = await editor.submit()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/tenants/t-2', 'POST /api/tenants/t-2:configure-database'])
    expect(calls[1]!.body).toEqual({ isolationMode: 'Database', connectionString: 'Host=db;Database=fabrikam', databaseProvider: null })
    expect(saved?.databaseStatus).toBe('Pending')
    expect(editor.open.value).toBe(false)
  })

  it('编辑：回填隔离方式与库类型、连接串不回显；没改不配库，只换库类型时连接串发 null（沿用），改回共库只发隔离方式', async () => {
    const row = dedicated() as never
    let calls = record(() => json(200, dedicated()))
    const editor = useTenantEditor(() => true)
    editor.startEdit(row)
    expect(editor.form).toMatchObject({ isolationMode: 'Database', databaseProvider: 'postgresql', connectionString: '' })
    expect(editor.hasConnection.value).toBe(true)
    expect(editor.databaseLocked.value).toBe(false)
    await editor.submit()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/tenants/t-1'])

    calls = record(call => json(200, call.url.endsWith(':configure-database') ? dedicated({ databaseProvider: 'mysql' }) : dedicated()))
    editor.startEdit(row)
    editor.form.databaseProvider = 'mysql'
    expect((await editor.submit())?.databaseProvider).toBe('mysql')
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/tenants/t-1', 'POST /api/tenants/t-1:configure-database'])
    expect(calls[1]!.body).toEqual({ isolationMode: 'Database', connectionString: null, databaseProvider: 'mysql' })

    calls = record(call => json(200, call.url.endsWith(':configure-database') ? tenant() : dedicated()))
    editor.startEdit(row)
    editor.form.isolationMode = 'Field'
    expect((await editor.submit())?.databaseStatus).toBe('NotRequired')
    expect(calls[1]).toMatchObject({ method: 'POST', url: '/api/tenants/t-1:configure-database', body: { isolationMode: 'Field' } })
    expect(calls[1]!.body).toEqual({ isolationMode: 'Field' })
  })

  it('已就绪的独立库只读、不再配库；没有配库权限时也不配', async () => {
    let calls = record(() => json(200, dedicated({ databaseStatus: 'Ready' })))
    const editor = useTenantEditor(() => true)
    editor.startEdit(dedicated({ databaseStatus: 'Ready' }) as never)
    expect(editor.databaseLocked.value).toBe(true)
    Object.assign(editor.form, { isolationMode: 'Field', connectionString: 'Host=elsewhere' })
    await editor.submit()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['PUT /api/tenants/t-1'])

    calls = record(() => json(201, tenant({ id: 't-2' })))
    const restricted = useTenantEditor(() => false)
    restricted.startCreate()
    Object.assign(restricted.form, { tenantId: 'fabrikam', name: 'Fabrikam', isolationMode: 'Database', connectionString: 'Host=db' })
    await restricted.submit()
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['POST /api/tenants'])
  })

  it('初始化独立库：就绪即成功；初始化失败照常 200、带回原因；请求被拒（已就绪等）给错误详情', async () => {
    let calls = record(() => json(200, dedicated({ databaseStatus: 'Ready' })))
    let outcome = await initializeTenantDatabase(dedicated() as never)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenants/t-1:initialize-database', body: {} })
    expect(outcome).toMatchObject({ ready: true, error: null, tenant: { databaseStatus: 'Ready' } })

    record(() => json(200, dedicated({ databaseStatus: 'Failed', databaseError: 'connection refused' })))
    outcome = await initializeTenantDatabase(dedicated() as never)
    expect(outcome).toMatchObject({ ready: false, error: 'connection refused', tenant: { databaseStatus: 'Failed' } })

    calls = record(() => problem(409, 'TENANCY_CONFLICT_013', 'database already initialized'))
    outcome = await initializeTenantDatabase(dedicated() as never)
    expect(calls).toHaveLength(1)
    expect(outcome).toEqual({ ready: false, tenant: null, error: 'database already initialized' })
  })

  it('开通三步的门控：独立库没就绪才出「初始化数据库」；没有管理员且共库或独立库已就绪才出「初始化管理员」', () => {
    expect(canInitializeDatabase(tenant() as never)).toBe(false)
    expect(canInitializeDatabase(dedicated() as never)).toBe(true)
    expect(canInitializeDatabase(dedicated({ databaseStatus: 'Failed' }) as never)).toBe(true)
    expect(canInitializeDatabase(dedicated({ databaseStatus: 'Ready' }) as never)).toBe(false)

    expect(canInitializeAdmin(tenant() as never)).toBe(true)
    expect(canInitializeAdmin(tenant({ adminUserId: 'u-0' }) as never)).toBe(false)
    expect(canInitializeAdmin(dedicated() as never)).toBe(false)
    expect(canInitializeAdmin(dedicated({ databaseStatus: 'Failed' }) as never)).toBe(false)
    expect(canInitializeAdmin(dedicated({ databaseStatus: 'Ready' }) as never)).toBe(true)
    expect(canInitializeAdmin(dedicated({ databaseStatus: 'Ready', adminUserId: 'u-0' }) as never)).toBe(false)
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
    let roster = [member('m-1', 'alice'), member('m-2', 'bob', { memberType: 'Support', homeTenantId: 'platform' })]
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

  it('原地重取（转移所有权之后）：不清空已显示的详情，重取租户与成员；读失败原样抛出', async () => {
    let owner = 'u-0'
    const calls = record((call) => {
      if (call.url === '/api/tenants/t-1')
        return owner === 'gone' ? problem(404, 'TENANCY_NOTFOUND_001', 'tenant gone') : json(200, { tenant: tenant({ adminUserId: owner }), adminUserName: owner, userCount: 3, quota: quota('contoso') })
      return json(200, { items: [member('m-1', 'alice')] })
    })
    const detail = useTenantDetail(() => true)
    await detail.show(tenant() as never)
    owner = 'u-a'
    const refreshing = detail.refresh()
    expect(detail.detail.value?.tenant.adminUserId).toBe('u-0')
    await refreshing
    expect(detail.detail.value?.tenant.adminUserId).toBe('u-a')
    expect(calls.map(c => c.url)).toEqual(['/api/tenants/t-1', '/api/tenant-members?tenantId=contoso', '/api/tenants/t-1', '/api/tenant-members?tenantId=contoso'])

    owner = 'gone'
    await expect(detail.refresh()).rejects.toThrow()
    expect(detail.detail.value?.tenant.adminUserId).toBe('u-a')
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
  it('打开即按空关键字检索普通成员的候选（只在租户用户里挑、去掉注册地就是本租户的），提交按目录键加成员；冲突给整条提示', async () => {
    let calls = record(() => json(200, { items: [user('u-7', 'carol', 'fabrikam'), user('u-8', 'dave', 'contoso')], total: 2, page: { index: 1, size: 20 } }))
    const editor = useMemberEditor()
    editor.start('contoso')
    await flushPromises()
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users:list', body: { page: { index: 1, size: 20 }, filters: [{ field: 'homeTenantId', operator: 'ne', value: 'platform' }], sorts: [], keyword: null } })
    expect(editor.userOptions.value).toEqual([{ label: 'carol', value: 'u-7', description: 'Carol · fabrikam' }])
    expect(await editor.submit()).toBe(false)

    calls = record(() => json(201, member('m-3', 'carol')))
    editor.form.userId = 'u-7'
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenant-members', body: { tenantId: 'contoso', userId: 'u-7', memberType: 'Member', effectiveAt: null, expiresAt: null } })

    record(() => problem(409, 'TENANCY_CONFLICT_005', 'already a member'))
    editor.open.value = true
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.submitError.value).toBe('already a member')
  })

  it('换成运维支持：清掉已选的用户，按空关键字只在平台账号里重新检索；换回同一类型不重查', async () => {
    let calls = record(() => json(200, { items: [user('u-7', 'carol', 'fabrikam')], total: 1, page: { index: 1, size: 20 } }))
    const editor = useMemberEditor()
    editor.start('contoso')
    await flushPromises()
    editor.form.userId = 'u-7'

    calls = record(() => json(200, { items: [user('u-1', 'ops', 'platform')], total: 1, page: { index: 1, size: 20 } }))
    editor.changeMemberType('Support')
    expect(editor.form).toMatchObject({ memberType: 'Support', userId: null })
    await flushPromises()
    expect(calls).toHaveLength(1)
    expect(calls[0]!.body).toEqual({ page: { index: 1, size: 20 }, filters: [{ field: 'homeTenantId', operator: 'eq', value: 'platform' }], sorts: [], keyword: null })
    expect(editor.userOptions.value.map(o => o.value)).toEqual(['u-1'])

    editor.changeMemberType('Support')
    editor.changeMemberType('Owner')
    await flushPromises()
    expect(calls).toHaveLength(1)
  })

  it('有效期按 ISO 发出：生效与到期都选了即带上，类型与账号不符给整条提示', async () => {
    record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    const editor = useMemberEditor()
    editor.start('contoso')
    await flushPromises()
    editor.changeMemberType('Support')
    await flushPromises()

    let calls = record(() => json(201, member('m-4', 'ops', { memberType: 'Support', homeTenantId: 'platform', effectiveAt: '2026-11-01T00:00:00Z', expiresAt: '2026-12-01T00:00:00Z' })))
    Object.assign(editor.form, { userId: 'u-1', effectiveAt: Date.UTC(2026, 10, 1), expiresAt: Date.UTC(2026, 11, 1) })
    expect(await editor.submit()).toBe(true)
    expect(calls[0]!.body).toEqual({ tenantId: 'contoso', userId: 'u-1', memberType: 'Support', effectiveAt: '2026-11-01T00:00:00.000Z', expiresAt: '2026-12-01T00:00:00.000Z' })

    calls = record(() => problem(400, 'TENANCY_VALIDATION_006', 'support must be a platform account'))
    editor.open.value = true
    expect(await editor.submit()).toBe(false)
    expect(calls).toHaveLength(1)
    expect(editor.submitError.value).toBe('support must be a platform account')
  })

  it('只认最后一次检索：先发的慢请求回来不盖掉后发的结果', async () => {
    const pending: Array<(response: Response) => void> = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (() => new Promise<Response>(resolve => pending.push(resolve))) as unknown as typeof fetch,
    })
    const editor = useMemberEditor()
    editor.start('contoso')
    const latest = editor.searchUsers('dave')
    pending[1]!(json(200, { items: [user('u-9', 'dave', 'fabrikam')], total: 1, page: { index: 1, size: 20 } }))
    await latest
    pending[0]!(json(200, { items: [user('u-7', 'carol', 'fabrikam')], total: 1, page: { index: 1, size: 20 } }))
    await flushPromises()
    expect(editor.userOptions.value.map(o => o.value)).toEqual(['u-9'])
    expect(editor.userLoading.value).toBe(false)
  })
})

describe('成员有效期', () => {
  const now = Date.UTC(2026, 9, 30)

  it('生效时间为空或已到、到期时间为空或还没到即生效中；到了到期时间即已到期', () => {
    expect(isMemberInEffect({ effectiveAt: null, expiresAt: null }, now)).toBe(true)
    expect(isMemberInEffect({ effectiveAt: '2026-10-01T00:00:00Z', expiresAt: '2026-11-01T00:00:00Z' }, now)).toBe(true)
    expect(isMemberInEffect({ effectiveAt: '2026-11-01T00:00:00Z', expiresAt: null }, now)).toBe(false)
    expect(isMemberInEffect({ effectiveAt: null, expiresAt: '2026-10-30T00:00:00Z' }, now)).toBe(false)
    expect(isMemberExpired({ expiresAt: '2026-10-30T00:00:00Z' }, now)).toBe(true)
    expect(isMemberExpired({ expiresAt: '2026-10-31T00:00:00Z' }, now)).toBe(false)
    expect(isMemberExpired({ expiresAt: null }, now)).toBe(false)
  })
})

describe('转移所有权', () => {
  const now = Date.UTC(2026, 9, 30)

  it('候选：本租户用户在前、生效中的普通成员在后，去掉现任所有者与重复；运维支持、已到期与未生效的成员不进候选，成员按关键字本地匹配', () => {
    const users = [user('u-0', 'admin', 'contoso'), user('u-2', 'erin', 'contoso')] as never[]
    const roster = [
      member('m-1', 'alice', { userId: 'u-a' }),
      member('m-2', 'ops', { userId: 'u-s', memberType: 'Support' }),
      member('m-3', 'gone', { userId: 'u-g', expiresAt: '2026-10-01T00:00:00Z' }),
      member('m-4', 'later', { userId: 'u-l', effectiveAt: '2026-11-01T00:00:00Z' }),
      member('m-5', 'Bob', { userId: 'u-b', homeTenantId: 'fabrikam', displayName: 'Bobby' }),
    ] as never[]
    expect(ownerCandidates(users, roster, '', 'u-0', now)).toEqual([
      { label: 'erin', value: 'u-2', description: 'Erin · contoso' },
      { label: 'alice', value: 'u-a', description: 'alice · fabrikam' },
      { label: 'Bob', value: 'u-b', description: 'Bobby · fabrikam' },
    ])
    expect(ownerCandidates([], roster, ' bobb ', 'u-0', now).map(o => o.value)).toEqual(['u-b'])
    expect(isOwnerCandidate(roster[0]!, 'u-0', now)).toBe(true)
    expect(isOwnerCandidate(roster[1]!, 'u-0', now)).toBe(false)
    expect(isOwnerCandidate(roster[0]!, 'u-a', now)).toBe(false)
    expect(isOwnerCandidate(roster[0]!, null, now)).toBe(false)
  })

  it('打开即按空关键字检索本租户的启用用户并并入成员候选；提交走 tenants.transfer-owner，成功返回转移后的租户并关掉弹窗', async () => {
    let calls = record(() => json(200, { items: [user('u-0', 'admin', 'contoso'), user('u-2', 'erin', 'contoso')], total: 2, page: { index: 1, size: 20 } }))
    const transfer = useOwnerTransfer()
    transfer.start(tenant({ adminUserId: 'u-0' }) as never, [member('m-1', 'alice', { userId: 'u-a' })] as never[])
    await flushPromises()
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/users:list' })
    expect(calls[0]!.body).toEqual({ page: { index: 1, size: 20 }, filters: [{ field: 'homeTenantId', operator: 'eq', value: 'contoso' }, { field: 'status', operator: 'eq', value: 'Active' }], sorts: [], keyword: null })
    expect(transfer.options.value.map(o => o.value)).toEqual(['u-2', 'u-a'])
    expect(await transfer.submit()).toBeNull()

    calls = record(() => json(200, tenant({ adminUserId: 'u-a' })))
    transfer.userId.value = 'u-a'
    const saved = await transfer.submit()
    expect(saved?.adminUserId).toBe('u-a')
    expect(transfer.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/tenants/t-1:transfer-owner' })
    expect(calls[0]!.body).toMatchObject({ userId: 'u-a' })
  })

  it('从成员行进来即预选该成员（检索换了候选也留着）；用户检索失败时成员候选照给并报原因；不合格给整条提示、弹窗保持打开', async () => {
    record(() => problem(403, 'SECURITY_DENIED_002', 'denied'))
    const transfer = useOwnerTransfer()
    const roster = [member('m-1', 'alice', { userId: 'u-a' }), member('m-2', 'bob', { userId: 'u-b' })] as never[]
    transfer.start(tenant({ adminUserId: 'u-0' }) as never, roster, 'u-b')
    expect(transfer.userId.value).toBe('u-b')
    await flushPromises()
    expect(transfer.loadError.value).toBe('denied')
    expect(transfer.options.value.map(o => o.value)).toEqual(['u-a', 'u-b'])

    record(() => json(200, { items: [], total: 0, page: { index: 1, size: 20 } }))
    await transfer.search('ali')
    expect(transfer.loadError.value).toBeNull()
    expect(transfer.options.value.map(o => o.value)).toEqual(['u-b', 'u-a'])

    record(() => problem(400, 'TENANCY_VALIDATION_005', 'not eligible'))
    expect(await transfer.submit()).toBeNull()
    expect(transfer.open.value).toBe(true)
    expect(transfer.submitError.value).toBe('not eligible')

    // 预选的不是能接任的成员（运维支持）就不预选
    transfer.start(tenant({ adminUserId: 'u-0' }) as never, [member('m-3', 'ops', { userId: 'u-s', memberType: 'Support' })] as never[], 'u-s')
    expect(transfer.userId.value).toBeNull()
  })
})
