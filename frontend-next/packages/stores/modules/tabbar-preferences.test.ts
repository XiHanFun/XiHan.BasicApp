/**
 * 标签栏偏好组合式（tabbar-preferences）单元测试。
 * 职责边界：偏好抽屉里标签栏分区的 v-model 视图，八项可写 computed 直连 appStore setter。
 * 覆盖字段集合、读写双向、以及「关掉持久化开关会真正影响标签栏落地行为」这条联动。
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { TABBAR_MAX_COUNT_KEY, TABBAR_PERSIST_KEY, TABS_LIST_KEY } from '~/constants'
import { useAppStore } from '~/stores/app'
import { useTabbarStore } from '~/stores/tabbar'
import { useTabbarPreferences } from './tabbar-preferences'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('暴露的字段集合', () => {
  it('恰好八项标签栏偏好，顺序与偏好抽屉分区一致', () => {
    const preferences = useTabbarPreferences()

    expect(Object.keys(preferences)).toEqual([
      'tabbarEnabled',
      'tabbarPersist',
      'tabbarVisitHistory',
      'tabbarDraggable',
      'tabbarShowMore',
      'tabbarShowMaximize',
      'tabbarShowOverview',
      'tabbarMaxCount',
    ])
  })

  it('默认值：七个开关默认开启、最大数量默认 0（不限）', () => {
    const preferences = useTabbarPreferences()

    expect(preferences.tabbarEnabled.value).toBe(true)
    expect(preferences.tabbarPersist.value).toBe(true)
    expect(preferences.tabbarVisitHistory.value).toBe(true)
    expect(preferences.tabbarDraggable.value).toBe(true)
    expect(preferences.tabbarShowMore.value).toBe(true)
    expect(preferences.tabbarShowMaximize.value).toBe(true)
    expect(preferences.tabbarShowOverview.value).toBe(true)
    expect(preferences.tabbarMaxCount.value).toBe(0)
  })
})

describe('双向绑定', () => {
  it('每个开关写入后都落到 appStore', () => {
    const appStore = useAppStore()
    const preferences = useTabbarPreferences()

    preferences.tabbarEnabled.value = false
    preferences.tabbarVisitHistory.value = false
    preferences.tabbarDraggable.value = false
    preferences.tabbarShowMore.value = false
    preferences.tabbarShowMaximize.value = false
    preferences.tabbarShowOverview.value = false

    expect(appStore.tabbarEnabled).toBe(false)
    expect(appStore.tabbarVisitHistory).toBe(false)
    expect(appStore.tabbarDraggable).toBe(false)
    expect(appStore.tabbarShowMore).toBe(false)
    expect(appStore.tabbarShowMaximize).toBe(false)
    expect(appStore.tabbarShowOverview).toBe(false)
  })

  it('appStore 侧改动能被组合式读到', () => {
    const appStore = useAppStore()
    const preferences = useTabbarPreferences()

    appStore.setTabbarMaxCount(12)

    expect(preferences.tabbarMaxCount.value).toBe(12)
  })

  it('最大数量写入后落地 localStorage，且不做任何范围夹取', () => {
    const preferences = useTabbarPreferences()

    preferences.tabbarMaxCount.value = -5

    expect(preferences.tabbarMaxCount.value).toBe(-5)
    expect(localStorage.getItem(TABBAR_MAX_COUNT_KEY)).toBe('-5')
  })
})

describe('与标签栏 store 的联动', () => {
  it('关掉持久化开关后标签变更不再写会话缓存', () => {
    const preferences = useTabbarPreferences()
    const tabbar = useTabbarStore()

    preferences.tabbarPersist.value = false
    tabbar.ensureTab({ key: '/a', title: 'A', path: '/a', closable: true })

    expect(localStorage.getItem(TABBAR_PERSIST_KEY)).toBe('false')
    expect(sessionStorage.getItem(TABS_LIST_KEY)).toBeNull()
  })

  it('重新打开持久化开关后标签变更恢复落地', () => {
    const preferences = useTabbarPreferences()
    const tabbar = useTabbarStore()
    preferences.tabbarPersist.value = false
    tabbar.ensureTab({ key: '/a', title: 'A', path: '/a', closable: true })

    preferences.tabbarPersist.value = true
    tabbar.ensureTab({ key: '/b', title: 'B', path: '/b', closable: true })

    const persisted = JSON.parse(sessionStorage.getItem(TABS_LIST_KEY) ?? '[]') as Array<{ key: string }>
    expect(persisted.map(t => t.key)).toContain('/b')
  })
})
