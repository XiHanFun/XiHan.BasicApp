import type { CatalogItem } from './use-editions'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { editionsResource, setDefaultEdition, toWhitelistItems, useEditionEditor, useEditionPermissions } from './use-editions'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function problem(status: number, code: string, detail: string, errors?: Record<string, string[]>): Response {
  return json(status, { code, status, detail, traceId: 'x', ...(errors ? { errors } : {}) }, 'application/problem+json')
}

function edition(id: string, code: string, extra: Record<string, unknown> = {}) {
  return { id, code, name: code, description: null, isDefault: false, order: 10, permissionCount: 0, tenantCount: 0, createdAt: '2026-10-28T00:00:00Z', ...extra }
}

function permission(code: string): CatalogItem {
  const [pkg = '', resource = '', action = ''] = code.split('.')
  return { code, displayName: `Name of ${code}`, description: null, package: pkg, resource, action, scope: 'Both', audited: false }
}

const catalog = ['access.roles.read', 'identity.users.create', 'identity.users.read'].map(permission)

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

describe('editionsResource', () => {
  it('editions.list 一次全列：资源按页切片并换回 SchemaPage 的分页结构', async () => {
    const all = Array.from({ length: 5 }, (_, i) => edition(`e-${i + 1}`, `ed${i + 1}`))
    const calls = record(() => json(200, { items: all }))
    const result = await editionsResource().page!({ page: 2, pageSize: 2, filters: { keyword: ' ' } })
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/editions' })
    expect(result.items.map(e => e.code)).toEqual(['ed3', 'ed4'])
    expect(result.page).toMatchObject({ totalCount: 5, pageIndex: 2, pageSize: 2, totalPages: 3, startRecord: 3, endRecord: 4, hasNext: true })
  })

  it('后端不收筛选与排序：查询里出现就报错且不发请求；删除被逐项拒绝时带着错误码抛出', async () => {
    const calls = record(() => json(200, { succeeded: [], failed: [{ id: 'e-1', code: 'TENANCY_CONFLICT_007' }] }))
    const resource = editionsResource()
    await expect(resource.page!({ page: 1, pageSize: 20, filters: { code: 'free' } })).rejects.toThrow('code')
    await expect(resource.page!({ page: 1, pageSize: 20, filters: {}, sorts: [{ field: 'name', order: 'asc' }] })).rejects.toThrow('name')
    expect(calls).toHaveLength(0)
    await expect(resource.remove!('e-1')).rejects.toThrow('TENANCY_CONFLICT_007')
    expect(calls[0]).toMatchObject({ method: 'DELETE', url: '/api/editions?ids=e-1' })
  })

  it('设为缺省：先取当前值，再整体覆盖更新、只改缺省标记', async () => {
    const calls = record(call => call.method === 'GET'
      ? json(200, { edition: edition('e-2', 'pro', { name: 'Pro', description: 'For teams', order: 20 }), permissions: [] })
      : json(200, { edition: edition('e-2', 'pro', { isDefault: true }), permissions: [] }))
    const saved = await setDefaultEdition('e-2')
    expect(saved.isDefault).toBe(true)
    expect(calls.map(c => `${c.method} ${c.url}`)).toEqual(['GET /api/editions/e-2', 'PUT /api/editions/e-2'])
    expect(calls[1]!.body).toEqual({ name: 'Pro', description: 'For teams', isDefault: true, order: 20 })
  })
})

describe('toWhitelistItems', () => {
  it('目录条目按「包.资源」分段、权限码即主键；白名单里有、目录里没有的码列进目录外一段', () => {
    const items = toWhitelistItems(catalog, ['identity.users.read', 'files.legacy.read'], 'outside')
    expect(items.map(i => [i.basicId, i.groupCode, i.outside])).toEqual([
      ['access.roles.read', 'access.roles', false],
      ['identity.users.create', 'identity.users', false],
      ['identity.users.read', 'identity.users', false],
      ['files.legacy.read', 'outside', true],
    ])
    expect(items[0]).toMatchObject({ permissionCode: 'access.roles.read', permissionName: 'Name of access.roles.read' })
  })
})

