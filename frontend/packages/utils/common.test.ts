/**
 * packages/utils/common.ts 单元测试。
 *
 * 职责边界：只验证这一组通用纯函数（日期/体积格式化、防抖节流、深拷贝、空值判定、
 * 随机串、查询串解析、剪贴板、状态映射、选项取标签）的实际行为与边界，
 * 包括源码当前未做防护而产生的退化输出（用例锁定「当前真实行为」，不代表该行为正确）。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  copyToClipboard,
  debounce,
  deepClone,
  formatDate,
  formatFileSize,
  getOptionLabel,
  getStatusType,
  isEmpty,
  parseQuery,
  randomString,
  throttle,
} from './common'

/** 记录被临时覆盖的属性，afterEach 统一还原，避免污染同文件后续用例。 */
const propertyRestorers: Array<() => void> = []

function overrideProperty(target: object, key: string, value: unknown): void {
  const original = Object.getOwnPropertyDescriptor(target, key)
  Object.defineProperty(target, key, { value, configurable: true, writable: true })
  propertyRestorers.push(() => {
    if (original) {
      Object.defineProperty(target, key, original)
    }
    else {
      Reflect.deleteProperty(target, key)
    }
  })
}

afterEach(() => {
  while (propertyRestorers.length > 0) {
    propertyRestorers.pop()?.()
  }
  vi.useRealTimers()
})

describe('formatDate', () => {
  it('空字符串按占位符返回而不是 Invalid Date', () => {
    expect(formatDate('')).toBe('-')
  })

  it('时间戳 0 被当成空值返回占位符，纪元时刻无法格式化', () => {
    // 源码用 !date 判空，数字 0 一并落入占位分支
    expect(formatDate(0)).toBe('-')
  })

  it('默认格式为 YYYY-MM-DD HH:mm:ss 且按本地时区解释无时区标记的字符串', () => {
    expect(formatDate('2026-08-27 13:04:05')).toBe('2026-08-27 13:04:05')
  })

  it('自定义格式串只输出所要求的片段', () => {
    expect(formatDate('2026-08-27 13:04:05', 'YYYY/MM/DD')).toBe('2026/08/27')
  })

  it('传入 Date 实例与等价时间戳的格式化结果一致', () => {
    const date = new Date(2026, 7, 27, 13, 4, 5)
    expect(formatDate(date)).toBe(formatDate(date.getTime()))
  })

  it('不可解析的字符串输出 Invalid Date 而非抛错', () => {
    expect(formatDate('不是日期')).toBe('Invalid Date')
  })
})

describe('formatFileSize', () => {
  it('0 字节返回 0 B', () => {
    expect(formatFileSize(0)).toBe('0 B')
  })

  it('不足 1024 字节保持 B 单位且不补小数', () => {
    expect(formatFileSize(1)).toBe('1 B')
    expect(formatFileSize(1023)).toBe('1023 B')
  })

  it('进位边界 1024 恰好切到 KB', () => {
    expect(formatFileSize(1024)).toBe('1 KB')
    expect(formatFileSize(1023 * 1024)).toBe('1023 KB')
  })

  it('保留两位小数并去掉尾随零', () => {
    expect(formatFileSize(1536)).toBe('1.5 KB')
    expect(formatFileSize(1024 * 1024)).toBe('1 MB')
    expect(formatFileSize(1234567)).toBe('1.18 MB')
  })

  it('最大可用单位是 TB', () => {
    expect(formatFileSize(1024 ** 4)).toBe('1 TB')
  })

  it('超过 TB 量级时单位下标越界，输出 undefined 单位（当前行为，缺少上限收口）', () => {
    expect(formatFileSize(1024 ** 5)).toBe('1 undefined')
  })

  it('负数字节因 Math.log 返回 NaN 而输出 NaN undefined（当前行为，缺少入参校验）', () => {
    expect(formatFileSize(-1)).toBe('NaN undefined')
  })

  it('0 与 1 之间的小数使下标为 -1，输出 undefined 单位（当前行为）', () => {
    expect(formatFileSize(0.5)).toBe('512 undefined')
  })
})

