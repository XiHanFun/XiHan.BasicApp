/**
 * 应用 Store 门面（app）单元测试。
 * 职责边界：只测「组合门面」这一层职责——三个 slice 的成员必须全部对外暴露、
 * resetPreferences 覆盖三个 slice、草稿 API 与 helpers 的模块级实现是同一套。
 * 各 slice 自身的默认值与 setter 见 app/theme|layout|preferences 的用例。
 */
import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import {
  DEFAULT_LAYOUT_MODE,
  DEFAULT_LOCALE,
  DEFAULT_THEME,
  DEFAULT_THEME_COLOR,
  THEME_COLOR_KEY,
} from '~/constants'
import { useAppStore } from './app'
import { registerAppContext } from './app-context'
import { discardPreferenceDraft, preferenceDraftDirty, resetPreferenceBackendSync } from './helpers'

const saveApi = vi.fn().mockResolvedValue({ scene: 0, settingKey: 'global' })

function freshStore(): ReturnType<typeof useAppStore> {
  setActivePinia(createPinia())
  return useAppStore()
}

beforeEach(() => {
  resetPreferenceBackendSync()
  discardPreferenceDraft()
  saveApi.mockClear()
  registerAppContext({
    apis: { userSettingApi: { get: vi.fn(), save: saveApi } } as unknown as AppContextApis,
  })
  setActivePinia(createPinia())
})

describe('三个 slice 都被合进门面', () => {
  it('主题 / 布局 / 通用偏好三类状态都能从同一个 store 读到', () => {
    const store = freshStore()

    expect(store.themeMode).toBe(DEFAULT_THEME)
    expect(store.layoutMode).toBe(DEFAULT_LAYOUT_MODE)
    expect(store.locale).toBe(DEFAULT_LOCALE)
  })

  it('三类 setter 都能从同一个 store 调用', () => {
    const store = freshStore()

    store.setTheme('dark')
    store.setSidebarCollapsed(true)
    store.setSearchEnabled(false)

    expect(store.themeMode).toBe('dark')
    expect(store.sidebarCollapsed).toBe(true)
    expect(store.searchEnabled).toBe(false)
  })

  it('派生 getter（isDark）经门面透出', () => {
    const store = freshStore()

    store.setTheme('dark')

    expect(store.isDark).toBe(true)
  })
})

describe('resetPreferences 覆盖三个 slice', () => {
  it('主题、布局、通用偏好一次性还原为默认值', async () => {
    const store = freshStore()
    store.setThemeColor('#010203')
    store.setLayoutMode('top')
    store.setSearchEnabled(false)
    await nextTick()

    store.resetPreferences()
    await nextTick()

    expect(store.themeColor).toBe(DEFAULT_THEME_COLOR)
    expect(store.layoutMode).toBe(DEFAULT_LAYOUT_MODE)
    expect(store.searchEnabled).toBe(true)
  })

  it('还原后的默认值经 watch 落地 localStorage', async () => {
    const store = freshStore()
    store.setThemeColor('#010203')
    await nextTick()

    store.resetPreferences()
    await nextTick()

    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify(DEFAULT_THEME_COLOR))
  })

  it('resetPreferences 不动 pageLoading 这类纯内存状态', async () => {
    const store = freshStore()
    store.setPageLoading(true)

    store.resetPreferences()
    await nextTick()

    expect(store.pageLoading).toBe(true)
  })
})

describe('草稿 API 由门面透出且与 helpers 共享同一份状态', () => {
  it('经门面进入草稿后，偏好变更只预览不落地', async () => {
    const store = freshStore()

    store.beginPreferenceDraft()
    store.setThemeColor('#abcabc')
    await nextTick()

    expect(store.themeColor).toBe('#abcabc')
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBeNull()
  })

  it('门面上的 preferenceDraftDirty 与 helpers 导出的是同一个 ref', async () => {
    const store = freshStore()

    store.beginPreferenceDraft()
    store.setThemeColor('#abcabc')
    await nextTick()

    expect(store.preferenceDraftDirty).toBe(true)
    expect(preferenceDraftDirty.value).toBe(true)
  })

  it('经门面保存草稿后值落地本地', async () => {
    const store = freshStore()
    store.beginPreferenceDraft()
    store.setThemeColor('#abcabc')
    await nextTick()

    store.commitPreferenceDraft()

    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify('#abcabc'))
  })

  it('经门面放弃草稿后值还原', async () => {
    const store = freshStore()
    store.beginPreferenceDraft()
    store.setThemeColor('#abcabc')
    await nextTick()

    store.discardPreferenceDraft()
    await nextTick()

    expect(store.themeColor).toBe(DEFAULT_THEME_COLOR)
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBeNull()
  })

  it('还原动作本身又把 dirty 顶回 true（还原值经 watch 在暂停期被当成一次草稿变更）', async () => {
    const store = freshStore()
    store.beginPreferenceDraft()
    store.setThemeColor('#abcabc')
    await nextTick()

    store.discardPreferenceDraft()
    await nextTick()

    // 下一次打开抽屉时 beginPreferenceDraft 会把它重新归零，所以界面上看不出来
    expect(store.preferenceDraftDirty).toBe(true)
    store.beginPreferenceDraft()
    expect(store.preferenceDraftDirty).toBe(false)
  })
})
