/**
 * 核心路由表（routes/core.ts）的结构约束：name / path 唯一、认证与错误页齐备、通配路由压底。
 * 不涉及导航行为（那部分在 guard.test.ts）。
 */
import type { RouteRecordRaw } from 'vue-router'
import { describe, expect, it } from 'vitest'
import { AUTH_PATH, FORBIDDEN_PATH, LOGIN_PATH, NOT_FOUND_PATH, SERVER_ERROR_PATH } from '~/constants'
import { coreRoutes } from './routes/core'

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

function collectNames(routes: RouteRecordRaw[]): string[] {
  return routes.flatMap(route => [...(route.name ? [String(route.name)] : []), ...collectNames(route.children ?? [])])
}

function flatten(routes: RouteRecordRaw[]): RouteRecordRaw[] {
  return routes.flatMap(route => [route, ...flatten(route.children ?? [])])
}

describe('coreRoutes 结构约束', () => {
  it('路由名与完整路径全表唯一：重名会让 addRoute 静默覆盖，重复 path 会让后注册的那条永远匹配不到', () => {
    const names = collectNames(coreRoutes)
    expect(new Set(names).size).toBe(names.length)
    const paths = collectFullPaths(coreRoutes)
    expect(new Set(paths).size).toBe(paths.length)
  })

  it('认证父路由挂在 AUTH_PATH 下并默认重定向到登录页，登录页的完整路径等于 LOGIN_PATH', () => {
    const auth = coreRoutes.find(route => route.path === AUTH_PATH)
    expect(auth?.redirect).toBe(LOGIN_PATH)
    expect(collectFullPaths(coreRoutes)).toContain(LOGIN_PATH)
    // 认证子路由全部用相对路径，写成绝对路径会脱离 AUTH_PATH 前缀
    expect(auth?.children?.every(child => !child.path.startsWith('/'))).toBe(true)
  })

  it('403 / 500 / 404 三个兜底页齐备，守卫的白名单与失败跳转依赖它们存在', () => {
    const paths = coreRoutes.map(route => route.path)
    expect(paths).toEqual(expect.arrayContaining([FORBIDDEN_PATH, SERVER_ERROR_PATH, NOT_FOUND_PATH]))
  })

  it('通配路由是表内最后一条，NotFound 与 NotFoundCatchAll 两个名字都在表内（守卫按 name 判定 404）', () => {
    expect(coreRoutes.at(-1)?.path).toBe('/:pathMatch(.*)*')
    expect(collectNames(coreRoutes)).toEqual(expect.arrayContaining(['NotFound', 'NotFoundCatchAll']))
  })

  it('所有核心路由都标记 hidden 并带 title：避免混进侧边栏菜单，afterEach 的标题设置依赖 meta.title', () => {
    for (const route of flatten(coreRoutes)) {
      expect(route.meta?.hidden, String(route.name)).toBe(true)
      expect(typeof route.meta?.title, String(route.name)).toBe('string')
    }
  })
})
