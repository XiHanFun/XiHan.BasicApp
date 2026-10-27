/**
 * useRecentRoutes 最近访问路由单元测试。
 * 职责：锁定「按访问倒序、按 path 去重、容量上限 12、仅本地持久化」这四条约定，
 * 以及非法条目（缺 path / 缺 title）被静默丢弃、清空同时写回存储。
 */
import type { RecentRoute } from './useRecentRoutes'
import { describe, expect, it, vi } from 'vitest'
import { RECENT_ROUTES_KEY } from '~/constants'

/** 每个用例一份全新模块状态：recent 是模块级 ref，且在导入期读一次 localStorage */
async function loadModule() {
  vi.resetModules()
  const mod = await import('./useRecentRoutes')
  return mod.useRecentRoutes()
}

function route(path: string, title = `标题${path}`): RecentRoute {
  return { path, title }
}

function persisted(): RecentRoute[] {
  return JSON.parse(localStorage.getItem(RECENT_ROUTES_KEY) ?? '[]') as RecentRoute[]
}

describe('useRecentRoutes 初始化', () => {
  it('本地无记录时从空列表起步', async () => {
    const { recent } = await loadModule()

    expect(recent.value).toEqual([])
  })

  it('导入期读取本地已存记录并原样恢复', async () => {
    localStorage.setItem(RECENT_ROUTES_KEY, JSON.stringify([route('/a'), route('/b')]))

    const { recent } = await loadModule()

    expect(recent.value.map(item => item.path)).toEqual(['/a', '/b'])
  })

  it('本地存的是损坏 JSON 时退回空列表而不是抛错', async () => {
    localStorage.setItem(RECENT_ROUTES_KEY, '{不是合法 JSON')

    const { recent } = await loadModule()

    expect(recent.value).toEqual([])
  })
})

describe('recordRecent 倒序与去重', () => {
  it('最新访问排在最前', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent(route('/a'))
    recordRecent(route('/b'))

    expect(recent.value.map(item => item.path)).toEqual(['/b', '/a'])
  })

  it('重复访问同一路径时提到最前而不是新增一条', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent(route('/a'))
    recordRecent(route('/b'))
    recordRecent(route('/a'))

    expect(recent.value.map(item => item.path)).toEqual(['/a', '/b'])
    expect(recent.value).toHaveLength(2)
  })

  it('重复访问时用新条目覆盖旧标题与图标', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent({ path: '/a', title: '旧标题', icon: 'lucide:old' })
    recordRecent({ path: '/a', title: '新标题', icon: 'lucide:new' })

    expect(recent.value[0]).toEqual({ path: '/a', title: '新标题', icon: 'lucide:new' })
  })

  it('带 query 的路径与裸路径视为两条不同记录', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent(route('/a'))
    recordRecent(route('/a?tab=1'))

    expect(recent.value.map(item => item.path)).toEqual(['/a?tab=1', '/a'])
  })

  it('容量上限 12，超出时挤掉最旧的一条', async () => {
    const { recent, recordRecent } = await loadModule()

    for (let i = 1; i <= 13; i++) {
      recordRecent(route(`/p${i}`))
    }

    expect(recent.value).toHaveLength(12)
    expect(recent.value[0]?.path).toBe('/p13')
    expect(recent.value.at(-1)?.path).toBe('/p2')
    expect(recent.value.some(item => item.path === '/p1')).toBe(false)
  })

  it('满容量后重复访问旧条目只是提前，不会挤掉别的记录', async () => {
    const { recent, recordRecent } = await loadModule()

    for (let i = 1; i <= 12; i++) {
      recordRecent(route(`/p${i}`))
    }
    recordRecent(route('/p1'))

    expect(recent.value).toHaveLength(12)
    expect(recent.value[0]?.path).toBe('/p1')
    expect(recent.value.map(item => item.path)).toContain('/p2')
  })
})

describe('recordRecent 非法条目', () => {
  it('缺 path 的条目被静默丢弃', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent({ path: '', title: '有标题' })

    expect(recent.value).toEqual([])
  })

  it('缺 title 的条目被静默丢弃，避免命令面板出现空行', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent({ path: '/a', title: '' })

    expect(recent.value).toEqual([])
  })

  it('丢弃非法条目时不覆盖既有记录与本地存储', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent(route('/a'))
    recordRecent({ path: '/b', title: '' })

    expect(recent.value.map(item => item.path)).toEqual(['/a'])
    expect(persisted().map(item => item.path)).toEqual(['/a'])
  })

  it('中文与 emoji 标题正常保留', async () => {
    const { recent, recordRecent } = await loadModule()

    recordRecent({ path: '/仪表盘', title: '仪表盘 🚀', icon: 'lucide:gauge' })

    expect(recent.value[0]?.title).toBe('仪表盘 🚀')
    expect(persisted()[0]?.path).toBe('/仪表盘')
  })
})

describe('本地持久化', () => {
  it('每次记录都写回 localStorage', async () => {
    const { recordRecent } = await loadModule()

    recordRecent(route('/a'))
    recordRecent(route('/b'))

    expect(persisted().map(item => item.path)).toEqual(['/b', '/a'])
  })

  it('clearRecent 清空内存并把空数组写回存储（而不是删键）', async () => {
    const { recent, recordRecent, clearRecent } = await loadModule()

    recordRecent(route('/a'))
    clearRecent()

    expect(recent.value).toEqual([])
    expect(localStorage.getItem(RECENT_ROUTES_KEY)).toBe('[]')
  })

  it('清空后仍可继续记录', async () => {
    const { recent, recordRecent, clearRecent } = await loadModule()

    recordRecent(route('/a'))
    clearRecent()
    recordRecent(route('/b'))

    expect(recent.value.map(item => item.path)).toEqual(['/b'])
  })

  it('多处调用共享同一份模块级状态', async () => {
    vi.resetModules()
    const { useRecentRoutes } = await import('./useRecentRoutes')

    useRecentRoutes().recordRecent(route('/a'))

    expect(useRecentRoutes().recent.value.map(item => item.path)).toEqual(['/a'])
    expect(useRecentRoutes().recent).toBe(useRecentRoutes().recent)
  })
})
