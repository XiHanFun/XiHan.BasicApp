import { ref } from 'vue'
import {
  APP_TIMEZONE_KEY,
  CHECK_UPDATES_INTERVAL_KEY,
  CHECK_UPDATES_KEY,
  COPYRIGHT_DATE_KEY,
  COPYRIGHT_ENABLE_KEY,
  COPYRIGHT_ICP_KEY,
  COPYRIGHT_ICP_URL_KEY,
  COPYRIGHT_NAME_KEY,
  COPYRIGHT_SITE_KEY,
  DEFAULT_LOCALE,
  DYNAMIC_TITLE_KEY,
  FAVORITES_SYNC_KEY,
  FOOTER_ENABLE_KEY,
  FOOTER_FIXED_KEY,
  FOOTER_SHOW_DEV_INFO_KEY,
  LOCALE_KEY,
  PREFERENCE_SYNC_KEY,
  SEARCH_ENABLED_KEY,
  SEARCH_SYNC_KEY,
  SHORTCUT_ENABLE_KEY,
  SHORTCUT_LOCK_KEY,
  SHORTCUT_LOGOUT_KEY,
  SHORTCUT_SEARCH_KEY,
  SHORTCUT_TAB_OVERVIEW_KEY,
  TABLE_ROW_PEEK_KEY,
  TABLE_SYNC_KEY,
  WIDGET_DYNAMIC_ISLAND_KEY,
  WIDGET_FAVORITES_KEY,
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
import { bindPersist, save } from '../helpers'

/** 通用偏好、Widget、快捷键、页脚版权相关状态 */
export function createPreferencesSlice() {
  const locale = ref<string>(LocalStorage.get<string>(LOCALE_KEY) ?? DEFAULT_LOCALE)
  const searchEnabled = ref<boolean>(LocalStorage.get<boolean>(SEARCH_ENABLED_KEY) ?? true)
  const dynamicTitle = ref<boolean>(LocalStorage.get<boolean>(DYNAMIC_TITLE_KEY) ?? true)
  // 各类后端同步开关：默认开启（保存时上行后端并实时推送多端），关闭后仅本地存储
  const preferenceSyncEnabled = ref<boolean>(LocalStorage.get<boolean>(PREFERENCE_SYNC_KEY) ?? true)
  const favoritesSyncEnabled = ref<boolean>(LocalStorage.get<boolean>(FAVORITES_SYNC_KEY) ?? true)
  const searchSyncEnabled = ref<boolean>(LocalStorage.get<boolean>(SEARCH_SYNC_KEY) ?? true)
  const tableSyncEnabled = ref<boolean>(LocalStorage.get<boolean>(TABLE_SYNC_KEY) ?? true)
  // 表格行悬停速览（Peek & Pop）：悬停行浮出全字段详情卡，默认开启
  const tableRowPeek = ref<boolean>(LocalStorage.get<boolean>(TABLE_ROW_PEEK_KEY) ?? true)
  // 用户时区（IANA，如 Asia/Shanghai）：空串表示跟随浏览器；随请求头 X-Timezone 上行，后端按此换算返回时间
  const appTimezone = ref<string>(LocalStorage.get<string>(APP_TIMEZONE_KEY) ?? '')
  const enableCheckUpdates = ref<boolean>(LocalStorage.get<boolean>(CHECK_UPDATES_KEY) ?? true)
  const checkUpdatesInterval = ref<number>(LocalStorage.get<number>(CHECK_UPDATES_INTERVAL_KEY) ?? 30)

  // ---- Widget ----
  const widgetThemeToggle = ref<boolean>(LocalStorage.get<boolean>(WIDGET_THEME_TOGGLE_KEY) ?? true)
  const widgetLanguageToggle = ref<boolean>(
    LocalStorage.get<boolean>(WIDGET_LANGUAGE_TOGGLE_KEY) ?? true,
  )
  const widgetTimezone = ref<boolean>(LocalStorage.get<boolean>(WIDGET_TIMEZONE_KEY) ?? true)
  const widgetFullscreen = ref<boolean>(LocalStorage.get<boolean>(WIDGET_FULLSCREEN_KEY) ?? true)
  const widgetNotification = ref<boolean>(
    LocalStorage.get<boolean>(WIDGET_NOTIFICATION_KEY) ?? true,
  )
  const widgetLockScreen = ref<boolean>(LocalStorage.get<boolean>(WIDGET_LOCKSCREEN_KEY) ?? true)
  const widgetSidebarToggle = ref<boolean>(
    LocalStorage.get<boolean>(WIDGET_SIDEBAR_TOGGLE_KEY) ?? true,
  )
  const widgetRefresh = ref<boolean>(LocalStorage.get<boolean>(WIDGET_REFRESH_KEY) ?? true)
  const widgetFavorites = ref<boolean>(LocalStorage.get<boolean>(WIDGET_FAVORITES_KEY) ?? true)
  const widgetDynamicIsland = ref<boolean>(LocalStorage.get<boolean>(WIDGET_DYNAMIC_ISLAND_KEY) ?? true)
  const widgetPreferencePosition = ref<string>(
    LocalStorage.get<string>(WIDGET_PREFERENCE_POSITION_KEY) ?? 'auto',
  )

  // ---- 页脚与版权 ----
  const footerEnable = ref<boolean>(LocalStorage.get<boolean>(FOOTER_ENABLE_KEY) ?? true)
  const footerFixed = ref<boolean>(LocalStorage.get<boolean>(FOOTER_FIXED_KEY) ?? true)
  const footerShowDevInfo = ref<boolean>(
    LocalStorage.get<boolean>(FOOTER_SHOW_DEV_INFO_KEY) ?? true,
  )
  const copyrightEnable = ref<boolean>(LocalStorage.get<boolean>(COPYRIGHT_ENABLE_KEY) ?? true)
  const copyrightName = ref<string>(LocalStorage.get<string>(COPYRIGHT_NAME_KEY) ?? 'XiHan')
  const copyrightSite = ref<string>(
    LocalStorage.get<string>(COPYRIGHT_SITE_KEY) ?? 'https://www.xihanfun.com',
  )
  const copyrightDate = ref<string>(LocalStorage.get<string>(COPYRIGHT_DATE_KEY) ?? '2016')
  const copyrightIcp = ref<string>(LocalStorage.get<string>(COPYRIGHT_ICP_KEY) ?? '')
  const copyrightIcpUrl = ref<string>(LocalStorage.get<string>(COPYRIGHT_ICP_URL_KEY) ?? '')

  // ---- 快捷键 ----
  const shortcutEnable = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_ENABLE_KEY) ?? true)
  const shortcutSearch = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_SEARCH_KEY) ?? true)
  const shortcutLogout = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_LOGOUT_KEY) ?? false)
  const shortcutLock = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_LOCK_KEY) ?? false)
  const shortcutTabOverview = ref<boolean>(LocalStorage.get<boolean>(SHORTCUT_TAB_OVERVIEW_KEY) ?? true)

  // ---- 持久化绑定 ----
  bindPersist(LOCALE_KEY, locale, DEFAULT_LOCALE)
  bindPersist(SEARCH_ENABLED_KEY, searchEnabled, true)
  bindPersist(DYNAMIC_TITLE_KEY, dynamicTitle, true)
  bindPersist(PREFERENCE_SYNC_KEY, preferenceSyncEnabled, true)
  bindPersist(FAVORITES_SYNC_KEY, favoritesSyncEnabled, true)
  bindPersist(SEARCH_SYNC_KEY, searchSyncEnabled, true)
  bindPersist(TABLE_SYNC_KEY, tableSyncEnabled, true)
  bindPersist(TABLE_ROW_PEEK_KEY, tableRowPeek, true)
  bindPersist(APP_TIMEZONE_KEY, appTimezone, '')
  bindPersist(CHECK_UPDATES_KEY, enableCheckUpdates, true)
  bindPersist(CHECK_UPDATES_INTERVAL_KEY, checkUpdatesInterval, 30)
  bindPersist(WIDGET_THEME_TOGGLE_KEY, widgetThemeToggle, true)
  bindPersist(WIDGET_LANGUAGE_TOGGLE_KEY, widgetLanguageToggle, true)
  bindPersist(WIDGET_TIMEZONE_KEY, widgetTimezone, true)
  bindPersist(WIDGET_FULLSCREEN_KEY, widgetFullscreen, true)
  bindPersist(WIDGET_NOTIFICATION_KEY, widgetNotification, true)
  bindPersist(WIDGET_LOCKSCREEN_KEY, widgetLockScreen, true)
  bindPersist(WIDGET_SIDEBAR_TOGGLE_KEY, widgetSidebarToggle, true)
  bindPersist(WIDGET_REFRESH_KEY, widgetRefresh, true)
  bindPersist(WIDGET_FAVORITES_KEY, widgetFavorites, true)
  bindPersist(WIDGET_DYNAMIC_ISLAND_KEY, widgetDynamicIsland, true)
  bindPersist(WIDGET_PREFERENCE_POSITION_KEY, widgetPreferencePosition, 'auto')
  bindPersist(FOOTER_ENABLE_KEY, footerEnable, true)
  bindPersist(FOOTER_FIXED_KEY, footerFixed, true)
  bindPersist(FOOTER_SHOW_DEV_INFO_KEY, footerShowDevInfo, true)
  bindPersist(COPYRIGHT_ENABLE_KEY, copyrightEnable, true)
  bindPersist(COPYRIGHT_NAME_KEY, copyrightName, 'XiHan')
  bindPersist(COPYRIGHT_SITE_KEY, copyrightSite, 'https://www.xihanfun.com')
  bindPersist(COPYRIGHT_DATE_KEY, copyrightDate, '2016')
  bindPersist(COPYRIGHT_ICP_KEY, copyrightIcp, '')
  bindPersist(COPYRIGHT_ICP_URL_KEY, copyrightIcpUrl, '')
  bindPersist(SHORTCUT_ENABLE_KEY, shortcutEnable, true)
  bindPersist(SHORTCUT_SEARCH_KEY, shortcutSearch, true)
  bindPersist(SHORTCUT_LOGOUT_KEY, shortcutLogout, false)
  bindPersist(SHORTCUT_LOCK_KEY, shortcutLock, false)
  bindPersist(SHORTCUT_TAB_OVERVIEW_KEY, shortcutTabOverview, true)

  // ---- Actions ----
  function setLocale(lang: string) {
    save(LOCALE_KEY, locale, lang)
  }
  function setSearchEnabled(v: boolean) {
    save(SEARCH_ENABLED_KEY, searchEnabled, v)
  }
  function setDynamicTitle(v: boolean) {
    save(DYNAMIC_TITLE_KEY, dynamicTitle, v)
  }
  function setPreferenceSyncEnabled(v: boolean) {
    save(PREFERENCE_SYNC_KEY, preferenceSyncEnabled, v)
  }
  function setFavoritesSyncEnabled(v: boolean) {
    save(FAVORITES_SYNC_KEY, favoritesSyncEnabled, v)
  }
  function setSearchSyncEnabled(v: boolean) {
    save(SEARCH_SYNC_KEY, searchSyncEnabled, v)
  }
  function setTableSyncEnabled(v: boolean) {
    save(TABLE_SYNC_KEY, tableSyncEnabled, v)
  }
  function setTableRowPeek(v: boolean) {
    save(TABLE_ROW_PEEK_KEY, tableRowPeek, v)
  }
  function setAppTimezone(v: string) {
    save(APP_TIMEZONE_KEY, appTimezone, v)
  }
  function setEnableCheckUpdates(v: boolean) {
    save(CHECK_UPDATES_KEY, enableCheckUpdates, v)
  }
  function setCheckUpdatesInterval(v: number) {
    save(CHECK_UPDATES_INTERVAL_KEY, checkUpdatesInterval, v)
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
  function setWidgetFavorites(v: boolean) {
    save(WIDGET_FAVORITES_KEY, widgetFavorites, v)
  }
  function setWidgetDynamicIsland(v: boolean) {
    save(WIDGET_DYNAMIC_ISLAND_KEY, widgetDynamicIsland, v)
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
  function setFooterShowDevInfo(v: boolean) {
    save(FOOTER_SHOW_DEV_INFO_KEY, footerShowDevInfo, v)
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
  function setShortcutTabOverview(v: boolean) {
    save(SHORTCUT_TAB_OVERVIEW_KEY, shortcutTabOverview, v)
  }

  return {
    locale,
    searchEnabled,
    dynamicTitle,
    preferenceSyncEnabled,
    favoritesSyncEnabled,
    searchSyncEnabled,
    tableSyncEnabled,
    tableRowPeek,
    appTimezone,
    enableCheckUpdates,
    checkUpdatesInterval,
    widgetThemeToggle,
    widgetLanguageToggle,
    widgetTimezone,
    widgetFullscreen,
    widgetNotification,
    widgetLockScreen,
    widgetSidebarToggle,
    widgetRefresh,
    widgetFavorites,
    widgetDynamicIsland,
    widgetPreferencePosition,
    footerEnable,
    footerFixed,
    footerShowDevInfo,
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
    shortcutTabOverview,
    setLocale,
    setSearchEnabled,
    setDynamicTitle,
    setPreferenceSyncEnabled,
    setFavoritesSyncEnabled,
    setSearchSyncEnabled,
    setTableSyncEnabled,
    setTableRowPeek,
    setAppTimezone,
    setEnableCheckUpdates,
    setCheckUpdatesInterval,
    setWidgetThemeToggle,
    setWidgetLanguageToggle,
    setWidgetTimezone,
    setWidgetFullscreen,
    setWidgetNotification,
    setWidgetLockScreen,
    setWidgetSidebarToggle,
    setWidgetRefresh,
    setWidgetFavorites,
    setWidgetDynamicIsland,
    setWidgetPreferencePosition,
    setFooterEnable,
    setFooterFixed,
    setFooterShowDevInfo,
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
    setShortcutTabOverview,
  }
}
