/**
 * 标签栏 Store（tabbar）单元测试。
 * 职责边界：标签的增（去重合并）、删（固定页不可关）、批量关闭（其它/左侧/右侧/全部）、
 * 拖拽排序、固定切换、刷新种子，以及 tabbarPersist 开关对 SessionStorage 落地的门控。
 */
import type { TabItem } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { HOME_PATH, TABS_LIST_KEY } from '~/constants'
import { useAppStore } from './app'
import { useTabbarStore } from './tabbar'

function tab(path: string, overrides?: Partial<TabItem>): TabItem {
  return { key: path, title: path, path, closable: true, ...overrides }
}

function readPersisted(): TabItem[] | null {
  const raw = sessionStorage.getItem(TABS_LIST_KEY)
  return raw ? (JSON.parse(raw) as TabItem[]) : null
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('初始标签与会话恢复', () => {
  it('无会话缓存时只有一个不可关闭的首页固定标签', () => {
    const store = useTabbarStore()

    expect(store.tabs).toHaveLength(1)
    expect(store.tabs[0]?.path).toBe(HOME_PATH)
    expect(store.tabs[0]?.pinned).toBe(true)
    expect(store.tabs[0]?.closable).toBe(false)
    expect(store.activeTab).toBe(HOME_PATH)
  })

  it('会话缓存存在时以缓存为准（刷新保留已开标签）', () => {
    sessionStorage.setItem(TABS_LIST_KEY, JSON.stringify([tab('/a'), tab('/b')]))
    setActivePinia(createPinia())

    const store = useTabbarStore()

    expect(store.tabKeys).toEqual(['/a', '/b'])
  })

  it('会话缓存是损坏 JSON 时回落到默认首页标签，不抛异常', () => {
    sessionStorage.setItem(TABS_LIST_KEY, '{坏数据')
    setActivePinia(createPinia())

    const store = useTabbarStore()

    expect(store.tabKeys).toEqual([HOME_PATH])
  })

  it('会话缓存为空数组时按「已有值」处理，不再补默认首页标签', () => {
    sessionStorage.setItem(TABS_LIST_KEY, JSON.stringify([]))
    setActivePinia(createPinia())

    const store = useTabbarStore()

    expect(store.tabs).toEqual([])
  })
})

describe('ensureTab：新增与去重合并', () => {
  it('新 key 追加到末尾并成为当前标签', () => {
    const store = useTabbarStore()

    store.ensureTab(tab('/a'))

    expect(store.tabKeys).toEqual([HOME_PATH, '/a'])
    expect(store.activeTab).toBe('/a')
  })

  it('同 key 重复打开不新增，只激活已有标签', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.ensureTab(tab('/a'))

    expect(store.tabKeys).toEqual([HOME_PATH, '/a', '/b'])
    expect(store.activeTab).toBe('/a')
  })

  it('重复 ensureTab 会补齐旧持久化标签缺失的 name / keepAlive（KeepAlive 缓存名靠它）', () => {
    sessionStorage.setItem(TABS_LIST_KEY, JSON.stringify([{ key: '/a', title: '/a', path: '/a', closable: true }]))
    setActivePinia(createPinia())
    const store = useTabbarStore()

    store.ensureTab(tab('/a', { name: 'PageA', keepAlive: true }))

    expect(store.tabs[0]?.name).toBe('PageA')
    expect(store.tabs[0]?.keepAlive).toBe(true)
  })

  it('重复 ensureTab 不带 name 时会把已有 name 抹成 undefined（整字段覆盖语义）', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a', { name: 'PageA', keepAlive: true }))

    store.ensureTab(tab('/a'))

    expect(store.tabs[1]?.name).toBeUndefined()
    expect(store.tabs[1]?.keepAlive).toBeUndefined()
  })

  it('带 meta 的重复 ensureTab 才会合并 meta 并刷新标题', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a', { title: '旧标题', meta: { icon: 'old', keep: 1 } }))

    store.ensureTab(tab('/a', { title: '新标题', meta: { icon: 'new' } }))

    expect(store.tabs[1]?.title).toBe('新标题')
    expect(store.tabs[1]?.meta).toEqual({ icon: 'new', keep: 1 })
  })

  it('不带 meta 的重复 ensureTab 不会改动标题', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a', { title: '旧标题' }))

    store.ensureTab(tab('/a', { title: '新标题' }))

    expect(store.tabs[1]?.title).toBe('旧标题')
  })
})

