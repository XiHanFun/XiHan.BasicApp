/**
 * 通用偏好切片（app/preferences）单元测试。
 * 职责边界：语言、时区、五个后端同步开关（默认开启、设备本地维度）、Widget 显隐、
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
  LOCALE_KEY,
  PREFERENCE_SYNC_KEY,
  WIDGET_PREFERENCE_POSITION_KEY,
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

describe('默认值', () => {
  it('语言与时区默认：zh-CN + Asia/Shanghai（默认北京时间，不跟随浏览器）', () => {
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

  it('搜索、动态标题、行悬停速览、更新检查默认开启，检查间隔 30', () => {
    const store = freshStore()

    expect(store.searchEnabled).toBe(true)
    expect(store.dynamicTitle).toBe(true)
    expect(store.tableRowPeek).toBe(true)
    expect(store.enableCheckUpdates).toBe(true)
    expect(store.checkUpdatesInterval).toBe(30)
  })

  it('全部 Widget 默认显示，偏好入口位置默认 auto', () => {
    const store = freshStore()

    expect(store.widgetThemeToggle).toBe(true)
    expect(store.widgetLanguageToggle).toBe(true)
    expect(store.widgetTimezone).toBe(true)
    expect(store.widgetFullscreen).toBe(true)
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

    store.setWidgetThemeToggle(false)
    store.setWidgetLanguageToggle(false)
    store.setWidgetTimezone(false)
    store.setWidgetFullscreen(false)
    store.setWidgetNotification(false)
    store.setWidgetLockScreen(false)
    store.setWidgetSidebarToggle(false)
    store.setWidgetRefresh(false)
    store.setWidgetFavorites(false)
    store.setWidgetDynamicIsland(false)
    store.setNotifySound(false)

    expect([
      store.widgetThemeToggle,
      store.widgetLanguageToggle,
      store.widgetTimezone,
      store.widgetFullscreen,
      store.widgetNotification,
      store.widgetLockScreen,
      store.widgetSidebarToggle,
      store.widgetRefresh,
      store.widgetFavorites,
      store.widgetDynamicIsland,
      store.notifySound,
    ]).toEqual(Array.from<boolean>({ length: 11 }).fill(false))
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

  it('搜索、动态标题、行悬停速览、更新检查可关闭', () => {
    const store = freshStore()

    store.setSearchEnabled(false)
    store.setDynamicTitle(false)
    store.setTableRowPeek(false)
    store.setEnableCheckUpdates(false)

    expect(store.searchEnabled).toBe(false)
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
