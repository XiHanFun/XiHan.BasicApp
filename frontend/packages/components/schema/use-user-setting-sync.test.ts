/**
 * 页面设置的分区绑定（useUserSettingSync.bindSection）。
 * 锁定：分区关着同步不请求后端；同一页的几个分区只拉一次，回来后各自应用并写回本地；
 * 本会话拉过的页再绑直接用缓存；其它设备推来的分发给对应分区并写回本地；推来以后再保存，hydrate 取的是保存后的值；
 * 退出登录后缓存清掉、再绑重新拉取。
 */
import type { SyncedSettingTarget } from '~/composables/useSyncedSetting'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { resetPreferenceBackendSync, useAppStore } from '~/stores'
import { registerAppContext } from '~/stores/app-context'
import { applyRemotePageSetting, resetPageSettingCache, useUserSettingSync } from './useUserSettingSync'

let resolveGet: (dto: { settingValue?: null | string }) => void = () => {}
const api = {
  get: vi.fn(() => new Promise<{ scene: number, settingKey: string, settingValue?: null | string }>((resolve) => {
    resolveGet = dto => resolve({ scene: 0, settingKey: '', ...dto })
  })),
  save: vi.fn(() => Promise.resolve({ scene: 0, settingKey: '' })),
}
registerAppContext({ apis: { userSettingApi: api } as never })

function recorder() {
  const applied: unknown[] = []
  const written: unknown[] = []
  const target: SyncedSettingTarget<string[]> = {
    accept: (value): value is string[] => Array.isArray(value),
    apply: value => applied.push(value),
    writeLocal: value => written.push(value),
  }
  return { applied, written, target }
}

beforeEach(() => {
  setActivePinia(createPinia())
  const store = useAppStore()
  store.searchSyncEnabled = true
  store.tableSyncEnabled = true
  resetPageSettingCache()
  api.get.mockClear()
})

describe('分区关着同步', () => {
  it('立即就绪，不请求后端', () => {
    useAppStore().searchSyncEnabled = false
    const search = recorder()

    const binding = useUserSettingSync('page-off').bindSection('search', search.target)

    expect(binding.ready.value).toBe(true)
    expect(api.get).not.toHaveBeenCalled()
  })
})

describe('开着同步', () => {
  it('同一页的几个分区只拉一次；回来之前都不就绪，回来后各自应用并写回本地', async () => {
    const sync = useUserSettingSync('page-a')
    const search = recorder()
    const table = recorder()
    const searchBinding = sync.bindSection('search', search.target)
    const tableBinding = sync.bindSection('table', table.target)

    expect(api.get).toHaveBeenCalledTimes(1)
    expect(searchBinding.ready.value).toBe(false)
    expect(tableBinding.ready.value).toBe(false)

    resolveGet({ settingValue: JSON.stringify({ search: ['s'], table: [] }) })
    await Promise.all([searchBinding.settled, tableBinding.settled])

    expect(search.applied).toStrictEqual([['s']])
    expect(search.written).toStrictEqual([['s']])
    // 空数组也是存下的值
    expect(table.applied).toStrictEqual([[]])
    expect(searchBinding.ready.value).toBe(true)
    expect(tableBinding.ready.value).toBe(true)
  })

  it('本会话拉过的页再绑：直接用缓存、立即就绪，不再请求', async () => {
    const first = useUserSettingSync('page-b').bindSection('search', recorder().target)
    resolveGet({ settingValue: JSON.stringify({ search: ['cached'] }) })
    await first.settled
    api.get.mockClear()

    const again = recorder()
    const binding = useUserSettingSync('page-b').bindSection('search', again.target)

    expect(binding.ready.value).toBe(true)
    expect(again.applied).toStrictEqual([['cached']])
    expect(api.get).not.toHaveBeenCalled()
  })

  it('其它设备推来：分发给绑定的分区，应用并写回本地', async () => {
    const search = recorder()
    const binding = useUserSettingSync('page-c').bindSection('search', search.target)
    resolveGet({ settingValue: JSON.stringify({ search: ['old'] }) })
    await binding.settled

    applyRemotePageSetting('page-c', JSON.stringify({ search: [] }))

    expect(search.applied).toStrictEqual([['old'], []])
    expect(search.written).toStrictEqual([['old'], []])
  })

  it('其它设备推来以后本端再保存：hydrate 取到保存后的值，不是第一次拉取那份', async () => {
    const sync = useUserSettingSync('page-e')
    const first = sync.hydrate('designer')
    resolveGet({ settingValue: JSON.stringify({ designer: { theme: 'light' } }) })
    expect(await first).toStrictEqual({ theme: 'light' })

    applyRemotePageSetting('page-e', JSON.stringify({ designer: { theme: 'dark' } }))
    sync.save('designer', { theme: 'follow' })

    expect(await sync.hydrate('designer')).toStrictEqual({ theme: 'follow' })
  })

  it('退出登录后缓存清掉：再绑重新拉取，不沿用上一个人的那份', async () => {
    const first = useUserSettingSync('page-d').bindSection('search', recorder().target)
    resolveGet({ settingValue: JSON.stringify({ search: ['previous-user'] }) })
    await first.settled
    api.get.mockClear()

    resetPreferenceBackendSync()
    const next = recorder()
    const binding = useUserSettingSync('page-d').bindSection('search', next.target)

    expect(api.get).toHaveBeenCalledTimes(1)
    expect(binding.ready.value).toBe(false)
    expect(next.applied).toStrictEqual([])
  })
})