describe('debounce', () => {
  it('延迟窗口内的连续调用只执行最后一次并带最后一次的参数', () => {
    vi.useFakeTimers()
    const spy = vi.fn<(value: number) => void>()
    const debounced = debounce(spy as (...args: never[]) => unknown, 300) as (value: number) => void

    debounced(1)
    vi.advanceTimersByTime(200)
    debounced(2)
    vi.advanceTimersByTime(200)
    expect(spy).not.toHaveBeenCalled()

    vi.advanceTimersByTime(100)
    expect(spy).toHaveBeenCalledTimes(1)
    expect(spy).toHaveBeenCalledWith(2)
  })

  it('默认延迟为 300 毫秒', () => {
    vi.useFakeTimers()
    const spy = vi.fn()
    const debounced = debounce(spy)

    debounced()
    vi.advanceTimersByTime(299)
    expect(spy).not.toHaveBeenCalled()
    vi.advanceTimersByTime(1)
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('两次调用间隔超过延迟时分别独立触发', () => {
    vi.useFakeTimers()
    const spy = vi.fn()
    const debounced = debounce(spy, 100)

    debounced()
    vi.advanceTimersByTime(100)
    debounced()
    vi.advanceTimersByTime(100)
    expect(spy).toHaveBeenCalledTimes(2)
  })

  it('被防抖包装后的调用一律返回 undefined，拿不到原函数返回值', () => {
    vi.useFakeTimers()
    const debounced = debounce(() => 'value')
    expect(debounced()).toBeUndefined()
  })
})

describe('throttle', () => {
  it('首次调用立即执行且透传返回值', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00.000Z'))
    const throttled = throttle((...args: never[]) => `ok:${String(args[0])}`)

    expect(throttled('a' as never)).toBe('ok:a')
  })

  it('冷却窗口内的调用被丢弃且返回 undefined', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00.000Z'))
    const spy = vi.fn()
    const throttled = throttle(spy, 300)

    throttled()
    vi.advanceTimersByTime(299)
    expect(throttled()).toBeUndefined()
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('恰好达到延迟阈值即可再次执行（闭区间判定）', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00.000Z'))
    const spy = vi.fn()
    const throttled = throttle(spy, 300)

    throttled()
    vi.advanceTimersByTime(300)
    throttled()
    expect(spy).toHaveBeenCalledTimes(2)
  })

  it('被丢弃的调用不会在冷却结束后补发', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-27T00:00:00.000Z'))
    const spy = vi.fn()
    const throttled = throttle(spy, 300)

    throttled()
    vi.advanceTimersByTime(10)
    throttled()
    vi.advanceTimersByTime(1000)
    expect(spy).toHaveBeenCalledTimes(1)
  })
})

describe('deepClone', () => {
  it('原始值与 null 原样返回', () => {
    expect(deepClone(null)).toBeNull()
    expect(deepClone(undefined)).toBeUndefined()
    expect(deepClone(42)).toBe(42)
    expect(deepClone('中文与 emoji 🙂')).toBe('中文与 emoji 🙂')
  })

  it('嵌套对象逐层复制，修改副本不影响原对象', () => {
    const source = { a: 1, nested: { list: [1, 2, { deep: 'x' }] } }
    const cloned = deepClone(source)

    cloned.nested.list[2] = { deep: 'y' }
    expect(source.nested.list[2]).toEqual({ deep: 'x' })
    expect(cloned.nested).not.toBe(source.nested)
  })

  it('日期对象复制为新的 Date 实例且时间戳相同', () => {
    const source = new Date('2026-08-27T12:00:00.000Z')
    const cloned = deepClone(source)

    expect(cloned).not.toBe(source)
    expect(cloned.getTime()).toBe(source.getTime())
  })

  it('数组复制后是新数组且元素逐个深拷贝', () => {
    const source = [{ v: 1 }, { v: 2 }]
    const cloned = deepClone(source)

    expect(cloned).not.toBe(source)
    expect(cloned[0]).not.toBe(source[0])
    expect(cloned).toEqual(source)
  })

  it('只复制自有属性，原型链上的属性不进入副本', () => {
    const proto = { inherited: 'from-proto' }
    const source = Object.create(proto) as { own: string, inherited: string }
    source.own = 'mine'

    const cloned = deepClone(source)
    expect(Object.hasOwn(cloned, 'inherited')).toBe(false)
    expect(cloned.own).toBe('mine')
  })

  it('容器类型 Map 与 Set 会退化成空对象，结构化数据被丢失（当前行为，不支持这两类容器）', () => {
    const cloned = deepClone({ map: new Map([['k', 'v']]), set: new Set([1, 2]) })

    expect(cloned.map).toEqual({})
    expect(cloned.set).toEqual({})
  })

  it('空数组与空对象复制后依然为空但不是同一引用', () => {
    const list: number[] = []
    const obj = {}
    expect(deepClone(list)).toEqual([])
    expect(deepClone(list)).not.toBe(list)
    expect(deepClone(obj)).not.toBe(obj)
  })
})

