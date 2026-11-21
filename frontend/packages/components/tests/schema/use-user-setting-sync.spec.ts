/**
 * 页面设置的分区绑定（useUserSettingSync.bindSection）
 * 锁定：分区关着同步不请求后端；同一页的几个分区只拉一次，回来后各自应用并写回本地；
 * 本会话拉过的页再绑直接用缓存；其它设备推来的分发给对应分区并写回本地；推来以后再保存，hydrate 取的是保存后的值；
 * 退出登录后缓存清掉、再绑重新拉取；
 * 没拿到后端那份（拉取失败或还在拉）之前不上行，拿到后合并上传；推来的值不盖掉本机还没传上去的分区；退出登录取消还没发出的回写
 */
import type { SyncedSettingTarget } from '~/composables/useSyncedSetting'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { applyRemotePageSetting, resetPageSettingCache, useUserSettingSync } from '~/components/schema/useUserSettingSync'
import { resetPreferenceBackendSync, useAppStore } from '~/stores'
import { registerAppContext } from '~/stores/app-context'

let resolveGet: (dto: { settingValue?: null | string }) => void = () => {}
let rejectGet: (error: Error) => void = () => {}
const api = {
  get: vi.fn(() => new Promise<{ scene: number, settingKey: string, settingValue?: null | string }>((resolve, reject) => {
    resolveGet = dto => resolve({ scene: 0, settingKey: '', ...dto })
    rejectGet = reject
  })),
  save: vi.fn((_input: { settingValue?: null | string }) => Promise.resolve({ scene: 0, settingKey: '' })),
}
registerAppContext({
  apis: { userSettingApi: api } as never,
})

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

/** 上传过的整页载荷 */
function savedPayloads(): unknown[] {
  return api.save.mock.calls.map(([input]) => JSON.parse(input.settingValue ?? '{}'))
}

