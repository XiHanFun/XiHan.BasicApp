/**
 * bindSyncedSetting：跨端同步设置的通用接法。
 * 锁定：关着同步立即就绪不拉取；已在手的后端值同步采用；开着同步等后端那份、超时先就绪；
 * 拉取与推送都经 apply 应用、经 writeLocal 写回本地；空数组照样采用；编辑中不覆盖；onReady 只调一次；dispose 退订并清计时。
 */
import { afterEach, describe, expect, it, vi } from 'vitest'
import { bindSyncedSetting, SYNCED_SETTING_WAIT } from './useSyncedSetting'

function setup(options: { enabled?: boolean, peek?: { value: unknown }, dirty?: boolean } = {}) {
  let resolveLoad: (value: unknown) => void = () => {}
  let rejectLoad: (reason: unknown) => void = () => {}
  let pushed: ((value: unknown) => void) | null = null
  const applied: unknown[] = []
  const written: unknown[] = []
  const load = vi.fn(() => new Promise<unknown>((resolve, reject) => {
    resolveLoad = resolve
    rejectLoad = reject
  }))
  const unsubscribe = vi.fn()
  const onReady = vi.fn()
  const binding = bindSyncedSetting<string[]>(
    {
      enabled: () => options.enabled ?? true,
      peek: () => options.peek,
      load,
      subscribe: (listener) => {
        pushed = listener
        return unsubscribe
      },
    },
    {
      accept: (value): value is string[] => Array.isArray(value),
      apply: value => applied.push(value),
      writeLocal: value => written.push(value),
      isDirty: () => options.dirty ?? false,
    },
    { onReady },
  )
  return {
    binding,
    load,
    applied,
    written,
    unsubscribe,
    onReady,
    resolve: (value: unknown) => resolveLoad(value),
    reject: (reason: unknown) => rejectLoad(reason),
    push: (value: unknown) => pushed?.(value),
  }
}

afterEach(() => {
  vi.useRealTimers()
})

describe('关着同步', () => {
  it('立即就绪，不拉后端，onReady 当场调用', () => {
    const rig = setup({ enabled: false })

    expect(rig.binding.ready.value).toBe(true)
    expect(rig.load).not.toHaveBeenCalled()
    expect(rig.onReady).toHaveBeenCalledTimes(1)
  })
})

describe('已在手的后端值', () => {
  it('同步采用并写回本地，立即就绪，不再拉取', () => {
    const rig = setup({ peek: { value: ['a'] } })

    expect(rig.binding.ready.value).toBe(true)
    expect(rig.applied).toStrictEqual([['a']])
    expect(rig.written).toStrictEqual([['a']])
    expect(rig.load).not.toHaveBeenCalled()
  })

  it('在手的是「后端没有这一项」：立即就绪，保留本地', () => {
    const rig = setup({ peek: { value: undefined } })

    expect(rig.binding.ready.value).toBe(true)
    expect(rig.applied).toStrictEqual([])
  })
})

describe('开着同步：等后端那份', () => {
  it('回来之前不就绪；回来后应用、写回本地、就绪', async () => {
    const rig = setup()
    expect(rig.binding.ready.value).toBe(false)
    expect(rig.onReady).not.toHaveBeenCalled()

    rig.resolve(['b'])
    await rig.binding.settled

    expect(rig.applied).toStrictEqual([['b']])
    expect(rig.written).toStrictEqual([['b']])
    expect(rig.binding.ready.value).toBe(true)
    expect(rig.onReady).toHaveBeenCalledTimes(1)
  })

  it('空数组也是用户存下的值，照样采用', async () => {
    const rig = setup()
    rig.resolve([])
    await rig.binding.settled

    expect(rig.applied).toStrictEqual([[]])
    expect(rig.written).toStrictEqual([[]])
  })

  it('后端没有记录或载荷认不出：不动本地，照样就绪', async () => {
    const rig = setup()
    rig.resolve(undefined)
    await rig.binding.settled

    expect(rig.applied).toStrictEqual([])
    expect(rig.binding.ready.value).toBe(true)
  })

  it('拉取失败：不动本地，照样就绪', async () => {
    const rig = setup()
    rig.reject(new Error('离线'))
    await rig.binding.settled

    expect(rig.applied).toStrictEqual([])
    expect(rig.binding.ready.value).toBe(true)
  })

  it('迟迟不回：等满上限先就绪（按本地摆），回来后再应用，onReady 只调一次', async () => {
    vi.useFakeTimers()
    const rig = setup()

    vi.advanceTimersByTime(SYNCED_SETTING_WAIT - 1)
    expect(rig.binding.ready.value).toBe(false)
    vi.advanceTimersByTime(1)
    expect(rig.binding.ready.value).toBe(true)

    rig.resolve(['late'])
    await rig.binding.settled
    expect(rig.applied).toStrictEqual([['late']])
    expect(rig.onReady).toHaveBeenCalledTimes(1)
  })

  it('用户正在编辑：迟到的后端值不覆盖改动', async () => {
    const rig = setup({ dirty: true })
    rig.resolve(['remote'])
    await rig.binding.settled

    expect(rig.applied).toStrictEqual([])
    expect(rig.written).toStrictEqual([])
  })
})

describe('其它设备推来', () => {
  it('当场应用并写回本地，空数组同样照办', async () => {
    const rig = setup({ enabled: false })

    rig.push(['x'])
    rig.push([])

    expect(rig.applied).toStrictEqual([['x'], []])
    expect(rig.written).toStrictEqual([['x'], []])
  })

  it('dispose 退订推送、清掉等待计时', () => {
    vi.useFakeTimers()
    const rig = setup()

    rig.binding.dispose()
    vi.advanceTimersByTime(SYNCED_SETTING_WAIT)

    expect(rig.unsubscribe).toHaveBeenCalledTimes(1)
    expect(rig.binding.ready.value).toBe(false)
  })
})