describe('cachedTabNames 派生（KeepAlive include）', () => {
  it('只收「开了缓存且有路由名」的标签，且包含非当前标签', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a', { name: 'PageA', keepAlive: true }))
    store.ensureTab(tab('/b', { name: 'PageB', keepAlive: true }))

    expect(store.cachedTabNames).toEqual(['PageA', 'PageB'])
    expect(store.activeTab).toBe('/b')
  })

  it('keepAlive 为 false 或缺 name 的标签不进入缓存名单', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a', { name: 'PageA', keepAlive: false }))
    store.ensureTab(tab('/b', { keepAlive: true }))

    expect(store.cachedTabNames).toEqual([])
  })

  it('关闭标签后其缓存名立即从名单移除', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a', { name: 'PageA', keepAlive: true }))

    store.removeTab('/a')

    expect(store.cachedTabNames).toEqual([])
  })
})

describe('固定与关闭', () => {
  it('togglePin 固定后标签变为不可关闭', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))

    store.togglePin('/a')

    expect(store.tabs[1]?.pinned).toBe(true)
    expect(store.tabs[1]?.closable).toBe(false)
  })

  it('再次 togglePin 取消固定并恢复可关闭', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.togglePin('/a')

    store.togglePin('/a')

    expect(store.tabs[1]?.pinned).toBe(false)
    expect(store.tabs[1]?.closable).toBe(true)
  })

  it('首页标签不允许取消固定', () => {
    const store = useTabbarStore()

    store.togglePin(HOME_PATH)

    expect(store.tabs[0]?.pinned).toBe(true)
    expect(store.tabs[0]?.closable).toBe(false)
  })

  it('togglePin 传不存在的 key 静默返回', () => {
    const store = useTabbarStore()

    expect(() => store.togglePin('/nope')).not.toThrow()
    expect(store.tabKeys).toEqual([HOME_PATH])
  })

  it('removeTab 拒绝关闭 closable=false 的标签', () => {
    const store = useTabbarStore()

    store.removeTab(HOME_PATH)

    expect(store.tabKeys).toEqual([HOME_PATH])
  })

  it('removeTab 传不存在的 key 静默返回', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))

    store.removeTab('/nope')

    expect(store.tabKeys).toEqual([HOME_PATH, '/a'])
  })

  it('关闭当前标签后激活其左邻标签', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.removeTab('/b')

    expect(store.activeTab).toBe('/a')
  })

  it('关闭非当前标签不改变当前激活项', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.removeTab('/a')

    expect(store.activeTab).toBe('/b')
  })

  it('关闭首个可关闭标签且它是当前项时，激活序号 0 的标签', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.setActiveTab('/a')

    store.removeTab('/a')

    expect(store.activeTab).toBe(HOME_PATH)
  })

  it('关掉最后一个标签后 activeTab 回落到 HOME_PATH', () => {
    sessionStorage.setItem(TABS_LIST_KEY, JSON.stringify([tab('/only')]))
    setActivePinia(createPinia())
    const store = useTabbarStore()
    store.setActiveTab('/only')

    store.removeTab('/only')

    expect(store.tabs).toEqual([])
    expect(store.activeTab).toBe(HOME_PATH)
  })
})