/** 等手头的微任务跑完（不靠计时器，假计时器下也能用） */
async function drain(): Promise<void> {
  for (let i = 0; i < 10; i++) {
    await Promise.resolve()
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  const store = useAppStore()
  store.searchSyncEnabled = true
  store.tableSyncEnabled = true
  store.presetSyncEnabled = true
  resetPageSettingCache()
  api.get.mockClear()
  api.save.mockClear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('分区关着同步', () => {
  it('立即就绪，不请求后端', () => {
    useAppStore().searchSyncEnabled = false
    const search = recorder()

    const binding = useUserSettingSync('page-off').bindSection('search', search.target)

    expect(binding.ready.value).toBe(true)
    expect(api.get).not.toHaveBeenCalled()
  })

  it('条件预设跟自己的开关：关掉它不影响表格设置，关掉表格设置也不影响它', () => {
    const store = useAppStore()
    store.presetSyncEnabled = false
    const presets = useUserSettingSync('page-presets-off').bindSection('views', recorder().target)
    expect(presets.ready.value).toBe(true)
    expect(api.get).not.toHaveBeenCalled()

    const table = useUserSettingSync('page-presets-off').bindSection('table', recorder().target)
    expect(table.ready.value).toBe(false)
    expect(api.get).toHaveBeenCalledTimes(1)

    store.presetSyncEnabled = true
    store.tableSyncEnabled = false
    const tableOff = useUserSettingSync('page-table-off').bindSection('table', recorder().target)
    expect(tableOff.ready.value).toBe(true)
    const presetsOn = useUserSettingSync('page-table-off').bindSection('views', recorder().target)
    expect(presetsOn.ready.value).toBe(false)
    expect(api.get).toHaveBeenCalledTimes(2)
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

    resolveGet({
      settingValue: JSON.stringify({
        search: ['s'],
        table: [],
      }),
    })
    await Promise.all([searchBinding.settled, tableBinding.settled])

    expect(search.applied).toStrictEqual([
      ['s'],
    ])
    expect(search.written).toStrictEqual([
      ['s'],
    ])
    // 空数组也是存下的值
    expect(table.applied).toStrictEqual([[]])
    expect(searchBinding.ready.value).toBe(true)
    expect(tableBinding.ready.value).toBe(true)
  })

  it('本会话拉过的页再绑：直接用缓存、立即就绪，不再请求', async () => {
    const first = useUserSettingSync('page-b').bindSection('search', recorder().target)
    resolveGet({
      settingValue: JSON.stringify({
        search: ['cached'],
      }),
    })
    await first.settled
    api.get.mockClear()

    const again = recorder()
    const binding = useUserSettingSync('page-b').bindSection('search', again.target)

    expect(binding.ready.value).toBe(true)
    expect(again.applied).toStrictEqual([
      ['cached'],
    ])
    expect(api.get).not.toHaveBeenCalled()
  })

  it('其它设备推来：分发给绑定的分区，应用并写回本地', async () => {
    const search = recorder()
    const binding = useUserSettingSync('page-c').bindSection('search', search.target)
    resolveGet({
      settingValue: JSON.stringify({
        search: ['old'],
      }),
    })
    await binding.settled

    applyRemotePageSetting('page-c', JSON.stringify({ search: [] }))

    expect(search.applied).toStrictEqual([
      ['old'],
      [],
    ])
    expect(search.written).toStrictEqual([
      ['old'],
      [],
    ])
  })

  it('其它设备推来以后本端再保存：hydrate 取到保存后的值，不是第一次拉取那份', async () => {
    const sync = useUserSettingSync('page-e')
    const first = sync.hydrate('designer')
    resolveGet({
      settingValue: JSON.stringify({
        designer: { theme: 'light' },
      }),
    })
    expect(await first).toStrictEqual({ theme: 'light' })

    applyRemotePageSetting('page-e', JSON.stringify({
      designer: { theme: 'dark' },
    }))
    sync.save('designer', { theme: 'follow' })

    expect(await sync.hydrate('designer')).toStrictEqual({ theme: 'follow' })
  })

  it('退出登录后缓存清掉：再绑重新拉取，不沿用上一个人的那份', async () => {
    const first = useUserSettingSync('page-d').bindSection('search', recorder().target)
    resolveGet({
      settingValue: JSON.stringify({
        search: ['previous-user'],
      }),
    })
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

describe('一次保存传整页载荷', () => {
  it('拉取失败：之后的保存不上行；重拉成功后再保存，载荷带着后端原有的分区与之前没传上去的改动', async () => {
    vi.useFakeTimers()
    const sync = useUserSettingSync('page-f')
    const first = sync.hydrate('search')
    rejectGet(new Error('offline'))
    expect(await first).toBeUndefined()

    sync.save('table', ['mine'])
    await vi.advanceTimersByTimeAsync(600)
    // 保存时手里没有后端那份：先重拉，仍失败就不传
    expect(api.get).toHaveBeenCalledTimes(2)
    rejectGet(new Error('offline'))
    await drain()
    expect(api.save).not.toHaveBeenCalled()

    const again = sync.hydrate('search')
    expect(api.get).toHaveBeenCalledTimes(3)
    resolveGet({
      settingValue: JSON.stringify({
        search: ['kept'],
        views: ['v'],
      }),
    })
    expect(await again).toStrictEqual(['kept'])

    sync.save('views', ['v2'])
    await vi.advanceTimersByTimeAsync(600)
    await drain()
    expect(savedPayloads()).toStrictEqual([
      {
        search: ['kept'],
        views: ['v2'],
        table: ['mine'],
      },
    ])
  })

  it('保存早于拉取回来：等后端那份回来再合并上传', async () => {
    vi.useFakeTimers()
    const sync = useUserSettingSync('page-g')
    const loaded = sync.hydrate('search')
    sync.save('table', ['t'])
    await vi.advanceTimersByTimeAsync(600)

    expect(api.get).toHaveBeenCalledTimes(1)
    expect(api.save).not.toHaveBeenCalled()

    resolveGet({
      settingValue: JSON.stringify({
        search: ['s'],
        views: ['v'],
      }),
    })
    expect(await loaded).toStrictEqual(['s'])
    await drain()
    expect(savedPayloads()).toStrictEqual([
      {
        search: ['s'],
        views: ['v'],
        table: ['t'],
      },
    ])
  })

  it('其它设备推来时，本机还没传上去的分区不被盖掉；传上去以后推来的照常应用', async () => {
    vi.useFakeTimers()
    const sync = useUserSettingSync('page-h')
    const loaded = sync.hydrate('search')
    resolveGet({
      settingValue: JSON.stringify({
        search: ['s'],
        table: ['old'],
      }),
    })
    await loaded
    const table = recorder()
    sync.bindSection('table', table.target)
    sync.save('table', ['mine'])

    applyRemotePageSetting('page-h', JSON.stringify({
      search: ['s2'],
      table: ['theirs'],
    }))
    expect(table.applied).toStrictEqual([
      ['old'],
    ])

    await vi.advanceTimersByTimeAsync(600)
    await drain()
    expect(savedPayloads()).toStrictEqual([
      {
        search: ['s2'],
        table: ['mine'],
      },
    ])

    applyRemotePageSetting('page-h', JSON.stringify({
      search: ['s2'],
      table: ['theirs'],
    }))
    expect(table.applied).toStrictEqual([
      ['old'],
      ['theirs'],
    ])
  })

  it('退出登录时取消还没发出的回写，不带着新令牌写进下一个人的记录', async () => {
    vi.useFakeTimers()
    const sync = useUserSettingSync('page-i')
    const loaded = sync.hydrate('table')
    resolveGet({ settingValue: '{}' })
    await loaded
    sync.save('table', ['previous-user'])

    resetPreferenceBackendSync()
    await vi.advanceTimersByTimeAsync(600)
    await drain()

    expect(api.save).not.toHaveBeenCalled()
  })
})
