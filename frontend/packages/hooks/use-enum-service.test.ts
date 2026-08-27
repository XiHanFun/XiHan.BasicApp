/**
 * useEnumService 枚举元数据服务单元测试。
 * 职责：锁定「整库取一次、按语言缓存、同语言并发去重」这三条核心约定，
 * 以及 toSelectOptions 的排序/兜底、getLabel 的 value 与 valueText 双路匹配、
 * 切语言后整库重取一次（且未加载过时不发起无谓请求）。
 *
 * 模块级缓存跨用例可见，因此每个用例都用 vi.resetModules() 拿一份全新的模块状态。
 */
import type { AppEnumBatchQuery, AppEnumDefinition, AppEnumOption } from '~/types'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

type BatchFn = (query: AppEnumBatchQuery) => Promise<Record<string, AppEnumDefinition>>

function makeOption(partial: Partial<AppEnumOption> & Pick<AppEnumOption, 'label' | 'value'>): AppEnumOption {
  return {
    name: String(partial.value),
    valueText: String(partial.value),
    description: '',
    order: 0,
    disabled: false,
    source: 'enum',
    ...partial,
  }
}

function makeDefinition(enumName: string, items: AppEnumOption[]): AppEnumDefinition {
  return {
    enumName,
    fullName: `XiHan.${enumName}`,
    displayName: enumName,
    cultureName: 'zh-CN',
    isFlags: false,
    underlyingTypeName: 'Int32',
    items,
  }
}

/** 每个用例一份全新的模块状态 + 全新 pinia + 可注入的假 enumApi */
async function bootstrap(getBatch: BatchFn) {
  vi.resetModules()
  setActivePinia(createPinia())
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({ apis: { enumApi: { getBatch, getByName: () => Promise.reject(new Error('未使用')) } } as never })
  const { useEnumService } = await import('./useEnumService')
  const { useAppStore } = await import('~/stores')
  return { useEnumService, useAppStore }
}

const enableStatus = makeDefinition('EnableStatus', [
  makeOption({ label: '禁用', value: 0, valueText: '0', order: 2 }),
  makeOption({ label: '启用', value: 1, valueText: '1', order: 1 }),
])

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('useEnumService 整库拉取与缓存', () => {
  it('首次 ensureEnum 触发一次全量拉取，请求体为空 enumNames', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    await useEnumService().ensureEnum('EnableStatus')

    expect(getBatch).toHaveBeenCalledTimes(1)
    expect(getBatch.mock.calls[0]?.[0]).toEqual({
      enumNames: [],
      language: 'zh-CN',
      includeHidden: false,
      includeDict: true,
    })
  })

  it('同语言下第二次 ensureEnum 命中缓存，不再发请求', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    await useEnumService().ensureEnum('EnableStatus')
    await useEnumService().ensureEnum('PermissionType')

    expect(getBatch).toHaveBeenCalledTimes(1)
  })

  it('并发 ensure 共享同一次在途请求，只打一次接口', async () => {
    let resolveBatch: ((value: Record<string, AppEnumDefinition>) => void) | null = null
    const getBatch = vi.fn<BatchFn>(() => new Promise((resolve) => {
      resolveBatch = resolve
    }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()

    const first = service.ensureEnum('EnableStatus')
    const second = service.ensureEnum('EnableStatus')
    const third = useEnumService().ensureBatch(['Whatever'])

    expect(getBatch).toHaveBeenCalledTimes(1)

    resolveBatch!({ EnableStatus: enableStatus })
    await Promise.all([first, second, third])

    expect(getBatch).toHaveBeenCalledTimes(1)
  })

  it('拉取期间 loading 为真，落地后回落', async () => {
    let resolveBatch: ((value: Record<string, AppEnumDefinition>) => void) | null = null
    const getBatch = vi.fn<BatchFn>(() => new Promise((resolve) => {
      resolveBatch = resolve
    }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()

    const pending = service.ensureEnum('EnableStatus')
    expect(service.loading.value).toBe(true)

    resolveBatch!({ EnableStatus: enableStatus })
    await pending

    expect(service.loading.value).toBe(false)
  })

  it('拉取失败不写入缓存，下一次 ensure 重新拉取', async () => {
    const getBatch = vi.fn<BatchFn>()
      .mockRejectedValueOnce(new Error('网络异常'))
      .mockResolvedValueOnce({ EnableStatus: enableStatus })
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()

    await expect(service.ensureEnum('EnableStatus')).rejects.toThrow(/网络异常/)
    expect(service.enumMap.value).toEqual({})

    const definition = await service.ensureEnum('EnableStatus')

    expect(getBatch).toHaveBeenCalledTimes(2)
    expect(definition?.enumName).toBe('EnableStatus')
  })

  it('返回空映射时不算加载成功，下一次仍会重新拉取', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({}))
    const { useEnumService } = await bootstrap(getBatch)

    await useEnumService().ensureEnum('EnableStatus')
    await useEnumService().ensureEnum('EnableStatus')

    expect(getBatch).toHaveBeenCalledTimes(2)
  })

  it('枚举名为空串时直接返回 null，不触发任何请求', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    await expect(useEnumService().ensureEnum('')).resolves.toBeNull()
    expect(getBatch).not.toHaveBeenCalled()
  })

  it('目录里没有的枚举名返回 null，而不是抛错', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    await expect(useEnumService().ensureEnum('NotExists')).resolves.toBeNull()
  })

  it('ensureBatch 忽略传入的枚举名清单，返回整库映射', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    const all = await useEnumService().ensureBatch(['只要一个'])

    expect(Object.keys(all)).toEqual(['EnableStatus'])
    expect(getBatch.mock.calls[0]?.[0].enumNames).toEqual([])
  })
})