describe('批量关闭', () => {
  it('closeOthers 保留目标标签与全部不可关闭标签，并激活目标', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.ensureTab(tab('/c'))

    store.closeOthers('/b')

    expect(store.tabKeys).toEqual([HOME_PATH, '/b'])
    expect(store.activeTab).toBe('/b')
  })

  it('closeOthers 传不存在的 key 会关掉所有可关闭标签，并把它设为当前项', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))

    store.closeOthers('/nope')

    expect(store.tabKeys).toEqual([HOME_PATH])
    expect(store.activeTab).toBe('/nope')
  })

  it('closeLeft 只关左侧可关闭标签，目标及右侧保留', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.ensureTab(tab('/c'))

    store.closeLeft('/b')

    expect(store.tabKeys).toEqual([HOME_PATH, '/b', '/c'])
  })

  it('closeLeft 不改变当前激活标签（关左侧不切页）', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.setActiveTab('/b')

    store.closeLeft('/b')

    expect(store.activeTab).toBe('/b')
  })

  it('closeLeft 关掉了当前标签时 activeTab 仍指向已消失的标签（当前真实行为，不做兜底）', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.setActiveTab('/a')

    store.closeLeft('/b')

    expect(store.tabKeys).toEqual([HOME_PATH, '/b'])
    expect(store.activeTab).toBe('/a')
  })

  it('closeRight 关掉了当前标签时 activeTab 同样悬空（与 removeTab / closeAll 的兜底不一致）', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.closeRight('/a')

    expect(store.tabKeys).toEqual([HOME_PATH, '/a'])
    expect(store.activeTab).toBe('/b')
  })

  it('closeLeft 传不存在的 key 时不做任何事', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))

    store.closeLeft('/nope')

    expect(store.tabKeys).toEqual([HOME_PATH, '/a'])
  })

  it('closeRight 只关右侧可关闭标签，目标及左侧保留', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.ensureTab(tab('/c'))

    store.closeRight('/b')

    expect(store.tabKeys).toEqual([HOME_PATH, '/a', '/b'])
  })

  it('closeRight 不会误伤右侧的固定标签', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/pinned'))
    store.togglePin('/pinned')

    store.closeRight('/a')

    expect(store.tabKeys).toEqual([HOME_PATH, '/a', '/pinned'])
  })

  it('closeRight 传不存在的 key 时不做任何事', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))

    store.closeRight('/nope')

    expect(store.tabKeys).toEqual([HOME_PATH, '/a'])
  })

  it('closeAll 只留不可关闭标签并激活第一个', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.closeAll()

    expect(store.tabKeys).toEqual([HOME_PATH])
    expect(store.activeTab).toBe(HOME_PATH)
  })

  it('closeAll 在没有任何固定标签时 activeTab 回落到 HOME_PATH', () => {
    sessionStorage.setItem(TABS_LIST_KEY, JSON.stringify([tab('/a'), tab('/b')]))
    setActivePinia(createPinia())
    const store = useTabbarStore()

    store.closeAll()

    expect(store.tabs).toEqual([])
    expect(store.activeTab).toBe(HOME_PATH)
  })
})

describe('拖拽排序 moveTab（按 path 定位）', () => {
  it('把后面的标签拖到前面', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.ensureTab(tab('/c'))

    store.moveTab('/c', '/a')

    expect(store.tabKeys).toEqual([HOME_PATH, '/c', '/a', '/b'])
  })

  it('把前面的标签拖到后面', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    store.ensureTab(tab('/c'))

    store.moveTab('/a', '/c')

    expect(store.tabKeys).toEqual([HOME_PATH, '/b', '/c', '/a'])
  })

  it('源与目标相同、任一为空串时都不动', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))
    const before = [...store.tabKeys]

    store.moveTab('/a', '/a')
    store.moveTab('', '/b')
    store.moveTab('/a', '')

    expect(store.tabKeys).toEqual(before)
  })

  it('源或目标 path 不存在时不动', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    const before = [...store.tabKeys]

    store.moveTab('/nope', '/a')
    store.moveTab('/a', '/nope')

    expect(store.tabKeys).toEqual(before)
  })
})

describe('刷新种子', () => {
  it('未刷新过的路径种子为 0', () => {
    const store = useTabbarStore()

    expect(store.getRefreshSeed('/a')).toBe(0)
  })

  it('每次 refreshTab 让对应路径种子自增，其它路径不受影响', () => {
    const store = useTabbarStore()

    store.refreshTab('/a')
    store.refreshTab('/a')

    expect(store.getRefreshSeed('/a')).toBe(2)
    expect(store.getRefreshSeed('/b')).toBe(0)
  })
})

describe('tabbarPersist 门控 SessionStorage 落地', () => {
  it('默认开启持久化：ensureTab 后会话缓存被写入', () => {
    const store = useTabbarStore()

    store.ensureTab(tab('/a'))

    expect(readPersisted()?.map(t => t.key)).toEqual([HOME_PATH, '/a'])
  })

  it('关闭 tabbarPersist 后各类变更都不再写会话缓存', () => {
    const appStore = useAppStore()
    appStore.setTabbarPersist(false)
    const store = useTabbarStore()

    store.ensureTab(tab('/a'))
    store.togglePin('/a')
    store.removeTab('/a')
    store.closeAll()

    expect(sessionStorage.getItem(TABS_LIST_KEY)).toBeNull()
  })

  it('批量关闭同样受持久化开关控制，会话缓存与内存保持一致', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.closeOthers('/a')

    expect(readPersisted()?.map(t => t.key)).toEqual([HOME_PATH, '/a'])
  })

  it('moveTab 的新顺序会落地会话缓存', () => {
    const store = useTabbarStore()
    store.ensureTab(tab('/a'))
    store.ensureTab(tab('/b'))

    store.moveTab('/b', '/a')

    expect(readPersisted()?.map(t => t.key)).toEqual([HOME_PATH, '/b', '/a'])
  })
})
