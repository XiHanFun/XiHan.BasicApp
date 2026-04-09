import { ref } from 'vue'
import {
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
  FOOTER_ENABLE_KEY,
  FOOTER_FIXED_KEY,
  FOOTER_SHOW_DEV_INFO_KEY,
  LOCALE_KEY,
  SEARCH_ENABLED_KEY,
  SHORTCUT_ENABLE_KEY,
  SHORTCUT_LOCK_KEY,
  SHORTCUT_LOGOUT_KEY,
  SHORTCUT_SEARCH_KEY,
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
  const enableCheckUpdates = ref<boolean>(LocalStorage.get<boolean>(CHECK_UPDATES_KEY) ?? true)
  const checkUpdatesInterval = ref<number>(LocalStorage.get<number>(CHECK_UPDATES_INTERVAL_KEY) ?? 60)

  // ---- Widget ----
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

  // ---- 持久化绑定 ----
  bindPersist(LOCALE_KEY, locale)
  bindPersist(SEARCH_ENABLED_KEY, searchEnabled)
  bindPersist(DYNAMIC_TITLE_KEY, dynamicTitle)
  bindPersist(CHECK_UPDATES_KEY, enableCheckUpdates)
  bindPersist(CHECK_UPDATES_INTERVAL_KEY, checkUpdatesInterval)
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
  bindPersist(FOOTER_SHOW_DEV_INFO_KEY, footerShowDevInfo)
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

  return {
    locale,
    searchEnabled,
    dynamicTitle,
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
    setLocale,
    setSearchEnabled,
    setDynamicTitle,
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
  }
}
