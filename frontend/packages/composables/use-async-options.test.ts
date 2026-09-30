/**
 * 异步选项（代码生成的外键下拉等）：同一页面里表单与列表同时要同一份选项。
 * 锁住「在途时共用一次请求」「结束后不缓存，下次进页面重取」「失败只提示一次且不留脏数据」。
 */
import { describe, expect, it, vi } from 'vitest'

interface Option { label: string, value: string }

async function flush(): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 0))
}

async function bootstrap() {
  vi.resetModules()
  const danger = vi.fn()
  vi.doMock('./ui-service', () => ({ toast: { danger } }))
  const module = await import('./useAsyncOptions')
  return { ...module, danger }
}

const categories: Option[] = [{ label: '电子', value: '1' }, { label: '图书', value: '2' }]

describe('loadAsyncOptions', () => {
  it('在途时同一加载器只调一次，结束后再调会重新请求', async () => {
    const loader = vi.fn(async () => categories)
    const { loadAsyncOptions } = await bootstrap()

    const [first, second] = await Promise.all([loadAsyncOptions(loader), loadAsyncOptions(loader)])
    expect(loader).toHaveBeenCalledTimes(1)
    expect(second).toBe(first)

    await loadAsyncOptions(loader)
    expect(loader).toHaveBeenCalledTimes(2)
  })

  it('失败只提示一次，并发的调用方都拿到失败', async () => {
    const loader = vi.fn(async (): Promise<Option[]> => {
      throw new Error('没有权限')
    })
    const { loadAsyncOptions, danger } = await bootstrap()

    const results = await Promise.allSettled([loadAsyncOptions(loader), loadAsyncOptions(loader)])

    expect(results.map(result => result.status)).toEqual(['rejected', 'rejected'])
    expect(danger).toHaveBeenCalledTimes(1)
    expect(String(danger.mock.calls[0]![0])).toContain('没有权限')
  })
})

describe('useAsyncOptions', () => {
  it('调用即加载，加载前为空数组', async () => {
    const { useAsyncOptions } = await bootstrap()

    const options = useAsyncOptions(async () => categories)
    expect(options.value).toEqual([])

    await flush()
    expect(options.value).toEqual(categories)
  })

  it('加载失败保持空数组，不产生未处理的拒绝', async () => {
    const { useAsyncOptions, danger } = await bootstrap()

    const options = useAsyncOptions(async (): Promise<Option[]> => {
      throw new Error('网络错误')
    })
    await flush()

    expect(options.value).toEqual([])
    expect(danger).toHaveBeenCalledTimes(1)
  })
})
