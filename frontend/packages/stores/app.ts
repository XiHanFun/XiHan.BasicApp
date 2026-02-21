import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  BREADCRUMB_ENABLED_KEY,
  DEFAULT_LAYOUT_MODE,
  DEFAULT_LOCALE,
  DEFAULT_THEME,
  DEFAULT_THEME_COLOR,
  LAYOUT_MODE_KEY,
  LOCALE_KEY,
  SEARCH_ENABLED_KEY,
  SIDEBAR_COLLAPSED_KEY,
  TAGS_BAR_KEY,
  THEME_ANIMATION_ENABLED_KEY,
  THEME_COLOR_KEY,
  THEME_AUTO,
  THEME_MODE_KEY,
} from '~/constants'
import { storage } from '~/utils'

export const useAppStore = defineStore(
  'app',
  () => {
    const themeMode = ref<'light' | 'dark' | 'auto'>(
      (storage.get<'light' | 'dark' | 'auto'>(THEME_MODE_KEY)) ?? DEFAULT_THEME,
    )
    const locale = ref<string>(storage.get<string>(LOCALE_KEY) ?? DEFAULT_LOCALE)
    const sidebarCollapsed = ref<boolean>(storage.get<boolean>(SIDEBAR_COLLAPSED_KEY) ?? false)
    const tabbarEnabled = ref<boolean>(storage.get<boolean>(TAGS_BAR_KEY) ?? true)
    const breadcrumbEnabled = ref<boolean>(storage.get<boolean>(BREADCRUMB_ENABLED_KEY) ?? true)
    const searchEnabled = ref<boolean>(storage.get<boolean>(SEARCH_ENABLED_KEY) ?? true)
    const themeAnimationEnabled = ref<boolean>(
      storage.get<boolean>(THEME_ANIMATION_ENABLED_KEY) ?? true,
    )
    const layoutMode = ref<string>(storage.get<string>(LAYOUT_MODE_KEY) ?? DEFAULT_LAYOUT_MODE)
    const themeColor = ref<string>(storage.get<string>(THEME_COLOR_KEY) ?? DEFAULT_THEME_COLOR)
    const pageLoading = ref(false)

    const isDark = computed(() => themeMode.value === 'dark')

    function toggleTheme() {
      themeMode.value = themeMode.value === 'light' ? 'dark' : 'light'
      storage.set(THEME_MODE_KEY, themeMode.value)
    }

    function setTheme(mode: 'light' | 'dark' | 'auto') {
      themeMode.value = mode
      storage.set(THEME_MODE_KEY, mode)
    }

    function setFollowSystemTheme() {
      themeMode.value = THEME_AUTO
      storage.set(THEME_MODE_KEY, THEME_AUTO)
    }

    function setLocale(lang: string) {
      locale.value = lang
      storage.set(LOCALE_KEY, lang)
    }

    function toggleSidebar() {
      sidebarCollapsed.value = !sidebarCollapsed.value
      storage.set(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed.value)
    }

    function setSidebarCollapsed(collapsed: boolean) {
      sidebarCollapsed.value = collapsed
      storage.set(SIDEBAR_COLLAPSED_KEY, collapsed)
    }

    function setPageLoading(loading: boolean) {
      pageLoading.value = loading
    }

    function setThemeColor(color: string) {
      themeColor.value = color
      storage.set(THEME_COLOR_KEY, color)
    }

    function setLayoutMode(mode: string) {
      layoutMode.value = mode
      storage.set(LAYOUT_MODE_KEY, mode)
    }

    function setTabbarEnabled(enabled: boolean) {
      tabbarEnabled.value = enabled
      storage.set(TAGS_BAR_KEY, enabled)
    }

    function setBreadcrumbEnabled(enabled: boolean) {
      breadcrumbEnabled.value = enabled
      storage.set(BREADCRUMB_ENABLED_KEY, enabled)
    }

    function setSearchEnabled(enabled: boolean) {
      searchEnabled.value = enabled
      storage.set(SEARCH_ENABLED_KEY, enabled)
    }

    function setThemeAnimationEnabled(enabled: boolean) {
      themeAnimationEnabled.value = enabled
      storage.set(THEME_ANIMATION_ENABLED_KEY, enabled)
    }

    return {
      themeMode,
      locale,
      sidebarCollapsed,
      tabbarEnabled,
      breadcrumbEnabled,
      searchEnabled,
      themeAnimationEnabled,
      layoutMode,
      themeColor,
      pageLoading,
      isDark,
      toggleTheme,
      setTheme,
      setFollowSystemTheme,
      setLocale,
      toggleSidebar,
      setSidebarCollapsed,
      setPageLoading,
      setThemeColor,
      setLayoutMode,
      setTabbarEnabled,
      setBreadcrumbEnabled,
      setSearchEnabled,
      setThemeAnimationEnabled,
    }
  },
)
