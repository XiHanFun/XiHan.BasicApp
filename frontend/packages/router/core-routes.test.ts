/**
 * 核心路由表（routes/core.ts）单元测试。
 * 职责边界：只校验 coreRoutes 这份「唯一定义源」的结构约束——name/path 唯一、
 * 认证与错误页齐备、collectRouteNames 递归正确、CORE_ROUTE_NAMES 与路由树一致。
 * 不涉及导航行为（那部分在 guard-*.test.ts）。
 */
import type { RouteRecordRaw } from 'vue-router'
import { describe, expect, it } from 'vitest'
import { AUTH_PATH, FORBIDDEN_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
import { collectRouteNames, CORE_ROUTE_NAMES, coreRoutes } from './routes/core'

/** 收集路由树中全部 path（子路由拼上父级前缀，与 vue-router 的解析口径一致） */
function collectFullPaths(routes: RouteRecordRaw[], parent = ''): string[] {
  const paths: string[] = []
  for (const route of routes) {
    const full = route.path.startsWith('/')
      ? route.path
      : `${parent.replace(/\/$/, '')}/${route.path}`
    paths.push(full)
    if (route.children) {
      paths.push(...collectFullPaths(route.children, full))
    }
  }
  return paths
}

function asRoute(raw: unknown): RouteRecordRaw {
  return raw as RouteRecordRaw
}

describe('coreRoutes 结构约束', () => {
  it('路由名全表唯一，重名会让 router.addRoute 静默覆盖', () => {
    const names = collectRouteNames(coreRoutes)
    expect(names.length).toBeGreaterThan(0)
    expect(new Set(names).size).toBe(names.length)
  })

  it('完整路径全表唯一，重复 path 会让后注册的那条永远匹配不到', () => {
    const paths = collectFullPaths(coreRoutes)
    expect(new Set(paths).size).toBe(paths.length)
  })

  it('认证父路由挂在 AUTH_PATH 下并默认重定向到登录页', () => {
    const auth = coreRoutes.find(route => route.path === AUTH_PATH)
    expect(auth?.name).toBe('Authentication')
    expect(auth?.redirect).toBe(LOGIN_PATH)
  })

  it('登录页的完整路径必须等于常量 LOGIN_PATH，守卫按该常量跳转', () => {
    const paths = collectFullPaths(coreRoutes)
    expect(paths).toContain(LOGIN_PATH)
  })

  it('403/500/404 三个兜底页齐备，守卫的白名单与失败跳转依赖它们存在', () => {
    const topLevel = coreRoutes.map(route => route.path)
    expect(topLevel).toContain(FORBIDDEN_PATH)
    expect(topLevel).toContain(SERVER_ERROR_PATH)
    expect(topLevel).toContain(NOT_FOUND_PATH)
  })

  it('通配路由必须是表内最后一条，否则它会抢在后面的路由前面命中', () => {
    const lastRoute = coreRoutes[coreRoutes.length - 1]
    expect(lastRoute?.path).toBe('/:pathMatch(.*)*')
    expect(lastRoute?.name).toBe('NotFoundCatchAll')
    const catchAllCount = coreRoutes.filter(route => route.path.includes(':pathMatch')).length
    expect(catchAllCount).toBe(1)
  })

  it('守卫按 name 判定 404，NotFound 与 NotFoundCatchAll 两个名字都必须在表内', () => {
    const names = collectRouteNames(coreRoutes)
    expect(names).toContain('NotFound')
    expect(names).toContain('NotFoundCatchAll')
  })

  it('所有认证页与错误页都标记 hidden，避免混进侧边栏菜单', () => {
    const walk = (routes: RouteRecordRaw[]): void => {
      for (const route of routes) {
        expect((route.meta as { hidden?: boolean } | undefined)?.hidden).toBe(true)
        if (route.children) {
          walk(route.children)
        }
      }
    }
    walk(coreRoutes)
  })

  it('每条核心路由都带 title，afterEach 的标题设置依赖 meta.title', () => {
    const walk = (routes: RouteRecordRaw[]): void => {
      for (const route of routes) {
        const title = (route.meta as { title?: string } | undefined)?.title
        expect(typeof title).toBe('string')
        expect(title).not.toBe('')
        if (route.children) {
          walk(route.children)
        }
      }
    }
    walk(coreRoutes)
  })

  it('认证子路由全部用相对路径，写成绝对路径会脱离 AUTH_PATH 前缀', () => {
    const auth = coreRoutes.find(route => route.path === AUTH_PATH)
    const children = auth?.children ?? []
    expect(children.length).toBeGreaterThan(0)
    for (const child of children) {
      expect(child.path.startsWith('/')).toBe(false)
    }
  })
})

describe('collectRouteNames 递归提取', () => {
  it('空路由表返回空数组', () => {
    expect(collectRouteNames([])).toEqual([])
  })

  it('无 name 的路由被跳过，但仍继续递归它的子路由', () => {
    const names = collectRouteNames([
      asRoute({
        path: '/anonymous',
        children: [
          asRoute({ path: 'child', name: 'DeepChild' }),
        ],
      }),
    ])
    expect(names).toEqual(['DeepChild'])
  })

  it('父名先于子名入列，深层嵌套按深度优先展开', () => {
    const names = collectRouteNames([
      asRoute({
        path: '/a',
        name: 'A',
        children: [
          asRoute({
            path: 'b',
            name: 'B',
            children: [asRoute({ path: 'c', name: 'C' })],
          }),
        ],
      }),
      asRoute({ path: '/d', name: 'D' }),
    ])
    expect(names).toEqual(['A', 'B', 'C', 'D'])
  })

  it('路由名是 Symbol 时被 String 化后收集，不会漏掉也不会抛错', () => {
    const names = collectRouteNames([
      asRoute({ path: '/sym', name: Symbol('SymRoute') }),
    ])
    expect(names).toEqual(['Symbol(SymRoute)'])
  })

  it('children 为空数组时不产出任何额外名字', () => {
    const names = collectRouteNames([
      asRoute({ path: '/empty', name: 'Empty', children: [] }),
    ])
    expect(names).toEqual(['Empty'])
  })

  it('重名不会被去重——去重发生在 CORE_ROUTE_NAMES 的 Set 里', () => {
    const names = collectRouteNames([
      asRoute({ path: '/x', name: 'Same' }),
      asRoute({ path: '/y', name: 'Same' }),
    ])
    expect(names).toEqual(['Same', 'Same'])
  })
})

describe('登出保留名单 CORE_ROUTE_NAMES', () => {
  it('登出清路由时要保留的名字集合与 coreRoutes 的名字一一对应', () => {
    expect([...CORE_ROUTE_NAMES].sort()).toEqual([...new Set(collectRouteNames(coreRoutes))].sort())
  })

  it('业务动态路由名不在集合内，登出时才会被真正移除', () => {
    expect(CORE_ROUTE_NAMES.has('IdentityUser')).toBe(false)
    expect(CORE_ROUTE_NAMES.has('RootLayout')).toBe(false)
  })

  it('认证子页与兜底页都在集合内，登出后仍可停留在登录页', () => {
    for (const name of ['Login', 'Register', 'OAuthCallback', 'Forbidden', 'ServerError', 'NotFound', 'NotFoundCatchAll']) {
      expect(CORE_ROUTE_NAMES.has(name)).toBe(true)
    }
  })
})
