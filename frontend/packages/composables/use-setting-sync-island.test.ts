/**
 * useSettingSyncIsland 用户设置同步提示封装单元测试。
 * 职责：锁定上行保存 / 水合 / 远端下行三类场景共用同一套文案与节奏——
 * 起始「正在同步{name}…」、成功「{name}已同步」、失败「{name}同步失败」、
 * 远端下行一次性走「正在应用…」→「{name}已从其他设备同步」，
 * 以及 dismiss 为静默收尾（不落历史、不弹结果）。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'

/** 每个用例一份全新模块状态：灵动岛的 tasks / history 是模块级的 */
async function loadModule() {
  vi.resetModules()
  const { i18n } = await import('~/locales')
  i18n.global.locale.value = 'zh-CN'
  const island = await import('./useDynamicIsland')
  const sync = await import('./useSettingSyncIsland')
  return { ...sync, ...island, i18n }
}

afterEach(() => {
  vi.useRealTimers()
})

describe('settingSyncIsland 上行同步', () => {
  it('起始文案为「正在同步{name}…」并处于进行中态', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:pref', '偏好设置')

    const current = useDynamicIsland().current.value
    expect(current?.label).toBe('正在同步偏好设置…')
    expect(current?.state).toBe('loading')
  })

  it('成功收尾切到「{name}已同步」', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:fav', '收藏夹').success()

    const current = useDynamicIsland().current.value
    expect(current?.label).toBe('收藏夹已同步')
    expect(current?.state).toBe('success')
  })

  it('失败收尾切到「{name}同步失败」', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:table', '表格设置').error()

    const current = useDynamicIsland().current.value
    expect(current?.label).toBe('表格设置同步失败')
    expect(current?.state).toBe('error')
  })

  it('静默收尾直接移除任务，不弹结果也不入历史', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:search', '搜索设置').dismiss()

    const island = useDynamicIsland()
    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value).toEqual([])
  })

  it('同一个 id 复用同一条，重复保存不会堆出多条提示', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:pref', '偏好设置')
    settingSyncIsland('sync:pref', '偏好设置')

    expect(useDynamicIsland().activeTasks.value).toHaveLength(1)
  })

  it('不同 id 的同步任务各占一条，互不覆盖', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:pref', '偏好设置')
    settingSyncIsland('sync:fav', '收藏夹')

    expect(useDynamicIsland().activeTasks.value).toHaveLength(2)
  })

  it('成功收尾后按成功停留时长自动消失并入历史', async () => {
    vi.useFakeTimers()
    const { settingSyncIsland, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    settingSyncIsland('sync:pref', '偏好设置').success()
    vi.advanceTimersByTime(1600)

    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value.map(item => item.label)).toEqual(['偏好设置已同步'])
  })

  it('设置名为空串时文案退化为不带名称，仍不产生裸 key', async () => {
    const { settingSyncIsland, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:x', '')

    expect(useDynamicIsland().current.value?.label).toBe('正在同步…')
  })

  it('切到英文后同一封装产出英文文案', async () => {
    const { settingSyncIsland, useDynamicIsland, i18n } = await loadModule()
    i18n.global.locale.value = 'en-US'

    settingSyncIsland('sync:pref', 'Preferences').success()

    expect(useDynamicIsland().current.value?.label).toBe('Preferences synced')
  })
})

describe('settingSyncRemoteApplied 远端下行', () => {
  it('一次调用即完成「正在应用」→「已从其他设备同步」的收尾', async () => {
    const { settingSyncRemoteApplied, useDynamicIsland } = await loadModule()

    settingSyncRemoteApplied('sync:remote:pref', '偏好设置')

    const current = useDynamicIsland().current.value
    expect(current?.label).toBe('偏好设置已从其他设备同步')
    expect(current?.state).toBe('success')
  })

  it('下行提示同样按成功节奏消失并入历史', async () => {
    vi.useFakeTimers()
    const { settingSyncRemoteApplied, useDynamicIsland } = await loadModule()
    const island = useDynamicIsland()

    settingSyncRemoteApplied('sync:remote:fav', '收藏夹')
    vi.advanceTimersByTime(1600)

    expect(island.activeTasks.value).toEqual([])
    expect(island.history.value.map(item => item.label)).toEqual(['收藏夹已从其他设备同步'])
  })

  it('同 id 的多次下行提示复用同一条，不刷屏', async () => {
    const { settingSyncRemoteApplied, useDynamicIsland } = await loadModule()

    settingSyncRemoteApplied('sync:remote:pref', '偏好设置')
    settingSyncRemoteApplied('sync:remote:pref', '偏好设置')

    expect(useDynamicIsland().activeTasks.value).toHaveLength(1)
  })

  it('上行与下行用不同 id 时可并存，互不覆盖', async () => {
    const { settingSyncIsland, settingSyncRemoteApplied, useDynamicIsland } = await loadModule()

    settingSyncIsland('sync:pref', '偏好设置')
    settingSyncRemoteApplied('sync:remote:pref', '偏好设置')

    expect(useDynamicIsland().activeTasks.value).toHaveLength(2)
  })
})

describe('settingSyncIsland 与灵动岛开关联动', () => {
  it('灵动岛关闭时同步提示改由轻提示接管，不进岛', async () => {
    const { settingSyncIsland, configureDynamicIsland, useDynamicIsland } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    settingSyncIsland('sync:pref', '偏好设置').success()

    expect(useDynamicIsland().activeTasks.value).toEqual([])
    expect(message).toHaveBeenCalledWith('success', '偏好设置已同步')
  })

  it('灵动岛关闭时静默收尾不发任何轻提示（高频场景不刷屏）', async () => {
    const { settingSyncIsland, configureDynamicIsland } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    settingSyncIsland('sync:pref', '偏好设置').dismiss()

    expect(message).not.toHaveBeenCalled()
  })

  it('灵动岛关闭时失败仍会提示，不静默吞掉错误', async () => {
    const { settingSyncIsland, configureDynamicIsland } = await loadModule()
    const message = vi.fn()
    configureDynamicIsland({ isEnabled: () => false, message })

    settingSyncIsland('sync:table', '表格设置').error()

    expect(message).toHaveBeenCalledWith('error', '表格设置同步失败')
  })
})
