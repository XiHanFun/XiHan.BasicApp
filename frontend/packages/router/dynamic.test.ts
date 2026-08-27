/**
 * 后端菜单 → 路由对象映射（dynamic.ts）单元测试。
 * 职责边界：mapMenuToRoutes 的过滤、嵌套、重定向推导、meta 透传、组件解析优先级、
 * 「名字 = 路由名」壳组件的缓存语义，以及非法/缺失数据的容错。
 * 组件解析（resolveView）未导出，全部经 mapMenuToRoutes 间接验证。
 */
import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import type { MenuMeta, MenuRoute } from '~/types'
import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { registerAppContext } from '~/stores/app-context'
import { mapMenuToRoutes } from './dynamic'

type Loader = () => Promise<unknown>

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

beforeEach(() => {
  // 上下文是模块级单例，每个用例都重置成一份干净的注册表，保证任意顺序可跑
  registerAppContext({ viewModules: {}, explicitComponentMap: {} })
})

describe('菜单过滤', () => {
  it('外链菜单（meta.link）不生成路由，由菜单点击直接开新标签', () => {
    const routes = mapMenuToRoutes([
      menu({ path: '/docs', name: 'Docs', meta: { title: 'menu.docs', link: 'https://example.com' } }),
      menu({ path: '/inner', name: 'Inner' }),
    ])
    expect(routes.map(item => item.path)).toEqual(['/inner'])
  })

  it('path 为空字符串的菜单被剔除，空 path 会污染整棵路由表', () => {
    const routes = mapMenuToRoutes([
      menu({ path: '', name: 'NoPath' }),
      menu({ path: '/ok', name: 'Ok' }),
    ])
    expect(routes.map(item => item.path)).toEqual(['/ok'])
  })

  it('空菜单数组返回空路由数组', () => {
    expect(mapMenuToRoutes([])).toEqual([])
  })

  it('子级中的外链同样被剔除，其余兄弟节点保留', () => {
    const routes = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        children: [
          menu({ path: '/group/out', name: 'Out', meta: { title: 'menu.out', link: 'https://example.com' } }),
          menu({ path: '/group/in', name: 'In' }),
        ],
      }),
    ])
    expect((routes[0]?.children ?? []).map(item => item.path)).toEqual(['/group/in'])
  })
})

describe('基础字段与嵌套', () => {
  it('path/name 原样透传给路由对象', () => {
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', name: 'IdentityUser' })])
    expect(route?.path).toBe('/identity/user')
    expect(route?.name).toBe('IdentityUser')
  })

  it('meta 是同一个对象引用透传，不做拷贝或字段裁剪', () => {
    const meta: MenuMeta = {
      title: 'menu.identity.user',
      icon: 'mdi:account',
      keepAlive: true,
      affixTab: true,
      order: 3,
      badge: '新',
      dot: true,
    }
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', name: 'IdentityUser', meta })])
    expect(route?.meta).toBe(meta)
  })

  it('三层嵌套逐层递归转换，层级结构不被压平', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/l1',
        name: 'L1',
        children: [
          menu({
            path: '/l1/l2',
            name: 'L2',
            children: [menu({ path: '/l1/l2/l3', name: 'L3' })],
          }),
        ],
      }),
    ])
    const level2 = route?.children?.[0]
    expect(level2?.name).toBe('L2')
    expect(level2?.children?.[0]?.name).toBe('L3')
  })

  it('children 为空数组时不产生 children 字段之外的副作用，按叶子处理', () => {
    const [route] = mapMenuToRoutes([menu({ path: '/leaf', name: 'Leaf', children: [] })])
    expect(route?.children).toBeUndefined()
  })
})

