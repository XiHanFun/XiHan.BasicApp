import type { MenuNode } from '~/stores'
import { describe, expect, it } from 'vitest'
import { toMenuRoutes } from '~/stores'
import { firstRoute } from './menus'

function page(code: string, route: string, extra: Partial<MenuNode> = {}): MenuNode {
  return {
    code,
    kind: 'Page',
    route,
    component: `${route.slice(1)}/index`,
    titleKey: `${code}.title`,
    title: null,
    icon: null,
    order: 0,
    permission: null,
    externalUrl: null,
    hidden: false,
    keepAlive: false,
    affix: false,
    badge: null,
    badgeType: null,
    badgeDot: false,
    children: [],
    ...extra,
  }
}

const users = page('identity.users', '/identity/users', { icon: 'lucide:users', order: 100, permission: 'identity.users.read' })
const roles = page('access.roles', '/access/roles', { order: 200, permission: 'access.roles.read' })
const directory: MenuNode = {
  ...page('custom.system', ''),
  kind: 'Directory',
  component: '',
  titleKey: '',
  title: 'System',
}
const link: MenuNode = {
  ...page('custom.docs', ''),
  kind: 'Link',
  component: '',
  titleKey: '',
  title: 'Docs',
  externalUrl: 'https://docs.xihanfun.com',
}

describe('first route', () => {
  it('lands on the first visible page depth-first, skipping directories, links and hidden pages', () => {
    expect(firstRoute([users, roles])).toBe('/identity/users')
    expect(firstRoute([
      {
        ...directory,
        children: [link, roles],
      },
      users,
    ])).toBe('/access/roles')
    expect(firstRoute([
      { ...users, hidden: true },
      roles,
    ])).toBe('/access/roles')
    expect(firstRoute([
      { ...users, hidden: true },
    ])).toBe('/identity/users')
    expect(firstRoute([directory, link])).toBeNull()
    expect(firstRoute([])).toBeNull()
  })
})

describe('menu projection for the shell', () => {
  it('maps directories to groups, links to meta.link, and page flags to tab metadata', () => {
    const [group] = toMenuRoutes([
      {
        ...directory,
        children: [
          {
            ...roles,
            keepAlive: true,
            affix: true,
            hidden: true,
          },
          link,
        ],
      },
    ])
    expect(group).toMatchObject({
      path: '/custom.system',
      name: 'custom.system',
      component: undefined,
      meta: { title: 'System' },
    })
    expect(group?.children?.[0]).toMatchObject({
      path: '/access/roles',
      component: 'access/roles/index',
      meta: {
        title: 'access.roles.title',
        keepAlive: true,
        affixTab: true,
        hidden: true,
      },
    })
    expect(group?.children?.[1]).toMatchObject({
      path: '/custom.docs',
      component: undefined,
      meta: { title: 'Docs', link: 'https://docs.xihanfun.com' },
    })
  })

  it('maps badges to the shell metadata: text and tone, the dot flag, nothing when unset', () => {
    const [labelled, dotted, plain] = toMenuRoutes([
      { ...users, badge: 'New', badgeType: 'success' },
      { ...roles, badgeDot: true, badgeType: 'danger' },
      link,
    ])
    expect(labelled?.meta).toMatchObject({ badge: 'New', badgeType: 'success' })
    expect(labelled?.meta.dot).toBeUndefined()
    expect(dotted?.meta).toMatchObject({ dot: true, badgeType: 'danger' })
    expect(dotted?.meta.badge).toBeUndefined()
    expect(plain?.meta.badge).toBeUndefined()
    expect(plain?.meta.badgeType).toBeUndefined()
    expect(plain?.meta.dot).toBeUndefined()
  })
})
