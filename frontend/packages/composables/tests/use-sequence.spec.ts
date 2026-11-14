/**
 * 可取消的循环序列（useSequence）单元测试
 * 职责边界：一遍跑完自动从头再来、停下后还没到点的等待作废、重新开始让上一遍作废、
 * 无限等待让序列停在最后一步、所在作用域销毁时自动停下
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { useSequence } from '~/composables/useSequence'

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('循环序列', () => {
  it('一遍跑完自动从头再来', async () => {
    const steps: string[] = []
    const sequence = useSequence(async (wait) => {
      steps.push('a')
      await wait(100)
      steps.push('b')
      await wait(100)
    })

    sequence.start()
    await vi.advanceTimersByTimeAsync(450)
    sequence.stop()

    expect(steps).toEqual(['a', 'b', 'a', 'b', 'a'])
  })

  it('停下之后不再往下走', async () => {
    const steps: string[] = []
    const sequence = useSequence(async (wait) => {
      steps.push('a')
      await wait(100)
      steps.push('b')
      await wait(100)
    })

    sequence.start()
    await vi.advanceTimersByTimeAsync(50)
    sequence.stop()
    await vi.advanceTimersByTimeAsync(1000)

    expect(steps).toEqual(['a'])
  })

  it('重新开始让上一遍作废，只有新的一遍在跑', async () => {
    const steps: string[] = []
    const sequence = useSequence(async (wait) => {
      steps.push('a')
      await wait(100)
      steps.push('b')
      await wait(Number.POSITIVE_INFINITY)
    })

    sequence.start()
    await vi.advanceTimersByTimeAsync(50)
    sequence.start()
    await vi.advanceTimersByTimeAsync(100)

    expect(steps).toEqual(['a', 'a', 'b'])
  })

  it('无限等待让序列停在最后一步，不再循环', async () => {
    const steps: string[] = []
    const sequence = useSequence(async (wait) => {
      steps.push('a')
      await wait(Number.POSITIVE_INFINITY)
    })

    sequence.start()
    await vi.advanceTimersByTimeAsync(10_000)
    sequence.stop()

    expect(steps).toEqual(['a'])
  })

  it('所在的作用域销毁时自动停下', async () => {
    const steps: string[] = []
    const scope = effectScope()
    scope.run(() => {
      useSequence(async (wait) => {
        steps.push('a')
        await wait(100)
      }).start()
    })

    await vi.advanceTimersByTimeAsync(150)
    scope.stop()
    await vi.advanceTimersByTimeAsync(1000)

    expect(steps).toEqual(['a', 'a'])
  })
})
