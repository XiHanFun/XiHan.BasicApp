/**
 * 静态路由模式（static.ts）单元测试。
 * 职责边界：filterRoutesByPermission 的过滤口径（角色/权限/通配/递归/不可变性）
 * 与 isStaticRouteMode 的环境变量判定。不涉及路由安装（那部分在 guard-routes.test.ts）。
 *
 * 注意：本文件锁定的是**源码当前的真实行为**。filterRoutesByPermission 用的是
 * `if (!hasRoleAccess && !hasPermissionAccess) 排除`，即两侧「或」通过，
 * 因此只声明 roles（或只声明 permissions）的路由实际上恒被放行——
 * 这一点已作为疑似缺陷单独上报，用例在此如实锁定现状，避免默默改口径。
 */
import type { RouteRecordRaw } from 'vue-router'
import { describe, expect, it, vi } from 'vitest'
import { filterRoutesByPermission, isStaticRouteMode } from './static'

interface RouteMetaLike {
  roles?: string[]
  permissions?: string[]
  title?: string
}

function route(path: string, meta?: RouteMetaLike, children?: RouteRecordRaw[]): RouteRecordRaw {
  return { path, name: path, meta, children } as unknown as RouteRecordRaw
}

function paths(routes: RouteRecordRaw[]): string[] {
  return routes.map(item => item.path)
}

