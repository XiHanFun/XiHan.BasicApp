import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import {
  BRAND_LOGO_KEY,
  BRAND_TITLE_KEY,
  BREADCRUMB_ENABLED_KEY,
  BREADCRUMB_HIDE_ONLY_ONE_KEY,
  BREADCRUMB_SHOW_HOME_KEY,
  BREADCRUMB_SHOW_ICON_KEY,
  BREADCRUMB_STYLE_KEY,
  CHECK_UPDATES_KEY,
  COLOR_WEAKNESS_ENABLED_KEY,
  CONTENT_COMPACT_KEY,
  CONTENT_MAX_WIDTH_KEY,
  COPYRIGHT_NAME_KEY,
  COPYRIGHT_DATE_KEY,
  COPYRIGHT_ENABLE_KEY,
  COPYRIGHT_ICP_KEY,
  COPYRIGHT_ICP_URL_KEY,
  COPYRIGHT_SITE_KEY,
  DEFAULT_FONT_SIZE,
  DEFAULT_LAYOUT_MODE,
  DEFAULT_LOCALE,
  DEFAULT_THEME,
  DEFAULT_THEME_COLOR,
  DEFAULT_UI_RADIUS,
  DYNAMIC_TITLE_KEY,
  FONT_SIZE_KEY,
  FOOTER_ENABLE_KEY,
  FOOTER_FIXED_KEY,
  GRAYSCALE_ENABLED_KEY,
  HEADER_DARK_KEY,
  HEADER_MENU_ALIGN_KEY,
  HEADER_MODE_KEY,
  HEADER_SHOW_KEY,
  LAYOUT_MODE_KEY,
  LOCALE_KEY,
  NAV_ACCORDION_KEY,
  NAV_SPLIT_KEY,
  NAV_STYLE_KEY,
  SEARCH_ENABLED_KEY,
  SHORTCUT_ENABLE_KEY,
  SHORTCUT_LOCK_KEY,
  SHORTCUT_LOGOUT_KEY,
  SHORTCUT_SEARCH_KEY,
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
  THEME_ANIMATION_ENABLED_KEY,
  THEME_AUTO,
  THEME_COLOR_KEY,
  THEME_MODE_KEY,
  TRANSITION_ENABLE_KEY,
  TRANSITION_LOADING_KEY,
  TRANSITION_NAME_KEY,
  TRANSITION_PROGRESS_KEY,
  UI_RADIUS_KEY,
  WATERMARK_ENABLED_KEY,
  WATERMARK_TEXT_KEY,
  WIDGET_FULLSCREEN_KEY,
  WIDGET_LANGUAGE_TOGGLE_KEY,
  WIDGET_LOCKSCREEN_KEY,
  WIDGET_NOTIFICATION_KEY,
  WIDGET_PREFERENCE_POSITION_KEY,
  WIDGET_REFRESH_KEY,
  WIDGET_SIDEBAR_TOGGLE_KEY,
  WIDGET_THEME_TOGGLE_KEY,
  WIDGET_TIMEZONE_KEY,
} from '~/constants'
import { LocalStorage } from '~/utils'

