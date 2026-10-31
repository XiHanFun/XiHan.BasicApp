/**
 * 偏好持久化与跨端同步内核（helpers）单元测试。
 * 职责边界：偏好注册表、localStorage 落地、后端防抖上行的三道门（草稿 / 会话回写 / 用户开关）、
 * 草稿模式的预览-保存-还原三态、登录水合与远端推送的应用规则，以及构建时间失效清缓存。
 * 后端 API 经 AppContext 注入替身，不发真实请求；灵动岛提示不做断言。
 */
import type { AppContextApis } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick, ref } from 'vue'
import {
  DEFAULT_THEME_COLOR,
  FAVORITES_SYNC_KEY,
  PREFERENCE_SETTING_KEY,
  PREFERENCE_SYNC_KEY,
  SEARCH_SYNC_KEY,
  STORAGE_PREFIX,
  TABLE_SYNC_KEY,
  THEME_COLOR_KEY,
  THEME_MODE_KEY,
  UI_RADIUS_KEY,
  USER_SETTING_CLIENT_ID,
  UserSettingScene,
  WIDGETS_SYNC_KEY,
} from '~/constants'
import { useAppStore } from './app'
import { registerAppContext } from './app-context'
import {
  applyRemotePreferenceSnapshot,
  beginPreferenceDraft,
  bindPersist,
  commitPreferenceDraft,
  discardPreferenceDraft,
  hydratePreferencesFromBackend,
  invalidateCacheIfBuildTimeChanged,
  isFavoritesSyncEnabled,
  isPreferenceSyncEnabled,
  isSearchSyncEnabled,
  isTableSyncEnabled,
  isWidgetsSyncEnabled,
  preferenceDraftDirty,
  resetPreferenceBackendSync,
  resetRegisteredPreferences,
  save,
  setPendingPreferenceOrigin,
} from './helpers'

interface SettingDto { scene: number, settingKey: string, settingValue?: null | string }

const getApi = vi.fn<(input: { scene: number, settingKey: string }) => Promise<SettingDto>>()
const saveApi = vi.fn<(input: { scene: number, settingKey: string, settingValue?: null | string, origin?: null | string }) => Promise<SettingDto>>()

const BUILD_TIME_KEY = `${STORAGE_PREFIX}build_time`

/** 偏好注册表是模块级的，每个用例重建 pinia + appStore 让它重新指向新 store 的 ref */
function freshAppStore(): ReturnType<typeof useAppStore> {
  setActivePinia(createPinia())
  return useAppStore()
}

function lastSavedSnapshot(): Record<string, unknown> {
  const payload = saveApi.mock.calls.at(-1)?.[0]
  return JSON.parse(String(payload?.settingValue)) as Record<string, unknown>
}

beforeEach(() => {
  // 模块级同步门 / 草稿态在用例之间必须复位，否则测试顺序会影响结果
  resetPreferenceBackendSync()
  discardPreferenceDraft()
  setPendingPreferenceOrigin(null)
  getApi.mockReset()
  saveApi.mockReset()
  getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: null })
  saveApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY })
  registerAppContext({
    apis: { userSettingApi: { get: getApi, save: saveApi } } as unknown as AppContextApis,
  })
})

afterEach(() => {
  vi.useRealTimers()
})

describe('构建时间变化时作废本地偏好缓存', () => {
  it('构建时间与本地记录不同时清掉全部带前缀的键，并记下新构建时间', () => {
    localStorage.setItem(BUILD_TIME_KEY, JSON.stringify('远古版本'))
    localStorage.setItem(`${STORAGE_PREFIX}theme_color`, '"#000000"')
    localStorage.setItem(`${STORAGE_PREFIX}whatever`, '1')

    invalidateCacheIfBuildTimeChanged()

    expect(localStorage.getItem(`${STORAGE_PREFIX}theme_color`)).toBeNull()
    expect(localStorage.getItem(`${STORAGE_PREFIX}whatever`)).toBeNull()
    expect(localStorage.getItem(BUILD_TIME_KEY)).not.toBe(JSON.stringify('远古版本'))
  })

  it('不带前缀的第三方键不受影响', () => {
    localStorage.setItem(BUILD_TIME_KEY, JSON.stringify('远古版本'))
    localStorage.setItem('other_app_key', 'keep-me')

    invalidateCacheIfBuildTimeChanged()

    expect(localStorage.getItem('other_app_key')).toBe('keep-me')
  })

  it('构建时间未变时不清任何缓存（第二次调用是空操作）', () => {
    invalidateCacheIfBuildTimeChanged()
    localStorage.setItem(`${STORAGE_PREFIX}theme_color`, '"#000000"')

    invalidateCacheIfBuildTimeChanged()

    expect(localStorage.getItem(`${STORAGE_PREFIX}theme_color`)).toBe('"#000000"')
  })

  it('构建时间键自身不会被它清掉，否则每次启动都会误判为「变了」', () => {
    localStorage.setItem(BUILD_TIME_KEY, JSON.stringify('远古版本'))

    invalidateCacheIfBuildTimeChanged()

    expect(localStorage.getItem(BUILD_TIME_KEY)).not.toBeNull()
  })
})

