import { ref } from 'vue'
import {
  BRAND_LOGO_KEY,
  BRAND_TITLE_KEY,
  BREADCRUMB_ENABLED_KEY,
  BREADCRUMB_HIDE_ONLY_ONE_KEY,
  BREADCRUMB_NAV_BUTTONS_KEY,
  BREADCRUMB_SHOW_HOME_KEY,
  BREADCRUMB_SHOW_ICON_KEY,
  BREADCRUMB_STYLE_KEY,
  CONTENT_COMPACT_KEY,
  CONTENT_MAX_WIDTH_KEY,
  DEFAULT_LAYOUT_MODE,
  HEADER_DARK_KEY,
  HEADER_MENU_ALIGN_KEY,
  HEADER_MODE_KEY,
  HEADER_SHOW_KEY,
  LAYOUT_MODE_KEY,
  NAV_ACCORDION_KEY,
  NAV_SPLIT_KEY,
  NAV_STYLE_KEY,
  SIDEBAR_AUTO_ACTIVATE_CHILD_KEY,
  SIDEBAR_COLLAPSE_BUTTON_KEY,
  SIDEBAR_COLLAPSED_KEY,
  SIDEBAR_COLLAPSED_SHOW_TITLE_KEY,
  SIDEBAR_DARK_KEY,
  SIDEBAR_EXPAND_HOVER_KEY,
  SIDEBAR_FIXED_BUTTON_KEY,
  SIDEBAR_SHOW_KEY,
  SIDEBAR_SUB_DARK_KEY,
  SIDEBAR_WIDTH_KEY,
  TABBAR_DRAGGABLE_KEY,
  TABBAR_MAX_COUNT_KEY,
  TABBAR_MIDDLE_CLICK_CLOSE_KEY,
  TABBAR_PERSIST_KEY,
  TABBAR_SCROLL_RESPONSE_KEY,
  TABBAR_SHOW_ICON_KEY,
  TABBAR_SHOW_MAXIMIZE_KEY,
  TABBAR_SHOW_MORE_KEY,
  TABBAR_STYLE_KEY,
  TABBAR_VISIT_HISTORY_KEY,
  TAGS_BAR_KEY,
} from '~/constants'
import { LocalStorage } from '~/utils'
import { bindPersist, save } from '../helpers'