describe('isEmpty', () => {
  it('null 与 undefined 判为空', () => {
    expect(isEmpty(null)).toBe(true)
    expect(isEmpty(undefined)).toBe(true)
  })

  it('纯空白字符串判为空，含可见字符不为空', () => {
    expect(isEmpty('')).toBe(true)
    expect(isEmpty('   \t\n')).toBe(true)
    expect(isEmpty(' 中文 ')).toBe(false)
  })

  it('空数组判为空，含 undefined 元素的数组不为空', () => {
    expect(isEmpty([])).toBe(true)
    expect(isEmpty([undefined])).toBe(false)
  })

  it('无自有可枚举键的对象判为空', () => {
    expect(isEmpty({})).toBe(true)
    expect(isEmpty({ a: undefined })).toBe(false)
  })

  it('数字 0、false、NaN 都不算空值', () => {
    expect(isEmpty(0)).toBe(false)
    expect(isEmpty(false)).toBe(false)
    expect(isEmpty(Number.NaN)).toBe(false)
  })

  it('日期实例因没有自有可枚举键被判为空（当前行为，调用方需自行排除）', () => {
    expect(isEmpty(new Date())).toBe(true)
  })

  it('非空 Map 同样被判为空，size 不参与判定（当前行为）', () => {
    expect(isEmpty(new Map([['k', 'v']]))).toBe(true)
  })
})

describe('randomString', () => {
  it('默认长度为 8', () => {
    expect(randomString()).toHaveLength(8)
  })

  it('长度 0 返回空串，长度参数被严格遵守', () => {
    expect(randomString(0)).toBe('')
    expect(randomString(64)).toHaveLength(64)
  })

  it('只使用大小写字母与数字，不含符号', () => {
    expect(randomString(200)).toMatch(/^[A-Z0-9]+$/i)
  })

  it('取值由 Math.random 决定，可被打桩固定为字符表首字符', () => {
    vi.spyOn(Math, 'random').mockReturnValue(0)
    expect(randomString(4)).toBe('AAAA')
    vi.restoreAllMocks()
  })

  it('连续两次生成极大概率不相同，说明未返回常量', () => {
    expect(randomString(32)).not.toBe(randomString(32))
  })
})

describe('parseQuery', () => {
  it('带问号前缀与不带前缀解析结果一致', () => {
    expect(parseQuery('?a=1&b=2')).toEqual({ a: '1', b: '2' })
    expect(parseQuery('a=1&b=2')).toEqual({ a: '1', b: '2' })
  })

  it('空串解析为空对象', () => {
    expect(parseQuery('')).toEqual({})
  })

  it('同名参数后者覆盖前者，只保留一个值', () => {
    expect(parseQuery('?tag=a&tag=b')).toEqual({ tag: 'b' })
  })

  it('值被 URL 解码，中文与加号按查询串规则还原', () => {
    expect(parseQuery('?name=%E7%BE%B2%E5%92%8C&q=a+b')).toEqual({ name: '羲和', q: 'a b' })
  })

  it('无值参数解析为空字符串', () => {
    expect(parseQuery('?flag&empty=')).toEqual({ flag: '', empty: '' })
  })

  it('所有值都是字符串，数字不会被转型', () => {
    expect(parseQuery('?n=1')).toEqual({ n: '1' })
  })
})

