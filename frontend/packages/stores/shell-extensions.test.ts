/**
 * 壳层扩展注册点（shell-extensions）单元测试。
 * 职责边界：注册顺序、三类挂载物（顶栏按钮/浮层/集成钩子）可各自省略、
 * 读取结果与内部数组是同一响应式引用（布局层依赖它做增量渲染）。
 * 注册表是模块级单例，每个用例经 vi.resetModules + 动态 import 取全新副本。
 */
import type { Component } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { isReactive } from 'vue'

async function freshModule(): Promise<typeof import('./shell-extensions')> {
  return import('./shell-extensions')
}

function fakeComponent(name: string): Component {
  return { name, render: () => null }
}

beforeEach(() => {
  vi.resetModules()
})

describe('初始状态', () => {
  it('没有任何模块注册时扩展列表为空', async () => {
    const { useShellExtensions } = await freshModule()

    expect(useShellExtensions()).toEqual([])
  })

  it('useShellExtensions 返回的是同一个响应式数组，注册后当场可见', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const list = useShellExtensions()

    registerShellExtension({ overlays: [fakeComponent('Drawer')] })

    expect(isReactive(list)).toBe(true)
    expect(list).toHaveLength(1)
  })
})

describe('注册与顺序', () => {
  it('多个模块按注册顺序排列（布局层据此决定按钮先后）', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const first = fakeComponent('First')
    const second = fakeComponent('Second')

    registerShellExtension({ headerToolbarItems: [first] })
    registerShellExtension({ headerToolbarItems: [second] })

    expect(useShellExtensions().flatMap(e => e.headerToolbarItems ?? [])).toEqual([first, second])
  })

  it('同一次注册里的多个按钮保持数组内顺序', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const a = fakeComponent('A')
    const b = fakeComponent('B')

    registerShellExtension({ headerToolbarItems: [a, b] })

    expect(useShellExtensions()[0]?.headerToolbarItems).toEqual([a, b])
  })

  it('同一个扩展对象重复注册会出现两次 —— 注册点不做去重', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const extension = { overlays: [fakeComponent('Dialog')] }

    registerShellExtension(extension)
    registerShellExtension(extension)

    expect(useShellExtensions()).toHaveLength(2)
  })

  it('三类挂载物均可省略：空扩展照样入列但不带任何挂载物', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()

    registerShellExtension({})

    const only = useShellExtensions()[0]
    expect(only).toEqual({})
    expect(only?.headerToolbarItems).toBeUndefined()
    expect(only?.overlays).toBeUndefined()
    expect(only?.integrations).toBeUndefined()
  })

  it('集成钩子只被登记不被调用 —— 调用时机在布局 setup', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const hook = vi.fn()

    registerShellExtension({ integrations: [hook] })

    expect(hook).not.toHaveBeenCalled()
    expect(useShellExtensions()[0]?.integrations).toEqual([hook])
  })

  it('布局层遍历调用集成钩子时，每个钩子各执行一次', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const first = vi.fn()
    const second = vi.fn()
    registerShellExtension({ integrations: [first] })
    registerShellExtension({ integrations: [second] })

    useShellExtensions().forEach(ext => ext.integrations?.forEach(fn => fn()))

    expect(first).toHaveBeenCalledTimes(1)
    expect(second).toHaveBeenCalledTimes(1)
  })

  it('三类挂载物混合注册时各自归位，互不串扰', async () => {
    const { registerShellExtension, useShellExtensions } = await freshModule()
    const button = fakeComponent('Button')
    const overlay = fakeComponent('Overlay')
    const hook = vi.fn()

    registerShellExtension({ headerToolbarItems: [button], overlays: [overlay], integrations: [hook] })

    const only = useShellExtensions()[0]
    expect(only?.headerToolbarItems).toEqual([button])
    expect(only?.overlays).toEqual([overlay])
    expect(only?.integrations).toEqual([hook])
  })
})