describe('useEditionEditor', () => {
  it('新建白名单为空、编码去空白；编辑不发编码；保存成功返回套餐并关掉弹窗', async () => {
    let calls = record(() => json(201, { edition: edition('e-5', 'team'), permissions: [] }))
    const editor = useEditionEditor()
    editor.startCreate()
    Object.assign(editor.form, { code: ' team ', name: ' Team ', order: 30 })
    expect((await editor.submit())?.id).toBe('e-5')
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/editions' })
    expect(calls[0]!.body).toEqual({ code: 'team', name: 'Team', description: null, isDefault: false, order: 30, permissions: [] })

    calls = record(() => json(200, { edition: edition('e-5', 'team', { isDefault: true }), permissions: [] }))
    editor.startEdit(edition('e-5', 'team', { description: 'Small teams' }) as never)
    expect(editor.isEdit.value).toBe(true)
    expect(editor.form).toMatchObject({ code: 'team', name: 'team', description: 'Small teams', order: 10, isDefault: false })
    editor.form.isDefault = true
    expect((await editor.submit())?.isDefault).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'PUT', url: '/api/editions/e-5' })
    expect(calls[0]!.body).toEqual({ name: 'team', description: 'Small teams', isDefault: true, order: 10 })
  })

  it('字段级错误挂回字段，其余错误给整条提示；随机编码合后端编码格式', async () => {
    record(() => problem(400, 'KERNEL_VALIDATION_003', 'invalid', { code: ['pattern'] }))
    const editor = useEditionEditor()
    editor.startCreate()
    Object.assign(editor.form, { code: 'Bad Code', name: 'Bad' })
    expect(await editor.submit()).toBeNull()
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('code')).toBe('pattern')
    expect(editor.submitError.value).toBeNull()

    record(() => problem(409, 'TENANCY_CONFLICT_006', 'code taken'))
    expect(await editor.submit()).toBeNull()
    expect(editor.submitError.value).toBe('code taken')

    for (let i = 0; i < 20; i++) {
      editor.generateCode()
      expect(editor.form.code).toMatch(/^[a-z][a-z0-9]{7}$/)
    }
  })
})

describe('useEditionPermissions', () => {
  it('打开即取租户侧目录与白名单，本地勾选后整体替换保存（排序后发出），目录只取一次', async () => {
    let saved = ['identity.users.read']
    const calls = record((call) => {
      if (call.url === '/api/permissions?side=tenant')
        return json(200, { items: catalog })
      if (call.url.endsWith(':set-permissions')) {
        saved = (call.body as { permissions: string[] }).permissions
        return json(200, { edition: edition('e-1', 'free', { permissionCount: saved.length }), permissions: saved })
      }
      return json(200, { edition: edition('e-1', 'free', { permissionCount: saved.length }), permissions: saved })
    })
    const whitelist = useEditionPermissions()
    await whitelist.start(edition('e-1', 'free') as never)
    expect(whitelist.open.value).toBe(true)
    expect(whitelist.catalog.value).toHaveLength(3)
    expect(whitelist.checked.value).toEqual(['identity.users.read'])
    expect(whitelist.dirty.value).toBe(false)

    whitelist.setChecked(['identity.users.create', 'access.roles.read'])
    expect(whitelist.dirty.value).toBe(true)
    expect(await whitelist.save()).toBe(true)
    expect(calls.find(c => c.url.endsWith(':set-permissions'))).toMatchObject({ method: 'POST', url: '/api/editions/e-1:set-permissions', body: { permissions: ['access.roles.read', 'identity.users.create'] } })
    expect(whitelist.dirty.value).toBe(false)
    expect(whitelist.edition.value?.permissionCount).toBe(2)

    await whitelist.start(edition('e-1', 'free') as never)
    expect(calls.filter(c => c.url === '/api/permissions?side=tenant')).toHaveLength(1)
  })

  it('加载失败时只读（保存不发请求）；保存被拒给出原因、勾选保留', async () => {
    let calls = record(() => problem(403, 'SECURITY_DENIED_002', 'no catalog access'))
    const whitelist = useEditionPermissions()
    await whitelist.start(edition('e-1', 'free') as never)
    expect(whitelist.loadError.value).toBe('no catalog access')
    const sent = calls.length
    expect(await whitelist.save()).toBe(false)
    expect(calls).toHaveLength(sent)

    calls = record(call => call.url.endsWith(':set-permissions')
      ? problem(400, 'TENANCY_VALIDATION_002', 'platform-only permission')
      : call.url.startsWith('/api/permissions')
        ? json(200, { items: catalog })
        : json(200, { edition: edition('e-1', 'free'), permissions: [] }))
    await whitelist.start(edition('e-1', 'free') as never)
    expect(whitelist.loadError.value).toBeNull()
    whitelist.setChecked(['access.roles.read'])
    expect(await whitelist.save()).toBe(false)
    expect(whitelist.saveError.value).toBe('platform-only permission')
    expect(whitelist.checked.value).toEqual(['access.roles.read'])
    expect(calls.some(c => c.url.endsWith(':set-permissions'))).toBe(true)
  })
})
