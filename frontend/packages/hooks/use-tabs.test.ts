/**
 * useTabs / useRefresh 标签页组合式单元测试。
 * 职责：锁定 currentTab 与当前路由的绑定、disableState 五个禁用位的判定口径、
 * 各关闭动作对 tabbarStore 的委托与随后的路由跳转，以及刷新种子的递增。
 *
 * vue-router 用可控替身注入，不启真实路由。
 */
import type { TabItem } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { reactive } from 'vue'
import { HOME_PATH } from '~/constants'

const routeStub = reactive({ path: HOME_PATH })
const pushSpy = vi.fn()

vi.mock('vue-router', () => ({
  useRoute: () => routeStub,
  useRouter: () => ({ push: pushSpy }),
}))

const { useTabs } = await import('./useTabs')
const { useRefresh } = await import('./useRefresh')
const { useTabbarStore } = await import('~/stores')

function tab(path: string, closable: boolean): TabItem {
  return { key: path, path, title: path, closable }
}

/** 首页固定不可关闭，其余三个可关闭；当前停在 /b */
function seedTabs(): ReturnType<typeof useTabbarStore> {
  const store = useTabbarStore()
  store.tabs = [tab(HOME_PATH, false), tab('/a', true), tab('/b', true), tab('/c', true)]
  routeStub.path = '/b'
  return store
}

