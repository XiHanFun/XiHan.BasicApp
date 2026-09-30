/**
 * 系统字典选项：业务表单下拉、列表单元格与搜索框共用的选项来源。
 * 锁住「同一编码一个会话只取一次、并发去重」「失败就地提示且不进缓存」「响应式随编码加载」。
 */
import { describe, expect, it, vi } from 'vitest'
import { nextTick, ref } from 'vue'

type OptionsFn = (dictCode: string) => Promise<Array<{ value: string, label: string, parentValue?: null | string, isDefault: boolean, disabled: boolean }>>

async function flush(): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 0))
}

/** 每个用例一份全新的模块级缓存 */
async function bootstrap(options: OptionsFn) {
  vi.resetModules()
  const danger = vi.fn()
  vi.doMock('./ui-service', () => ({ toast: { danger } }))
  const { registerAppContext } = await import('~/stores/app-context')
  registerAppContext({ apis: { dictApi: { options } } as never })
  const module = await import('./useDictOptions')
  return { ...module, danger }
}

const levels = [
  { value: 'vip', label: '重要客户', parentValue: null, isDefault: false, disabled: false },
  { value: 'blocked', label: '已拉黑', isDefault: false, disabled: true },
]

describe('ensureDictOptions', () => {
  it('同一编码只取一次：并发请求共用一次调用，之后读缓存', async () => {
    const api = vi.fn<OptionsFn>(async () => levels)
    const { ensureDictOptions } = await bootstrap(api)

    const [first, second] = await Promise.all([ensureDictOptions('demo_level'), ensureDictOptions(' demo_level ')])
    const third = await ensureDictOptions('demo_level')

    expect(api).toHaveBeenCalledTimes(1)
    expect(api).toHaveBeenCalledWith('demo_level')
    expect(second).toStrictEqual(first)
    expect(third).toStrictEqual(first)
    expect(first.map(option => option.value)).toEqual(['vip', 'blocked'])
    // 缺省的上级编码归一成 null，停用标记原样保留
    expect(first[1]).toMatchObject({ parentValue: null, disabled: true })
  })

  it('空编码不发请求', async () => {
    const api = vi.fn<OptionsFn>(async () => levels)
    const { ensureDictOptions } = await bootstrap(api)

    await expect(ensureDictOptions('  ')).resolves.toEqual([])
    expect(api).not.toHaveBeenCalled()
  })

  it('失败时提示一次并抛出，不进缓存，下次调用重取', async () => {
    const api = vi.fn<OptionsFn>()
      .mockRejectedValueOnce(new Error('字典 no_such 不存在。'))
      .mockResolvedValueOnce(levels)
    const { ensureDictOptions, getDictOptions, danger } = await bootstrap(api)

    await expect(ensureDictOptions('no_such')).rejects.toThrow('不存在')
    expect(danger).toHaveBeenCalledTimes(1)
    expect(String(danger.mock.calls[0]![0])).toContain('no_such')
    expect(getDictOptions('no_such')).toEqual([])

    await expect(ensureDictOptions('no_such')).resolves.toHaveLength(2)
    expect(api).toHaveBeenCalledTimes(2)
  })
})

describe('useDictOptions', () => {
  it('随编码变化加载，未加载前为空数组', async () => {
    const api = vi.fn<OptionsFn>(async code => (code === 'demo_level' ? levels : [{ value: 'east', label: '华东', isDefault: true, disabled: false }]))
    const { useDictOptions } = await bootstrap(api)
    const code = ref('demo_level')

    const options = useDictOptions(code)
    expect(options.value).toEqual([])

    await flush()
    expect(options.value.map(option => option.label)).toEqual(['重要客户', '已拉黑'])

    code.value = 'demo_region'
    await nextTick()
    await flush()
    expect(options.value.map(option => option.value)).toEqual(['east'])
  })

  it('加载失败不产生未处理的拒绝，选项保持为空', async () => {
    const api = vi.fn<OptionsFn>(async () => {
      throw new Error('网络错误')
    })
    const { useDictOptions, danger } = await bootstrap(api)

    const options = useDictOptions('demo_level')
    await flush()

    expect(options.value).toEqual([])
    expect(danger).toHaveBeenCalledTimes(1)
  })
})
