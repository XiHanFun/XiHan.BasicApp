/**
 * 布局偏好组合式（layout-preferences）单元测试。
 * 职责边界：它是 layout-state store 的一层可写 computed 视图，供偏好抽屉 v-model 直接绑定。
 * 覆盖字段集合（只暴露偏好抽屉需要的九项、不含导航三项与 toggleSidebar）、
 * 双向绑定的读与写，以及写入最终仍落到 appStore setter 的约束链。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAppStore } from '~/stores/app'
import { useLayoutPreferences } from './layout-preferences'
import { useLayoutStateStore } from './layout-state'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('暴露的字段集合', () => {
  it('只暴露偏好抽屉需要的九项，导航三项与 toggleSidebar 不在其中', () => {
    const preferences = useLayoutPreferences()

    expect(Object.keys(preferences)).toEqual([
      'layoutMode',
      'sidebarCollapsed',
      'sidebarWidth',
      'sidebarShow',
      'sidebarExpandOnHover',
      'headerMode',
      'headerMenuAlign',
      'contentCompact',
      'contentMaxWidth',
    ])
  })
})

describe('读取跟随底层 store', () => {
  it('appStore 改动后组合式读到的值同步变化', () => {
    const appStore = useAppStore()
    const preferences = useLayoutPreferences()

    appStore.setLayoutMode('mixed')
    appStore.setContentCompact(true)

    expect(preferences.layoutMode.value).toBe('mixed')
    expect(preferences.contentCompact.value).toBe(true)
  })

  it('layout-state store 改动同样立即反映到组合式', () => {
    const layout = useLayoutStateStore()
    const preferences = useLayoutPreferences()

    layout.sidebarShow = false

    expect(preferences.sidebarShow.value).toBe(false)
  })
})

describe('写入穿透两层代理落到 appStore', () => {
  it('改布局模式最终写进 appStore', () => {
    const appStore = useAppStore()
    const preferences = useLayoutPreferences()

    preferences.layoutMode.value = 'top'

    expect(appStore.layoutMode).toBe('top')
  })

  it('改侧栏宽度仍受 [180, 320] 夹取 —— 代理层不绕过 setter', () => {
    const appStore = useAppStore()
    const preferences = useLayoutPreferences()

    preferences.sidebarWidth.value = 500

    expect(preferences.sidebarWidth.value).toBe(320)
    expect(appStore.sidebarWidth).toBe(320)
  })

  it('折叠、显示、悬停展开三个开关都能双向写入', () => {
    const appStore = useAppStore()
    const preferences = useLayoutPreferences()

    preferences.sidebarCollapsed.value = true
    preferences.sidebarShow.value = false
    preferences.sidebarExpandOnHover.value = false

    expect(appStore.sidebarCollapsed).toBe(true)
    expect(appStore.sidebarShow).toBe(false)
    expect(appStore.sidebarExpandOnHover).toBe(false)
  })

  it('顶栏模式与菜单对齐可写入并回读', () => {
    const appStore = useAppStore()
    const preferences = useLayoutPreferences()

    preferences.headerMode.value = 'static'
    preferences.headerMenuAlign.value = 'end'

    expect(appStore.headerMode).toBe('static')
    expect(appStore.headerMenuAlign).toBe('end')
  })

  it('内容区紧凑与最大宽度可写入并回读', () => {
    const appStore = useAppStore()
    const preferences = useLayoutPreferences()

    preferences.contentCompact.value = true
    preferences.contentMaxWidth.value = 1600

    expect(appStore.contentCompact).toBe(true)
    expect(appStore.contentMaxWidth).toBe(1600)
  })

  it('两次分别取到的组合式实例共享同一份底层状态', () => {
    const first = useLayoutPreferences()
    const second = useLayoutPreferences()

    first.contentMaxWidth.value = 1000

    expect(second.contentMaxWidth.value).toBe(1000)
  })
})