beforeEach(() => {
  setActivePinia(createPinia())
  routeStub.path = HOME_PATH
  pushSpy.mockClear()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('useTabs.currentTab', () => {
  it('按当前路由 path 定位对应标签', () => {
    seedTabs()

    expect(useTabs().currentTab.value?.path).toBe('/b')
  })

  it('当前路由没有对应标签时为 undefined，而不是回落到首个标签', () => {
    seedTabs()
    routeStub.path = '/not-opened'

    expect(useTabs().currentTab.value).toBeUndefined()
  })

  it('路由切换后 currentTab 跟着换人', () => {
    seedTabs()
    const { currentTab } = useTabs()

    expect(currentTab.value?.path).toBe('/b')

    routeStub.path = '/c'

    expect(currentTab.value?.path).toBe('/c')
  })
})

describe('useTabs.disableState 禁用位判定', () => {
  it('中间标签：五个动作全部可用', () => {
    seedTabs()

    expect(useTabs().disableState.value).toEqual({
      closeCurrent: false,
      closeLeft: false,
      closeRight: false,
      closeOthers: false,
      closeAll: false,
    })
  })

  it('停在不可关闭的首页时禁用「关闭当前」，但左右仍按可关闭标签算', () => {
    const store = useTabbarStore()
    store.tabs = [tab(HOME_PATH, false), tab('/a', true)]
    routeStub.path = HOME_PATH

    const state = useTabs().disableState.value

    expect(state.closeCurrent).toBe(true)
    expect(state.closeLeft).toBe(true)
    expect(state.closeRight).toBe(false)
  })

  it('左侧只有不可关闭的首页时禁用「关闭左侧」', () => {
    const store = useTabbarStore()
    store.tabs = [tab(HOME_PATH, false), tab('/a', true)]
    routeStub.path = '/a'

    expect(useTabs().disableState.value.closeLeft).toBe(true)
  })

  it('当前标签已是最后一个时禁用「关闭右侧」', () => {
    seedTabs()
    routeStub.path = '/c'

    expect(useTabs().disableState.value.closeRight).toBe(true)
  })

  it('当前标签自身可关闭时，只剩它一个可关闭标签就禁用「关闭其它」', () => {
    const store = useTabbarStore()
    store.tabs = [tab(HOME_PATH, false), tab('/a', true)]
    routeStub.path = '/a'

    // 可关闭标签数 1 <= 阈值 1 → 没有别的可关，禁用
    expect(useTabs().disableState.value.closeOthers).toBe(true)
    expect(useTabs().disableState.value.closeAll).toBe(false)

    store.tabs = [tab(HOME_PATH, false), tab('/a', true), tab('/b', true)]

    expect(useTabs().disableState.value.closeOthers).toBe(false)
  })

  it('当前标签不可关闭时「关闭其它」的阈值降到 0，仅剩不可关闭标签才禁用', () => {
    const store = useTabbarStore()
    store.tabs = [tab(HOME_PATH, false)]
    routeStub.path = HOME_PATH

    const state = useTabs().disableState.value

    expect(state.closeOthers).toBe(true)
    expect(state.closeAll).toBe(true)
  })

  it('只有一个可关闭标签且当前就是它时，关闭其它被禁用而关闭全部仍可用', () => {
    const store = useTabbarStore()
    store.tabs = [tab('/only', true)]
    routeStub.path = '/only'

    const state = useTabs().disableState.value

    expect(state.closeOthers).toBe(true)
    expect(state.closeAll).toBe(false)
  })

  // 回归锚点（清单条目 17）：currentIndex 为 -1 时 slice(0, -1) / slice(0) 会误判「左右有可关闭项」，
  // 但 store 的 closeLeft/closeRight 在找不到 key 时直接 return，菜单项可点却无效；
  // closeOthers 更会把全部可关闭标签清空。故除「关闭全部」外必须全部禁用。
  it('当前路由不在标签列表中时，除关闭全部外的动作一律禁用', () => {
    seedTabs()
    routeStub.path = '/ghost'

    expect(useTabs().disableState.value).toEqual({
      closeCurrent: true,
      closeLeft: true,
      closeRight: true,
      closeOthers: true,
      closeAll: false,
    })
  })

  // 回归锚点（清单条目 17）：无当前标签且没有任何可关闭标签时，关闭全部也应禁用。
  it('当前路由不在标签列表中且无可关闭标签时，关闭全部同样禁用', () => {
    const store = useTabbarStore()
    store.tabs = [tab(HOME_PATH, false)]
    routeStub.path = '/ghost'

    expect(useTabs().disableState.value.closeAll).toBe(true)
  })
})

describe('useTabs 关闭动作', () => {
  it('关闭当前标签后跳转到 store 计算出的新激活标签', () => {
    const store = seedTabs()
    store.setActiveTab('/b')

    useTabs().closeCurrentTab()

    expect(store.tabs.map(item => item.path)).toEqual([HOME_PATH, '/a', '/c'])
    expect(pushSpy).toHaveBeenCalledWith('/a')
  })

  it('关闭不可关闭的标签时列表不变，仍按当前激活项跳转', () => {
    const store = useTabbarStore()
    store.tabs = [tab(HOME_PATH, false), tab('/a', true)]
    routeStub.path = HOME_PATH
    store.setActiveTab(HOME_PATH)

    useTabs().closeCurrentTab()

    expect(store.tabs).toHaveLength(2)
    expect(pushSpy).toHaveBeenCalledWith(HOME_PATH)
  })

  it('关闭左侧只清可关闭标签，保留不可关闭的首页与当前标签', () => {
    const store = seedTabs()

    useTabs().closeLeftTabs()

    expect(store.tabs.map(item => item.path)).toEqual([HOME_PATH, '/b', '/c'])
  })

  it('关闭右侧只清当前之后的可关闭标签', () => {
    const store = seedTabs()

    useTabs().closeRightTabs()

    expect(store.tabs.map(item => item.path)).toEqual([HOME_PATH, '/a', '/b'])
  })

  it('关闭其它保留当前与不可关闭标签', () => {
    const store = seedTabs()

    useTabs().closeOtherTabs()

    expect(store.tabs.map(item => item.path)).toEqual([HOME_PATH, '/b'])
  })

  it('关闭全部清掉所有可关闭标签并跳回首页', () => {
    const store = seedTabs()

    useTabs().closeAllTabs()

    expect(store.tabs.map(item => item.path)).toEqual([HOME_PATH])
    expect(pushSpy).toHaveBeenCalledWith(HOME_PATH)
  })

  it('关闭全部时即使首页已被移出列表也仍跳首页', () => {
    const store = useTabbarStore()
    store.tabs = [tab('/a', true), tab('/b', true)]
    routeStub.path = '/a'

    useTabs().closeAllTabs()

    expect(store.tabs).toEqual([])
    expect(pushSpy).toHaveBeenCalledWith(HOME_PATH)
  })
})

describe('useTabs 固定与新窗口', () => {
  it('不传路径时切换当前路由标签的固定状态', () => {
    const store = seedTabs()

    useTabs().toggleTabPin()

    expect(store.tabs.find(item => item.path === '/b')?.pinned).toBe(true)
  })

  it('显式传路径时切换指定标签，不受当前路由影响', () => {
    const store = seedTabs()

    useTabs().toggleTabPin('/c')

    expect(store.tabs.find(item => item.path === '/c')?.pinned).toBe(true)
    expect(store.tabs.find(item => item.path === '/b')?.pinned).toBeFalsy()
  })

  it('固定后的标签变为不可关闭，再次切换恢复可关闭', () => {
    const store = seedTabs()
    const { toggleTabPin } = useTabs()

    toggleTabPin('/c')
    expect(store.tabs.find(item => item.path === '/c')?.closable).toBe(false)

    toggleTabPin('/c')
    expect(store.tabs.find(item => item.path === '/c')?.closable).toBe(true)
  })

  it('新窗口打开当前路由时带上 noopener,noreferrer', () => {
    seedTabs()
    const openSpy = vi.spyOn(window, 'open').mockReturnValue(null)

    useTabs().openTabInNewWindow()

    expect(openSpy).toHaveBeenCalledWith('/b', '_blank', 'noopener,noreferrer')
  })

  it('新窗口可显式指定路径', () => {
    seedTabs()
    const openSpy = vi.spyOn(window, 'open').mockReturnValue(null)

    useTabs().openTabInNewWindow('/other?x=1')

    expect(openSpy).toHaveBeenCalledWith('/other?x=1', '_blank', 'noopener,noreferrer')
  })
})

describe('useRefresh 与 refreshCurrentTab', () => {
  it('刷新使当前路径的种子递增，其它路径不受影响', () => {
    const store = seedTabs()

    useRefresh().refresh()

    expect(store.getRefreshSeed('/b')).toBe(1)
    expect(store.getRefreshSeed('/c')).toBe(0)
  })

  it('连续刷新逐次递增，保证组件每次都被重建', () => {
    const store = seedTabs()
    const { refresh } = useRefresh()

    refresh()
    refresh()
    refresh()

    expect(store.getRefreshSeed('/b')).toBe(3)
  })

  it('useTabs.refreshCurrentTab 与 useRefresh 走同一条种子', () => {
    const store = seedTabs()

    useRefresh().refresh()
    useTabs().refreshCurrentTab()

    expect(store.getRefreshSeed('/b')).toBe(2)
  })

  it('切换路由后刷新记在新路径上', () => {
    const store = seedTabs()
    const { refresh } = useRefresh()

    refresh()
    routeStub.path = '/c'
    refresh()

    expect(store.getRefreshSeed('/b')).toBe(1)
    expect(store.getRefreshSeed('/c')).toBe(1)
  })
})