describe('重定向推导', () => {
  it('叶子节点的 redirect 原样透传', () => {
    const [route] = mapMenuToRoutes([
      menu({ path: '/old', name: 'Old', redirect: '/new' }),
    ])
    expect(route?.redirect).toBe('/new')
  })

  it('redirect 指向自身时不写入，避免自跳死循环', () => {
    const [route] = mapMenuToRoutes([
      menu({ path: '/self', name: 'Self', redirect: '/self' }),
    ])
    expect(route?.redirect).toBeUndefined()
  })

  it('叶子节点 redirect 为空串视同未配置', () => {
    const [route] = mapMenuToRoutes([
      menu({ path: '/blank', name: 'Blank', redirect: '' }),
    ])
    expect(route?.redirect).toBeUndefined()
  })

  it('有子路由且未配置 redirect 时，自动重定向到第一个可导航子路径', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        children: [
          menu({ path: '/group/a', name: 'GroupA' }),
          menu({ path: '/group/b', name: 'GroupB' }),
        ],
      }),
    ])
    expect(route?.redirect).toBe('/group/a')
  })

  it('第一个子路由 hidden 时跳过它，重定向到第一个可见子路径', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        children: [
          menu({ path: '/group/hidden', name: 'GroupHidden', meta: { title: 'x', hidden: true } }),
          menu({ path: '/group/visible', name: 'GroupVisible' }),
        ],
      }),
    ])
    expect(route?.redirect).toBe('/group/visible')
  })

  it('显式 redirect 命中可见子路由时优先于「第一个子路径」', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        redirect: '/group/b',
        children: [
          menu({ path: '/group/a', name: 'GroupA' }),
          menu({ path: '/group/b', name: 'GroupB' }),
        ],
      }),
    ])
    expect(route?.redirect).toBe('/group/b')
  })

  it('显式 redirect 指向隐藏子路由时被丢弃，退回第一个可见子路径', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        redirect: '/group/hidden',
        children: [
          menu({ path: '/group/hidden', name: 'GroupHidden', meta: { title: 'x', hidden: true } }),
          menu({ path: '/group/visible', name: 'GroupVisible' }),
        ],
      }),
    ])
    expect(route?.redirect).toBe('/group/visible')
  })

  it('显式 redirect 指向不存在的路径时被丢弃，退回第一个可见子路径', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        redirect: '/nowhere',
        children: [menu({ path: '/group/a', name: 'GroupA' })],
      }),
    ])
    expect(route?.redirect).toBe('/group/a')
  })

  it('子路由全部隐藏时仍能沿隐藏节点向下找到可导航路径', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        children: [
          menu({
            path: '/group/hidden',
            name: 'GroupHidden',
            meta: { title: 'x', hidden: true },
            children: [menu({ path: '/group/hidden/leaf', name: 'HiddenLeaf' })],
          }),
        ],
      }),
    ])
    expect(route?.redirect).toBe('/group/hidden/leaf')
  })

  it('子路由被外链过滤成空数组后按叶子处理，显式 redirect 仍生效', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: 'Group',
        redirect: '/elsewhere',
        children: [
          menu({ path: '/group/out', name: 'Out', meta: { title: 'x', link: 'https://example.com' } }),
        ],
      }),
    ])
    expect(route?.children).toEqual([])
    expect(route?.redirect).toBe('/elsewhere')
  })
})

