/**
 * Store ID 契约测试。
 * 职责边界：SetupStoreId 是全站 Pinia store 的唯一命名来源，
 * 重复取值会让两个 store 在同一个 pinia 实例里互相覆盖，这里锁住「取值唯一」与
 * 「枚举里登记的 id 与实际 defineStore 使用的 id 一致」。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAccessStore } from './access'
import { useAppStore } from './app'
import { useFavoritesStore } from './favorites'
import { useLayoutBridgeStore } from './modules/layout-bridge'
import { useLayoutStateStore } from './modules/layout-state'
import { useNotificationStore } from './notification'
import { useSplitViewStore } from './split-view'
import { SetupStoreId } from './store-ids'
import { useTabbarStore } from './tabbar'
import { useUserStore } from './user'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('枚举取值本身', () => {
  it('所有 store id 取值互不重复', () => {
    const values = Object.values(SetupStoreId)

    expect(new Set(values).size).toBe(values.length)
  })

  it('所有 store id 均为非空的 kebab-case / 小写字符串', () => {
    for (const value of Object.values(SetupStoreId)) {
      expect(value).toMatch(/^[a-z][a-z-]*$/)
    }
  })

  it('枚举键名与取值一一对应，没有重复的键名映射', () => {
    expect(Object.keys(SetupStoreId)).toEqual([
      'App',
      'Access',
      'Auth',
      'User',
      'Tabbar',
      'Favorites',
      'SplitView',
      'LayoutState',
      'LayoutBridge',
      'LayoutPreferences',
      'Notification',
      'TabbarPreferences',
    ])
  })
})

describe('实际 store 的 $id 与枚举登记一致', () => {
  it('各 store 实例的 $id 与 SetupStoreId 对应项相同', () => {
    expect(useAppStore().$id).toBe(SetupStoreId.App)
    expect(useAccessStore().$id).toBe(SetupStoreId.Access)
    expect(useUserStore().$id).toBe(SetupStoreId.User)
    expect(useTabbarStore().$id).toBe(SetupStoreId.Tabbar)
    expect(useFavoritesStore().$id).toBe(SetupStoreId.Favorites)
    expect(useSplitViewStore().$id).toBe(SetupStoreId.SplitView)
    expect(useLayoutStateStore().$id).toBe(SetupStoreId.LayoutState)
    expect(useLayoutBridgeStore().$id).toBe(SetupStoreId.LayoutBridge)
    expect(useNotificationStore().$id).toBe(SetupStoreId.Notification)
  })

  it('同一个 pinia 实例里各 store 互相独立，不因 id 冲突而共享状态', () => {
    const ids = new Set([
      useAppStore().$id,
      useAccessStore().$id,
      useUserStore().$id,
      useTabbarStore().$id,
      useFavoritesStore().$id,
      useSplitViewStore().$id,
      useLayoutStateStore().$id,
      useLayoutBridgeStore().$id,
      useNotificationStore().$id,
    ])

    expect(ids.size).toBe(9)
  })
})