describe('copyToClipboard', () => {
  it('剪贴板 API 可用时直接写入并返回成功', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined)
    overrideProperty(navigator, 'clipboard', { writeText })

    await expect(copyToClipboard('要复制的文本')).resolves.toBe(true)
    expect(writeText).toHaveBeenCalledWith('要复制的文本')
  })

  it('剪贴板 API 抛错时回退到 textarea + execCommand，并清理临时节点', async () => {
    overrideProperty(navigator, 'clipboard', {
      writeText: vi.fn().mockRejectedValue(new Error('拒绝访问')),
    })
    const execCommand = vi.fn().mockReturnValue(true)
    overrideProperty(document, 'execCommand', execCommand)

    await expect(copyToClipboard('回退文本')).resolves.toBe(true)
    expect(execCommand).toHaveBeenCalledWith('copy')
    // 临时 textarea 必须被移除，否则每复制一次都会在 body 里留一个残留节点
    expect(document.querySelectorAll('textarea')).toHaveLength(0)
  })

  it('回退路径执行失败时如实返回 false', async () => {
    overrideProperty(navigator, 'clipboard', undefined)
    overrideProperty(document, 'execCommand', vi.fn().mockReturnValue(false))

    await expect(copyToClipboard('失败文本')).resolves.toBe(false)
  })

  it('回退路径把待复制文本放进 textarea 的 value 而不是 innerHTML', async () => {
    overrideProperty(navigator, 'clipboard', undefined)
    let capturedValue = ''
    overrideProperty(document, 'execCommand', () => {
      capturedValue = (document.querySelector('textarea')?.value) ?? ''
      return true
    })

    await copyToClipboard('<script>1</script>')
    expect(capturedValue).toBe('<script>1</script>')
  })
})

describe('getStatusType', () => {
  it('1 映射成功档、0 映射危险档、2 映射警告档', () => {
    expect(getStatusType(1)).toBe('success')
    expect(getStatusType(0)).toBe('danger')
    expect(getStatusType(2)).toBe('warning')
  })

  it('未登记的状态值一律落到 neutral 兜底', () => {
    expect(getStatusType(3)).toBe('neutral')
    expect(getStatusType(-1)).toBe('neutral')
    expect(getStatusType(Number.NaN)).toBe('neutral')
  })
})

describe('getOptionLabel', () => {
  const options = [
    { label: '启用', value: 1 },
    { label: '禁用', value: 0 },
    { label: '草稿', value: 'draft' },
  ]

  it('命中选项返回其标签', () => {
    expect(getOptionLabel(options, 1)).toBe('启用')
    expect(getOptionLabel(options, 'draft')).toBe('草稿')
  })

  it('值为 0 时同样命中，不会因假值落到兜底', () => {
    expect(getOptionLabel(options, 0)).toBe('禁用')
  })

  it('按全等比较，字符串 1 匹配不上数字 1', () => {
    expect(getOptionLabel(options, '1')).toBe('-')
  })

  it('null / undefined 与空选项集都返回默认兜底 -', () => {
    expect(getOptionLabel(options, null)).toBe('-')
    expect(getOptionLabel(options, undefined)).toBe('-')
    expect(getOptionLabel([], 1)).toBe('-')
  })

  it('可自定义兜底文案', () => {
    expect(getOptionLabel(options, 99, '未知')).toBe('未知')
  })

  it('选项标签为空串时原样返回空串而不是兜底', () => {
    expect(getOptionLabel([{ label: '', value: 5 }], 5)).toBe('')
  })
})
