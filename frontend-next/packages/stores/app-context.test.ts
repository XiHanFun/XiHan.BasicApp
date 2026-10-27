/**
 * 应用上下文（app-context）单元测试。
 * 职责边界：packages 只从这里取 src 注入的 API/路由/视图模块。
 * 覆盖未注册时的默认值与失败提示、逐字段注册（合并 vs 整体替换）的差异，
 * 以及注释里点名的坑：shellRoutes 必须逐字段拷贝，漏掉分支会静默丢配置。
 * 上下文是模块级单例，每个用例通过 vi.resetModules + 动态 import 拿到全新副本，保证可任意顺序执行。
 */
import type { AppContext } from './app-context'
import type { AppContextApis } from '~/types'
import { beforeEach, describe, expect, it, vi } from 'vitest'

type ContextModule = typeof import('./app-context')

async function freshContext(): Promise<ContextModule> {
  return import('./app-context')
}

beforeEach(() => {
  vi.resetModules()
})

describe('未注册时的默认上下文', () => {
  it('getRouter 默认返回 rejected promise，并带上「Router not registered」提示', async () => {
    const { useAppContext } = await freshContext()

    await expect(useAppContext().getRouter()).rejects.toThrow(/Router not registered/)
  })

  it('未注册时静态路由为空数组、视图模块与组件映射为空对象、shellRoutes 无任何入口', async () => {
    const { useAppContext } = await freshContext()
    const ctx = useAppContext()

    expect(ctx.getStaticRoutes()).toEqual([])
    expect(ctx.viewModules).toEqual({})
    expect(ctx.explicitComponentMap).toEqual({})
    expect(ctx.shellRoutes).toEqual({})
  })

  it('useAppContext 多次调用返回同一个单例对象', async () => {
    const { useAppContext } = await freshContext()

    expect(useAppContext()).toBe(useAppContext())
  })
})

describe('apis 逐次合并而非整体替换', () => {
  it('两次注册不同的 API 函数时都保留下来', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    const first = vi.fn()
    const second = vi.fn()

    registerAppContext({ apis: { logoutApi: first } as unknown as AppContextApis })
    registerAppContext({ apis: { getUserInfoApi: second } as unknown as AppContextApis })

    const apis = useAppContext().apis as unknown as Record<string, unknown>
    expect(apis.logoutApi).toBe(first)
    expect(apis.getUserInfoApi).toBe(second)
  })

  it('同名 API 二次注册以后者为准', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    const older = vi.fn()
    const newer = vi.fn()

    registerAppContext({ apis: { logoutApi: older } as unknown as AppContextApis })
    registerAppContext({ apis: { logoutApi: newer } as unknown as AppContextApis })

    expect((useAppContext().apis as unknown as Record<string, unknown>).logoutApi).toBe(newer)
  })

  it('传空 apis 对象不会清空已注册的 API', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    const api = vi.fn()
    registerAppContext({ apis: { logoutApi: api } as unknown as AppContextApis })

    registerAppContext({ apis: {} as AppContextApis })

    expect((useAppContext().apis as unknown as Record<string, unknown>).logoutApi).toBe(api)
  })
})

describe('shellRoutes 逐字段合并（注释点名的坑）', () => {
  it('分两次注册不同入口时都保留 —— 后注册的不会顶掉先注册的', async () => {
    const { registerAppContext, useAppContext } = await freshContext()

    registerAppContext({ shellRoutes: { profile: '/profile' } })
    registerAppContext({ shellRoutes: { controlCenter: '/control-center' } })

    expect(useAppContext().shellRoutes).toEqual({
      profile: '/profile',
      controlCenter: '/control-center',
    })
  })

  it('三个入口全量注册后一个都不能丢（profile / controlCenter / inbox）', async () => {
    const { registerAppContext, useAppContext } = await freshContext()

    registerAppContext({
      shellRoutes: { profile: '/p', controlCenter: '/c', inbox: '/i' },
    })

    expect(useAppContext().shellRoutes).toEqual({ profile: '/p', controlCenter: '/c', inbox: '/i' })
  })

  it('同名入口二次注册以后者为准', async () => {
    const { registerAppContext, useAppContext } = await freshContext()

    registerAppContext({ shellRoutes: { profile: '/old' } })
    registerAppContext({ shellRoutes: { profile: '/new' } })

    expect(useAppContext().shellRoutes.profile).toBe('/new')
  })
})

describe('其余字段为整体替换语义', () => {
  it('getRouter 注册后由注入实现接管', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    const router = { id: 'router' }

    registerAppContext({ getRouter: () => Promise.resolve(router as never) })

    await expect(useAppContext().getRouter()).resolves.toBe(router)
  })

  it('getStaticRoutes 二次注册整体替换，不做数组拼接', async () => {
    const { registerAppContext, useAppContext } = await freshContext()

    registerAppContext({ getStaticRoutes: () => [{ path: '/a', component: {} }] })
    registerAppContext({ getStaticRoutes: () => [{ path: '/b', component: {} }] })

    expect(useAppContext().getStaticRoutes().map(r => r.path)).toEqual(['/b'])
  })

  it('viewModules 与 explicitComponentMap 二次注册整体替换，旧键消失', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    const loader = () => Promise.resolve({})

    registerAppContext({ viewModules: { '/a.vue': loader }, explicitComponentMap: { A: loader } })
    registerAppContext({ viewModules: { '/b.vue': loader }, explicitComponentMap: { B: loader } })

    expect(Object.keys(useAppContext().viewModules)).toEqual(['/b.vue'])
    expect(Object.keys(useAppContext().explicitComponentMap)).toEqual(['B'])
  })
})

describe('空 partial 与未给出的字段', () => {
  it('传空对象时上下文保持原样', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    registerAppContext({ shellRoutes: { profile: '/p' } })

    registerAppContext({})

    expect(useAppContext().shellRoutes.profile).toBe('/p')
    expect(useAppContext().getStaticRoutes()).toEqual([])
  })

  it('只注册 apis 时不会顺手把 getRouter 覆盖成 undefined', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    registerAppContext({ getRouter: () => Promise.resolve({ id: 'r' } as never) })

    registerAppContext({ apis: { logoutApi: vi.fn() } as unknown as AppContextApis })

    await expect(useAppContext().getRouter()).resolves.toEqual({ id: 'r' })
  })

  it('上下文的字段集合与注册函数处理的分支一一对应（漏掉分支即静默丢配置）', async () => {
    const { registerAppContext, useAppContext } = await freshContext()
    const partial: Required<Pick<AppContext, 'getStaticRoutes' | 'viewModules' | 'explicitComponentMap' | 'shellRoutes'>> = {
      getStaticRoutes: () => [{ path: '/x', component: {} }],
      viewModules: { '/x.vue': () => Promise.resolve({}) },
      explicitComponentMap: { X: () => Promise.resolve({}) },
      shellRoutes: { inbox: '/inbox' },
    }

    registerAppContext(partial)

    const ctx = useAppContext()
    expect(ctx.getStaticRoutes().map(r => r.path)).toEqual(['/x'])
    expect(Object.keys(ctx.viewModules)).toEqual(['/x.vue'])
    expect(Object.keys(ctx.explicitComponentMap)).toEqual(['X'])
    expect(ctx.shellRoutes.inbox).toBe('/inbox')
  })
})
