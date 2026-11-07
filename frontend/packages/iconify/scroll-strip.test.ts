/**
 * packages/iconify/scroll-strip.ts 单元测试
 *
 * 职责边界：横条排不排得下、两端到没到头的判定，箭头翻一屏的位移与方向（含 RTL 与减弱动效），
 * 滚轮折成横向，当前项滚进可视区，以及视口挂上、换掉、卸掉时监听的接与拆
 * jsdom 不排版：尺寸与滚动位置由替身给，scrollBy / scrollIntoView 由替身接管
 */
import type { EffectScope } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref } from 'vue'

const motion = vi.hoisted(() => ({ preference: 'full' as 'full' | 'reduce' }))

vi.mock('@xihan-ui/motion', () => ({
  resolveMotionPreference: () => motion.preference,
}))

const { useScrollStrip } = await import('./scroll-strip')

interface Metrics {
  scrollWidth: number
  clientWidth: number
  scrollLeft: number
}

/** 造一个尺寸可控的滚动盒 */
function viewportOf(metrics: Metrics) {
  const el = document.createElement('div')
  el.appendChild(document.createElement('div'))
  for (const key of ['scrollWidth', 'clientWidth', 'scrollLeft'] as const) {
    Object.defineProperty(el, key, {
      configurable: true,
      get: () => metrics[key],
    })
  }
  const scrollBy = vi.fn()
  el.scrollBy = scrollBy as unknown as typeof el.scrollBy
  document.body.appendChild(el)
  return {
    el,
    scrollBy,
    metrics,
  }
}

const observed: Element[] = []
let disconnects = 0
let scope: EffectScope

beforeEach(() => {
  motion.preference = 'full'
  observed.length = 0
  disconnects = 0
  vi.stubGlobal('ResizeObserver', class {
    observe(target: Element) {
      observed.push(target)
    }

    disconnect() {
      disconnects++
    }
  })
  scope = effectScope()
})

afterEach(() => {
  scope.stop()
  vi.unstubAllGlobals()
  document.body.innerHTML = ''
})

/** 在作用域里起一份，等挂上（flush: post） */
async function mount(target: HTMLElement | null) {
  const viewport = ref<HTMLElement | null>(target)
  const strip = scope.run(() => useScrollStrip(viewport))!
  await nextTick()
  return {
    viewport,
    strip,
  }
}

describe('useScrollStrip', () => {
  it('排得下：不显示箭头，两端都算到头，滚轮不拦', async () => {
    const { el, scrollBy } = viewportOf({ scrollWidth: 500, clientWidth: 500, scrollLeft: 0 })
    const { strip } = await mount(el)
    expect([strip.overflowing.value, strip.atStart.value, strip.atEnd.value]).toEqual([false, true, true])

    const wheel = new WheelEvent('wheel', { deltaY: 100, cancelable: true })
    el.dispatchEvent(wheel)
    expect(wheel.defaultPrevented).toBe(false)
    expect(scrollBy).not.toHaveBeenCalled()
  })

  it('排不下：滚动时跟着更新两端的到头状态', async () => {
    const { el, metrics } = viewportOf({ scrollWidth: 740, clientWidth: 500, scrollLeft: 0 })
    const { strip } = await mount(el)
    expect([strip.overflowing.value, strip.atStart.value, strip.atEnd.value]).toEqual([true, true, false])

    metrics.scrollLeft = 120
    el.dispatchEvent(new Event('scroll'))
    expect([strip.atStart.value, strip.atEnd.value]).toEqual([false, false])

    metrics.scrollLeft = 240
    el.dispatchEvent(new Event('scroll'))
    expect([strip.atStart.value, strip.atEnd.value]).toEqual([false, true])
  })

  it('箭头一次翻一屏、留一段重叠；盒太窄时至少翻一段重叠的宽度', async () => {
    const wide = viewportOf({ scrollWidth: 2000, clientWidth: 500, scrollLeft: 0 })
    const first = await mount(wide.el)
    first.strip.scrollToward('end')
    first.strip.scrollToward('start')
    expect(wide.scrollBy.mock.calls).toEqual([
      [
        { behavior: 'smooth', left: 380 },
      ],
      [
        { behavior: 'smooth', left: -380 },
      ],
    ])

    const narrow = viewportOf({ scrollWidth: 2000, clientWidth: 150, scrollLeft: 0 })
    const second = await mount(narrow.el)
    second.strip.scrollToward('end')
    expect(narrow.scrollBy).toHaveBeenCalledWith({ behavior: 'smooth', left: 120 })
  })

  it('减弱动效时不走平滑滚动；RTL 下方向对调、负的 scrollLeft 照样判得出两端', async () => {
    motion.preference = 'reduce'
    const { el, scrollBy, metrics } = viewportOf({ scrollWidth: 740, clientWidth: 500, scrollLeft: 0 })
    el.style.direction = 'rtl'
    const { strip } = await mount(el)
    strip.scrollToward('end')
    expect(scrollBy).toHaveBeenCalledWith({ behavior: 'instant', left: -380 })

    metrics.scrollLeft = -240
    el.dispatchEvent(new Event('scroll'))
    expect([strip.atStart.value, strip.atEnd.value]).toEqual([false, true])
  })

  it('滚轮把纵向的一格折成横向，只有横向分量时用横向分量', async () => {
    const { el, scrollBy } = viewportOf({ scrollWidth: 740, clientWidth: 500, scrollLeft: 0 })
    await mount(el)
    const vertical = new WheelEvent('wheel', { deltaY: 100, cancelable: true })
    el.dispatchEvent(vertical)
    const horizontal = new WheelEvent('wheel', { deltaX: -40, cancelable: true })
    el.dispatchEvent(horizontal)
    expect([vertical.defaultPrevented, horizontal.defaultPrevented]).toEqual([true, true])
    expect(scrollBy.mock.calls).toEqual([
      [
        { left: 300 },
      ],
      [
        { left: -120 },
      ],
    ])
  })

  it('当前项只在排不下时滚进可视区', async () => {
    const fits = viewportOf({ scrollWidth: 500, clientWidth: 500, scrollLeft: 0 })
    const overflow = viewportOf({ scrollWidth: 740, clientWidth: 500, scrollLeft: 0 })
    const target = document.createElement('button')
    const scrollIntoView = vi.fn()
    target.scrollIntoView = scrollIntoView

    const first = await mount(fits.el)
    first.strip.reveal(target)
    expect(scrollIntoView).not.toHaveBeenCalled()

    const second = await mount(overflow.el)
    second.strip.reveal(null)
    second.strip.reveal(target)
    expect(scrollIntoView).toHaveBeenCalledExactlyOnceWith({
      behavior: 'smooth',
      inline: 'nearest',
      block: 'nearest',
    })
  })

  it('视口后挂上才开始量并盯住它和里面的内容；卸掉即解绑', async () => {
    const { viewport, strip } = await mount(null)
    expect(observed).toHaveLength(0)
    strip.scrollToward('end')
    strip.measure()

    const { el, scrollBy } = viewportOf({ scrollWidth: 740, clientWidth: 500, scrollLeft: 0 })
    viewport.value = el
    await nextTick()
    expect(observed).toEqual([el, el.firstElementChild])
    expect(strip.overflowing.value).toBe(true)

    viewport.value = null
    await nextTick()
    expect(disconnects).toBe(1)
    el.dispatchEvent(new WheelEvent('wheel', { deltaY: 100, cancelable: true }))
    expect(scrollBy).not.toHaveBeenCalled()
  })
})