describe('组件解析', () => {
  it('后端 Component 命中 /src/views/<path>.vue', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/identity/user.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', component: 'Identity/User' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('后端 Component 命中 /src/views/<path>/index.vue', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/identity/user/index.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', component: 'Identity/User' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('大驼峰目录名转 kebab-case 后匹配', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/system-log/operation-log.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/log/op', component: 'SystemLog/OperationLog' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('连续大写缩写按「最后一个大写归下一个词」切分（APIKey → api-key）', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/api-key/index.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/api-key', component: 'APIKey/Index' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('下划线在 kebab 化时转成连字符（user_center → user-center）', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/user-center/index.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/uc', component: 'user_center/index' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('去掉尾部 /Index 后的候选也参与匹配（Identity/User/Index → identity/user.vue）', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/identity/user.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', component: 'Identity/User/Index' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('剥离前导斜杠、views/ 前缀与 .vue 后缀后再匹配', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/identity/user.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', component: '//views/Identity/User.vue' })])
    expect(loaderOf(route)).toBe(loader)
  })

  it('src 显式映射优先于 glob 扫描结果', () => {
    const explicit = makeLoader(emptyComponent)
    const glob = makeLoader(emptyComponent)
    registerAppContext({
      explicitComponentMap: { 'identity/user': explicit },
      viewModules: { '/src/views/identity/user.vue': glob },
    })
    const [route] = mapMenuToRoutes([menu({ path: '/identity/user', component: 'Identity/User' })])
    expect(loaderOf(route)).toBe(explicit)
    expect(loaderOf(route)).not.toBe(glob)
  })

  it('packages 自带的 _core 视图优先于 src 的同名显式映射', () => {
    const explicit = makeLoader(emptyComponent)
    registerAppContext({ explicitComponentMap: { '_core/about/index': explicit } })
    const [route] = mapMenuToRoutes([menu({ path: '/about', component: '_core/About/Index' })])
    expect(typeof loaderOf(route)).toBe('function')
    expect(loaderOf(route)).not.toBe(explicit)
  })

  it('component 未配置的叶子节点回落到统一的 not-found 兜底视图', () => {
    const routes = mapMenuToRoutes([
      menu({ path: '/a', name: '' }),
      menu({ path: '/b', name: '' }),
    ])
    const first = loaderOf(routes[0])
    expect(typeof first).toBe('function')
    // 兜底视图是模块级单例：两条未命中的路由必须指向同一个加载函数
    expect(loaderOf(routes[1])).toBe(first)
  })

  it('component 配了但解析不到时同样回落兜底视图，而不是留空导致白屏', () => {
    const known = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/known.vue': known } })
    const routes = mapMenuToRoutes([
      menu({ path: '/unknown', name: '', component: 'Not/Exist' }),
      menu({ path: '/known', name: '', component: 'Known' }),
    ])
    expect(loaderOf(routes[0])).not.toBe(known)
    expect(typeof loaderOf(routes[0])).toBe('function')
    expect(loaderOf(routes[1])).toBe(known)
  })

  it('有子路由的父级解析不到组件时不套兜底视图，交给 RouterView 直穿', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: '',
        children: [menu({ path: '/group/a', name: '' })],
      }),
    ])
    expect(loaderOf(route)).toBeUndefined()
  })

  it('子路由被外链过滤成空数组的父级仍会套兜底视图', () => {
    const [route] = mapMenuToRoutes([
      menu({
        path: '/group',
        name: '',
        children: [menu({ path: '/group/out', name: '', meta: { title: 'x', link: 'https://e.com' } })],
      }),
    ])
    expect(typeof loaderOf(route)).toBe('function')
  })

  it('注册表被替换后重新解析，不吃上一次的注册结果', () => {
    const first = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/swap.vue': first } })
    expect(loaderOf(mapMenuToRoutes([menu({ path: '/swap', component: 'Swap' })])[0])).toBe(first)

    const second = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/swap.vue': second } })
    expect(loaderOf(mapMenuToRoutes([menu({ path: '/swap', component: 'Swap' })])[0])).toBe(second)
  })
})

