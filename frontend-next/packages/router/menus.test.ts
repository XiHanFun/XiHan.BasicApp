import type { MenuNode } from '~/stores'
import { describe, expect, it } from 'vitest'
import { firstRoute, menuRoutes } from './menus'

const users: MenuNode = { code: 'identity.users', route: '/identity/users', component: 'identity/users/index', titleKey: 'identity.users.title', icon: 'lucide:users', order: 100, permission: 'identity.users.read', children: [] }
const roles: MenuNode = { code: 'access.roles', route: '/access/roles', component: 'access/roles/index', titleKey: 'access.roles.title', icon: null, order: 200, permission: 'access.roles.read', children: [] }

describe('menu routes', () => {
  it('turns the menu tree into flat named routes with permission metadata', () => {
    const records = menuRoutes([{ ...users, children: [roles] }])
    expect(records.map(r => r.name)).toEqual(['identity.users', 'access.roles'])
    expect(records[0]?.path).toBe('/identity/users')
    expect(records[0]?.meta).toEqual({ permission: 'identity.users.read', titleKey: 'identity.users.title' })
    expect(typeof records[1]?.component).toBe('function')
  })

  it('fails loudly when a menu points at a view that does not exist', () => {
    expect(() => menuRoutes([{ ...users, component: 'identity/nope/index' }])).toThrow(/is not found/)
  })

  it('picks the first page depth-first for the home redirect', () => {
    expect(firstRoute([users, roles])).toBe('/identity/users')
    expect(firstRoute([])).toBeNull()
  })
})