export const useAppStore = defineStore('app', () => {
  const themeMode = ref<'light' | 'dark' | 'auto'>(
    LocalStorage.get<'light' | 'dark' | 'auto'>(THEME_MODE_KEY) ?? DEFAULT_THEME,
  )
  const locale = ref<string>(LocalStorage.get<string>(LOCALE_KEY) ?? DEFAULT_LOCALE)
  const layoutMode = ref<string>(LocalStorage.get<string>(LAYOUT_MODE_KEY) ?? DEFAULT_LAYOUT_MODE)
  const themeColor = ref<string>(LocalStorage.get<string>(THEME_COLOR_KEY) ?? DEFAULT_THEME_COLOR)
  const brandTitle = ref<string>(
    LocalStorage.get<string>(BRAND_TITLE_KEY) ?? import.meta.env.VITE_APP_TITLE,
  )
  const brandLogo = ref<string>(
    LocalStorage.get<string>(BRAND_LOGO_KEY) ?? (import.meta.env.VITE_APP_LOGO || '/favicon.png'),
  )
  const uiRadius = ref<number>(LocalStorage.get<number>(UI_RADIUS_KEY) ?? DEFAULT_UI_RADIUS)
  const fontSize = ref<number>(LocalStorage.get<number>(FONT_SIZE_KEY) ?? DEFAULT_FONT_SIZE)
  const pageLoading = ref(false)

  const sidebarCollapsed = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_COLLAPSED_KEY) ?? false)
  const sidebarWidth = ref<number>((() => {
    const saved = LocalStorage.get<number>(SIDEBAR_WIDTH_KEY)
    if (typeof saved === 'number' && Number.isFinite(saved)) {
      return Math.min(320, Math.max(180, saved))
    }
    return 224
  })())
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

  const headerShow = ref<boolean>(LocalStorage.get<boolean>(HEADER_SHOW_KEY) ?? true)
  const headerMenuAlign = ref<'start' | 'center' | 'end'>((() => {
    const saved = LocalStorage.get<string>(HEADER_MENU_ALIGN_KEY)
    if (saved === 'left') {
      return 'start' as const
    }
    if (saved === 'right') {
      return 'end' as const
    }
    if (saved === 'center' || saved === 'start' || saved === 'end') {
      return saved
    }
    return 'start' as const
  })())
  const headerMode = ref<'fixed' | 'static' | 'auto' | 'auto-scroll'>(LocalStorage.get(HEADER_MODE_KEY) ?? 'fixed')
  const sidebarDark = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_DARK_KEY) ?? false)
  const sidebarSubDark = ref<boolean>(LocalStorage.get<boolean>(SIDEBAR_SUB_DARK_KEY) ?? false)
  const headerDark = ref<boolean>(LocalStorage.get<boolean>(HEADER_DARK_KEY) ?? false)
  const navigationStyle = ref<'rounded' | 'plain'>(LocalStorage.get(NAV_STYLE_KEY) ?? 'rounded')
  const navigationSplit = ref<boolean>(LocalStorage.get<boolean>(NAV_SPLIT_KEY) ?? true)
  const navigationAccordion = ref<boolean>(LocalStorage.get<boolean>(NAV_ACCORDION_KEY) ?? true)
  const contentCompact = ref<boolean>(LocalStorage.get<boolean>(CONTENT_COMPACT_KEY) ?? false)
  const contentMaxWidth = ref<number>(LocalStorage.get<number>(CONTENT_MAX_WIDTH_KEY) ?? 1280)

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

  const breadcrumbEnabled = ref<boolean>(LocalStorage.get<boolean>(BREADCRUMB_ENABLED_KEY) ?? true)
  const breadcrumbShowHome = ref<boolean>(
    LocalStorage.get<boolean>(BREADCRUMB_SHOW_HOME_KEY) ?? true,
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

  const searchEnabled = ref<boolean>(LocalStorage.get<boolean>(SEARCH_ENABLED_KEY) ?? true)
  const dynamicTitle = ref<boolean>(LocalStorage.get<boolean>(DYNAMIC_TITLE_KEY) ?? true)
  const enableCheckUpdates = ref<boolean>(LocalStorage.get<boolean>(CHECK_UPDATES_KEY) ?? true)
  const themeAnimationEnabled = ref<boolean>(
    LocalStorage.get<boolean>(THEME_ANIMATION_ENABLED_KEY) ?? true,
  )
  const transitionEnable = ref<boolean>(LocalStorage.get<boolean>(TRANSITION_ENABLE_KEY) ?? true)
  const transitionName = ref<string>(LocalStorage.get<string>(TRANSITION_NAME_KEY) ?? 'fade')
  const transitionProgress = ref<boolean>(
    LocalStorage.get<boolean>(TRANSITION_PROGRESS_KEY) ?? true,
  )
  const transitionLoading = ref<boolean>(LocalStorage.get<boolean>(TRANSITION_LOADING_KEY) ?? true)

  const grayscaleEnabled = ref<boolean>(LocalStorage.get<boolean>(GRAYSCALE_ENABLED_KEY) ?? false)
  const colorWeaknessEnabled = ref<boolean>(
    LocalStorage.get<boolean>(COLOR_WEAKNESS_ENABLED_KEY) ?? false,
  )
  const watermarkEnabled = ref<boolean>(LocalStorage.get<boolean>(WATERMARK_ENABLED_KEY) ?? false)
  const watermarkText = ref<string>(
    LocalStorage.get<string>(WATERMARK_TEXT_KEY) ?? 'XiHan BasicApp',
  )

  const widgetThemeToggle = ref<boolean>(LocalStorage.get<boolean>(WIDGET_THEME_TOGGLE_KEY) ?? true)
  const widgetLanguageToggle = ref<boolean>(
    LocalStorage.get<boolean>(WIDGET_LANGUAGE_TOGGLE_KEY) ?? true,
  )
  const widgetTimezone = ref<boolean>(LocalStorage.get<boolean>(WIDGET_TIMEZONE_KEY) ?? false)
  const widgetFullscreen = ref<boolean>(LocalStorage.get<boolean>(WIDGET_FULLSCREEN_KEY) ?? true)
  const widgetNotification = ref<boolean>(
    LocalStorage.get<boolean>(WIDGET_NOTIFICATION_KEY) ?? true,
  )
  const widgetLockScreen = ref<boolean>(LocalStorage.get<boolean>(WIDGET_LOCKSCREEN_KEY) ?? true)
  const widgetSidebarToggle = ref<boolean>(
    LocalStorage.get<boolean>(WIDGET_SIDEBAR_TOGGLE_KEY) ?? true,
  )
  const widgetRefresh = ref<boolean>(LocalStorage.get<boolean>(WIDGET_REFRESH_KEY) ?? true)
  const widgetPreferencePosition = ref<string>(
    LocalStorage.get<string>(WIDGET_PREFERENCE_POSITION_KEY) ?? 'auto',
  )

  const footerEnable = ref<boolean>(LocalStorage.get<boolean>(FOOTER_ENABLE_KEY) ?? false)
  const footerFixed = ref<boolean>(LocalStorage.get<boolean>(FOOTER_FIXED_KEY) ?? false)
  const copyrightEnable = ref<boolean>(LocalStorage.get<boolean>(COPYRIGHT_ENABLE_KEY) ?? true)
  const copyrightName = ref<string>(LocalStorage.get<string>(COPYRIGHT_NAME_KEY) ?? 'XiHan')
  const copyrightSite = ref<string>(
    LocalStorage.get<string>(COPYRIGHT_SITE_KEY) ?? 'https://xihanfun.com',
  )
  const copyrightDate = ref<string>(
    LocalStorage.get<string>(COPYRIGHT_DATE_KEY) ?? String(new Date().getFullYear()),
  )
  const copyrightIcp = ref<string>(LocalStorage.get<string>(COPYRIGHT_ICP_KEY) ?? '')
  const copyrightIcpUrl = ref<string>(LocalStorage.get<string>(COPYRIGHT_ICP_URL_KEY) ?? '')

  const shortcutEnable = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_ENABLE_KEY) ?? true)
  const shortcutSearch = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_SEARCH_KEY) ?? true)
  const shortcutLogout = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_LOGOUT_KEY) ?? false)
  const shortcutLock = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_LOCK_KEY) ?? false)

  const isDark = computed(() => themeMode.value === 'dark')

  function bindPersist<T>(key: string, source: { value: T }) {
    watch(source, (value) => {
      LocalStorage.set(key, value)
    })
  }

  bindPersist(THEME_MODE_KEY, themeMode)
  bindPersist(LOCALE_KEY, locale)
  bindPersist(LAYOUT_MODE_KEY, layoutMode)
  bindPersist(THEME_COLOR_KEY, themeColor)
  bindPersist(BRAND_TITLE_KEY, brandTitle)
  bindPersist(BRAND_LOGO_KEY, brandLogo)
  bindPersist(UI_RADIUS_KEY, uiRadius)
  bindPersist(FONT_SIZE_KEY, fontSize)
  bindPersist(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed)
  bindPersist(SIDEBAR_WIDTH_KEY, sidebarWidth)
  bindPersist(SIDEBAR_SHOW_KEY, sidebarShow)
  bindPersist(SIDEBAR_COLLAPSE_BUTTON_KEY, sidebarCollapseButton)
  bindPersist(SIDEBAR_FIXED_BUTTON_KEY, sidebarFixedButton)
  bindPersist(SIDEBAR_EXPAND_HOVER_KEY, sidebarExpandOnHover)
  bindPersist(SIDEBAR_AUTO_ACTIVATE_CHILD_KEY, sidebarAutoActivateChild)
  bindPersist(SIDEBAR_COLLAPSED_SHOW_TITLE_KEY, sidebarCollapsedShowTitle)
  bindPersist(HEADER_SHOW_KEY, headerShow)
  bindPersist(HEADER_MENU_ALIGN_KEY, headerMenuAlign)
  bindPersist(HEADER_MODE_KEY, headerMode)
  bindPersist(HEADER_DARK_KEY, headerDark)
  bindPersist(SIDEBAR_DARK_KEY, sidebarDark)
  bindPersist(SIDEBAR_SUB_DARK_KEY, sidebarSubDark)
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
  bindPersist(SEARCH_ENABLED_KEY, searchEnabled)
  bindPersist(DYNAMIC_TITLE_KEY, dynamicTitle)
  bindPersist(CHECK_UPDATES_KEY, enableCheckUpdates)
  bindPersist(THEME_ANIMATION_ENABLED_KEY, themeAnimationEnabled)
  bindPersist(TRANSITION_ENABLE_KEY, transitionEnable)
  bindPersist(TRANSITION_NAME_KEY, transitionName)
  bindPersist(TRANSITION_PROGRESS_KEY, transitionProgress)
  bindPersist(TRANSITION_LOADING_KEY, transitionLoading)
  bindPersist(GRAYSCALE_ENABLED_KEY, grayscaleEnabled)
  bindPersist(COLOR_WEAKNESS_ENABLED_KEY, colorWeaknessEnabled)
  bindPersist(WATERMARK_ENABLED_KEY, watermarkEnabled)
  bindPersist(WATERMARK_TEXT_KEY, watermarkText)
  bindPersist(WIDGET_THEME_TOGGLE_KEY, widgetThemeToggle)
  bindPersist(WIDGET_LANGUAGE_TOGGLE_KEY, widgetLanguageToggle)
  bindPersist(WIDGET_TIMEZONE_KEY, widgetTimezone)
  bindPersist(WIDGET_FULLSCREEN_KEY, widgetFullscreen)
  bindPersist(WIDGET_NOTIFICATION_KEY, widgetNotification)
  bindPersist(WIDGET_LOCKSCREEN_KEY, widgetLockScreen)
  bindPersist(WIDGET_SIDEBAR_TOGGLE_KEY, widgetSidebarToggle)
  bindPersist(WIDGET_REFRESH_KEY, widgetRefresh)
  bindPersist(WIDGET_PREFERENCE_POSITION_KEY, widgetPreferencePosition)
  bindPersist(FOOTER_ENABLE_KEY, footerEnable)
  bindPersist(FOOTER_FIXED_KEY, footerFixed)
  bindPersist(COPYRIGHT_ENABLE_KEY, copyrightEnable)
  bindPersist(COPYRIGHT_NAME_KEY, copyrightName)
  bindPersist(COPYRIGHT_SITE_KEY, copyrightSite)
  bindPersist(COPYRIGHT_DATE_KEY, copyrightDate)
  bindPersist(COPYRIGHT_ICP_KEY, copyrightIcp)
  bindPersist(COPYRIGHT_ICP_URL_KEY, copyrightIcpUrl)
  bindPersist(SHORTCUT_ENABLE_KEY, shortcutEnable)
  bindPersist(SHORTCUT_SEARCH_KEY, shortcutSearch)
  bindPersist(SHORTCUT_LOGOUT_KEY, shortcutLogout)
  bindPersist(SHORTCUT_LOCK_KEY, shortcutLock)

  function save<T>(key: string, target: { value: T }, value: T) {
    target.value = value
    LocalStorage.set(key, value)
  }

  function toggleTheme() {
    save(THEME_MODE_KEY, themeMode, themeMode.value === 'light' ? 'dark' : 'light')
  }

  function setTheme(mode: 'light' | 'dark' | 'auto') {
    save(THEME_MODE_KEY, themeMode, mode)
  }
  function setFollowSystemTheme() {
    save(THEME_MODE_KEY, themeMode, THEME_AUTO)
  }
  function setLocale(lang: string) {
    save(LOCALE_KEY, locale, lang)
  }
  function setPageLoading(loading: boolean) {
    pageLoading.value = loading
  }
  function setThemeColor(color: string) {
    save(THEME_COLOR_KEY, themeColor, color)
  }
  function setBrandTitle(title: string) {
    save(BRAND_TITLE_KEY, brandTitle, title)
  }
  function setBrandLogo(logo: string) {
    save(BRAND_LOGO_KEY, brandLogo, logo)
  }
  function setBranding(branding: { title?: string; logo?: string }) {
    if (branding.title) {
      setBrandTitle(branding.title)
    }
    if (branding.logo) {
      setBrandLogo(branding.logo)
    }
  }
  function setLayoutMode(mode: string) {
    save(LAYOUT_MODE_KEY, layoutMode, mode)
  }
  function setUiRadius(value: number) {
    save(UI_RADIUS_KEY, uiRadius, value)
  }
  function setFontSize(size: number) {
    save(FONT_SIZE_KEY, fontSize, size)
  }
  function toggleSidebar() {
    save(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed, !sidebarCollapsed.value)
  }
  function setSidebarCollapsed(value: boolean) {
    save(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed, value)
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

  function setSearchEnabled(v: boolean) {
    save(SEARCH_ENABLED_KEY, searchEnabled, v)
  }
  function setDynamicTitle(v: boolean) {
    save(DYNAMIC_TITLE_KEY, dynamicTitle, v)
  }
  function setEnableCheckUpdates(v: boolean) {
    save(CHECK_UPDATES_KEY, enableCheckUpdates, v)
  }
  function setThemeAnimationEnabled(v: boolean) {
    save(THEME_ANIMATION_ENABLED_KEY, themeAnimationEnabled, v)
  }
  function setTransitionEnable(v: boolean) {
    save(TRANSITION_ENABLE_KEY, transitionEnable, v)
  }
  function setTransitionName(v: string) {
    save(TRANSITION_NAME_KEY, transitionName, v)
  }
  function setTransitionProgress(v: boolean) {
    save(TRANSITION_PROGRESS_KEY, transitionProgress, v)
  }
  function setTransitionLoading(v: boolean) {
    save(TRANSITION_LOADING_KEY, transitionLoading, v)
  }

  function setGrayscaleEnabled(v: boolean) {
    save(GRAYSCALE_ENABLED_KEY, grayscaleEnabled, v)
  }
  function setColorWeaknessEnabled(v: boolean) {
    save(COLOR_WEAKNESS_ENABLED_KEY, colorWeaknessEnabled, v)
  }
  function setWatermarkEnabled(v: boolean) {
    save(WATERMARK_ENABLED_KEY, watermarkEnabled, v)
  }
  function setWatermarkText(v: string) {
    save(WATERMARK_TEXT_KEY, watermarkText, v)
  }

  function setContentCompact(v: boolean) {
    save(CONTENT_COMPACT_KEY, contentCompact, v)
  }
  function setContentMaxWidth(v: number) {
    save(CONTENT_MAX_WIDTH_KEY, contentMaxWidth, v)
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
  function setSidebarDark(v: boolean) {
    save(SIDEBAR_DARK_KEY, sidebarDark, v)
  }
  function setSidebarSubDark(v: boolean) {
    save(SIDEBAR_SUB_DARK_KEY, sidebarSubDark, v)
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

  function setWidgetThemeToggle(v: boolean) {
    save(WIDGET_THEME_TOGGLE_KEY, widgetThemeToggle, v)
  }
  function setWidgetLanguageToggle(v: boolean) {
    save(WIDGET_LANGUAGE_TOGGLE_KEY, widgetLanguageToggle, v)
  }
  function setWidgetTimezone(v: boolean) {
    save(WIDGET_TIMEZONE_KEY, widgetTimezone, v)
  }
  function setWidgetFullscreen(v: boolean) {
    save(WIDGET_FULLSCREEN_KEY, widgetFullscreen, v)
  }
  function setWidgetNotification(v: boolean) {
    save(WIDGET_NOTIFICATION_KEY, widgetNotification, v)
  }
  function setWidgetLockScreen(v: boolean) {
    save(WIDGET_LOCKSCREEN_KEY, widgetLockScreen, v)
  }
  function setWidgetSidebarToggle(v: boolean) {
    save(WIDGET_SIDEBAR_TOGGLE_KEY, widgetSidebarToggle, v)
  }
  function setWidgetRefresh(v: boolean) {
    save(WIDGET_REFRESH_KEY, widgetRefresh, v)
  }
  function setWidgetPreferencePosition(v: string) {
    save(WIDGET_PREFERENCE_POSITION_KEY, widgetPreferencePosition, v)
  }

  function setFooterEnable(v: boolean) {
    save(FOOTER_ENABLE_KEY, footerEnable, v)
  }
  function setFooterFixed(v: boolean) {
    save(FOOTER_FIXED_KEY, footerFixed, v)
  }
  function setCopyrightEnable(v: boolean) {
    save(COPYRIGHT_ENABLE_KEY, copyrightEnable, v)
  }
  function setCopyrightName(v: string) {
    save(COPYRIGHT_NAME_KEY, copyrightName, v)
  }
  function setCopyrightSite(v: string) {
    save(COPYRIGHT_SITE_KEY, copyrightSite, v)
  }
  function setCopyrightDate(v: string) {
    save(COPYRIGHT_DATE_KEY, copyrightDate, v)
  }
  function setCopyrightIcp(v: string) {
    save(COPYRIGHT_ICP_KEY, copyrightIcp, v)
  }
  function setCopyrightIcpUrl(v: string) {
    save(COPYRIGHT_ICP_URL_KEY, copyrightIcpUrl, v)
  }

  function setShortcutEnable(v: boolean) {
    save(SHORTCUT_ENABLE_KEY, shortcutEnable, v)
  }
  function setShortcutSearch(v: boolean) {
    save(SHORTCUT_SEARCH_KEY, shortcutSearch, v)
  }
  function setShortcutLogout(v: boolean) {
    save(SHORTCUT_LOGOUT_KEY, shortcutLogout, v)
  }
  function setShortcutLock(v: boolean) {
    save(SHORTCUT_LOCK_KEY, shortcutLock, v)
  }

  return {
    themeMode,
    locale,
    layoutMode,
    themeColor,
    brandTitle,
    brandLogo,
    uiRadius,
    fontSize,
    isDark,
    pageLoading,
    sidebarCollapsed,
    sidebarWidth,
    sidebarShow,
    sidebarCollapseButton,
    sidebarFixedButton,
    sidebarExpandOnHover,
    sidebarAutoActivateChild,
    sidebarCollapsedShowTitle,
    headerShow,
    headerMenuAlign,
    headerMode,
    sidebarDark,
    sidebarSubDark,
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
    searchEnabled,
    dynamicTitle,
    enableCheckUpdates,
    themeAnimationEnabled,
    transitionEnable,
    transitionName,
    transitionProgress,
    transitionLoading,
    grayscaleEnabled,
    colorWeaknessEnabled,
    watermarkEnabled,
    watermarkText,
    widgetThemeToggle,
    widgetLanguageToggle,
    widgetTimezone,
    widgetFullscreen,
    widgetNotification,
    widgetLockScreen,
    widgetSidebarToggle,
    widgetRefresh,
    widgetPreferencePosition,
    footerEnable,
    footerFixed,
    copyrightEnable,
    copyrightName,
    copyrightSite,
    copyrightDate,
    copyrightIcp,
    copyrightIcpUrl,
    shortcutEnable,
    shortcutSearch,
    shortcutLogout,
    shortcutLock,
    toggleTheme,
    setTheme,
    setFollowSystemTheme,
    setLocale,
    setPageLoading,
    setThemeColor,
    setBrandTitle,
    setBrandLogo,
    setBranding,
    setLayoutMode,
    setUiRadius,
    setFontSize,
    toggleSidebar,
    setSidebarCollapsed,
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
    setSearchEnabled,
    setDynamicTitle,
    setEnableCheckUpdates,
    setThemeAnimationEnabled,
    setTransitionEnable,
    setTransitionName,
    setTransitionProgress,
    setTransitionLoading,
    setGrayscaleEnabled,
    setColorWeaknessEnabled,
    setWatermarkEnabled,
    setWatermarkText,
    setContentCompact,
    setContentMaxWidth,
    setHeaderShow,
    setHeaderMenuAlign,
    setHeaderMode,
    setSidebarDark,
    setSidebarSubDark,
    setHeaderDark,
    setNavigationStyle,
    setNavigationSplit,
    setNavigationAccordion,
    setSidebarWidth,
    setSidebarShow,
    setSidebarCollapseButton,
    setSidebarFixedButton,
    setSidebarExpandOnHover,
    setSidebarAutoActivateChild,
    setSidebarCollapsedShowTitle,
    setWidgetThemeToggle,
    setWidgetLanguageToggle,
    setWidgetTimezone,
    setWidgetFullscreen,
    setWidgetNotification,
    setWidgetLockScreen,
    setWidgetSidebarToggle,
    setWidgetRefresh,
    setWidgetPreferencePosition,
    setFooterEnable,
    setFooterFixed,
    setCopyrightEnable,
    setCopyrightName,
    setCopyrightSite,
    setCopyrightDate,
    setCopyrightIcp,
    setCopyrightIcpUrl,
    setShortcutEnable,
    setShortcutSearch,
    setShortcutLogout,
    setShortcutLock,
  }
})