describe('五个同步开关：默认开启且属设备本地维度', () => {
  it('新装用户五个开关默认全部开启', () => {
    freshAppStore()

    expect(isPreferenceSyncEnabled()).toBe(true)
    expect(isFavoritesSyncEnabled()).toBe(true)
    expect(isSearchSyncEnabled()).toBe(true)
    expect(isTableSyncEnabled()).toBe(true)
    expect(isWidgetsSyncEnabled()).toBe(true)
  })

  it('每个开关各自独立关闭，互不牵连', () => {
    const appStore = freshAppStore()

    appStore.setFavoritesSyncEnabled(false)

    expect(isFavoritesSyncEnabled()).toBe(false)
    expect(isPreferenceSyncEnabled()).toBe(true)
    expect(isSearchSyncEnabled()).toBe(true)
    expect(isTableSyncEnabled()).toBe(true)
    expect(isWidgetsSyncEnabled()).toBe(true)
  })

  it('开关判定是严格 true 比较：注册表里存的是非布尔值时视为关闭', () => {
    freshAppStore()

    bindPersist(SEARCH_SYNC_KEY, { value: 'true' })

    expect(isSearchSyncEnabled()).toBe(false)
  })

  it('五个开关不进入上行快照 —— 它们只属于本设备', async () => {
    vi.useFakeTimers()
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })

    appStore.setThemeColor('#123456')
    await vi.advanceTimersByTimeAsync(800)

    const snapshot = lastSavedSnapshot()
    expect(snapshot[THEME_COLOR_KEY]).toBe('#123456')
    for (const key of [PREFERENCE_SYNC_KEY, FAVORITES_SYNC_KEY, SEARCH_SYNC_KEY, TABLE_SYNC_KEY, WIDGETS_SYNC_KEY]) {
      expect(key in snapshot).toBe(false)
    }
  })
})

describe('bindPersist：变更落地 localStorage', () => {
  it('ref 变化后异步写入 localStorage', async () => {
    freshAppStore()
    const source = ref(1)
    bindPersist(`${STORAGE_PREFIX}test_bind`, source, 0)

    source.value = 2
    await nextTick()

    expect(localStorage.getItem(`${STORAGE_PREFIX}test_bind`)).toBe('2')
  })

  it('未传默认值的偏好不参与「重置为默认值」', () => {
    freshAppStore()
    const source = ref('x')
    bindPersist(`${STORAGE_PREFIX}test_nodefault`, source)

    source.value = 'y'
    resetRegisteredPreferences()

    expect(source.value).toBe('y')
  })

  it('重置偏好把登记了默认值的项还原，并经 watch 落地本地', async () => {
    const appStore = freshAppStore()
    appStore.setThemeColor('#010203')
    appStore.setSidebarCollapsed(true)
    appStore.setUiRadius(1)
    await nextTick()

    resetRegisteredPreferences()
    await nextTick()

    expect(appStore.themeColor).toBe(DEFAULT_THEME_COLOR)
    expect(appStore.sidebarCollapsed).toBe(false)
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify(DEFAULT_THEME_COLOR))
  })
})

describe('save：立即写入，草稿期只预览', () => {
  it('非草稿期立即写 localStorage', () => {
    const target = { value: 'a' }

    save(`${STORAGE_PREFIX}test_save`, target, 'b')

    expect(target.value).toBe('b')
    expect(localStorage.getItem(`${STORAGE_PREFIX}test_save`)).toBe('"b"')
  })

  it('草稿期只改内存不落地', () => {
    freshAppStore()
    const target = { value: 'a' }
    beginPreferenceDraft()

    save(`${STORAGE_PREFIX}test_save_draft`, target, 'b')

    expect(target.value).toBe('b')
    expect(localStorage.getItem(`${STORAGE_PREFIX}test_save_draft`)).toBeNull()
  })
})