describe('filterRoutesByPermission 过滤口径', () => {
  it('无任何权限声明的路由对所有用户放行', () => {
    const result = filterRoutesByPermission([route('/free')], [], [])
    expect(paths(result)).toEqual(['/free'])
  })

  it('空路由表返回空数组，不抛错', () => {
    expect(filterRoutesByPermission([], ['admin'], ['*'])).toEqual([])
  })

  it('roles 与 permissions 都声明且两者都不满足时被剔除', () => {
    const result = filterRoutesByPermission(
      [route('/secret', { roles: ['admin'], permissions: ['sys:view'] })],
      ['guest'],
      ['other:view'],
    )
    expect(result).toEqual([])
  })

  it('roles 与 permissions 都声明时命中角色即放行（或的关系，不是与）', () => {
    const result = filterRoutesByPermission(
      [route('/secret', { roles: ['admin'], permissions: ['sys:view'] })],
      ['admin'],
      [],
    )
    expect(paths(result)).toEqual(['/secret'])
  })

  it('roles 与 permissions 都声明时命中权限即放行', () => {
    const result = filterRoutesByPermission(
      [route('/secret', { roles: ['admin'], permissions: ['sys:view'] })],
      [],
      ['sys:view'],
    )
    expect(paths(result)).toEqual(['/secret'])
  })

  it('权限通配 * 顶替任何具体权限码（当前口径下与角色无关）', () => {
    const result = filterRoutesByPermission(
      [route('/secret', { roles: ['admin'], permissions: ['sys:view'] })],
      ['guest'],
      ['*'],
    )
    expect(paths(result)).toEqual(['/secret'])
  })

  it('通配 * 只作用于权限侧：只声明 roles 的路由不靠 * 通过（当前实现下它本就恒通过）', () => {
    const denied = filterRoutesByPermission(
      [route('/role-only', { roles: ['admin'] })],
      ['guest'],
      [],
    )
    expect(paths(denied)).toEqual(['/role-only'])
  })

  it('仅声明 roles 且用户无该角色时依然放行——当前实现的真实行为（疑似缺陷，已上报）', () => {
    const result = filterRoutesByPermission(
      [route('/role-only', { roles: ['admin'] })],
      ['guest'],
      ['nothing'],
    )
    expect(paths(result)).toEqual(['/role-only'])
  })

  it('仅声明 permissions 且用户无该权限时依然放行——当前实现的真实行为（疑似缺陷，已上报）', () => {
    const result = filterRoutesByPermission(
      [route('/perm-only', { permissions: ['sys:view'] })],
      ['guest'],
      ['other'],
    )
    expect(paths(result)).toEqual(['/perm-only'])
  })

  it('roles 为空数组视同未声明，不参与过滤', () => {
    const result = filterRoutesByPermission(
      [route('/empty-arrays', { roles: [], permissions: [] })],
      [],
      [],
    )
    expect(paths(result)).toEqual(['/empty-arrays'])
  })

  it('meta 缺失（undefined）的路由按无声明处理', () => {
    const result = filterRoutesByPermission([route('/no-meta')], [], [])
    expect(paths(result)).toEqual(['/no-meta'])
  })

  it('子路由递归过滤：父级保留时不合权限的子级被剔除', () => {
    const tree = [
      route('/parent', undefined, [
        route('child-a', { roles: ['admin'], permissions: ['a:view'] }),
        route('child-b', { roles: ['admin'], permissions: ['b:view'] }),
      ]),
    ]
    const result = filterRoutesByPermission(tree, ['guest'], ['b:view'])
    expect(paths(result)).toEqual(['/parent'])
    expect(paths(result[0]?.children ?? [])).toEqual(['child-b'])
  })

  it('子级全部被剔除时父级仍保留，children 变成空数组', () => {
    const tree = [
      route('/parent', undefined, [
        route('child-a', { roles: ['admin'], permissions: ['a:view'] }),
      ]),
    ]
    const result = filterRoutesByPermission(tree, ['guest'], [])
    expect(paths(result)).toEqual(['/parent'])
    expect(result[0]?.children).toEqual([])
  })

  it('父级被剔除时整棵子树一并消失，不会把子级提升到顶层', () => {
    const tree = [
      route('/parent', { roles: ['admin'], permissions: ['p:view'] }, [
        route('child', undefined),
      ]),
    ]
    const result = filterRoutesByPermission(tree, ['guest'], [])
    expect(result).toEqual([])
  })

  it('三层嵌套逐层过滤，最深一层同样受权限约束', () => {
    const tree = [
      route('/l1', undefined, [
        route('l2', undefined, [
          route('l3-deny', { roles: ['admin'], permissions: ['deep:view'] }),
          route('l3-allow', undefined),
        ]),
      ]),
    ]
    const result = filterRoutesByPermission(tree, [], [])
    const level2 = result[0]?.children ?? []
    expect(paths(level2[0]?.children ?? [])).toEqual(['l3-allow'])
  })

  it('不修改入参：原路由对象与原 children 数组保持不变', () => {
    const child = route('child', { roles: ['admin'], permissions: ['x'] })
    const parent = route('/parent', undefined, [child])
    const originalChildren = parent.children
    filterRoutesByPermission([parent], ['guest'], [])
    expect(parent.children).toBe(originalChildren)
    expect(parent.children).toHaveLength(1)
  })

  it('返回的是浅拷贝的新对象，不是原路由的引用', () => {
    const original = route('/free')
    const [cloned] = filterRoutesByPermission([original], [], [])
    expect(cloned).not.toBe(original)
    expect(cloned?.path).toBe('/free')
  })

  it('children 为空数组的路由不被当作父级二次处理，原样保留空数组', () => {
    const result = filterRoutesByPermission([route('/leaf', undefined, [])], [], [])
    expect(result[0]?.children).toEqual([])
  })

  it('保留顺序：过滤后剩余路由的相对次序与输入一致', () => {
    const tree = [
      route('/a'),
      route('/b', { roles: ['admin'], permissions: ['b'] }),
      route('/c'),
      route('/d', { roles: ['admin'], permissions: ['d'] }),
    ]
    const result = filterRoutesByPermission(tree, [], ['d'])
    expect(paths(result)).toEqual(['/a', '/c', '/d'])
  })

  it('权限码大小写敏感，大小写不同视为不同权限', () => {
    const result = filterRoutesByPermission(
      [route('/case', { roles: ['Admin'], permissions: ['Sys:View'] })],
      ['admin'],
      ['sys:view'],
    )
    expect(result).toEqual([])
  })

  it('中文与 emoji 权限码按字符串精确匹配', () => {
    const kept = filterRoutesByPermission(
      [route('/i18n', { roles: ['管理员'], permissions: ['报表:查看🚀'] })],
      ['访客'],
      ['报表:查看🚀'],
    )
    expect(paths(kept)).toEqual(['/i18n'])

    const dropped = filterRoutesByPermission(
      [route('/i18n', { roles: ['管理员'], permissions: ['报表:查看🚀'] })],
      ['访客'],
      ['报表:查看'],
    )
    expect(dropped).toEqual([])
  })

  it('同一权限码在用户列表中重复出现不影响判定', () => {
    const result = filterRoutesByPermission(
      [route('/dup', { roles: ['admin'], permissions: ['x'] })],
      ['guest', 'guest'],
      ['x', 'x', 'x'],
    )
    expect(paths(result)).toEqual(['/dup'])
  })
})

describe('isStaticRouteMode 环境判定', () => {
  it('环境变量 VITE_AUTH_ROUTE_MODE 为 static 时进入静态模式', () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', 'static')
    expect(isStaticRouteMode()).toBe(true)
  })

  it('环境变量 VITE_AUTH_ROUTE_MODE 为 dynamic 时不是静态模式', () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', 'dynamic')
    expect(isStaticRouteMode()).toBe(false)
  })

  it('未配置 VITE_AUTH_ROUTE_MODE 时默认走动态（后端菜单）模式', () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', '')
    expect(isStaticRouteMode()).toBe(false)
  })

  it('大小写不匹配（Static）不算静态模式，判定是全等而非忽略大小写', () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', 'Static')
    expect(isStaticRouteMode()).toBe(false)
  })

  it('带首尾空格的 static 不算静态模式，不做 trim', () => {
    vi.stubEnv('VITE_AUTH_ROUTE_MODE', ' static ')
    expect(isStaticRouteMode()).toBe(false)
  })
})
