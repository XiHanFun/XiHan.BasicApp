import type { MenuRow } from './use-menus'
import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { filterMenuTree, menusResource, resetMenu, useMenuEditor, useMenuTree } from './use-menus'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function node(id: string, code: string, kind: 'Page' | 'Directory' | 'Link', extra: Record<string, unknown> = {}): MenuRow {
  return { id, code, kind, package: kind === 'Page' ? code.split('.')[0]! : 'custom', parentId: null, route: kind === 'Page' ? `/${code.replace('.', '/')}` : '', component: kind === 'Page' ? `${code.replace('.', '/')}/index` : '', titleKey: kind === 'Page' ? `${code}.title` : '', title: kind === 'Page' ? null : code, icon: null, order: 0, permission: null, externalUrl: null, isVisible: true, keepAlive: false, isAffix: false, isCustomized: false, children: [], ...extra } as MenuRow
}

/** 测试树：顶层目录 System（挂一个外链 Docs）、顶层页面 Roles（挂登记的子页面 Audit，已隐藏）、顶层页面 Users（已定制） */
function sampleTree(): MenuRow[] {
  const docs = node('m-docs', 'custom.docs', 'Link', { title: 'Docs', parentId: 'm-sys', externalUrl: 'https://docs.xihanfun.com' })
  const system = node('m-sys', 'custom.system', 'Directory', { title: 'System', children: [docs] })
  const audit = node('m-audit', 'access.roles-audit', 'Page', { parentId: 'm-roles', isVisible: false })
  const roles = node('m-roles', 'access.roles', 'Page', { order: 200, permission: 'access.roles.read', children: [audit] })
  const users = node('m-users', 'identity.users', 'Page', { isCustomized: true, title: 'People' })
  return [system, roles, users]
}

/** 测试里的「翻译」：页面按文案键给个可读标题 */
function titleOf(row: MenuRow): string {
  return row.title ?? ({ 'access.roles.title': 'Roles', 'access.roles-audit.title': 'Role audit' } as Record<string, string>)[row.titleKey] ?? row.titleKey
}