describe('「名字 = 路由名」的壳组件', () => {
  it('带路由名时包一层壳，壳的 name 恒等于路由名而非页面组件自身的名字', async () => {
    const inner = defineComponent({ name: 'PlatformUserPage', render: () => h('div', 'inner') })
    registerAppContext({ viewModules: { '/src/views/wrap-a.vue': makeLoader(inner) } })
    const [route] = mapMenuToRoutes([menu({ path: '/wrap-a', name: 'WrapA', component: 'WrapA' })])
    const shell = await loaderOf(route)?.() as Component & { name?: string }
    expect(shell.name).toBe('WrapA')
    expect(shell.name).not.toBe('PlatformUserPage')
  })

  it('壳组件渲染时把页面组件原样渲染出来', async () => {
    const inner = defineComponent({ name: 'InnerPage', render: () => h('div', '页面内容') })
    registerAppContext({ viewModules: { '/src/views/wrap-render.vue': makeLoader(inner) } })
    const [route] = mapMenuToRoutes([menu({ path: '/wrap-render', name: 'WrapRender', component: 'WrapRender' })])
    const shell = await loaderOf(route)?.() as Component
    const wrapper = mount(shell)
    expect(wrapper.text()).toBe('页面内容')
    wrapper.unmount()
  })

  it('同一路由名重复解析返回同一个壳实例，底层加载函数只执行一次（KeepAlive 命中的前提）', async () => {
    const loader = makeLoader(defineComponent({ render: () => null }))
    registerAppContext({ viewModules: { '/src/views/wrap-cache.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/wrap-cache', name: 'WrapCache', component: 'WrapCache' })])
    const resolve = loaderOf(route)
    const first = await resolve?.()
    const second = await resolve?.()
    expect(second).toBe(first)
    expect(loader).toHaveBeenCalledTimes(1)
  })

  it('同一路由名在两次 mapMenuToRoutes 之间共享同一个壳，重建路由表不会串出新组件标识', async () => {
    const loader = makeLoader(defineComponent({ render: () => null }))
    registerAppContext({ viewModules: { '/src/views/wrap-shared.vue': loader } })
    const build = () => mapMenuToRoutes([menu({ path: '/wrap-shared', name: 'WrapShared', component: 'WrapShared' })])
    const first = await loaderOf(build()[0])?.()
    const second = await loaderOf(build()[0])?.()
    expect(second).toBe(first)
    expect(loader).toHaveBeenCalledTimes(1)
  })

  it('两个菜单指向同一页面组件但路由名不同时，各自拿到互不相同的壳', async () => {
    const inner = defineComponent({ render: () => null })
    const loader = makeLoader(inner)
    registerAppContext({ viewModules: { '/src/views/wrap-twin.vue': loader } })
    const routes = mapMenuToRoutes([
      menu({ path: '/twin-a', name: 'TwinA', component: 'WrapTwin' }),
      menu({ path: '/twin-b', name: 'TwinB', component: 'WrapTwin' }),
    ])
    const shellA = await loaderOf(routes[0])?.() as Component & { name?: string }
    const shellB = await loaderOf(routes[1])?.() as Component & { name?: string }
    expect(shellA).not.toBe(shellB)
    expect(shellA.name).toBe('TwinA')
    expect(shellB.name).toBe('TwinB')
  })

  it('模块没有 default 导出时直接把模块本身当组件包进壳', async () => {
    const bare = defineComponent({ render: () => h('div', '裸模块') })
    registerAppContext({ viewModules: { '/src/views/wrap-bare.vue': () => Promise.resolve(bare) } })
    const [route] = mapMenuToRoutes([menu({ path: '/wrap-bare', name: 'WrapBare', component: 'WrapBare' })])
    const shell = await loaderOf(route)?.() as Component
    const wrapper = mount(shell)
    expect(wrapper.text()).toBe('裸模块')
    wrapper.unmount()
  })

  it('路由名为空串时不包壳，component 就是解析到的加载函数本身', () => {
    const loader = makeLoader(emptyComponent)
    registerAppContext({ viewModules: { '/src/views/no-name.vue': loader } })
    const [route] = mapMenuToRoutes([menu({ path: '/no-name', name: '', component: 'NoName' })])
    expect(loaderOf(route)).toBe(loader)
  })
})

describe('非法与缺失数据容错', () => {
  it('component 为空串按未配置处理，叶子节点走兜底视图', () => {
    const [route] = mapMenuToRoutes([menu({ path: '/empty-component', name: '', component: '' })])
    expect(typeof loaderOf(route)).toBe('function')
  })

  it('meta 缺失时不抛错，路由的 meta 为 undefined', () => {
    const raw = { path: '/no-meta', name: 'NoMeta' } as unknown as MenuRoute
    const [route] = mapMenuToRoutes([raw])
    expect(route?.path).toBe('/no-meta')
    expect(route?.meta).toBeUndefined()
  })

  it('children 为 null 时按叶子处理，不抛错', () => {
    const raw = { path: '/null-children', name: 'NullChildren', meta: { title: 't' }, children: null } as unknown as MenuRoute
    const [route] = mapMenuToRoutes([raw])
    expect(route?.children).toBeUndefined()
    expect(typeof loaderOf(route)).toBe('function')
  })

  it('path 为 undefined 的菜单同样被剔除', () => {
    const raw = { name: 'NoPath', meta: { title: 't' } } as unknown as MenuRoute
    expect(mapMenuToRoutes([raw])).toEqual([])
  })

  it('带中文与 emoji 的路径与名字原样保留，不做任何编码转换', () => {
    const [route] = mapMenuToRoutes([menu({ path: '/报表/汇总🚀', name: '报表汇总' })])
    expect(route?.path).toBe('/报表/汇总🚀')
    expect(route?.name).toBe('报表汇总')
  })

  it('超长路径不被截断', () => {
    const long = `/${'a'.repeat(2000)}`
    const [route] = mapMenuToRoutes([menu({ path: long, name: 'Long' })])
    expect(route?.path).toBe(long)
  })
})
