/**
 * 工作台小组件登记点（workbench-widgets）单元测试
 * 职责边界：登记顺序、读取结果与内部数组是同一响应式引用（看板在模块 setup 之后读取）
 * 登记表是模块级单例，每个用例经 vi.resetModules + 动态 import 取全新副本
 */
import type { WorkbenchListWidget } from './workbench-widgets'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { isReactive } from 'vue'

async function freshModule(): Promise<typeof import('./workbench-widgets')> {
  return import('./workbench-widgets')
}

function widget(key: string): WorkbenchListWidget {
  return { key, dataKey: `pkg.${key}`, titleKey: `${key}.title`, descKey: `${key}.desc`, emptyKey: `${key}.empty`, icon: 'lucide:box', defaultSpan: 4, page: `pkg.${key}`, moreLink: `/pkg/${key}` }
}

beforeEach(() => {
  vi.resetModules()
})

describe('工作台小组件登记', () => {
  it('没有模块登记时为空', async () => {
    const { useWorkbenchWidgets } = await freshModule()

    expect(useWorkbenchWidgets()).toEqual([])
  })

  it('按登记顺序列出，读取的是同一个响应式数组', async () => {
    const { registerWorkbenchWidget, useWorkbenchWidgets } = await freshModule()
    const list = useWorkbenchWidgets()

    registerWorkbenchWidget(widget('a'))
    registerWorkbenchWidget(widget('b'))

    expect(isReactive(list)).toBe(true)
    expect(list.map(item => item.key)).toEqual(['a', 'b'])
  })
})
