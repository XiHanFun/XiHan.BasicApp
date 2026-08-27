/**
 * 收藏夹 Store（favorites）单元测试。
 * 职责边界：本地即时事实源（localStorage）+ 后端「尽力而为」跨端同步。
 * 覆盖增删改查去重、拖拽排序边界、防抖落库、同步开关门控、
 * hydrate 的 in-flight 去重与失败静默、远端推送覆盖与脏数据过滤。
 * 后端 API 经 AppContext 注入替身，不发真实请求。
 */
import type { AppContextApis, FavoriteItem } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { FAVORITES_KEY, FAVORITES_SETTING_KEY, USER_SETTING_CLIENT_ID, UserSettingScene } from '~/constants'
import { useAppStore } from './app'
import { registerAppContext } from './app-context'
import { useFavoritesStore } from './favorites'

interface SettingDto { scene: number, settingKey: string, settingValue?: null | string }

const getApi = vi.fn<(input: { scene: number, settingKey: string }) => Promise<SettingDto>>()
const saveApi = vi.fn<(input: { scene: number, settingKey: string, settingValue?: null | string }) => Promise<SettingDto>>()

function item(path: string, overrides?: Partial<FavoriteItem>): FavoriteItem {
  return { key: path, title: path, path, ...overrides }
}

function readLocal(): FavoriteItem[] | null {
  const raw = localStorage.getItem(FAVORITES_KEY)
  return raw ? (JSON.parse(raw) as FavoriteItem[]) : null
}

/** 每个用例都重建 pinia 并重新注册偏好（helpers 的偏好注册表是模块级的，必须刷新指向新 store 的 ref） */
function setupStores(): ReturnType<typeof useFavoritesStore> {
  setActivePinia(createPinia())
  useAppStore()
  return useFavoritesStore()
}

beforeEach(() => {
  getApi.mockReset()
  saveApi.mockReset()
  getApi.mockResolvedValue({ scene: 0, settingKey: FAVORITES_SETTING_KEY, settingValue: null })
  saveApi.mockResolvedValue({ scene: 0, settingKey: FAVORITES_SETTING_KEY })
  registerAppContext({
    apis: { userSettingApi: { get: getApi, save: saveApi } } as unknown as AppContextApis,
  })
})

afterEach(() => {
  vi.useRealTimers()
})

describe('初始状态与本地还原', () => {
  it('无本地缓存时收藏为空，count 为 0', () => {
    const store = setupStores()

    expect(store.favorites).toEqual([])
    expect(store.count).toBe(0)
  })

  it('store 初始化时从 localStorage 还原收藏列表', () => {
    localStorage.setItem(FAVORITES_KEY, JSON.stringify([item('/a'), item('/b')]))
    const store = setupStores()

    expect(store.favorites.map(x => x.path)).toEqual(['/a', '/b'])
    expect(store.count).toBe(2)
  })

  it('本地缓存是损坏 JSON 时降级为空列表，不抛异常', () => {
    localStorage.setItem(FAVORITES_KEY, '{损坏')
    const store = setupStores()

    expect(store.favorites).toEqual([])
  })
})

describe('新增、移除与切换', () => {
  it('新增成功返回 true，并把 key 强制对齐 path', () => {
    const store = setupStores()

    const added = store.add({ key: '与path不一致', title: '标题', path: '/a' })

    expect(added).toBe(true)
    expect(store.favorites[0]?.key).toBe('/a')
  })

  it('重复 path 新增返回 false 且列表不增长', () => {
    const store = setupStores()
    store.add(item('/a'))

    const added = store.add(item('/a', { title: '另一个标题' }))

    expect(added).toBe(false)
    expect(store.count).toBe(1)
    expect(store.favorites[0]?.title).toBe('/a')
  })

  it('path 为空串时拒绝新增', () => {
    const store = setupStores()

    expect(store.add({ key: '', title: 't', path: '' })).toBe(false)
    expect(store.count).toBe(0)
  })

  it('has 按 path 精确判定，不做前缀匹配', () => {
    const store = setupStores()
    store.add(item('/system/user'))

    expect(store.has('/system/user')).toBe(true)
    expect(store.has('/system')).toBe(false)
    expect(store.has('')).toBe(false)
  })

  it('remove 不存在的 path 时不改列表', () => {
    const store = setupStores()
    store.add(item('/a'))
    const before = store.favorites

    store.remove('/nope')

    expect(store.favorites).toBe(before)
  })

  it('remove 命中时按 path 移除', () => {
    const store = setupStores()
    store.add(item('/a'))
    store.add(item('/b'))

    store.remove('/a')

    expect(store.favorites.map(x => x.path)).toEqual(['/b'])
  })

  it('toggle 未收藏 → 收藏，返回 true', () => {
    const store = setupStores()

    expect(store.toggle(item('/a'))).toBe(true)
    expect(store.has('/a')).toBe(true)
  })

  it('toggle 已收藏 → 取消收藏，返回 false', () => {
    const store = setupStores()
    store.add(item('/a'))

    expect(store.toggle(item('/a'))).toBe(false)
    expect(store.has('/a')).toBe(false)
  })

  it('toggle 一个 path 为空的项返回 false（add 拒绝，也没有可移除的）', () => {
    const store = setupStores()

    expect(store.toggle({ key: '', title: 't', path: '' })).toBe(false)
  })

  it('clear 清空全部收藏；已经为空时不做任何事', () => {
    const store = setupStores()
    store.add(item('/a'))

    store.clear()
    const emptied = store.favorites
    store.clear()

    expect(store.favorites).toBe(emptied)
    expect(store.count).toBe(0)
  })

  it('中文与 emoji 路径按原样存取', () => {
    const store = setupStores()

    store.add(item('/系统/用户🚀'))

    expect(store.has('/系统/用户🚀')).toBe(true)
  })
})

