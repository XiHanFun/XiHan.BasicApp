/**
 * 通用偏好切片（app/preferences）单元测试。
 * 职责边界：语言、时区、五个后端同步开关（默认开启、设备本地维度）、Widget 显隐与位置（含旧版开关迁移）、
 * 快捷键、页脚版权等偏好的默认值、本地还原与落地；以及
 * 「locale ref 是 vue-i18n 的唯一入口」这条回归锚点。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import {
  APP_TIMEZONE_KEY,
  CHECK_UPDATES_INTERVAL_KEY,
  DEFAULT_LOCALE,
  LEGACY_SEARCH_ENABLED_KEY,
  LEGACY_WIDGET_FULLSCREEN_KEY,
  LEGACY_WIDGET_LANGUAGE_TOGGLE_KEY,
  LEGACY_WIDGET_THEME_TOGGLE_KEY,
  LEGACY_WIDGET_TIMEZONE_KEY,
  LOCALE_KEY,
  PREFERENCE_SYNC_KEY,
  WIDGET_FULLSCREEN_PLACEMENT_KEY,
  WIDGET_LANGUAGE_PLACEMENT_KEY,
  WIDGET_PREFERENCE_POSITION_KEY,
  WIDGET_SEARCH_PLACEMENT_KEY,
  WIDGET_THEME_PLACEMENT_KEY,
  WIDGET_TIMEZONE_PLACEMENT_KEY,
} from '~/constants'
import { i18n } from '~/locales'
import { useAppStore } from '../app'

function freshStore(): ReturnType<typeof useAppStore> {
  setActivePinia(createPinia())
  return useAppStore()
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = DEFAULT_LOCALE as typeof i18n.global.locale.value
})

describe('旧版工具开关迁移为位置', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('旧版关掉的工具迁成隐藏，开着的迁成自动；迁移后旧键删除、新键落地', () => {
    localStorage.setItem(LEGACY_SEARCH_ENABLED_KEY, 'false')
    localStorage.setItem(LEGACY_WIDGET_THEME_TOGGLE_KEY, 'true')
    localStorage.setItem(LEGACY_WIDGET_LANGUAGE_TOGGLE_KEY, 'false')
    localStorage.setItem(LEGACY_WIDGET_FULLSCREEN_KEY, 'false')

    const store = freshStore()

    expect(store.widgetSearchPlacement).toBe('hidden')
    expect(store.widgetThemePlacement).toBe('auto')
    expect(store.widgetLanguagePlacement).toBe('hidden')
    expect(store.widgetTimezonePlacement).toBe('auto')
    expect(store.widgetFullscreenPlacement).toBe('hidden')
    for (const key of [
      LEGACY_SEARCH_ENABLED_KEY,
      LEGACY_WIDGET_THEME_TOGGLE_KEY,
      LEGACY_WIDGET_LANGUAGE_TOGGLE_KEY,
      LEGACY_WIDGET_TIMEZONE_KEY,
      LEGACY_WIDGET_FULLSCREEN_KEY,
    ]) {
      expect(localStorage.getItem(key)).toBeNull()
    }
    expect(localStorage.getItem(WIDGET_SEARCH_PLACEMENT_KEY)).toBe(JSON.stringify('hidden'))
    expect(localStorage.getItem(WIDGET_TIMEZONE_PLACEMENT_KEY)).toBe(JSON.stringify('auto'))
  })

  it('已有新位置时以新位置为准，不被旧开关覆盖', () => {
    localStorage.setItem(WIDGET_LANGUAGE_PLACEMENT_KEY, JSON.stringify('floating'))
    localStorage.setItem(LEGACY_WIDGET_LANGUAGE_TOGGLE_KEY, 'false')

    const store = freshStore()

    expect(store.widgetLanguagePlacement).toBe('floating')
  })

  it('新键里存的不是合法位置时按旧开关重新迁移', () => {
    localStorage.setItem(WIDGET_FULLSCREEN_PLACEMENT_KEY, JSON.stringify('corner'))
    localStorage.setItem(LEGACY_WIDGET_FULLSCREEN_KEY, 'false')

    const store = freshStore()

    expect(store.widgetFullscreenPlacement).toBe('hidden')
    expect(localStorage.getItem(WIDGET_FULLSCREEN_PLACEMENT_KEY)).toBe(JSON.stringify('hidden'))
  })
})

describe('默认值', () => {
  it('语言默认跟随浏览器（测试环境固定为 zh-CN），时区默认 Asia/Shanghai 不跟随浏览器', () => {
    const store = freshStore()

    expect(store.locale).toBe(DEFAULT_LOCALE)
    expect(store.appTimezone).toBe('Asia/Shanghai')
  })

  it('五个后端同步开关默认全部开启', () => {
    const store = freshStore()

    expect(store.preferenceSyncEnabled).toBe(true)
    expect(store.favoritesSyncEnabled).toBe(true)
    expect(store.searchSyncEnabled).toBe(true)
    expect(store.tableSyncEnabled).toBe(true)
    expect(store.widgetsSyncEnabled).toBe(true)
  })

  it('动态标题、行悬停速览、更新检查默认开启，检查间隔 30', () => {
    const store = freshStore()

    expect(store.dynamicTitle).toBe(true)
    expect(store.tableRowPeek).toBe(true)
    expect(store.enableCheckUpdates).toBe(true)
    expect(store.checkUpdatesInterval).toBe(30)
  })

  it('全部 Widget 默认显示，五个工具与偏好入口的位置默认 auto', () => {
    const store = freshStore()

    expect(store.widgetSearchPlacement).toBe('auto')
    expect(store.widgetThemePlacement).toBe('auto')
    expect(store.widgetLanguagePlacement).toBe('auto')
    expect(store.widgetTimezonePlacement).toBe('auto')
    expect(store.widgetFullscreenPlacement).toBe('auto')
    expect(store.widgetNotification).toBe(true)
    expect(store.widgetLockScreen).toBe(true)
    expect(store.widgetSidebarToggle).toBe(true)
    expect(store.widgetRefresh).toBe(true)
    expect(store.widgetFavorites).toBe(true)
    expect(store.widgetDynamicIsland).toBe(true)
    expect(store.notifySound).toBe(true)
    expect(store.widgetPreferencePosition).toBe('auto')
  })

  it('页脚与版权默认：全部开启，署名 XiHan / 2016 起，ICP 为空', () => {
    const store = freshStore()

    expect(store.footerEnable).toBe(true)
    expect(store.footerFixed).toBe(true)
    expect(store.footerShowDevInfo).toBe(true)
    expect(store.copyrightEnable).toBe(true)
    expect(store.copyrightName).toBe('XiHan')
    expect(store.copyrightSite).toBe('https://www.xihanfun.com')
    expect(store.copyrightDate).toBe('2016')
    expect(store.copyrightIcp).toBe('')
    expect(store.copyrightIcpUrl).toBe('')
  })

  it('快捷键总开关与四个子项默认开启', () => {
    const store = freshStore()

    expect(store.shortcutEnable).toBe(true)
    expect(store.shortcutSearch).toBe(true)
    expect(store.shortcutLogout).toBe(true)
    expect(store.shortcutLock).toBe(true)
    expect(store.shortcutTabOverview).toBe(true)
  })
})

describe('本地还原', () => {
  it('已保存的语言与时区在初始化时读回', () => {
    localStorage.setItem(LOCALE_KEY, JSON.stringify('en-US'))
    localStorage.setItem(APP_TIMEZONE_KEY, JSON.stringify('UTC'))

    const store = freshStore()

    expect(store.locale).toBe('en-US')
    expect(store.appTimezone).toBe('UTC')
  })

  it('未保存语言时跟随浏览器语言，且不把偵测结果写回本地存储', () => {
    Object.defineProperty(window.navigator, 'languages', { configurable: true, get: () => ['ja'] })
    try {
      const store = freshStore()

      expect(store.locale).toBe('ja-JP')
      expect(localStorage.getItem(LOCALE_KEY)).toBeNull()
    }
    finally {
      Object.defineProperty(window.navigator, 'languages', { configurable: true, get: () => ['zh-CN'] })
    }
  })

  it('同步开关存的 false 会被正确读回（不被 ?? 当成缺省）', () => {
    localStorage.setItem(PREFERENCE_SYNC_KEY, 'false')

    expect(freshStore().preferenceSyncEnabled).toBe(false)
  })

  it('时区存空串表示跟随浏览器，空串必须被保留而不是回落默认', () => {
    localStorage.setItem(APP_TIMEZONE_KEY, '""')

    expect(freshStore().appTimezone).toBe('')
  })
})

describe('locale 是 vue-i18n 的唯一入口', () => {
  it('setLocale 之后 vue-i18n 当前语言随之切换', async () => {
    const store = freshStore()

    store.setLocale('en-US')
    await nextTick()

    expect(i18n.global.locale.value).toBe('en-US')
  })

  it('直接改 locale ref（模拟远端推送覆盖）同样能带动 vue-i18n', async () => {
    const store = freshStore()

    store.locale = 'en-US'
    await nextTick()

    expect(i18n.global.locale.value).toBe('en-US')
  })

  it('设置成空串时不改动 vue-i18n（无效语言不生效）', async () => {
    const store = freshStore()
    store.setLocale('en-US')
    await nextTick()

    store.setLocale('')
    await nextTick()

    expect(i18n.global.locale.value).toBe('en-US')
  })

  it('store 初始化时立即把已保存语言同步给 vue-i18n（immediate）', () => {
    localStorage.setItem(LOCALE_KEY, JSON.stringify('en-US'))

    freshStore()

    expect(i18n.global.locale.value).toBe('en-US')
  })
})

describe('重置偏好', () => {
  it('语言重置为浏览器语言，而不是固定回退 DEFAULT_LOCALE', () => {
    Object.defineProperty(window.navigator, 'languages', { configurable: true, get: () => ['de-DE'] })
    try {
      const store = freshStore()
      store.setLocale('ja-JP')

      store.resetPreferences()

      expect(store.locale).toBe('de-DE')
    }
    finally {
      Object.defineProperty(window.navigator, 'languages', { configurable: true, get: () => ['zh-CN'] })
    }
  })
})

describe('setter 落地', () => {
  it('五个同步开关可逐个关闭并落地本地', () => {
    const store = freshStore()

    store.setPreferenceSyncEnabled(false)
    store.setFavoritesSyncEnabled(false)
    store.setSearchSyncEnabled(false)
    store.setTableSyncEnabled(false)
    store.setWidgetsSyncEnabled(false)

    expect(store.preferenceSyncEnabled).toBe(false)
    expect(store.favoritesSyncEnabled).toBe(false)
    expect(store.searchSyncEnabled).toBe(false)
    expect(store.tableSyncEnabled).toBe(false)
    expect(store.widgetsSyncEnabled).toBe(false)
  })

  it('更新检查间隔不做范围校验，0 与负数原样写入', () => {
    const store = freshStore()

    store.setCheckUpdatesInterval(0)
    expect(store.checkUpdatesInterval).toBe(0)

    store.setCheckUpdatesInterval(-10)
    expect(localStorage.getItem(CHECK_UPDATES_INTERVAL_KEY)).toBe('-10')
  })

  it('各个 Widget 显隐逐项可关', () => {
    const store = freshStore()

    store.setWidgetNotification(false)
    store.setWidgetLockScreen(false)
    store.setWidgetSidebarToggle(false)
    store.setWidgetRefresh(false)
    store.setWidgetFavorites(false)
    store.setWidgetDynamicIsland(false)
    store.setNotifySound(false)

    expect([
      store.widgetNotification,
      store.widgetLockScreen,
      store.widgetSidebarToggle,
      store.widgetRefresh,
      store.widgetFavorites,
      store.widgetDynamicIsland,
      store.notifySound,
    ]).toEqual(Array.from<boolean>({ length: 7 }).fill(false))
  })

  it('五个工具的位置逐项可设，含隐藏', () => {
    const store = freshStore()

    store.setWidgetSearchPlacement('floating')
    store.setWidgetThemePlacement('hidden')
    store.setWidgetLanguagePlacement('header')
    store.setWidgetTimezonePlacement('hidden')
    store.setWidgetFullscreenPlacement('floating')

    expect(store.widgetSearchPlacement).toBe('floating')
    expect(store.widgetThemePlacement).toBe('hidden')
    expect(store.widgetLanguagePlacement).toBe('header')
    expect(store.widgetTimezonePlacement).toBe('hidden')
    expect(store.widgetFullscreenPlacement).toBe('floating')
    expect(localStorage.getItem(WIDGET_SEARCH_PLACEMENT_KEY)).toBe(JSON.stringify('floating'))
    expect(localStorage.getItem(WIDGET_THEME_PLACEMENT_KEY)).toBe(JSON.stringify('hidden'))
  })

  it('偏好入口位置可切到固定角落', () => {
    const store = freshStore()

    store.setWidgetPreferencePosition('header')

    expect(store.widgetPreferencePosition).toBe('header')
    expect(localStorage.getItem(WIDGET_PREFERENCE_POSITION_KEY)).toBe(JSON.stringify('header'))
  })

  it('页脚与版权文案可自定义，含中文与备案链接', () => {
    const store = freshStore()

    store.setFooterEnable(false)
    store.setFooterFixed(false)
    store.setFooterShowDevInfo(false)
    store.setCopyrightEnable(false)
    store.setCopyrightName('曦寒科技')
    store.setCopyrightSite('https://example.com')
    store.setCopyrightDate('2020')
    store.setCopyrightIcp('京ICP备00000000号')
    store.setCopyrightIcpUrl('https://beian.miit.gov.cn')

    expect(store.footerEnable).toBe(false)
    expect(store.footerFixed).toBe(false)
    expect(store.footerShowDevInfo).toBe(false)
    expect(store.copyrightEnable).toBe(false)
    expect(store.copyrightName).toBe('曦寒科技')
    expect(store.copyrightSite).toBe('https://example.com')
    expect(store.copyrightDate).toBe('2020')
    expect(store.copyrightIcp).toBe('京ICP备00000000号')
    expect(store.copyrightIcpUrl).toBe('https://beian.miit.gov.cn')
  })

  it('快捷键总开关与四个子项互相独立', () => {
    const store = freshStore()

    store.setShortcutSearch(false)

    expect(store.shortcutEnable).toBe(true)
    expect(store.shortcutSearch).toBe(false)
    expect(store.shortcutLogout).toBe(true)
    expect(store.shortcutLock).toBe(true)
    expect(store.shortcutTabOverview).toBe(true)
  })

  it('动态标题、行悬停速览、更新检查可关闭', () => {
    const store = freshStore()

    store.setDynamicTitle(false)
    store.setTableRowPeek(false)
    store.setEnableCheckUpdates(false)

    expect(store.dynamicTitle).toBe(false)
    expect(store.tableRowPeek).toBe(false)
    expect(store.enableCheckUpdates).toBe(false)
  })

  it('时区可切换到任意 IANA 名，空串表示跟随浏览器', () => {
    const store = freshStore()

    store.setAppTimezone('America/New_York')
    expect(store.appTimezone).toBe('America/New_York')

    store.setAppTimezone('')
    expect(store.appTimezone).toBe('')
    expect(localStorage.getItem(APP_TIMEZONE_KEY)).toBe('""')
  })
})
