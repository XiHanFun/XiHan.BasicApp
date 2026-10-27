/**
 * packages/utils/storage.ts 单元测试。
 *
 * 职责边界：LocalStorage / SessionStorage 两个 JSON 封装的读写、缺键、损坏 JSON 容错、
 * 写入异常吞掉（配额满/隐私模式）、has 与 get 的判定差异，以及 storage 旧命名别名的等价性。
 * 不覆盖任何业务侧的 key 语义（那属于 packages/constants）。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocalStorage, SessionStorage, storage } from './storage'

afterEach(() => {
  vi.restoreAllMocks()
  localStorage.clear()
  sessionStorage.clear()
})

describe('本地存储读写往返', () => {
  it('对象写入后读出结构相等但不是同一引用', () => {
    const value = { name: '羲和', tags: ['a', 'b'], nested: { n: 1 } }
    LocalStorage.set('obj', value)

    const restored = LocalStorage.get<typeof value>('obj')
    expect(restored).toEqual(value)
    expect(restored).not.toBe(value)
  })

  it('数字、布尔、数组各自按原类型还原，不退化成字符串', () => {
    LocalStorage.set('num', 0)
    LocalStorage.set('bool', false)
    LocalStorage.set('arr', [1, '2', null])

    expect(LocalStorage.get('num')).toBe(0)
    expect(LocalStorage.get('bool')).toBe(false)
    expect(LocalStorage.get('arr')).toEqual([1, '2', null])
  })

  it('中文与 emoji 原样往返', () => {
    LocalStorage.set('text', '羲和 🙂 测试')
    expect(LocalStorage.get('text')).toBe('羲和 🙂 测试')
  })

  it('值以 JSON 形式落盘，字符串带引号，便于与裸字符串写入区分', () => {
    LocalStorage.set('raw', 'abc')
    expect(localStorage.getItem('raw')).toBe('"abc"')
  })

  it('写入 undefined 等同于删键，has 与 get 的结论保持一致', () => {
    // 回归锚点：JSON.stringify(undefined) 返回 undefined，setItem 会把它转成字面量字符串
    // 'undefined' 落盘，随后 has 判真、get 解析失败返回 null——以 has 做「是否已初始化」
    // 判断的调用方拿到 true 再 get 却是 null，走进未预期分支。
    LocalStorage.set('u', { a: 1 })
    LocalStorage.set('u', undefined)

    expect(localStorage.getItem('u')).toBeNull()
    expect(LocalStorage.get('u')).toBeNull()
    expect(LocalStorage.has('u')).toBe(false)
  })

  it('写入 undefined 不抛错也不残留脏值（会话级存储同口径）', () => {
    SessionStorage.set('u', 'v')
    SessionStorage.set('u', undefined)

    expect(sessionStorage.getItem('u')).toBeNull()
    expect(SessionStorage.get('u')).toBeNull()
  })

  it('写入 null 能正确往返为 null', () => {
    LocalStorage.set('n', null)
    expect(localStorage.getItem('n')).toBe('null')
    expect(LocalStorage.get('n')).toBeNull()
  })
})

describe('本地存储容错', () => {
  it('键不存在时返回 null 而不是抛错', () => {
    expect(LocalStorage.get('missing')).toBeNull()
  })

  it('已损坏的 JSON 返回 null，不让解析异常冒泡到调用方', () => {
    localStorage.setItem('broken', '{不是 JSON')
    expect(LocalStorage.get('broken')).toBeNull()
  })

  it('外部写入的空串被当作缺失值返回 null', () => {
    localStorage.setItem('empty', '')
    expect(LocalStorage.get('empty')).toBeNull()
  })

  it('存在循环引用时写入被静默忽略，不抛错也不写脏值', () => {
    const circular: Record<string, unknown> = { name: 'x' }
    circular.self = circular

    expect(() => LocalStorage.set('circular', circular)).not.toThrow()
    expect(localStorage.getItem('circular')).toBeNull()
  })

  it('配额写满导致 setItem 抛错时被吞掉，调用方不需要 try/catch', () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('QuotaExceededError')
    })

    expect(() => LocalStorage.set('k', { a: 1 })).not.toThrow()
    expect(setItem).toHaveBeenCalledTimes(1)
  })

  it('getItem 抛错（隐私模式）时同样降级为 null', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('SecurityError')
    })

    expect(LocalStorage.get('k')).toBeNull()
  })
})

describe('本地存储的 remove / clear / has', () => {
  it('remove 后读取返回 null 且 has 为假', () => {
    LocalStorage.set('k', 1)
    LocalStorage.remove('k')

    expect(LocalStorage.get('k')).toBeNull()
    expect(LocalStorage.has('k')).toBe(false)
  })

  it('remove 不存在的键不抛错', () => {
    expect(() => LocalStorage.remove('never-set')).not.toThrow()
  })

  it('clear 清空全部键，包括绕过封装直接写入的键', () => {
    LocalStorage.set('a', 1)
    localStorage.setItem('b', 'raw')
    LocalStorage.clear()

    expect(localStorage.length).toBe(0)
  })

  it('has 只看键是否存在，空串值仍判定为存在（与 get 返回 null 不一致）', () => {
    localStorage.setItem('empty', '')

    expect(LocalStorage.has('empty')).toBe(true)
    expect(LocalStorage.get('empty')).toBeNull()
  })

  it('set(key, null) 是一次合法写入，has 判真而 get 如实返回 null —— has 有意保持键存在性语义', () => {
    LocalStorage.set('n', null)

    expect(LocalStorage.has('n')).toBe(true)
    expect(LocalStorage.get('n')).toBeNull()
  })

  it('has 对损坏 JSON 的键返回真，判存与可读是两件事', () => {
    localStorage.setItem('broken', '{')
    expect(LocalStorage.has('broken')).toBe(true)
  })
})

describe('会话级存储封装', () => {
  it('读写走 sessionStorage，不落到 localStorage', () => {
    SessionStorage.set('k', { v: 1 })

    expect(sessionStorage.getItem('k')).toBe('{"v":1}')
    expect(localStorage.getItem('k')).toBeNull()
  })

  it('缺键与损坏 JSON 一律返回 null', () => {
    sessionStorage.setItem('broken', 'oops')

    expect(SessionStorage.get('missing')).toBeNull()
    expect(SessionStorage.get('broken')).toBeNull()
  })

  it('remove 与 clear 只影响 sessionStorage', () => {
    SessionStorage.set('a', 1)
    LocalStorage.set('a', 2)
    SessionStorage.clear()

    expect(SessionStorage.get('a')).toBeNull()
    expect(LocalStorage.get('a')).toBe(2)
  })

  it('写入异常被吞掉，不打断调用方流程', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('QuotaExceededError')
    })

    expect(() => SessionStorage.set('k', 1)).not.toThrow()
  })
})

describe('旧命名别名', () => {
  it('storage 与 LocalStorage 是同一个对象，迁移期两处写入互相可见', () => {
    expect(storage).toBe(LocalStorage)

    storage.set('shared', 'v')
    expect(LocalStorage.get('shared')).toBe('v')
  })
})
