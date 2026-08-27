/**
 * useTimezoneOptions 时区目录单元测试。
 * 职责：锁定「模块级缓存一个会话只拉一次」「并发共享同一次请求」「失败静默兜底空目录」，
 * label 只由 IANA ID + 自算偏移拼成（不用后端 displayName），
 * 常用时区按 COMMON_TIMEZONE_IDS 顺序取且缺项跳过，withCurrent 的补位规则。
 */
import { describe, expect, it, vi } from 'vitest'

interface TimeZoneDto {
  id: string
  displayName: string
  baseUtcOffsetMinutes: number
  supportsDaylightSavingTime: boolean
}

type OptionsFn = () => Promise<TimeZoneDto[]>

function dto(id: string, baseUtcOffsetMinutes: number): TimeZoneDto {
  return {
    id,
    displayName: `(UTC+08:00) ${id} 北京, 重庆`,
    baseUtcOffsetMinutes,
    supportsDaylightSavingTime: false,
  }
}

/** 每个用例一份全新模块状态：options / pending 都是模块级的 */
async function bootstrap(options: OptionsFn) {
  vi.resetModules()
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({ apis: { timeZoneApi: { options } } as never })
  const mod = await import('./useTimezoneOptions')
  return mod
}

const catalog: TimeZoneDto[] = [
  dto('UTC', 0),
  dto('Asia/Shanghai', 480),
  dto('Asia/Kolkata', 330),
  dto('America/New_York', -300),
  dto('Pacific/Marquesas', -570),
  dto('Europe/London', 0),
]

describe('useTimezoneOptions 常量约定', () => {
  it('常用时区清单无重复项，且以 UTC 打头', async () => {
    const { COMMON_TIMEZONE_IDS } = await bootstrap(async () => [])

    expect(new Set(COMMON_TIMEZONE_IDS).size).toBe(COMMON_TIMEZONE_IDS.length)
    expect(COMMON_TIMEZONE_IDS[0]).toBe('UTC')
  })
})

describe('useTimezoneOptions 拉取与缓存', () => {
  it('首次 ensureLoaded 拉取一次目录', async () => {
    const options = vi.fn<OptionsFn>(async () => catalog)
    const { useTimezoneOptions } = await bootstrap(options)

    await useTimezoneOptions().ensureLoaded()

    expect(options).toHaveBeenCalledTimes(1)
    expect(useTimezoneOptions().options.value).toHaveLength(catalog.length)
  })

  it('目录已有内容时再次 ensureLoaded 直接返回，不重复请求', async () => {
    const options = vi.fn<OptionsFn>(async () => catalog)
    const { useTimezoneOptions } = await bootstrap(options)

    await useTimezoneOptions().ensureLoaded()
    await useTimezoneOptions().ensureLoaded()

    expect(options).toHaveBeenCalledTimes(1)
  })

  it('并发 ensureLoaded 共享同一次在途请求', async () => {
    let resolveList: ((value: TimeZoneDto[]) => void) | null = null
    const options = vi.fn<OptionsFn>(() => new Promise((resolve) => {
      resolveList = resolve
    }))
    const { useTimezoneOptions } = await bootstrap(options)

    const first = useTimezoneOptions().ensureLoaded()
    const second = useTimezoneOptions().ensureLoaded()

    expect(options).toHaveBeenCalledTimes(1)

    resolveList!(catalog)
    await Promise.all([first, second])

    expect(options).toHaveBeenCalledTimes(1)
  })

  it('拉取期间 loading 为真，落地后回落', async () => {
    let resolveList: ((value: TimeZoneDto[]) => void) | null = null
    const options = vi.fn<OptionsFn>(() => new Promise((resolve) => {
      resolveList = resolve
    }))
    const { useTimezoneOptions } = await bootstrap(options)
    const tz = useTimezoneOptions()

    const pending = tz.ensureLoaded()
    expect(tz.loading.value).toBe(true)

    resolveList!(catalog)
    await pending

    expect(tz.loading.value).toBe(false)
  })

  it('拉取失败时静默兜底为空目录，且 loading 复位', async () => {
    const options = vi.fn<OptionsFn>(async () => {
      throw new Error('网络异常')
    })
    const { useTimezoneOptions } = await bootstrap(options)
    const tz = useTimezoneOptions()

    await expect(tz.ensureLoaded()).resolves.toBeUndefined()

    expect(tz.options.value).toEqual([])
    expect(tz.loading.value).toBe(false)
  })

  it('拉取失败不缓存，下一次 ensureLoaded 重新拉取', async () => {
    const options = vi.fn<OptionsFn>()
      .mockRejectedValueOnce(new Error('网络异常'))
      .mockResolvedValueOnce(catalog)
    const { useTimezoneOptions } = await bootstrap(options)

    await useTimezoneOptions().ensureLoaded()
    await useTimezoneOptions().ensureLoaded()

    expect(options).toHaveBeenCalledTimes(2)
    expect(useTimezoneOptions().options.value).toHaveLength(catalog.length)
  })

  it('后端返回空目录时不算加载成功，下一次仍会重试', async () => {
    const options = vi.fn<OptionsFn>(async () => [])
    const { useTimezoneOptions } = await bootstrap(options)

    await useTimezoneOptions().ensureLoaded()
    await useTimezoneOptions().ensureLoaded()

    expect(options).toHaveBeenCalledTimes(2)
  })

  it('多处调用共享同一份模块级目录', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)

    await useTimezoneOptions().ensureLoaded()

    expect(useTimezoneOptions().options).toBe(useTimezoneOptions().options)
    expect(useTimezoneOptions().options.value).toHaveLength(catalog.length)
  })
})

