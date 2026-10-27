/**
 * usePinyin 拼音搜索索引单元测试。
 * 职责：锁定「词典懒加载、未就绪时静默退化为 null」「按文本缓存」
 * 「全拼下标 → 原字符下标 的精确映射」以及非汉字文本一律不建索引这几条约定。
 */
import { describe, expect, it, vi } from 'vitest'

/** 每个用例一份全新模块状态：pinyinFn / cache / pinyinReady 都是模块级的 */
async function loadModule() {
  vi.resetModules()
  return import('./usePinyin')
}

describe('usePinyin 懒加载', () => {
  it('词典未加载前含汉字文本也返回 null，匹配静默退化', async () => {
    const { getPinyinIndex } = await loadModule()

    expect(getPinyinIndex('仪表盘')).toBeNull()
  })

  it('词典未加载前就绪标记为 false，加载完成后翻转为 true', async () => {
    const { ensurePinyin, usePinyinReady } = await loadModule()
    const ready = usePinyinReady()

    expect(ready.value).toBe(false)

    await ensurePinyin()

    expect(ready.value).toBe(true)
  })

  it('并发调用 ensurePinyin 共享同一个加载 Promise，不重复加载词典', async () => {
    const { ensurePinyin } = await loadModule()

    const first = ensurePinyin()
    const second = ensurePinyin()

    expect(first).toBe(second)
    await Promise.all([first, second])
  })

  it('加载完成后再次 ensurePinyin 立即兑现，不再挂起', async () => {
    const { ensurePinyin, usePinyinReady } = await loadModule()
    await ensurePinyin()

    await ensurePinyin()

    expect(usePinyinReady().value).toBe(true)
  })

  it('就绪标记是同一个 ref，供 computed 订阅后自动重算', async () => {
    const { ensurePinyin, usePinyinReady } = await loadModule()
    const before = usePinyinReady()

    await ensurePinyin()

    expect(usePinyinReady()).toBe(before)
    expect(before.value).toBe(true)
  })
})

describe('getPinyinIndex 索引内容', () => {
  it('纯汉字文本产出全拼、首字母与逐字映射', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('仪表盘')).toEqual({
      full: 'yibiaopan',
      fullMap: [0, 0, 1, 1, 1, 1, 2, 2, 2],
      initials: 'ybp',
    })
  })

  it('全拼不带声调，避免搜索时输入声调符号', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('系统')?.full).toBe('xitong')
  })

  it('fullMap 长度与全拼长度严格一致，保证高亮能逐字符映射回原文', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()
    const index = getPinyinIndex('用户管理')

    expect(index?.fullMap).toHaveLength(index?.full.length ?? -1)
    expect(index?.initials).toHaveLength(4)
    expect(index?.fullMap.at(-1)).toBe(3)
  })

  it('中英混排时英文字符小写化后原样进入全拼，且各占一位映射', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('A盘')).toEqual({
      full: 'apan',
      fullMap: [0, 1, 1, 1],
      initials: 'ap',
    })
  })

  it('数字与分隔符原样保留在全拼中', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('2号楼')?.full).toBe('2haolou')
    expect(getPinyinIndex('2号楼')?.initials).toBe('2hl')
  })

  // 回归锚点（清单条目 44）：代理对必须按码点整体入索引。
  // 修复前 fullMap 是 [0, 1, 2, 2, 2]——emoji 的低位半字符被映射成一个独立字符下标，
  // 高亮到该位会切出半个代理对（乱码方块），首字母串也多算一位。
  it('emoji 按码点整体入索引，代理对两个码元一并映射到同一原字符', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()
    const index = getPinyinIndex('🚀盘')

    expect(index?.full).toBe('🚀pan')
    expect(index?.fullMap).toEqual([0, 0, 2, 2, 2])
    expect(index?.initials).toBe('🚀p')
    // 首字母串按码点数只有两位（emoji + p），而不是三位
    expect([...(index?.initials ?? '')]).toHaveLength(2)
  })

  // 回归锚点（清单条目 44）：fullMap 必须与 full 逐码元等长，
  // 消费方按 full 的码元下标反查 fullMap，短一位就会取到 undefined。
  it('含 emoji 时 fullMap 仍与全拼逐码元等长', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()
    const index = getPinyinIndex('🚀盘符🎉')

    expect(index?.fullMap).toHaveLength(index?.full.length ?? -1)
    // 'pan' 的三位全部映射到 '盘' 所在的码元下标 2
    expect(index?.fullMap.slice(2, 5)).toEqual([2, 2, 2])
  })
})

describe('getPinyinIndex 不建索引的输入', () => {
  it('纯英文文本返回 null', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('dashboard')).toBeNull()
  })

  it('空串返回 null', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('')).toBeNull()
  })

  it('纯数字与纯符号返回 null', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('12345')).toBeNull()
    expect(getPinyinIndex('!@#$%^&*()')).toBeNull()
  })

  it('纯 emoji 不含汉字，返回 null', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('🚀🎉')).toBeNull()
  })

  it('只要含一个汉字就建索引，哪怕它在末尾', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('id-码')?.initials).toBe('id-m')
  })
})

describe('getPinyinIndex 缓存', () => {
  it('同一文本二次取索引复用同一个对象引用', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    const first = getPinyinIndex('仪表盘')
    const second = getPinyinIndex('仪表盘')

    expect(first).toBe(second)
  })

  it('不同文本各自建索引，互不串味', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()

    expect(getPinyinIndex('仪表盘')?.initials).toBe('ybp')
    expect(getPinyinIndex('用户')?.initials).toBe('yh')
  })

  it('超长文本同样建全量索引，映射末位指向最后一个字符', async () => {
    const { ensurePinyin, getPinyinIndex } = await loadModule()
    await ensurePinyin()
    const text = '数'.repeat(500)
    const index = getPinyinIndex(text)

    expect(index?.initials).toHaveLength(500)
    expect(index?.fullMap.at(-1)).toBe(499)
    expect(index?.full).toHaveLength((index?.fullMap ?? []).length)
  })
})