function codes(rows: readonly MenuRow[]): unknown[] {
  return rows.map(row => (row.children.length > 0 ? [row.code, codes(row.children)] : row.code))
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

describe('menusResource', () => {
  it('树形模式取整棵树：整树交给 onLoaded（上级候选不受搜索影响），表格拿本地筛选后的树', async () => {
    const calls = record(() => json(200, { items: sampleTree() }))
    let loaded: MenuRow[] = []
    const resource = menusResource(titleOf, (nodes) => {
      loaded = nodes
    })
    const all = await resource.tree!({ page: 1, pageSize: 20, filters: {} })
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/menus' })
    expect(codes(all)).toEqual([['custom.system', ['custom.docs']], ['access.roles', ['access.roles-audit']], 'identity.users'])

    const filtered = await resource.tree!({ page: 1, pageSize: 20, filters: { keyword: 'audit' } })
    expect(codes(filtered)).toEqual([['access.roles', ['access.roles-audit']]])
    expect(loaded).toHaveLength(3)
  })

  it('本地筛选：关键字按显示标题 / 编码 / 路由 / 外链地址，种类、显示、已定制精确匹配；没登记的字段与区间 / 多选报错', () => {
    const tree = sampleTree()
    expect(codes(filterMenuTree(tree, { filters: { keyword: 'people' } }, titleOf))).toEqual(['identity.users'])
    expect(codes(filterMenuTree(tree, { filters: { keyword: 'XIHANFUN.COM' } }, titleOf))).toEqual([['custom.system', ['custom.docs']]])
    expect(codes(filterMenuTree(tree, { filters: { kind: 'Link' } }, titleOf))).toEqual([['custom.system', ['custom.docs']]])
    expect(codes(filterMenuTree(tree, { filters: { isVisible: 'false' } }, titleOf))).toEqual([['access.roles', ['access.roles-audit']]])
    expect(codes(filterMenuTree(tree, { filters: { isCustomized: 'true', kind: 'Page' } }, titleOf))).toEqual(['identity.users'])
    expect(codes(filterMenuTree(tree, { filters: { keyword: '  ', kind: null } }, titleOf))).toHaveLength(3)
    expect(() => filterMenuTree(tree, { filters: { permission: 'access.roles.read' } }, titleOf)).toThrow('permission')
    expect(() => filterMenuTree(tree, { filters: {}, conditionFilters: [{ field: 'order', operator: 'In', value: [1] } as never] }, titleOf)).toThrow()
  })

  it('删除页面节点被后端逐项拒绝时带着错误码抛出；恢复默认调 menus.reset', async () => {
    const calls = record((call) => {
      if (call.method === 'DELETE')
        return json(200, { succeeded: [], failed: [{ id: 'm-roles', code: 'ACCESS_CONFLICT_004' }] })
      return json(200, node('m-users', 'identity.users', 'Page'))
    })
    await expect(menusResource(titleOf).remove!('m-roles')).rejects.toThrow('ACCESS_CONFLICT_004')
    expect(calls[0]!.url).toBe('/api/menus?ids=m-roles')
    expect((await resetMenu('m-users')).isCustomized).toBe(false)
    expect(calls[1]).toMatchObject({ method: 'POST', url: '/api/menus/m-users:reset' })
  })
})

describe('useMenuTree', () => {
  it('摊平整树找目录候选；上级是页面的页面即登记的子页面', () => {
    const tree = useMenuTree()
    tree.setNodes(sampleTree())
    expect(tree.flat.value.map(row => row.code)).toEqual(['custom.system', 'custom.docs', 'access.roles', 'access.roles-audit', 'identity.users'])
    expect(tree.directories.value.map(row => row.id)).toEqual(['m-sys'])
    expect(tree.isChildPage(tree.find('m-audit')!)).toBe(true)
    expect(tree.isChildPage(tree.find('m-roles')!)).toBe(false)
    expect(tree.isChildPage(tree.find('m-docs')!)).toBe(false)
    expect(tree.find(null)).toBeUndefined()
  })
})

describe('useMenuEditor', () => {
  function editorWithTree() {
    const tree = useMenuTree()
    tree.setNodes(sampleTree())
    return { tree, editor: useMenuEditor(tree.isChildPage) }
  }

  it('新建：缺省目录（不带上级与地址）；切到外链可挂目录；在目录行上新建即外链挂在该目录', async () => {
    const calls = record(call => json(201, node('m-new', `custom.${(call.body as { code: string }).code}`, (call.body as { kind: 'Directory' }).kind)))
    const { editor } = editorWithTree()
    editor.startCreate()
    expect(editor.form.kind).toBe('Directory')
    expect(editor.parentLocked.value).toBe(true)
    Object.assign(editor.form, { code: ' ops ', title: ' Ops ', order: 5, externalUrl: 'https://ignored', parentId: 'm-sys' })
    expect(await editor.submit()).toBe(true)
    expect(editor.open.value).toBe(false)
    expect(calls[0]).toMatchObject({ method: 'POST', url: '/api/menus' })
    expect(calls[0]!.body).toEqual({ kind: 'Directory', code: 'ops', title: 'Ops', icon: null, order: 5, parentId: null, externalUrl: null, permission: null, isVisible: true })

    editor.startCreate('Link', 'm-sys')
    expect(editor.form).toMatchObject({ kind: 'Link', parentId: 'm-sys' })
    expect(editor.parentLocked.value).toBe(false)
    Object.assign(editor.form, { code: 'docs2', title: 'Docs 2', externalUrl: ' https://docs.xihanfun.com/2 ', permission: ' access.roles.read ', isVisible: false })
    expect(await editor.submit()).toBe(true)
    expect(calls[1]!.body).toEqual({ kind: 'Link', code: 'docs2', title: 'Docs 2', icon: null, order: 0, parentId: 'm-sys', externalUrl: 'https://docs.xihanfun.com/2', permission: 'access.roles.read', isVisible: false })

    editor.startCreate('Link', 'm-sys')
    editor.changeKind('Directory')
    expect(editor.form.parentId).toBeNull()
  })

  it('编辑页面节点只送展示字段：标题留空即用登记标题，外链与权限不送，可挂到目录', async () => {
    const calls = record(call => json(200, { ...node('m-roles', 'access.roles', 'Page'), ...(call.body as object), isCustomized: true }))
    const { tree, editor } = editorWithTree()
    editor.startEdit(tree.find('m-roles')!)
    expect(editor.isPage.value).toBe(true)
    expect(editor.parentLocked.value).toBe(false)
    expect(editor.form).toMatchObject({ title: '', order: 200, permission: 'access.roles.read', parentId: null })
    Object.assign(editor.form, { title: '  Roles & grants ', parentId: 'm-sys', keepAlive: true })
    expect(await editor.submit()).toBe(true)
    expect(calls[0]).toMatchObject({ method: 'PUT', url: '/api/menus/m-roles' })
    expect(calls[0]!.body).toEqual({ title: 'Roles & grants', icon: null, order: 200, parentId: 'm-sys', isVisible: true, keepAlive: true, isAffix: false, externalUrl: null, permission: null })

    editor.startEdit(tree.find('m-audit')!)
    expect(editor.parentLocked.value).toBe(true)
    editor.form.isVisible = true
    expect(await editor.submit()).toBe(true)
    expect(calls[1]!.body).toMatchObject({ parentId: 'm-roles', isVisible: true, permission: null })
  })

  it('编辑运营节点：外链送地址与权限，目录恒在顶层', async () => {
    const calls = record(() => json(200, node('m-x', 'custom.x', 'Link')))
    const { tree, editor } = editorWithTree()
    editor.startEdit(tree.find('m-docs')!)
    editor.form.parentId = null
    editor.form.permission = 'identity.users.read'
    expect(await editor.submit()).toBe(true)
    expect(calls[0]!.body).toEqual({ title: 'Docs', icon: null, order: 0, parentId: null, isVisible: true, keepAlive: false, isAffix: false, externalUrl: 'https://docs.xihanfun.com', permission: 'identity.users.read' })

    editor.startEdit(tree.find('m-sys')!)
    expect(editor.parentLocked.value).toBe(true)
    editor.form.title = 'Platform'
    expect(await editor.submit()).toBe(true)
    expect(calls[1]).toMatchObject({ url: '/api/menus/m-sys', body: { title: 'Platform', parentId: null, externalUrl: null, permission: null } })
  })

  it('字段级错误挂回字段，其余错误给整条提示，弹窗保持打开', async () => {
    record(() => json(400, { code: 'ACCESS_VALIDATION_007', status: 400, detail: 'bad url', traceId: 'x', errors: { externalUrl: ['must be http(s)'] } }, 'application/problem+json'))
    const { editor } = editorWithTree()
    editor.startCreate('Link')
    Object.assign(editor.form, { code: 'docs', title: 'Docs', externalUrl: 'ftp://x' })
    expect(await editor.submit()).toBe(false)
    expect(editor.open.value).toBe(true)
    expect(editor.fieldError('externalUrl')).toBe('must be http(s)')
    expect(editor.submitError.value).toBeNull()

    record(() => json(409, { code: 'ACCESS_CONFLICT_003', status: 409, detail: 'code taken', traceId: 'x' }, 'application/problem+json'))
    expect(await editor.submit()).toBe(false)
    expect(editor.submitError.value).toBe('code taken')
  })
})