describe('useTimezoneOptions 展示文本', () => {
  it('label 只由 IANA ID 与自算偏移拼成，不带后端 displayName 的重复前缀', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.options.value.find(item => item.value === 'Asia/Shanghai')?.label)
      .toBe('Asia/Shanghai (UTC+08:00)')
  })

  it('零偏移显示为 UTC+00:00 而不是 UTC-00:00', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.options.value.find(item => item.value === 'UTC')?.label).toBe('UTC (UTC+00:00)')
  })

  it('负偏移带负号且小时分钟各补两位', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.options.value.find(item => item.value === 'America/New_York')?.label)
      .toBe('America/New_York (UTC-05:00)')
  })

  it('非整小时偏移的分钟位如实展示', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.options.value.find(item => item.value === 'Asia/Kolkata')?.label)
      .toBe('Asia/Kolkata (UTC+05:30)')
    expect(tz.options.value.find(item => item.value === 'Pacific/Marquesas')?.label)
      .toBe('Pacific/Marquesas (UTC-09:30)')
  })

  it('偏移分钟数原样透出，供调用方排序或计算', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.options.value.find(item => item.value === 'America/New_York')?.offsetMinutes).toBe(-300)
  })
})

describe('useTimezoneOptions.commonOptions', () => {
  it('按常用清单的顺序取，而不是后端目录顺序', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.commonOptions.value.map(item => item.value))
      .toEqual(['UTC', 'Asia/Shanghai', 'Europe/London', 'America/New_York'])
  })

  it('目录里没有的常用条目自动跳过，不留空洞', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => [dto('UTC', 0)])
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.commonOptions.value.map(item => item.value)).toEqual(['UTC'])
  })

  it('目录未加载时常用列表为空数组而不是报错', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)

    expect(useTimezoneOptions().commonOptions.value).toEqual([])
  })

  it('常用条目复用完整目录里的同一个对象，不另行硬编码', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.commonOptions.value[0]).toBe(tz.options.value.find(item => item.value === 'UTC'))
  })
})

describe('useTimezoneOptions.withCurrent', () => {
  it('当前值已在目录中时原样返回目录，不重复插入', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.withCurrent('Asia/Shanghai')).toBe(tz.options.value)
  })

  it('当前值不在目录中时补到最前，偏移按 0 占位', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    const list = tz.withCurrent('Antarctica/Troll')

    expect(list[0]).toEqual({ value: 'Antarctica/Troll', label: 'Antarctica/Troll', offsetMinutes: 0 })
    expect(list).toHaveLength(catalog.length + 1)
  })

  it('补位不污染模块级目录本身', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    tz.withCurrent('Antarctica/Troll')

    expect(tz.options.value.some(item => item.value === 'Antarctica/Troll')).toBe(false)
  })

  it('当前值前后带空白时按去空白后的 ID 判断与展示', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.withCurrent('  Asia/Shanghai  ')).toBe(tz.options.value)
    expect(tz.withCurrent('  Antarctica/Troll  ')[0]?.value).toBe('Antarctica/Troll')
  })

  it('当前值为空串 / 纯空白 / null / undefined 时都原样返回目录', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => catalog)
    const tz = useTimezoneOptions()
    await tz.ensureLoaded()

    expect(tz.withCurrent('')).toBe(tz.options.value)
    expect(tz.withCurrent('   ')).toBe(tz.options.value)
    expect(tz.withCurrent(null)).toBe(tz.options.value)
    expect(tz.withCurrent()).toBe(tz.options.value)
  })

  it('目录为空时补位结果只有当前值一条', async () => {
    const { useTimezoneOptions } = await bootstrap(async () => [])

    expect(useTimezoneOptions().withCurrent('Legacy/Zone'))
      .toEqual([{ value: 'Legacy/Zone', label: 'Legacy/Zone', offsetMinutes: 0 }])
  })
})