/** 布局、侧边栏、顶栏、导航、标签栏、面包屑相关状态 */
export function createLayoutSlice() {
  const layoutMode = ref<string>(LocalStorage.get<string>(LAYOUT_MODE_KEY) ?? DEFAULT_LAYOUT_MODE)
  const brandTitle = ref<string>(
    LocalStorage.get<string>(BRAND_TITLE_KEY) ?? import.meta.env.VITE_APP_TITLE,
  )
  const brandLogo = ref<string>(
    LocalStorage.get<string>(BRAND_LOGO_KEY) ?? (import.meta.env.VITE_APP_LOGO || '/favicon.png'),
  )
  const pageLoading = ref(false)

  // ---- 侧边栏 ----
  const sidebarCollapsed = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_COLLAPSED_KEY) ?? false)
  const sidebarWidth = ref<number>(
    (() => {
      const saved = LocalStorage.get<number>(SIDEBAR_WIDTH_KEY)
      if (typeof saved === 'number' && Number.isFinite(saved)) {
        return Math.min(320, Math.max(180, saved))
      }
      return 224
    })(),
  )
  const sidebarShow = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_SHOW_KEY) ?? true)
  const sidebarCollapseButton = ref<boolean>(
    LocalStorage.get<boolean>(SIDEBAR_COLLAPSE_BUTTON_KEY) ?? true,
  )
  const sidebarFixedButton = ref<boolean>(
    LocalStorage.get<boolean>(SIDEBAR_FIXED_BUTTON_KEY) ?? true,
  )
  const sidebarExpandOnHover = ref<boolean>(
    LocalStorage.get<boolean>(SIDEBAR_EXPAND_HOVER_KEY) ?? true,
  )
  const sidebarAutoActivateChild = ref<boolean>(
    LocalStorage.get<boolean>(SIDEBAR_AUTO_ACTIVATE_CHILD_KEY) ?? true,
  )
  const sidebarCollapsedShowTitle = ref<boolean>(
    LocalStorage.get<boolean>(SIDEBAR_COLLAPSED_SHOW_TITLE_KEY) ?? false,
  )
  const sidebarDark = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_DARK_KEY) ?? false)
  const sidebarSubDark = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_SUB_DARK_KEY) ?? false)

  // ---- 顶栏 ----
  const headerShow = ref<boolean>(LocalStorage.get<boolean>(HEADER_SHOW_KEY) ?? true)
  const headerMenuAlign = ref<'start' | 'center' | 'end'>(
    (() => {
      const saved = LocalStorage.get<string>(HEADER_MENU_ALIGN_KEY)
      if (saved === 'left')
        return 'start' as const
      if (saved === 'right')
        return 'end' as const
      if (saved === 'center' || saved === 'start' || saved === 'end')
        return saved
      return 'start' as const
    })(),
  )
  const headerMode = ref<'fixed' | 'static' | 'auto' | 'auto-scroll'>(
    LocalStorage.get(HEADER_MODE_KEY) ?? 'fixed',
  )
  const headerDark = ref<boolean>(LocalStorage.get<boolean>(HEADER_DARK_KEY) ?? false)

  // ---- 导航 ----
  const navigationStyle = ref<'rounded' | 'plain'>(LocalStorage.get(NAV_STYLE_KEY) ?? 'rounded')
  const navigationSplit = ref<boolean>(LocalStorage.get<boolean>(NAV_SPLIT_KEY) ?? true)
  const navigationAccordion = ref<boolean>(LocalStorage.get<boolean>(NAV_ACCORDION_KEY) ?? true)

  // ---- 内容区域 ----
  const contentCompact = ref<boolean>(LocalStorage.get<boolean>(CONTENT_COMPACT_KEY) ?? false)
  const contentMaxWidth = ref<number>(LocalStorage.get<number>(CONTENT_MAX_WIDTH_KEY) ?? 1280)

  // ---- 标签栏 ----
  const tabbarEnabled = ref<boolean>(LocalStorage.get<boolean>(TAGS_BAR_KEY) ?? true)
  const tabbarPersist = ref<boolean>(LocalStorage.get<boolean>(TABBAR_PERSIST_KEY) ?? true)
  const tabbarVisitHistory = ref<boolean>(
    LocalStorage.get<boolean>(TABBAR_VISIT_HISTORY_KEY) ?? true,
  )
  const tabbarDraggable = ref<boolean>(LocalStorage.get<boolean>(TABBAR_DRAGGABLE_KEY) ?? true)
  const tabbarShowMore = ref<boolean>(LocalStorage.get<boolean>(TABBAR_SHOW_MORE_KEY) ?? true)
  const tabbarShowMaximize = ref<boolean>(
    LocalStorage.get<boolean>(TABBAR_SHOW_MAXIMIZE_KEY) ?? true,
  )
  const tabbarMaxCount = ref<number>(LocalStorage.get<number>(TABBAR_MAX_COUNT_KEY) ?? 0)
  const tabbarScrollResponse = ref<boolean>(
    LocalStorage.get<boolean>(TABBAR_SCROLL_RESPONSE_KEY) ?? true,
  )
  const tabbarMiddleClickClose = ref<boolean>(
    LocalStorage.get<boolean>(TABBAR_MIDDLE_CLICK_CLOSE_KEY) ?? true,
  )
  const tabbarShowIcon = ref<boolean>(LocalStorage.get<boolean>(TABBAR_SHOW_ICON_KEY) ?? true)
  const tabbarStyle = ref<string>(LocalStorage.get<string>(TABBAR_STYLE_KEY) ?? 'chrome')

  // ---- 面包屑 ----
  const breadcrumbEnabled = ref<boolean>(LocalStorage.get<boolean>(BREADCRUMB_ENABLED_KEY) ?? true)
  const breadcrumbShowHome = ref<boolean>(
    LocalStorage.get<boolean>(BREADCRUMB_SHOW_HOME_KEY) ?? false,
  )
  const breadcrumbShowIcon = ref<boolean>(
    LocalStorage.get<boolean>(BREADCRUMB_SHOW_ICON_KEY) ?? true,
  )
  const breadcrumbHideOnlyOne = ref<boolean>(
    LocalStorage.get<boolean>(BREADCRUMB_HIDE_ONLY_ONE_KEY) ?? false,
  )
  const breadcrumbStyle = ref<'normal' | 'background'>(
    LocalStorage.get(BREADCRUMB_STYLE_KEY) ?? 'background',
  )
  const breadcrumbNavButtons = ref<boolean>(
    LocalStorage.get<boolean>(BREADCRUMB_NAV_BUTTONS_KEY) ?? true,
  )

  // ---- 持久化绑定 ----
  bindPersist(LAYOUT_MODE_KEY, layoutMode)
  bindPersist(BRAND_TITLE_KEY, brandTitle)
  bindPersist(BRAND_LOGO_KEY, brandLogo)
  bindPersist(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed)
  bindPersist(SIDEBAR_WIDTH_KEY, sidebarWidth)
  bindPersist(SIDEBAR_SHOW_KEY, sidebarShow)
  bindPersist(SIDEBAR_COLLAPSE_BUTTON_KEY, sidebarCollapseButton)
  bindPersist(SIDEBAR_FIXED_BUTTON_KEY, sidebarFixedButton)
  bindPersist(SIDEBAR_EXPAND_HOVER_KEY, sidebarExpandOnHover)
  bindPersist(SIDEBAR_AUTO_ACTIVATE_CHILD_KEY, sidebarAutoActivateChild)
  bindPersist(SIDEBAR_COLLAPSED_SHOW_TITLE_KEY, sidebarCollapsedShowTitle)
  bindPersist(SIDEBAR_DARK_KEY, sidebarDark)
  bindPersist(SIDEBAR_SUB_DARK_KEY, sidebarSubDark)
  bindPersist(HEADER_SHOW_KEY, headerShow)
  bindPersist(HEADER_MENU_ALIGN_KEY, headerMenuAlign)
  bindPersist(HEADER_MODE_KEY, headerMode)
  bindPersist(HEADER_DARK_KEY, headerDark)
  bindPersist(NAV_STYLE_KEY, navigationStyle)
  bindPersist(NAV_SPLIT_KEY, navigationSplit)
  bindPersist(NAV_ACCORDION_KEY, navigationAccordion)
  bindPersist(CONTENT_COMPACT_KEY, contentCompact)
  bindPersist(CONTENT_MAX_WIDTH_KEY, contentMaxWidth)
  bindPersist(TAGS_BAR_KEY, tabbarEnabled)
  bindPersist(TABBAR_PERSIST_KEY, tabbarPersist)
  bindPersist(TABBAR_VISIT_HISTORY_KEY, tabbarVisitHistory)
  bindPersist(TABBAR_DRAGGABLE_KEY, tabbarDraggable)
  bindPersist(TABBAR_SHOW_MORE_KEY, tabbarShowMore)
  bindPersist(TABBAR_SHOW_MAXIMIZE_KEY, tabbarShowMaximize)
  bindPersist(TABBAR_MAX_COUNT_KEY, tabbarMaxCount)
  bindPersist(TABBAR_SCROLL_RESPONSE_KEY, tabbarScrollResponse)
  bindPersist(TABBAR_MIDDLE_CLICK_CLOSE_KEY, tabbarMiddleClickClose)
  bindPersist(TABBAR_SHOW_ICON_KEY, tabbarShowIcon)
  bindPersist(TABBAR_STYLE_KEY, tabbarStyle)
  bindPersist(BREADCRUMB_ENABLED_KEY, breadcrumbEnabled)
  bindPersist(BREADCRUMB_SHOW_HOME_KEY, breadcrumbShowHome)
  bindPersist(BREADCRUMB_SHOW_ICON_KEY, breadcrumbShowIcon)
  bindPersist(BREADCRUMB_HIDE_ONLY_ONE_KEY, breadcrumbHideOnlyOne)
  bindPersist(BREADCRUMB_STYLE_KEY, breadcrumbStyle)
  bindPersist(BREADCRUMB_NAV_BUTTONS_KEY, breadcrumbNavButtons)

  // ---- Actions ----
  function setLayoutMode(mode: string) {
    save(LAYOUT_MODE_KEY, layoutMode, mode)
  }
  function setBrandTitle(title: string) {
    save(BRAND_TITLE_KEY, brandTitle, title)
  }
  function setBrandLogo(logo: string) {
    save(BRAND_LOGO_KEY, brandLogo, logo)
  }
  function setBranding(branding: { title?: string, logo?: string }) {
    if (branding.title)
      setBrandTitle(branding.title)
    if (branding.logo)
      setBrandLogo(branding.logo)
  }
  function setPageLoading(loading: boolean) {
    pageLoading.value = loading
  }
  function toggleSidebar() {
    save(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed, !sidebarCollapsed.value)
  }
  function setSidebarCollapsed(v: boolean) {
    save(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed, v)
  }
  function setSidebarWidth(v: number) {
    save(SIDEBAR_WIDTH_KEY, sidebarWidth, Math.min(320, Math.max(180, v)))
  }
  function setSidebarShow(v: boolean) {
    save(SIDEBAR_SHOW_KEY, sidebarShow, v)
  }
  function setSidebarCollapseButton(v: boolean) {
    save(SIDEBAR_COLLAPSE_BUTTON_KEY, sidebarCollapseButton, v)
  }
  function setSidebarFixedButton(v: boolean) {
    save(SIDEBAR_FIXED_BUTTON_KEY, sidebarFixedButton, v)
  }
  function setSidebarExpandOnHover(v: boolean) {
    save(SIDEBAR_EXPAND_HOVER_KEY, sidebarExpandOnHover, v)
  }
  function setSidebarAutoActivateChild(v: boolean) {
    save(SIDEBAR_AUTO_ACTIVATE_CHILD_KEY, sidebarAutoActivateChild, v)
  }
  function setSidebarCollapsedShowTitle(v: boolean) {
    save(SIDEBAR_COLLAPSED_SHOW_TITLE_KEY, sidebarCollapsedShowTitle, v)
  }
  function setSidebarDark(v: boolean) {
    save(SIDEBAR_DARK_KEY, sidebarDark, v)
  }
  function setSidebarSubDark(v: boolean) {
    save(SIDEBAR_SUB_DARK_KEY, sidebarSubDark, v)
  }
  function setHeaderShow(v: boolean) {
    save(HEADER_SHOW_KEY, headerShow, v)
  }
  function setHeaderMenuAlign(v: 'start' | 'center' | 'end') {
    save(HEADER_MENU_ALIGN_KEY, headerMenuAlign, v)
  }
  function setHeaderMode(v: 'fixed' | 'static' | 'auto' | 'auto-scroll') {
    save(HEADER_MODE_KEY, headerMode, v)
  }
  function setHeaderDark(v: boolean) {
    save(HEADER_DARK_KEY, headerDark, v)
  }
  function setNavigationStyle(v: 'rounded' | 'plain') {
    save(NAV_STYLE_KEY, navigationStyle, v)
  }
  function setNavigationSplit(v: boolean) {
    save(NAV_SPLIT_KEY, navigationSplit, v)
  }
  function setNavigationAccordion(v: boolean) {
    save(NAV_ACCORDION_KEY, navigationAccordion, v)
  }
  function setContentCompact(v: boolean) {
    save(CONTENT_COMPACT_KEY, contentCompact, v)
  }
  function setContentMaxWidth(v: number) {
    save(CONTENT_MAX_WIDTH_KEY, contentMaxWidth, v)
  }
  function setTabbarEnabled(v: boolean) {
    save(TAGS_BAR_KEY, tabbarEnabled, v)
  }
  function setTabbarPersist(v: boolean) {
    save(TABBAR_PERSIST_KEY, tabbarPersist, v)
  }
  function setTabbarVisitHistory(v: boolean) {
    save(TABBAR_VISIT_HISTORY_KEY, tabbarVisitHistory, v)
  }
  function setTabbarDraggable(v: boolean) {
    save(TABBAR_DRAGGABLE_KEY, tabbarDraggable, v)
  }
  function setTabbarShowMore(v: boolean) {
    save(TABBAR_SHOW_MORE_KEY, tabbarShowMore, v)
  }
  function setTabbarShowMaximize(v: boolean) {
    save(TABBAR_SHOW_MAXIMIZE_KEY, tabbarShowMaximize, v)
  }
  function setTabbarMaxCount(v: number) {
    save(TABBAR_MAX_COUNT_KEY, tabbarMaxCount, v)
  }
  function setTabbarScrollResponse(v: boolean) {
    save(TABBAR_SCROLL_RESPONSE_KEY, tabbarScrollResponse, v)
  }
  function setTabbarMiddleClickClose(v: boolean) {
    save(TABBAR_MIDDLE_CLICK_CLOSE_KEY, tabbarMiddleClickClose, v)
  }
  function setTabbarShowIcon(v: boolean) {
    save(TABBAR_SHOW_ICON_KEY, tabbarShowIcon, v)
  }
  function setTabbarStyle(v: string) {
    save(TABBAR_STYLE_KEY, tabbarStyle, v)
  }
  function setBreadcrumbEnabled(v: boolean) {
    save(BREADCRUMB_ENABLED_KEY, breadcrumbEnabled, v)
  }
  function setBreadcrumbShowHome(v: boolean) {
    save(BREADCRUMB_SHOW_HOME_KEY, breadcrumbShowHome, v)
  }
  function setBreadcrumbShowIcon(v: boolean) {
    save(BREADCRUMB_SHOW_ICON_KEY, breadcrumbShowIcon, v)
  }
  function setBreadcrumbHideOnlyOne(v: boolean) {
    save(BREADCRUMB_HIDE_ONLY_ONE_KEY, breadcrumbHideOnlyOne, v)
  }
  function setBreadcrumbStyle(v: 'normal' | 'background') {
    save(BREADCRUMB_STYLE_KEY, breadcrumbStyle, v)
  }
  function setBreadcrumbNavButtons(v: boolean) {
    save(BREADCRUMB_NAV_BUTTONS_KEY, breadcrumbNavButtons, v)
  }

  return {
    layoutMode,
    brandTitle,
    brandLogo,
    pageLoading,
    sidebarCollapsed,
    sidebarWidth,
    sidebarShow,
    sidebarCollapseButton,
    sidebarFixedButton,
    sidebarExpandOnHover,
    sidebarAutoActivateChild,
    sidebarCollapsedShowTitle,
    sidebarDark,
    sidebarSubDark,
    headerShow,
    headerMenuAlign,
    headerMode,
    headerDark,
    navigationStyle,
    navigationSplit,
    navigationAccordion,
    contentCompact,
    contentMaxWidth,
    tabbarEnabled,
    tabbarPersist,
    tabbarVisitHistory,
    tabbarDraggable,
    tabbarShowMore,
    tabbarShowMaximize,
    tabbarMaxCount,
    tabbarScrollResponse,
    tabbarMiddleClickClose,
    tabbarShowIcon,
    tabbarStyle,
    breadcrumbEnabled,
    breadcrumbShowHome,
    breadcrumbShowIcon,
    breadcrumbHideOnlyOne,
    breadcrumbStyle,
    breadcrumbNavButtons,
    setLayoutMode,
    setBrandTitle,
    setBrandLogo,
    setBranding,
    setPageLoading,
    toggleSidebar,
    setSidebarCollapsed,
    setSidebarWidth,
    setSidebarShow,
    setSidebarCollapseButton,
    setSidebarFixedButton,
    setSidebarExpandOnHover,
    setSidebarAutoActivateChild,
    setSidebarCollapsedShowTitle,
    setSidebarDark,
    setSidebarSubDark,
    setHeaderShow,
    setHeaderMenuAlign,
    setHeaderMode,
    setHeaderDark,
    setNavigationStyle,
    setNavigationSplit,
    setNavigationAccordion,
    setContentCompact,
    setContentMaxWidth,
    setTabbarEnabled,
    setTabbarPersist,
    setTabbarVisitHistory,
    setTabbarDraggable,
    setTabbarShowMore,
    setTabbarShowMaximize,
    setTabbarMaxCount,
    setTabbarScrollResponse,
    setTabbarMiddleClickClose,
    setTabbarShowIcon,
    setTabbarStyle,
    setBreadcrumbEnabled,
    setBreadcrumbShowHome,
    setBreadcrumbShowIcon,
    setBreadcrumbHideOnlyOne,
    setBreadcrumbStyle,
    setBreadcrumbNavButtons,
  }
}
