/**
 * 壳层菜单模型 → 路由对象映射（dynamic.ts，与 4.x 同一套规则）。
 * 职责边界：mapMenuToRoutes 的过滤、嵌套、重定向推导、meta 透传、组件解析与兜底视图、
 * 「名字 = 路由名」壳组件的缓存语义。组件路径按页面登记解析（views.ts），这里换成可控的登记表。
 */
import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import type { MenuMeta, MenuRoute } from '~/types'
import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'

type Loader = () => Promise<unknown>

const registry = vi.hoisted(() => ({ views: {} as Record<string, () => Promise<unknown>> }))

vi.mock('./views', () => ({
  resolveView: (component: string) => {
    const loader = registry.views[component]
    if (!loader)
      throw new Error(`View '${component}' is not found`)
    return loader
  },
  viewKeys: () => Object.keys(registry.views),
}))

// 兜底视图换成轻量替身：用例只关心落到了 404 兜底，不加载真实页面（XiHan.UI 与图标集，冷加载会超时）
vi.mock('~/views/_core/fallback/not-found.vue', async () => {
  const { defineComponent: define } = await import('vue')
  return { default: define({ name: 'NotFoundPage', render: () => null }) }
})

const { mapMenuToRoutes } = await import('./dynamic')

function menu(partial: Partial<MenuRoute> & { path: string }): MenuRoute {
  return {
    name: partial.name ?? '',
    component: partial.component,
    redirect: partial.redirect,
    children: partial.children,
    meta: (partial.meta ?? { title: 'menu.test' }) as MenuMeta,
    path: partial.path,
  }
}

/** 取路由上的组件懒加载函数（RouteRecordRaw 的联合类型不方便直接取 component） */
function loaderOf(route: RouteRecordRaw | undefined): Loader | undefined {
  return (route as unknown as { component?: Loader } | undefined)?.component
}

function makeLoader(inner: Component) {
  return vi.fn(async () => ({ default: inner }))
}

const emptyComponent = defineComponent({ name: 'EmptyProbe', render: () => null })
let consoleErrorSpy: ReturnType<typeof vi.spyOn>

beforeEach(() => {
  registry.views = {}
  consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  consoleErrorSpy.mockRestore()
})

describe('菜单过滤', () => {
  it('外链菜单（meta.link）不生成路由，子级中的外链同样被剔除，其余兄弟节点保留', () => {
    const routes = mapMenuToRoutes([
      menu({ path: '/docs', name: 'Docs', meta: { title: 'Docs', link: 'https://docs.xihanfun.com' } }),
      menu({
        path: '/system',
        name: 'System',
        children: [
          menu({ path: '/system/users', name: 'Users' }),
          menu({ path: '/system/help', name: 'Help', meta: { title: 'Help', link: 'https://help.xihanfun.com' } }),
        ],
      }),
    ])
    expect(routes.map(r => r.name)).toEqual(['System'])
    expect(routes[0]?.children?.map(r => r.name)).toEqual(['Users'])
  })

  it('path 为空的菜单被剔除，空菜单数组返回空路由数组', () => {
    expect(mapMenuToRoutes([menu({ path: '', name: 'Empty' })])).toEqual([])
    expect(mapMenuToRoutes([])).toEqual([])
  })
})

describe('基础字段与嵌套', () => {
  it('path / name 原样透传，meta 是同一个对象引用', () => {
    const meta = { title: 'identity.users.title', icon: 'lucide:users', keepAlive: true } as MenuMeta
    const [route] = mapMenuToRoutes([menu({ path: '/identity/users', name: 'identity.users', meta })])
    expect(route?.path).toBe('/identity/users')
    expect(route?.name).toBe('identity.users')
    expect(route?.meta).toBe(meta)
  })

  it('多层嵌套逐层递归转换，层级结构不被压平', () => {
    const [route] = mapMenuToRoutes([menu({
      path: '/a',
      name: 'A',
      children: [menu({ path: '/a/b', name: 'B', children: [menu({ path: '/a/b/c', name: 'C' })] })],
    })])
    expect(route?.children?.[0]?.children?.[0]?.name).toBe('C')
  })
})

describe('重定向推导', () => {
  it('有子路由且未配置 redirect 时，自动重定向到第一个可见子路径', () => {
    const [route] = mapMenuToRoutes([menu({
      path: '/system',
      name: 'System',
      children: [
        menu({ path: '/system/hidden', name: 'Hidden', meta: { title: 'h', hidden: true } }),
        menu({ path: '/system/users', name: 'Users' }),
      ],
    })])
    expect(route?.redirect).toBe('/system/users')
  })

  it('显式 redirect 命中可见子路由时优先，指向隐藏或不存在的路径时退回第一个可见子路径', () => {
    const children = [
      menu({ path: '/system/users', name: 'Users' }),
      menu({ path: '/system/roles', name: 'Roles' }),
      menu({ path: '/system/hidden', name: 'Hidden', meta: { title: 'h', hidden: true } }),
    ]
    expect(mapMenuToRoutes([menu({ path: '/system', name: 'S1', redirect: '/system/roles', children })])[0]?.redirect).toBe('/system/roles')
    expect(mapMenuToRoutes([menu({ path: '/system', name: 'S2', redirect: '/system/hidden', children })])[0]?.redirect).toBe('/system/users')
    expect(mapMenuToRoutes([menu({ path: '/system', name: 'S3', redirect: '/nope', children })])[0]?.redirect).toBe('/system/users')
  })

  it('叶子节点的 redirect 原样透传，指向自身时不写入，避免自跳死循环', () => {
    expect(mapMenuToRoutes([menu({ path: '/a', name: 'A', redirect: '/b' })])[0]?.redirect).toBe('/b')
    expect(mapMenuToRoutes([menu({ path: '/a', name: 'A', redirect: '/a' })])[0]?.redirect).toBeUndefined()
  })
})