describe('偏好草稿：预览 / 保存 / 还原', () => {
  it('进入草稿后改偏好只在内存生效，不写 localStorage', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()

    appStore.setThemeColor('#abcabc')
    await nextTick()

    expect(appStore.themeColor).toBe('#abcabc')
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBeNull()
  })

  it('草稿期发生变更后 dirty 置位，供「保存」按钮启用', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()
    expect(preferenceDraftDirty.value).toBe(false)

    appStore.setUiRadius(0.75)
    await nextTick()

    expect(preferenceDraftDirty.value).toBe(true)
  })

  it('保存草稿把内存值落地本地并复位 dirty', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()
    appStore.setThemeColor('#abcabc')
    await nextTick()

    commitPreferenceDraft()

    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify('#abcabc'))
    expect(preferenceDraftDirty.value).toBe(false)
  })

  it('保存草稿会立即上行后端（不走 800ms 防抖）', () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()
    appStore.setThemeColor('#abcabc')

    commitPreferenceDraft()

    expect(saveApi).toHaveBeenCalledTimes(1)
    expect(saveApi.mock.calls[0]?.[0]).toMatchObject({
      scene: UserSettingScene.Preference,
      settingKey: PREFERENCE_SETTING_KEY,
      clientId: USER_SETTING_CLIENT_ID,
    })
  })

  it('关掉偏好同步时保存草稿只落本地，不上行', () => {
    const appStore = freshAppStore()
    appStore.setPreferenceSyncEnabled(false)
    beginPreferenceDraft()
    appStore.setThemeColor('#abcabc')

    commitPreferenceDraft()

    expect(saveApi).not.toHaveBeenCalled()
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify('#abcabc'))
  })

  it('放弃草稿还原到进入草稿时的基线', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()
    appStore.setThemeColor('#abcabc')
    await nextTick()

    discardPreferenceDraft()
    await nextTick()

    expect(appStore.themeColor).toBe(DEFAULT_THEME_COLOR)
  })

  it('放弃草稿时的还原值不会被回写 localStorage —— 解除暂停要等 watch flush 之后', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()
    appStore.setThemeColor('#abcabc')
    await nextTick()

    discardPreferenceDraft()
    await nextTick()
    await nextTick()

    expect(localStorage.getItem(THEME_COLOR_KEY)).toBeNull()
  })

  it('保存过的变更不会被随后的放弃还原掉（commit 会更新基线）', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()
    appStore.setThemeColor('#abcabc')
    await nextTick()
    commitPreferenceDraft()

    discardPreferenceDraft()
    await nextTick()

    expect(appStore.themeColor).toBe('#abcabc')
  })

  it('没有任何变更就放弃草稿时同步解除暂停，后续变更正常落地', async () => {
    const appStore = freshAppStore()
    beginPreferenceDraft()

    discardPreferenceDraft()
    appStore.setThemeColor('#111111')
    await nextTick()

    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify('#111111'))
  })

  it('未进入草稿就调用放弃是空操作，也不会误置 dirty', () => {
    freshAppStore()

    discardPreferenceDraft()

    expect(preferenceDraftDirty.value).toBe(false)
  })

  it('未进入草稿就调用保存是空操作，不上行也不落地', () => {
    const appStore = freshAppStore()
    appStore.$state.themeColor = '#999999'

    commitPreferenceDraft()

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('草稿期不上行后端，哪怕会话回写门已经打开', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: '{}' })
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()
    beginPreferenceDraft()

    appStore.setThemeColor('#abcabc')
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })

  // 回归锚点（缺陷 11）：定时器回调原先只复查同步开关，进入草稿前排队的那一次上行会照常触发，
  // 用当前内存值构建整份快照，把未保存的草稿预览值落库并实时推给其它设备。
  it('进入草稿前排队的防抖上行到期时被草稿门拦下，草稿预览值不会上行', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: '{}' })
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()

    appStore.setThemeColor('#111111')
    await vi.advanceTimersByTimeAsync(400)
    beginPreferenceDraft()
    appStore.setUiRadius(0.99)
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })

  // 回归锚点（缺陷 11）：草稿期被拦下的上行不是丢数据——快照是全量的，保存草稿即整份补上
  it('草稿期被拦下后点保存，最新值仍会整份上行', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: '{}' })
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()

    appStore.setThemeColor('#111111')
    await vi.advanceTimersByTimeAsync(400)
    beginPreferenceDraft()
    appStore.setUiRadius(0.99)
    await vi.advanceTimersByTimeAsync(2000)
    commitPreferenceDraft()

    expect(saveApi).toHaveBeenCalledTimes(1)
    const snapshot = lastSavedSnapshot()
    expect(snapshot[THEME_COLOR_KEY]).toBe('#111111')
    expect(snapshot[UI_RADIUS_KEY]).toBe(0.99)
  })

  // 回归锚点（缺陷 11）：退出登录同样是一道门——排队中的上行不得在登出后打到后端
  it('排队期间退出登录（关闭会话回写门）后不再上行', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: '{}' })
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()

    appStore.setThemeColor('#111111')
    await vi.advanceTimersByTimeAsync(400)
    resetPreferenceBackendSync()
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })
})

