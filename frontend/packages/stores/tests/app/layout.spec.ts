/**
 * 布局切片（app/layout）单元测试
 * 职责边界：布局模式、品牌、侧栏、顶栏、导航、内容区、标签栏、面包屑等布局偏好的
 * 默认值与本地还原、侧栏宽度夹取（读与写两侧）、顶栏对齐的旧值迁移、
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { DEFAULT_LAYOUT_MODE, HEADER_MENU_ALIGN_KEY, SIDEBAR_WIDTH_KEY } from '~/constants'
import { useAppStore } from '~/stores/app'

function freshStore(): ReturnType<typeof useAppStore> {
  setActivePinia(createPinia())
  return useAppStore()
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('默认值', () => {
  it('布局模式、侧栏宽度、内容最大宽度的默认取值', () => {
    const store = freshStore()

    expect(store.layoutMode).toBe(DEFAULT_LAYOUT_MODE)
    expect(store.sidebarWidth).toBe(224)
    expect(store.contentMaxWidth).toBe(1280)
  })

  it('侧栏相关开关的默认：显示/折叠按钮/固定按钮/悬停展开/自动激活子项默认开，折叠态标题与深色默认关', () => {
    const store = freshStore()

    expect(store.sidebarShow).toBe(true)
    expect(store.sidebarCollapsed).toBe(false)
    expect(store.sidebarCollapseButton).toBe(true)
    expect(store.sidebarFixedButton).toBe(true)
    expect(store.sidebarExpandOnHover).toBe(true)
    expect(store.sidebarAutoActivateChild).toBe(true)
    expect(store.sidebarCollapsedShowTitle).toBe(false)
    expect(store.sidebarDark).toBe(false)
    expect(store.sidebarSubDark).toBe(false)
  })

  it('顶栏与导航默认：显示、fixed、start 对齐、rounded 样式、分栏与手风琴开启', () => {
    const store = freshStore()

    expect(store.headerShow).toBe(true)
    expect(store.headerMode).toBe('fixed')
    expect(store.headerMenuAlign).toBe('start')
    expect(store.headerDark).toBe(false)
    expect(store.navigationStyle).toBe('rounded')
    expect(store.navigationSplit).toBe(true)
    expect(store.navigationAccordion).toBe(true)
  })

  it('标签栏默认：启用、持久化、拖拽、更多、最大化、总览开启，最大数量 0 表示不限，样式 chrome', () => {
    const store = freshStore()

    expect(store.tabbarEnabled).toBe(true)
    expect(store.tabbarPersist).toBe(true)
    expect(store.tabbarVisitHistory).toBe(true)
    expect(store.tabbarDraggable).toBe(true)
    expect(store.tabbarShowMore).toBe(true)
    expect(store.tabbarShowMaximize).toBe(true)
    expect(store.tabbarShowOverview).toBe(true)
    expect(store.tabbarScrollResponse).toBe(true)
    expect(store.tabbarMiddleClickClose).toBe(true)
    expect(store.tabbarShowIcon).toBe(true)
    expect(store.tabbarMaxCount).toBe(0)
    expect(store.tabbarStyle).toBe('chrome')
  })

  it('面包屑默认：启用、显示图标、导航按钮开启；显示首页与「仅一项时隐藏」默认关闭，样式 background', () => {
    const store = freshStore()

    expect(store.breadcrumbEnabled).toBe(true)
    expect(store.breadcrumbShowIcon).toBe(true)
    expect(store.breadcrumbNavButtons).toBe(true)
    expect(store.breadcrumbShowHome).toBe(false)
    expect(store.breadcrumbHideOnlyOne).toBe(false)
    expect(store.breadcrumbStyle).toBe('background')
  })

  it('pageLoading 是纯内存状态，默认 false', () => {
    const store = freshStore()

    store.setPageLoading(true)
    expect(store.pageLoading).toBe(true)

    store.setPageLoading(false)
    expect(store.pageLoading).toBe(false)
  })
})

describe('侧栏宽度的两侧夹取', () => {
  it('本地存的超大宽度在初始化时被夹到 320', () => {
    localStorage.setItem(SIDEBAR_WIDTH_KEY, '9999')

    expect(freshStore().sidebarWidth).toBe(320)
  })

  it('本地存的过小宽度在初始化时被夹到 180', () => {
    localStorage.setItem(SIDEBAR_WIDTH_KEY, '10')

    expect(freshStore().sidebarWidth).toBe(180)
  })

  it('本地存的不是有限数值时回落 224', () => {
    localStorage.setItem(SIDEBAR_WIDTH_KEY, '"224px"')

    expect(freshStore().sidebarWidth).toBe(224)
  })

  it('setSidebarWidth 同样夹在 [180, 320]', () => {
    const store = freshStore()

    store.setSidebarWidth(1000)
    expect(store.sidebarWidth).toBe(320)

    store.setSidebarWidth(-1)
    expect(store.sidebarWidth).toBe(180)

    store.setSidebarWidth(260)
    expect(store.sidebarWidth).toBe(260)
  })
})

describe('顶栏菜单对齐的旧值迁移', () => {
  it('历史值 left 迁移为 start', () => {
    localStorage.setItem(HEADER_MENU_ALIGN_KEY, JSON.stringify('left'))

    expect(freshStore().headerMenuAlign).toBe('start')
  })

  it('历史值 right 迁移为 end', () => {
    localStorage.setItem(HEADER_MENU_ALIGN_KEY, JSON.stringify('right'))

    expect(freshStore().headerMenuAlign).toBe('end')
  })

  it('新值 center / start / end 原样保留', () => {
    for (const value of ['center', 'start', 'end'] as const) {
      localStorage.setItem(HEADER_MENU_ALIGN_KEY, JSON.stringify(value))
      expect(freshStore().headerMenuAlign).toBe(value)
    }
  })

  it('无法识别的值回落 start', () => {
    localStorage.setItem(HEADER_MENU_ALIGN_KEY, JSON.stringify('乱写的'))

    expect(freshStore().headerMenuAlign).toBe('start')
  })
})

describe('侧栏折叠与其余 setter', () => {
  it('toggleSidebar 在折叠与展开之间切换', () => {
    const store = freshStore()

    store.toggleSidebar()
    expect(store.sidebarCollapsed).toBe(true)

    store.toggleSidebar()
    expect(store.sidebarCollapsed).toBe(false)
  })

  it('布局模式、顶栏模式、导航样式、面包屑样式可自由设置并回读', () => {
    const store = freshStore()

    store.setLayoutMode('mixed')
    store.setHeaderMode('auto')
    store.setNavigationStyle('plain')
    store.setBreadcrumbStyle('normal')

    expect(store.layoutMode).toBe('mixed')
    expect(store.headerMode).toBe('auto')
    expect(store.navigationStyle).toBe('plain')
    expect(store.breadcrumbStyle).toBe('normal')
  })

  it('标签栏各开关与最大数量写入后回读一致', () => {
    const store = freshStore()

    store.setTabbarEnabled(false)
    store.setTabbarShowIcon(false)
    store.setTabbarScrollResponse(false)
    store.setTabbarMiddleClickClose(false)
    store.setTabbarStyle('card')
    store.setTabbarMaxCount(10)

    expect(store.tabbarEnabled).toBe(false)
    expect(store.tabbarShowIcon).toBe(false)
    expect(store.tabbarScrollResponse).toBe(false)
    expect(store.tabbarMiddleClickClose).toBe(false)
    expect(store.tabbarStyle).toBe('card')
    expect(store.tabbarMaxCount).toBe(10)
  })

  it('面包屑各开关写入后回读一致', () => {
    const store = freshStore()

    store.setBreadcrumbEnabled(false)
    store.setBreadcrumbShowHome(true)
    store.setBreadcrumbShowIcon(false)
    store.setBreadcrumbHideOnlyOne(true)
    store.setBreadcrumbNavButtons(false)

    expect(store.breadcrumbEnabled).toBe(false)
    expect(store.breadcrumbShowHome).toBe(true)
    expect(store.breadcrumbShowIcon).toBe(false)
    expect(store.breadcrumbHideOnlyOne).toBe(true)
    expect(store.breadcrumbNavButtons).toBe(false)
  })

  it('侧栏各开关与深色项写入后回读一致', () => {
    const store = freshStore()

    store.setSidebarShow(false)
    store.setSidebarCollapseButton(false)
    store.setSidebarFixedButton(false)
    store.setSidebarExpandOnHover(false)
    store.setSidebarAutoActivateChild(false)
    store.setSidebarCollapsedShowTitle(true)
    store.setSidebarDark(true)
    store.setSidebarSubDark(true)
    store.setHeaderShow(false)
    store.setHeaderDark(true)

    expect(store.sidebarShow).toBe(false)
    expect(store.sidebarCollapseButton).toBe(false)
    expect(store.sidebarFixedButton).toBe(false)
    expect(store.sidebarExpandOnHover).toBe(false)
    expect(store.sidebarAutoActivateChild).toBe(false)
    expect(store.sidebarCollapsedShowTitle).toBe(true)
    expect(store.sidebarDark).toBe(true)
    expect(store.sidebarSubDark).toBe(true)
    expect(store.headerShow).toBe(false)
    expect(store.headerDark).toBe(true)
  })

  it('内容区紧凑与最大宽度不做夹取，原样写入', () => {
    const store = freshStore()

    store.setContentCompact(true)
    store.setContentMaxWidth(0)

    expect(store.contentCompact).toBe(true)
    expect(store.contentMaxWidth).toBe(0)
  })
})
