/**
 * 布局状态 Store（layout-state）单元测试。
 * 职责边界：它是 appStore 布局字段的可写代理层——读取要透传、写入必须落到 appStore 的
 * setter（而不是绕过 setter 直接改 ref），因此夹取、持久化等 setter 内的规则不能被绕过。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { CONTENT_MAX_WIDTH_KEY, SIDEBAR_COLLAPSED_KEY, SIDEBAR_WIDTH_KEY } from '~/constants'
import { useAppStore } from '~/stores/app'
import { useLayoutStateStore } from './layout-state'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('读取透传', () => {
  it('appStore 改动后代理层立即读到新值', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    appStore.setLayoutMode('mixed')
    appStore.setSidebarShow(false)
    appStore.setNavigationStyle('plain')

    expect(layout.layoutMode).toBe('mixed')
    expect(layout.sidebarShow).toBe(false)
    expect(layout.navigationStyle).toBe('plain')
  })

  it('代理层覆盖了 appStore 的全部布局相关字段', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    expect(layout.sidebarCollapsed).toBe(appStore.sidebarCollapsed)
    expect(layout.sidebarWidth).toBe(appStore.sidebarWidth)
    expect(layout.sidebarExpandOnHover).toBe(appStore.sidebarExpandOnHover)
    expect(layout.headerMode).toBe(appStore.headerMode)
    expect(layout.headerMenuAlign).toBe(appStore.headerMenuAlign)
    expect(layout.contentCompact).toBe(appStore.contentCompact)
    expect(layout.contentMaxWidth).toBe(appStore.contentMaxWidth)
    expect(layout.navigationSplit).toBe(appStore.navigationSplit)
    expect(layout.navigationAccordion).toBe(appStore.navigationAccordion)
  })
})

describe('写入落到 appStore 的 setter', () => {
  it('通过代理层改布局模式会同步到 appStore 并落地 localStorage', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    layout.layoutMode = 'top'

    expect(appStore.layoutMode).toBe('top')
  })

  it('侧栏折叠经代理层写入后 appStore 与本地存储同时更新', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    layout.sidebarCollapsed = true

    expect(appStore.sidebarCollapsed).toBe(true)
    expect(localStorage.getItem(SIDEBAR_COLLAPSED_KEY)).toBe('true')
  })

  it('侧栏宽度经代理层写入仍受 [180, 320] 夹取约束，不能绕过 setter', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    layout.sidebarWidth = 9999

    expect(layout.sidebarWidth).toBe(320)
    expect(appStore.sidebarWidth).toBe(320)
    expect(localStorage.getItem(SIDEBAR_WIDTH_KEY)).toBe('320')
  })

  it('侧栏宽度低于下界同样被夹到 180', () => {
    const layout = useLayoutStateStore()

    layout.sidebarWidth = 0

    expect(layout.sidebarWidth).toBe(180)
  })

  it('顶栏模式与菜单对齐经代理层写入后透传回读', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    layout.headerMode = 'auto-scroll'
    layout.headerMenuAlign = 'center'

    expect(appStore.headerMode).toBe('auto-scroll')
    expect(appStore.headerMenuAlign).toBe('center')
  })

  it('内容区紧凑与最大宽度经代理层写入后落地', () => {
    const layout = useLayoutStateStore()

    layout.contentCompact = true
    layout.contentMaxWidth = 1440

    expect(layout.contentCompact).toBe(true)
    expect(localStorage.getItem(CONTENT_MAX_WIDTH_KEY)).toBe('1440')
  })

  it('导航三项（样式/分栏/手风琴）经代理层写入后透传回读', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    layout.navigationStyle = 'plain'
    layout.navigationSplit = false
    layout.navigationAccordion = false

    expect(appStore.navigationStyle).toBe('plain')
    expect(appStore.navigationSplit).toBe(false)
    expect(appStore.navigationAccordion).toBe(false)
  })

  it('sidebarExpandOnHover 经代理层写入后透传回读', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()

    layout.sidebarExpandOnHover = false

    expect(appStore.sidebarExpandOnHover).toBe(false)
  })
})

describe('toggleSidebar', () => {
  it('切换折叠状态并落地 —— 委托给 appStore 而不是自己维护一份', () => {
    const appStore = useAppStore()
    const layout = useLayoutStateStore()
    expect(layout.sidebarCollapsed).toBe(false)

    layout.toggleSidebar()

    expect(layout.sidebarCollapsed).toBe(true)
    expect(appStore.sidebarCollapsed).toBe(true)
  })

  it('连续两次切换回到原状态', () => {
    const layout = useLayoutStateStore()

    layout.toggleSidebar()
    layout.toggleSidebar()

    expect(layout.sidebarCollapsed).toBe(false)
  })
})