describe('拖拽排序 move 的下标边界', () => {
  it('把末位移到首位', () => {
    const store = setupStores()
    store.add(item('/a'))
    store.add(item('/b'))
    store.add(item('/c'))

    store.move(2, 0)

    expect(store.favorites.map(x => x.path)).toEqual(['/c', '/a', '/b'])
  })

  it('把首位移到末位', () => {
    const store = setupStores()
    store.add(item('/a'))
    store.add(item('/b'))
    store.add(item('/c'))

    store.move(0, 2)

    expect(store.favorites.map(x => x.path)).toEqual(['/b', '/c', '/a'])
  })

  it('负数下标、越界下标、同下标一律不动', () => {
    const store = setupStores()
    store.add(item('/a'))
    store.add(item('/b'))
    const before = store.favorites

    store.move(-1, 0)
    store.move(0, -1)
    store.move(2, 0)
    store.move(0, 2)
    store.move(1, 1)

    expect(store.favorites).toBe(before)
  })

  it('空列表上的 move 不抛异常', () => {
    const store = setupStores()

    expect(() => store.move(0, 0)).not.toThrow()
    expect(store.favorites).toEqual([])
  })
})

describe('持久化：本地即时、后端防抖', () => {
  it('新增后立刻写入 localStorage（不等防抖）', () => {
    vi.useFakeTimers()
    const store = setupStores()

    store.add(item('/a'))

    expect(readLocal()?.map(x => x.path)).toEqual(['/a'])
    expect(saveApi).not.toHaveBeenCalled()
  })

  it('600ms 防抖后才上行后端，且只发一次（多次变更合并）', async () => {
    vi.useFakeTimers()
    const store = setupStores()

    store.add(item('/a'))
    store.add(item('/b'))
    store.add(item('/c'))
    await vi.advanceTimersByTimeAsync(600)

    expect(saveApi).toHaveBeenCalledTimes(1)
    const payload = saveApi.mock.calls[0]?.[0]
    expect(payload).toMatchObject({
      scene: UserSettingScene.Preference,
      settingKey: FAVORITES_SETTING_KEY,
      clientId: USER_SETTING_CLIENT_ID,
    })
    expect(JSON.parse(String(payload?.settingValue)).map((x: FavoriteItem) => x.path)).toEqual(['/a', '/b', '/c'])
  })

  it('防抖未到期就不会发出请求', async () => {
    vi.useFakeTimers()
    const store = setupStores()

    store.add(item('/a'))
    await vi.advanceTimersByTimeAsync(599)

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('上行失败不抛出、不阻塞交互，本地数据照常保留', async () => {
    vi.useFakeTimers()
    saveApi.mockRejectedValue(new Error('网络断了'))
    const store = setupStores()

    store.add(item('/a'))
    await vi.advanceTimersByTimeAsync(600)

    expect(store.has('/a')).toBe(true)
    expect(readLocal()?.map(x => x.path)).toEqual(['/a'])
  })

  it('关闭收藏夹同步后仍写本地，但不再上行后端', async () => {
    vi.useFakeTimers()
    setActivePinia(createPinia())
    useAppStore().setFavoritesSyncEnabled(false)
    const store = useFavoritesStore()

    store.add(item('/a'))
    await vi.advanceTimersByTimeAsync(1000)

    expect(readLocal()?.map(x => x.path)).toEqual(['/a'])
    expect(saveApi).not.toHaveBeenCalled()
  })
})

describe('hydrate：从后端拉取覆盖本地', () => {
  it('远端返回数组时覆盖内存与本地，并只保留 key/title/path/icon 四个字段', async () => {
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: FAVORITES_SETTING_KEY,
      settingValue: JSON.stringify([{ key: '乱写', title: 'T', path: '/remote', icon: 'i', 多余字段: 1 }]),
    })
    const store = setupStores()
    store.add(item('/local'))

    await store.hydrate()

    expect(store.favorites).toEqual([{ key: '/remote', title: 'T', path: '/remote', icon: 'i' }])
    expect(readLocal()).toEqual([{ key: '/remote', title: 'T', path: '/remote', icon: 'i' }])
  })

  it('远端数组里 path 非字符串的脏数据被过滤掉', async () => {
    getApi.mockResolvedValue({
      scene: 0,
      settingKey: FAVORITES_SETTING_KEY,
      settingValue: JSON.stringify([null, { title: 'no-path' }, { path: 123 }, { path: '/ok', title: 'OK' }]),
    })
    const store = setupStores()

    await store.hydrate()

    expect(store.favorites.map(x => x.path)).toEqual(['/ok'])
  })

  it('远端返回非数组（对象）时保留本地，不清空收藏', async () => {
    getApi.mockResolvedValue({ scene: 0, settingKey: FAVORITES_SETTING_KEY, settingValue: '{"a":1}' })
    const store = setupStores()
    store.add(item('/local'))

    await store.hydrate()

    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('远端 settingValue 为空时保留本地', async () => {
    getApi.mockResolvedValue({ scene: 0, settingKey: FAVORITES_SETTING_KEY, settingValue: null })
    const store = setupStores()
    store.add(item('/local'))

    await store.hydrate()

    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('远端返回损坏 JSON 时静默保留本地，不抛异常', async () => {
    getApi.mockResolvedValue({ scene: 0, settingKey: FAVORITES_SETTING_KEY, settingValue: '{坏' })
    const store = setupStores()
    store.add(item('/local'))

    await expect(store.hydrate()).resolves.toBeUndefined()
    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('请求失败时静默保留本地', async () => {
    getApi.mockRejectedValue(new Error('503'))
    const store = setupStores()
    store.add(item('/local'))

    await expect(store.hydrate()).resolves.toBeUndefined()
    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('并发调用共享同一次请求（in-flight 去重）', async () => {
    const store = setupStores()

    await Promise.all([store.hydrate(), store.hydrate(), store.hydrate()])

    expect(getApi).toHaveBeenCalledTimes(1)
  })

  it('上一次完成后再次 hydrate 会重新拉取（切换用户需重新水合）', async () => {
    const store = setupStores()

    await store.hydrate()
    await store.hydrate()

    expect(getApi).toHaveBeenCalledTimes(2)
  })

  it('关闭收藏夹同步时 hydrate 直接返回，不请求后端', async () => {
    setActivePinia(createPinia())
    useAppStore().setFavoritesSyncEnabled(false)
    const store = useFavoritesStore()

    await store.hydrate()

    expect(getApi).not.toHaveBeenCalled()
  })
})

describe('applyRemote：其它设备推来的变更', () => {
  it('推送数组时直接覆盖内存与本地', () => {
    const store = setupStores()
    store.add(item('/local'))

    store.applyRemote(JSON.stringify([{ path: '/pushed', title: 'P' }]))

    expect(store.favorites).toEqual([{ key: '/pushed', title: 'P', path: '/pushed', icon: undefined }])
    expect(readLocal()?.map(x => x.path)).toEqual(['/pushed'])
  })

  it('推送空数组会清空收藏（远端确实清空了）', () => {
    const store = setupStores()
    store.add(item('/local'))

    store.applyRemote('[]')

    expect(store.favorites).toEqual([])
  })

  it('settingValue 为空 / undefined 时忽略推送', () => {
    const store = setupStores()
    store.add(item('/local'))

    store.applyRemote(undefined)
    store.applyRemote(null)
    store.applyRemote('')

    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('推送内容不是 JSON 时忽略，不抛异常', () => {
    const store = setupStores()
    store.add(item('/local'))

    expect(() => store.applyRemote('{坏数据')).not.toThrow()
    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('推送内容是非数组 JSON 时忽略', () => {
    const store = setupStores()
    store.add(item('/local'))

    store.applyRemote('{"a":1}')

    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })

  it('应用远端不会回环上行后端', async () => {
    vi.useFakeTimers()
    const store = setupStores()

    store.applyRemote(JSON.stringify([{ path: '/pushed', title: 'P' }]))
    await vi.advanceTimersByTimeAsync(1000)

    expect(saveApi).not.toHaveBeenCalled()
  })

  it('关闭收藏夹同步时忽略远端推送', () => {
    setActivePinia(createPinia())
    useAppStore().setFavoritesSyncEnabled(false)
    const store = useFavoritesStore()
    store.add(item('/local'))

    store.applyRemote(JSON.stringify([{ path: '/pushed', title: 'P' }]))

    expect(store.favorites.map(x => x.path)).toEqual(['/local'])
  })
})