describe('防抖上行的三道门', () => {
  it('未水合（会话回写门未开）时变更不上行', async () => {
    vi.useFakeTimers()
    const appStore = freshAppStore()

    appStore.setThemeColor('#123456')
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('水合后变更经 800ms 防抖上行一次，多次变更合并', async () => {
    vi.useFakeTimers()
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()

    appStore.setThemeColor('#111111')
    appStore.setThemeColor('#222222')
    appStore.setUiRadius(0.5)
    await vi.advanceTimersByTimeAsync(800)

    expect(saveApi).toHaveBeenCalledTimes(1)
    const snapshot = lastSavedSnapshot()
    expect(snapshot[THEME_COLOR_KEY]).toBe('#222222')
    expect(snapshot[UI_RADIUS_KEY]).toBe(0.5)
  })

  it('防抖窗口内关掉偏好同步则最终不上行', async () => {
    vi.useFakeTimers()
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()

    appStore.setThemeColor('#111111')
    appStore.setPreferenceSyncEnabled(false)
    await vi.advanceTimersByTimeAsync(800)

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('上行失败不抛出，本地值照常保留', async () => {
    vi.useFakeTimers()
    saveApi.mockRejectedValue(new Error('后端 500'))
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })

    appStore.setThemeColor('#111111')
    await vi.advanceTimersByTimeAsync(800)

    expect(appStore.themeColor).toBe('#111111')
    expect(localStorage.getItem(THEME_COLOR_KEY)).toBe(JSON.stringify('#111111'))
  })

  it('退出登录后关闭回写门并清掉待发的防抖请求', async () => {
    vi.useFakeTimers()
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()
    appStore.setThemeColor('#111111')

    resetPreferenceBackendSync()
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })
})

describe('动画起点只跟随它那一次变更', () => {
  it('登记的起点随下一次上行携带', () => {
    freshAppStore()
    beginPreferenceDraft()
    setPendingPreferenceOrigin('50,50')

    commitPreferenceDraft()

    expect(saveApi.mock.calls[0]?.[0]?.origin).toBe('50,50')
  })

  it('上行一次后起点即清空，不会粘到后续变更上', () => {
    freshAppStore()
    beginPreferenceDraft()
    setPendingPreferenceOrigin('50,50')
    commitPreferenceDraft()

    commitPreferenceDraft()

    expect(saveApi.mock.calls[1]?.[0]?.origin).toBeNull()
  })

  it('传 null 表示无起点（键盘 / 程序触发）', () => {
    freshAppStore()
    beginPreferenceDraft()
    setPendingPreferenceOrigin(null)

    commitPreferenceDraft()

    expect(saveApi.mock.calls[0]?.[0]?.origin).toBeNull()
  })
})

describe('登录水合：拉取后端偏好覆盖本地', () => {
  it('远端有记录时按 key 覆盖已注册偏好', async () => {
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: PREFERENCE_SETTING_KEY,
      settingValue: JSON.stringify({ [THEME_COLOR_KEY]: '#ff0000', [UI_RADIUS_KEY]: 0.75 }),
    })
    const appStore = freshAppStore()

    await hydratePreferencesFromBackend({ showIsland: false })

    expect(appStore.themeColor).toBe('#ff0000')
    expect(appStore.uiRadius).toBe(0.75)
  })

  it('远端快照里的同步开关被忽略 —— 开关是设备本地维度', async () => {
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: PREFERENCE_SETTING_KEY,
      settingValue: JSON.stringify({ [PREFERENCE_SYNC_KEY]: false, [FAVORITES_SYNC_KEY]: false }),
    })
    const appStore = freshAppStore()

    await hydratePreferencesFromBackend({ showIsland: false })

    expect(appStore.preferenceSyncEnabled).toBe(true)
    expect(appStore.favoritesSyncEnabled).toBe(true)
  })

  it('远端快照里未出现的 key 保持本地值', async () => {
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: PREFERENCE_SETTING_KEY,
      settingValue: JSON.stringify({ [THEME_COLOR_KEY]: '#ff0000' }),
    })
    const appStore = freshAppStore()
    appStore.setUiRadius(0.9)

    await hydratePreferencesFromBackend({ showIsland: false })

    expect(appStore.uiRadius).toBe(0.9)
  })

  it('一次会话只水合一次，第二次调用不再请求后端', async () => {
    freshAppStore()

    await hydratePreferencesFromBackend({ showIsland: false })
    await hydratePreferencesFromBackend({ showIsland: false })

    expect(getApi).toHaveBeenCalledTimes(1)
  })

  it('退出登录复位后允许下次登录重新水合', async () => {
    freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })

    resetPreferenceBackendSync()
    await hydratePreferencesFromBackend({ showIsland: false })

    expect(getApi).toHaveBeenCalledTimes(2)
  })

  it('未开启偏好同步时不拉取后端，但仍打开回写门以便随后启用同步即时生效', async () => {
    vi.useFakeTimers()
    const appStore = freshAppStore()
    appStore.setPreferenceSyncEnabled(false)

    await hydratePreferencesFromBackend({ showIsland: false })
    appStore.setPreferenceSyncEnabled(true)
    appStore.setThemeColor('#123456')
    await vi.advanceTimersByTimeAsync(800)

    expect(getApi).not.toHaveBeenCalled()
    expect(saveApi).toHaveBeenCalledTimes(1)
  })

  it('后端无记录时以本地当前偏好播种（水合后自动上行一次）', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: null })
    freshAppStore()

    await hydratePreferencesFromBackend({ showIsland: false })
    await vi.advanceTimersByTimeAsync(800)

    expect(saveApi).toHaveBeenCalledTimes(1)
  })

  it('后端已有记录时不重复播种', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: PREFERENCE_SETTING_KEY,
      settingValue: JSON.stringify({ [THEME_COLOR_KEY]: '#ff0000' }),
    })
    freshAppStore()

    await hydratePreferencesFromBackend({ showIsland: false })
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('拉取失败时静默保留本地，并照常打开回写门', async () => {
    vi.useFakeTimers()
    getApi.mockRejectedValue(new Error('端点未就绪'))
    const appStore = freshAppStore()
    appStore.setThemeColor('#0f0f0f')

    await hydratePreferencesFromBackend({ showIsland: false })

    expect(appStore.themeColor).toBe('#0f0f0f')

    saveApi.mockClear()
    appStore.setUiRadius(0.4)
    await vi.advanceTimersByTimeAsync(800)
    expect(saveApi).toHaveBeenCalledTimes(1)
  })

  it('远端返回损坏 JSON 时静默保留本地', async () => {
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: '{坏数据' })
    const appStore = freshAppStore()
    appStore.setThemeColor('#0f0f0f')

    await hydratePreferencesFromBackend({ showIsland: false })

    expect(appStore.themeColor).toBe('#0f0f0f')
  })

  it('水合过程本身不会把刚拉下来的远端值原样回传', async () => {
    vi.useFakeTimers()
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: PREFERENCE_SETTING_KEY,
      settingValue: JSON.stringify({ [THEME_COLOR_KEY]: '#ff0000' }),
    })
    freshAppStore()

    await hydratePreferencesFromBackend({ showIsland: false })
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })
})

