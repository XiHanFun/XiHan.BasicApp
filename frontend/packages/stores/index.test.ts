/**
 * Stores 桶文件（index / app/index / modules/index / plugins/index）出口契约测试。
 * 职责边界：只验证「对外出口没有漏导出」——消费者一律从 ~/stores 取用，
 * 漏掉一个 re-export 就会在业务侧变成运行时 undefined。
 * 各单元的行为在各自的用例文件里覆盖。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import * as appBarrel from './app/index'
import * as barrel from './index'
import * as modulesBarrel from './modules/index'
import * as pluginsBarrel from './plugins/index'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('~/stores 顶层出口', () => {
  it('每个 store 工厂都能从桶文件取到并建出带正确 id 的实例', () => {
    expect(barrel.useAccessStore().$id).toBe('access')
    expect(barrel.useAppStore().$id).toBe('app')
    expect(barrel.useAuthStore().$id).toBe('auth')
    expect(barrel.useUserStore().$id).toBe('user')
    expect(barrel.useTabbarStore().$id).toBe('tabbar')
    expect(barrel.useFavoritesStore().$id).toBe('favorites')
    expect(barrel.useSplitViewStore().$id).toBe('split-view')
    expect(barrel.useNotificationStore().$id).toBe('notification')
  })

  it('偏好内核与上下文注册函数经桶文件透出，且与直接导入的是同一实现', async () => {
    const helpers = await import('./helpers')
    const context = await import('./app-context')

    expect(barrel.bindPersist).toBe(helpers.bindPersist)
    expect(barrel.hydratePreferencesFromBackend).toBe(helpers.hydratePreferencesFromBackend)
    expect(barrel.applyRemotePreferenceSnapshot).toBe(helpers.applyRemotePreferenceSnapshot)
    expect(barrel.registerAppContext).toBe(context.registerAppContext)
    expect(barrel.useAppContext).toBe(context.useAppContext)
  })

  it('壳层扩展注册点与 store id 枚举经桶文件透出', () => {
    expect(barrel.useShellExtensions()).toEqual([])
    expect(barrel.SetupStoreId.App).toBe('app')
  })
})

describe('子桶文件出口', () => {
  it('app/index 导出的三个切片工厂各自能建出可用状态', () => {
    const theme = appBarrel.createThemeSlice()
    const layout = appBarrel.createLayoutSlice()
    const preferences = appBarrel.createPreferencesSlice()

    expect(theme.themeMode.value).toBe('light')
    expect(layout.sidebarWidth.value).toBe(224)
    expect(preferences.locale.value).toBe('zh-CN')
  })

  it('modules/index 透出布局与标签栏相关的四个入口', () => {
    expect(modulesBarrel.useLayoutBridgeStore().$id).toBe('layout-bridge')
    expect(modulesBarrel.useLayoutStateStore().$id).toBe('layout-state')
    expect(Object.keys(modulesBarrel.useLayoutPreferences())).toHaveLength(9)
    expect(Object.keys(modulesBarrel.useTabbarPreferences())).toHaveLength(8)
  })

  it('plugins/index 透出的重置插件是可注册的 pinia 插件工厂', () => {
    const plugin = pluginsBarrel.resetSetupStorePlugin()

    expect(typeof plugin).toBe('function')
    expect(plugin.length).toBe(1)
  })
})
