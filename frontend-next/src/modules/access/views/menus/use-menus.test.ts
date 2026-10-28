import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { useMenus } from './use-menus'

function json(status: number, body: object, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function node(id: string, code: string, kind: 'Page' | 'Directory' | 'Link', extra: Record<string, unknown> = {}) {
  return { id, code, kind, package: kind === 'Page' ? code.split('.')[0] : 'custom', parentId: null, route: kind === 'Page' ? `/${code.replace('.', '/')}` : '', component: kind === 'Page' ? `${code.replace('.', '/')}/index` : '', titleKey: kind === 'Page' ? `${code}.title` : '', title: kind === 'Page' ? null : code, icon: null, order: 0, permission: null, externalUrl: null, isVisible: true, keepAlive: false, isAffix: false, isCustomized: false, children: [], ...extra }
}

describe('useMenus', () => {
  it('flattens the tree, edits a page without its managed fields, resets it and reports a refused delete', async () => {
    const calls: { method: string, url: string, body: unknown }[] = []
    const roles = node('m-roles', 'access.roles', 'Page', { order: 200, permission: 'access.roles.read' })
    const child = node('m-child', 'access.roles-audit', 'Page', { parentId: 'm-roles' })
    const system = node('m-sys', 'custom.system', 'Directory', { title: 'System' })
    let current: Record<string, unknown> = { ...roles, children: [child] }
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        const path = String(url)
        const method = init?.method ?? 'GET'
        const body = init?.body ? JSON.parse(String(init.body)) : undefined
        calls.push({ method, url: path, body })
        if (path.endsWith('/api/menus') && method === 'GET')
          return json(200, { items: [system, current] })
        if (path.endsWith('/api/menus/m-roles') && method === 'GET')
          return json(200, { ...current, children: [] })
        if (path.endsWith('/api/menus/m-roles') && method === 'PUT') {
          current = { ...current, ...body, isCustomized: true }
          return json(200, { ...current, children: [] })
        }
        if (path.endsWith('/api/menus/m-roles:reset')) {
          current = { ...roles, children: [child] }
          return json(200, { ...roles })
        }
        if (path.includes('/api/menus?ids='))
          return json(200, { succeeded: [], failed: [{ id: 'm-roles', code: 'ACCESS_CONFLICT_004', detail: 'built in' }] })
        return json(404, { code: 'PROTOCOLS_NOTFOUND_000', status: 404, detail: path }, 'application/problem+json')
      }) as unknown as typeof fetch,
    })

    const page = useMenus()
    await page.load()
    expect(page.flat.value.map(row => [row.node.code, row.depth])).toEqual([['custom.system', 0], ['access.roles', 0], ['access.roles-audit', 1]])
    expect(page.directories.value.map(d => d.code)).toEqual(['custom.system'])
    expect(page.isChildPage(page.flat.value[2]!.node)).toBe(true)
    expect(page.isChildPage(page.flat.value[1]!.node)).toBe(false)

    await page.select('m-roles')
    expect(page.edit.order).toBe('200')
    page.edit.title = '  Roles & grants '
    page.edit.parentId = 'm-sys'
    page.edit.keepAlive = true
    expect(await page.save()).toBe(true)
    expect(calls.find(c => c.method === 'PUT')?.body).toEqual({ title: 'Roles & grants', icon: null, order: 200, parentId: 'm-sys', isVisible: true, keepAlive: true, isAffix: false, externalUrl: null, permission: null })
    expect(page.selected.value?.isCustomized).toBe(true)
    expect(page.notice.value).toBe('saved')

    expect(await page.reset()).toBe(true)
    expect(page.selected.value?.isCustomized).toBe(false)
    expect(page.edit.title).toBe('')
    expect(page.notice.value).toBe('reset')

    expect(await page.remove()).toBe(false)
    expect(page.detailError.value).toBe('ACCESS_CONFLICT_004')
  })

  it('creates a link inside a directory and a directory without parent or address', async () => {
    const bodies: unknown[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (_url: RequestInfo | URL, init?: RequestInit) => {
        const method = init?.method ?? 'GET'
        if (method === 'POST') {
          const body = JSON.parse(String(init!.body))
          bodies.push(body)
          return json(201, node('m-new', `custom.${body.code}`, body.kind, { title: body.title, parentId: body.parentId, externalUrl: body.externalUrl }))
        }
        return json(200, { items: [] })
      }) as unknown as typeof fetch,
    })
    const page = useMenus()
    Object.assign(page.form, { kind: 'Link', code: 'docs', title: 'Docs', parentId: 'm-sys', externalUrl: ' https://docs.xihanfun.com ', order: '10' })
    expect(await page.submit()).toBe(true)
    expect(bodies[0]).toEqual({ kind: 'Link', code: 'docs', title: 'Docs', icon: null, order: 10, parentId: 'm-sys', externalUrl: 'https://docs.xihanfun.com', permission: null, isVisible: true })
    expect(page.selected.value?.code).toBe('custom.docs')

    Object.assign(page.form, { kind: 'Directory', code: 'system', title: 'System', parentId: 'm-sys', externalUrl: 'https://ignored' })
    expect(await page.submit()).toBe(true)
    expect(bodies[1]).toMatchObject({ kind: 'Directory', parentId: null, externalUrl: null })
  })
})