describe('远端推送：应用其它设备的偏好变更', () => {
  async function hydrated(): Promise<ReturnType<typeof useAppStore>> {
    // 让后端「已有记录」，避免播种上行的防抖计时器混进后续断言
    getApi.mockResolvedValue({ scene: 0, settingKey: PREFERENCE_SETTING_KEY, settingValue: '{}' })
    const appStore = freshAppStore()
    await hydratePreferencesFromBackend({ showIsland: false })
    saveApi.mockClear()
    return appStore
  }

  it('推送的偏好被应用到内存', async () => {
    const appStore = await hydrated()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [UI_RADIUS_KEY]: 0.6 }))

    expect(appStore.uiRadius).toBe(0.6)
  })

  it('推送里的同步开关被忽略 —— 设备本地维度不受远端覆盖', async () => {
    const appStore = await hydrated()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [WIDGETS_SYNC_KEY]: false }))

    expect(appStore.widgetsSyncEnabled).toBe(true)
  })

  it('明暗真的翻转时经主题过渡应用，值最终落地', async () => {
    const appStore = await hydrated()
    expect(appStore.themeMode).toBe('light')

    await applyRemotePreferenceSnapshot(JSON.stringify({ [THEME_MODE_KEY]: 'dark' }))

    expect(appStore.themeMode).toBe('dark')
  })

  it('明暗未变化时直接应用其余偏好', async () => {
    const appStore = await hydrated()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [THEME_MODE_KEY]: 'light', [UI_RADIUS_KEY]: 0.35 }))

    expect(appStore.themeMode).toBe('light')
    expect(appStore.uiRadius).toBe(0.35)
  })

  it('应用远端不会回环上行后端', async () => {
    vi.useFakeTimers()
    await hydrated()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [UI_RADIUS_KEY]: 0.6 }))
    await vi.advanceTimersByTimeAsync(2000)

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('应用完远端后回写门恢复，本机随后的变更照常上行', async () => {
    vi.useFakeTimers()
    const appStore = await hydrated()
    await applyRemotePreferenceSnapshot(JSON.stringify({ [UI_RADIUS_KEY]: 0.6 }))

    appStore.setThemeColor('#654321')
    await vi.advanceTimersByTimeAsync(800)

    expect(saveApi).toHaveBeenCalledTimes(1)
    expect(lastSavedSnapshot()[THEME_COLOR_KEY]).toBe('#654321')
  })

  it('settingValue 为空 / null / undefined 时忽略', async () => {
    const appStore = await hydrated()
    const before = appStore.uiRadius

    await applyRemotePreferenceSnapshot('')
    await applyRemotePreferenceSnapshot(null)
    await applyRemotePreferenceSnapshot(undefined)

    expect(appStore.uiRadius).toBe(before)
  })

  it('推送内容不是 JSON 时忽略，不抛异常', async () => {
    const appStore = await hydrated()
    const before = appStore.uiRadius

    await expect(applyRemotePreferenceSnapshot('{坏数据')).resolves.toBeUndefined()
    expect(appStore.uiRadius).toBe(before)
  })

  it('推送内容是 JSON 但不是对象时忽略', async () => {
    const appStore = await hydrated()
    const before = appStore.uiRadius

    await applyRemotePreferenceSnapshot('123')

    expect(appStore.uiRadius).toBe(before)
  })

  it('草稿模式（偏好抽屉打开）期间忽略远端推送，界面不跳变', async () => {
    const appStore = await hydrated()
    beginPreferenceDraft()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [UI_RADIUS_KEY]: 0.6 }))

    expect(appStore.uiRadius).not.toBe(0.6)
  })

  it('未水合（回写门未开）时忽略远端推送', async () => {
    const appStore = freshAppStore()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [UI_RADIUS_KEY]: 0.6 }))

    expect(appStore.uiRadius).not.toBe(0.6)
  })

  it('关闭偏好同步时忽略远端推送', async () => {
    const appStore = await hydrated()
    appStore.setPreferenceSyncEnabled(false)

    await applyRemotePreferenceSnapshot(JSON.stringify({ [UI_RADIUS_KEY]: 0.6 }))

    expect(appStore.uiRadius).not.toBe(0.6)
  })

  it('带上发起端动画起点（视口百分比）时同样能落地', async () => {
    const appStore = await hydrated()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [THEME_MODE_KEY]: 'dark' }), '25,75')

    expect(appStore.themeMode).toBe('dark')
  })

  it('起点是非法数值时退化为默认扩散，值仍然落地', async () => {
    const appStore = await hydrated()

    await applyRemotePreferenceSnapshot(JSON.stringify({ [THEME_MODE_KEY]: 'dark' }), 'abc,def')

    expect(appStore.themeMode).toBe('dark')
  })
})
