/**
 * packages/iconify/offline.ts 单元测试。
 *
 * 职责边界：离线图标集的元数据契约、按 prefix 懒加载与未知 prefix 的回退、
 * 预载清单的范围（四个图标集全部预载；IconPicker 与更多图标集随后续批次）、以及单个图标包加载失败时的降级。
 * addCollection 由替身接管，不真正写入 iconify 的全局注册表。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const { addCollection } = vi.hoisted(() => ({ addCollection: vi.fn() }))

vi.mock('@iconify/vue/offline', () => ({ addCollection }))

const { ICON_SET_META, loadIconNames, setupIconifyOffline } = await import('./offline')

type OfflineModule = typeof import('./offline')

/** 取出历次 addCollection 调用所注册集合的 prefix。 */
function registeredPrefixes(): string[] {
  return addCollection.mock.calls.map(([collection]) => (collection as { prefix: string }).prefix)
}

beforeEach(() => {
  addCollection.mockClear()
})

afterEach(() => {
  vi.doUnmock('@iconify-json/mdi')
  vi.doUnmock('@iconify-json/mdi')
  vi.resetModules()
})

describe('图标集元数据契约', () => {
  it('收录四个图标集（lucide / tabler / mdi / simple-icons）', () => {
    expect(ICON_SET_META).toHaveLength(4)
  })

  it('每个图标集的 prefix 与 npm 包短名一致，装载器据此建索引', () => {
    const mismatched = ICON_SET_META.filter(meta => meta.prefix !== meta.package)
    expect(mismatched).toEqual([])
  })

  it('prefix 互不重复，否则 loadIconNames 会命中错误的图标包', () => {
    const prefixes = ICON_SET_META.map(meta => meta.prefix)
    expect(new Set(prefixes).size).toBe(prefixes.length)
  })

  it('展示名称非空且互不重复', () => {
    const names = ICON_SET_META.map(meta => meta.name)
    expect(names.filter(name => name.trim() === '')).toEqual([])
    expect(new Set(names).size).toBe(names.length)
  })

  it('prefix 全为小写短横线命名，与 iconify 的 prefix:name 书写一致', () => {
    const invalid = ICON_SET_META.filter(meta => !/^[a-z][a-z0-9-]*$/.test(meta.prefix))
    expect(invalid).toEqual([])
  })
})

describe('loadIconNames 查找与回退', () => {
  it('已登记的 prefix 返回该集合的全部图标名并注册集合', async () => {
    const names = await loadIconNames('mdi')

    expect(names.length).toBeGreaterThan(0)
    expect(names.every(name => typeof name === 'string' && name.length > 0)).toBe(true)
    expect(registeredPrefixes()).toEqual(['mdi'])
  })

  it('返回的图标名按字典序排列，供选择器直接渲染', async () => {
    const names = await loadIconNames('mdi')

    expect(names).toEqual([...names].sort())
  })

  it('未登记的 prefix 直接返回空数组，不触发任何加载与注册', async () => {
    await expect(loadIconNames('不存在的图标集')).resolves.toEqual([])
    expect(addCollection).not.toHaveBeenCalled()
  })

  it('空 prefix 走同一条回退路径', async () => {
    await expect(loadIconNames('')).resolves.toEqual([])
    expect(addCollection).not.toHaveBeenCalled()
  })

  it('prefix 大小写敏感，大写形式不会被容错匹配', async () => {
    await expect(loadIconNames('MDI')).resolves.toEqual([])
    expect(addCollection).not.toHaveBeenCalled()
  })

  it('同一 prefix 重复调用每次都重新注册，注册是幂等操作', async () => {
    await loadIconNames('mdi')
    await loadIconNames('mdi')

    expect(registeredPrefixes()).toEqual(['mdi', 'mdi'])
  })

  it('图标包加载失败时返回空数组，不注册也不把异常抛给调用方', async () => {
    vi.resetModules()
    vi.doMock('@iconify-json/mdi', () => {
      throw new Error('chunk 加载失败')
    })
    const mod: OfflineModule = await import('./offline')

    await expect(mod.loadIconNames('mdi')).resolves.toEqual([])
    expect(addCollection).not.toHaveBeenCalled()
  })

  it('图标包内容缺失 icons 字段时返回空数组而不是抛错', async () => {
    vi.resetModules()
    vi.doMock('@iconify-json/mdi', () => ({ icons: { prefix: 'mdi' } }))
    const mod: OfflineModule = await import('./offline')

    await expect(mod.loadIconNames('mdi')).resolves.toEqual([])
  })

  it('图标名未按序给出时由本函数补齐排序', async () => {
    vi.resetModules()
    vi.doMock('@iconify-json/mdi', () => ({
      icons: { prefix: 'mdi', icons: { zebra: {}, apple: {}, mango: {} } },
    }))
    const mod: OfflineModule = await import('./offline')

    await expect(mod.loadIconNames('mdi')).resolves.toEqual(['apple', 'mango', 'zebra'])
  })
})

describe('setupIconifyOffline 预载范围', () => {
  it('预载 lucide / tabler / mdi / simple-icons 四个运行期常用图标集', async () => {
    await setupIconifyOffline()

    expect(registeredPrefixes().sort()).toEqual(['lucide', 'mdi', 'simple-icons', 'tabler'])
  })

  it('simple-icons 必须在预载之列，第三方登录品牌 logo 离线渲染依赖它', async () => {
    await setupIconifyOffline()

    expect(registeredPrefixes()).toContain('simple-icons')
  })

  it('登记的四个图标集全部预载，没有留给按需加载的', async () => {
    await setupIconifyOffline()

    const prefixes = registeredPrefixes()
    expect(prefixes.sort()).toEqual(ICON_SET_META.map(meta => meta.prefix).sort())
  })

  it('预载的图标集都在元数据清单内，不存在未登记的隐式依赖', async () => {
    await setupIconifyOffline()

    const known = new Set<string>(ICON_SET_META.map(meta => meta.prefix))
    expect(registeredPrefixes().filter(prefix => !known.has(prefix))).toEqual([])
  })

  it('单个图标包加载失败时其余照常预载，整体不抛错', async () => {
    vi.resetModules()
    vi.doMock('@iconify-json/mdi', () => {
      throw new Error('chunk 加载失败')
    })
    const mod: OfflineModule = await import('./offline')

    await expect(mod.setupIconifyOffline()).resolves.toBeUndefined()
    expect(registeredPrefixes().sort()).toEqual(['lucide', 'simple-icons', 'tabler'])
  })
})