describe('useEnumService 语言维度', () => {
  it('显式传入语言时按该语言请求，而不是取 store 的当前语言', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    await useEnumService().ensureEnum('EnableStatus', { language: 'en-US' })

    expect(getBatch.mock.calls[0]?.[0].language).toBe('en-US')
  })

  it('换一个语言再取会重新拉取，而不是复用上一语言的缓存', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)

    await useEnumService().ensureEnum('EnableStatus', { language: 'zh-CN' })
    await useEnumService().ensureEnum('EnableStatus', { language: 'en-US' })

    expect(getBatch).toHaveBeenCalledTimes(2)
    expect(getBatch.mock.calls[1]?.[0].language).toBe('en-US')
  })

  it('切语言后整库重取一次（全局监听只装一次，不随下拉数量放大请求）', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService, useAppStore } = await bootstrap(getBatch)

    // 多个消费者各自取一次 service，模拟一屏多个下拉
    useEnumService()
    useEnumService()
    await useEnumService().ensureEnum('EnableStatus')
    expect(getBatch).toHaveBeenCalledTimes(1)

    useAppStore().setLocale('en-US')
    await nextTick()
    await Promise.resolve()

    expect(getBatch).toHaveBeenCalledTimes(2)
    expect(getBatch.mock.calls[1]?.[0].language).toBe('en-US')
  })

  it('从未加载过枚举时切语言不发起请求', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService, useAppStore } = await bootstrap(getBatch)

    useEnumService()
    useAppStore().setLocale('en-US')
    await nextTick()
    await Promise.resolve()

    expect(getBatch).not.toHaveBeenCalled()
  })
})

describe('useEnumService.toSelectOptions', () => {
  it('按 order 升序输出，不受返回顺序影响', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.toSelectOptions('EnableStatus')).toEqual([
      { label: '启用', value: 1, disabled: false },
      { label: '禁用', value: 0, disabled: false },
    ])
  })

  it('排序不改动源定义的 items 顺序（内部先 slice 再排）', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    service.toSelectOptions('EnableStatus')

    expect(service.getDefinition('EnableStatus')?.items.map(item => item.label)).toEqual(['禁用', '启用'])
  })

  it('缺 order 的条目按 0 参与排序', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({
      Mixed: makeDefinition('Mixed', [
        makeOption({ label: '有序', value: 'a', order: 5 }),
        { ...makeOption({ label: '无序', value: 'b' }), order: undefined as unknown as number },
      ]),
    }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('Mixed')

    expect(service.toSelectOptions('Mixed').map(item => item.label)).toEqual(['无序', '有序'])
  })

  it('定义不存在时返回传入的兜底选项', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({}))
    const { useEnumService } = await bootstrap(getBatch)
    const fallback = [{ label: '兜底', value: 'x' }]

    expect(useEnumService().toSelectOptions('NotExists', fallback)).toBe(fallback)
  })

  it('定义存在但 items 为空数组时同样走兜底', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ Empty: makeDefinition('Empty', []) }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('Empty')

    expect(service.toSelectOptions('Empty', [{ label: '兜底', value: 0 }])).toEqual([{ label: '兜底', value: 0 }])
  })

  it('未传兜底且定义缺失时返回空数组', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({}))
    const { useEnumService } = await bootstrap(getBatch)

    expect(useEnumService().toSelectOptions('NotExists')).toEqual([])
  })

  it('保留条目的 disabled 标记，供下拉禁用项', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({
      Flag: makeDefinition('Flag', [makeOption({ label: '停用中', value: 9, disabled: true })]),
    }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('Flag')

    expect(service.toSelectOptions('Flag')[0]?.disabled).toBe(true)
  })
})

describe('useEnumService.getLabel', () => {
  it('按 value 全等命中标签', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.getLabel('EnableStatus', 1)).toBe('启用')
  })

  it('value 类型不一致时回退比对 valueText，字符串 "1" 同样命中', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.getLabel('EnableStatus', '1')).toBe('启用')
  })

  it('未命中时返回默认占位 -', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.getLabel('EnableStatus', 999)).toBe('-')
  })

  it('未命中时可指定自定义兜底文案', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.getLabel('EnableStatus', 999, '未知')).toBe('未知')
  })

  it('定义未加载时直接返回兜底，不抛错', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({}))
    const { useEnumService } = await bootstrap(getBatch)

    expect(useEnumService().getLabel('EnableStatus', 1, '空')).toBe('空')
  })

  it('传 null 时按 valueText 为 "null" 比对，正常枚举不会命中', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.getLabel('EnableStatus', null)).toBe('-')
    expect(service.getLabel('EnableStatus', undefined)).toBe('-')
  })

  it('值 0 不被当成空值吞掉，仍能命中对应标签', async () => {
    const getBatch = vi.fn<BatchFn>(async () => ({ EnableStatus: enableStatus }))
    const { useEnumService } = await bootstrap(getBatch)
    const service = useEnumService()
    await service.ensureEnum('EnableStatus')

    expect(service.getLabel('EnableStatus', 0)).toBe('禁用')
  })
})