describe('组件解析与兜底视图', () => {
  it('页面登记里有的组件路径包进路由名壳', async () => {
    registry.views['identity/users/index'] = makeLoader(emptyComponent)
    const [route] = mapMenuToRoutes([menu({ path: '/identity/users', name: 'identity.users', component: 'identity/users/index' })])
    const shell = await loaderOf(route)?.() as Component & { name?: string }
    expect(shell.name).toBe('identity.users')
  })

  it('component 未配置的叶子节点回落到统一的 not-found 兜底视图', async () => {
    const [route] = mapMenuToRoutes([menu({ path: '/blank', name: 'Blank' })])
    const mod = await loaderOf(route)?.() as { default: Component & { name?: string } }
    expect(mod.default.name).toBe('NotFoundPage')
    expect(consoleErrorSpy).not.toHaveBeenCalled()
  })

  it('component 配了但登记里没有时同样回落兜底视图，并留下错误日志而不是白屏或静默', async () => {
    const [route] = mapMenuToRoutes([menu({ path: '/broken', name: 'Broken', component: 'identity/nope/index' })])
    const mod = await loaderOf(route)?.() as { default: Component & { name?: string } }
    expect(mod.default.name).toBe('NotFoundPage')
    expect(consoleErrorSpy).toHaveBeenCalledWith(expect.stringContaining('[router]'), 'identity/nope/index', expect.any(Error))
  })

  it('有子路由的父级（目录）不套兜底视图，交给 RouterView 直穿', () => {
    const [route] = mapMenuToRoutes([menu({ path: '/system', name: 'System', children: [menu({ path: '/system/users', name: 'Users' })] })])
    expect(loaderOf(route)).toBeUndefined()
  })
})

describe('「名字 = 路由名」的壳组件', () => {
  it('壳的 name 恒等于路由名而非页面组件自身的名字，渲染时把页面组件原样渲染出来', async () => {
    const inner = defineComponent({ name: 'IdentityUsersPage', render: () => h('div', '页面内容') })
    registry.views['identity/users/index'] = makeLoader(inner)
    const [route] = mapMenuToRoutes([menu({ path: '/identity/users', name: 'identity.users.wrap', component: 'identity/users/index' })])
    const shell = await loaderOf(route)?.() as Component & { name?: string }
    expect(shell.name).toBe('identity.users.wrap')
    const wrapper = mount(shell)
    expect(wrapper.text()).toBe('页面内容')
    wrapper.unmount()
  })

  it('同一路由名重复解析、重建路由表都返回同一个壳，底层加载函数只执行一次（KeepAlive 命中的前提）', async () => {
    const loader = makeLoader(emptyComponent)
    registry.views['identity/cache/index'] = loader
    const build = () => mapMenuToRoutes([menu({ path: '/identity/cache', name: 'identity.cache', component: 'identity/cache/index' })])
    const resolve = loaderOf(build()[0])
    const first = await resolve?.()
    const second = await resolve?.()
    const rebuilt = await loaderOf(build()[0])?.()
    expect(second).toBe(first)
    expect(rebuilt).toBe(first)
    expect(loader).toHaveBeenCalledTimes(1)
  })

  it('两个菜单指向同一页面组件但路由名不同时，各自拿到互不相同的壳', async () => {
    registry.views['identity/twin/index'] = makeLoader(emptyComponent)
    const routes = mapMenuToRoutes([
      menu({ path: '/twin-a', name: 'twin.a', component: 'identity/twin/index' }),
      menu({ path: '/twin-b', name: 'twin.b', component: 'identity/twin/index' }),
    ])
    const shellA = await loaderOf(routes[0])?.() as Component & { name?: string }
    const shellB = await loaderOf(routes[1])?.() as Component & { name?: string }
    expect(shellA).not.toBe(shellB)
    expect([shellA.name, shellB.name]).toEqual(['twin.a', 'twin.b'])
  })

  it('模块没有 default 导出时直接把模块本身当组件包进壳', async () => {
    registry.views['identity/bare/index'] = () => Promise.resolve(defineComponent({ render: () => h('div', '裸模块') }))
    const [route] = mapMenuToRoutes([menu({ path: '/identity/bare', name: 'identity.bare', component: 'identity/bare/index' })])
    const wrapper = mount(await loaderOf(route)?.() as Component)
    expect(wrapper.text()).toBe('裸模块')
    wrapper.unmount()
  })

  it('路由名为空串时不包壳，component 就是解析到的加载函数本身', () => {
    const loader = makeLoader(emptyComponent)
    registry.views['identity/no-name/index'] = loader
    const [route] = mapMenuToRoutes([menu({ path: '/no-name', name: '', component: 'identity/no-name/index' })])
    expect(loaderOf(route)).toBe(loader)
  })
})
