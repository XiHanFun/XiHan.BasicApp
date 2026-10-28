import type { MenuNode } from '~/stores'
import { describe, expect, it } from 'vitest'
import { toMenuRoutes } from '~/stores'
import { firstRoute, menuRoutes } from './menus'

function page(code: string, route: string, extra: Partial<MenuNode> = {}): MenuNode {
  return { code, kind: 'Page', route, component: `${route.slice(1)}/index`, titleKey: `${code}.title`, title: null, icon: null, order: 0, permission: null, externalUrl: null, hidden: false, keepAlive: false, affix: false, children: [], ...extra }
}

const users = page('identity.users', '/identity/users', { icon: 'lucide:users', order: 100, permission: 'identity.users.read' })
const roles = page('access.roles', '/access/roles', { order: 200, permission: 'access.roles.read' })
const directory: MenuNode = { ...page('custom.system', ''), kind: 'Directory', component: '', titleKey: '', title: 'System' }
const link: MenuNode = { ...page('custom.docs', ''), kind: 'Link', component: '', titleKey: '', title: 'Docs', externalUrl: 'https://docs.xihanfun.com' }

describe('menu routes', () => {
  it('turns the page nodes into flat named routes with permission metadata', () => {
    const records = menuRoutes([{ ...users, children: [roles] }])
    expect(records.map(r => r.name)).toEqual(['identity.users', 'access.roles'])
    expect(records[0]?.path).toBe('/identity/users')
    // 壳层（标签栏、面包屑、全局搜索）读 meta.title / icon
    expect(records[0]?.meta).toEqual({ permission: 'identity.users.read', titleKey: 'identity.users.title', title: 'identity.users.title', icon: 'lucide:users', keepAlive: undefined })
    expect(typeof records[1]?.component).toBe('function')
  })

  it('skips directories and links, keeps hidden pages routable, and carries overrides and keep-alive', () => {
    const renamed = { ...roles, title: 'Roles & grants', keepAlive: true, hidden: true }
    const records = menuRoutes([{ ...directory, children: [renamed, link] }])
    expect(records.map(r => r.name)).toEqual(['access.roles'])
    expect(records[0]?.meta).toMatchObject({ titleKey: undefined, title: 'Roles & grants', keepAlive: true })
  })

  it('fails loudly when a menu points at a view that does not exist', () => {
    expect(() => menuRoutes([{ ...users, component: 'identity/nope/index' }])).toThrow(/is not found/)
  })

  it('lands on the first visible page depth-first, skipping directories, links and hidden pages', () => {
    expect(firstRoute([users, roles])).toBe('/identity/users')
    expect(firstRoute([{ ...directory, children: [link, roles] }, users])).toBe('/access/roles')
    expect(firstRoute([{ ...users, hidden: true }, roles])).toBe('/access/roles')
    expect(firstRoute([{ ...users, hidden: true }])).toBe('/identity/users')
    expect(firstRoute([directory, link])).toBeNull()
    expect(firstRoute([])).toBeNull()
  })
})

describe('menu projection for the shell', () => {
  it('maps directories to groups, links to meta.link, and page flags to tab metadata', () => {
    const [group] = toMenuRoutes([{ ...directory, children: [{ ...roles, keepAlive: true, affix: true, hidden: true }, link] }])
    expect(group).toMatchObject({ path: '/custom.system', name: 'custom.system', component: undefined, meta: { title: 'System' } })
    expect(group?.children?.[0]).toMatchObject({ path: '/access/roles', component: 'access/roles/index', meta: { title: 'access.roles.title', keepAlive: true, affixTab: true, hidden: true } })
    expect(group?.children?.[1]).toMatchObject({ path: '/custom.docs', component: undefined, meta: { title: 'Docs', link: 'https://docs.xihanfun.com' } })
  })
})
