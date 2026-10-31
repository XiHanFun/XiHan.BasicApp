/**
 * 图表进入视口才挂载（ChartInView）。
 *
 * 偏好「视口内动画」开着时，图表露出一半才挂上，入场动画在用户眼前播；关着照常立刻挂。
 * 挂过之后偏好再打开也不撤；换配色重播入场：露出一半的当场重挂，其余退回占位、滚进来再挂。
 * 打印时没进过视口的一并挂上，打印期间外壳写 data-motion="reduce"，组件库的图表据此直接画终态，
 * 减弱动效下仍留着的有限时长动画在挂上后一步播完。
 */
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { h, nextTick } from 'vue'
import { useAppStore } from '~/stores'
import ChartInView from './ChartInView.vue'

/** 观察器替身：记下回调与目标，由用例报告进没进视口 */
class FakeIntersectionObserver {
  static instances: FakeIntersectionObserver[] = []
  readonly targets = new Set<Element>()
  disconnected = false

  constructor(private readonly callback: IntersectionObserverCallback) {
    FakeIntersectionObserver.instances.push(this)
  }

  observe(target: Element): void {
    this.targets.add(target)
  }

  unobserve(target: Element): void {
    this.targets.delete(target)
  }

  disconnect(): void {
    this.disconnected = true
    this.targets.clear()
  }

  takeRecords(): IntersectionObserverEntry[] {
    return []
  }

  report(intersectionRatio: number): void {
    const entries = [...this.targets].map(target => ({ target, intersectionRatio, isIntersecting: intersectionRatio > 0 }) as IntersectionObserverEntry)
    this.callback(entries, this as unknown as IntersectionObserver)
  }
}

// 断言失败时用例走不到末尾，残留的实例会接着响应 beforeprint：每条用例结束一律卸掉
enableAutoUnmount(afterEach)

beforeEach(() => {
  setActivePinia(createPinia())
  FakeIntersectionObserver.instances = []
  vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver)
  document.body.innerHTML = ''
})

afterEach(() => {
  delete (HTMLElement.prototype as Partial<HTMLElement>).getAnimations
})

/** jsdom 没有 getAnimations：给外壳补上，交出一条有限时长的入场动画与一条循环动画 */
function stubAnimations() {
  const entry = { effect: { getComputedTiming: () => ({ endTime: 400 }) }, finish: vi.fn() }
  const spinner = { effect: { getComputedTiming: () => ({ endTime: Infinity }) }, finish: vi.fn() }
  Object.defineProperty(HTMLElement.prototype, 'getAnimations', {
    configurable: true,
    value: () => [entry, spinner],
  })
  return { entry, spinner }
}

function mountChart() {
  return mount(ChartInView, {
    slots: { default: () => h('div', { class: 'chart' }) },
    attachTo: document.body,
  })
}

/** 挂上图表：占位露出一半，返回挂上之后盯着图表本身的那台观察器 */
async function reveal(): Promise<FakeIntersectionObserver> {
  FakeIntersectionObserver.instances[0]!.report(0.5)
  await flushPromises()
  return FakeIntersectionObserver.instances.at(-1)!
}

describe('视口内动画关着', () => {
  it('图表立刻挂上，不建观察器', async () => {
    useAppStore().chartAnimateInView = false
    const wrapper = mountChart()
    await flushPromises()

    expect(wrapper.find('.chart').exists()).toBe(true)
    expect(FakeIntersectionObserver.instances).toHaveLength(0)
  })

  it('换配色时整张图当场重挂，重播入场', async () => {
    const store = useAppStore()
    store.chartAnimateInView = false
    const wrapper = mountChart()
    await flushPromises()
    const before = wrapper.find('.chart').element

    store.chartPalette = 'muted'
    await nextTick()

    expect(wrapper.find('.chart').element).not.toBe(before)
  })
})

describe('视口内动画开着时换配色', () => {
  it('露出一半的图表当场重挂，重播入场', async () => {
    const wrapper = mountChart()
    await flushPromises()
    const tracker = await reveal()
    tracker.report(0.8)
    const before = wrapper.find('.chart').element

    useAppStore().chartPalette = 'brand'
    await nextTick()

    expect(wrapper.find('.chart').element).not.toBe(before)
  })

  it('没露出一半的退回占位，滚进来再按新配色挂上', async () => {
    const wrapper = mountChart()
    await flushPromises()
    const tracker = await reveal()
    tracker.report(0.2)

    useAppStore().chartPalette = 'brand'
    await flushPromises()
    expect(wrapper.find('.chart').exists()).toBe(false)
    expect(tracker.disconnected).toBe(true)

    FakeIntersectionObserver.instances.at(-1)!.report(0.5)
    await nextTick()
    expect(wrapper.find('.chart').exists()).toBe(true)
  })

  it('还在占位的不受影响，滚进来时自然按新配色挂上', async () => {
    const wrapper = mountChart()
    await flushPromises()

    useAppStore().chartPalette = 'muted'
    await flushPromises()

    expect(wrapper.find('.chart').exists()).toBe(false)
    expect(FakeIntersectionObserver.instances).toHaveLength(1)
  })
})

describe('视口内动画开着', () => {
  it('露出一半之前只有占位，露出一半才挂上图表，观察器随之撤掉', async () => {
    const wrapper = mountChart()
    await flushPromises()

    expect(wrapper.find('.chart').exists()).toBe(false)
    expect(wrapper.find('[aria-hidden="true"]').exists()).toBe(true)
    const [observer] = FakeIntersectionObserver.instances
    expect(observer?.targets.size).toBe(1)

    observer!.report(0)
    await nextTick()
    expect(wrapper.find('.chart').exists()).toBe(false)

    // 一露头就播，滚到整张图进来时入场已经播完
    observer!.report(0.2)
    await nextTick()
    expect(wrapper.find('.chart').exists()).toBe(false)

    observer!.report(0.5)
    await nextTick()
    expect(wrapper.find('.chart').exists()).toBe(true)
    expect(observer!.disconnected).toBe(true)
  })

  it('挂过之后偏好从关换到开，已挂上的图表不撤回占位', async () => {
    const store = useAppStore()
    store.chartAnimateInView = false
    const wrapper = mountChart()
    await flushPromises()

    store.chartAnimateInView = true
    await nextTick()

    expect(wrapper.find('.chart').exists()).toBe(true)
  })

  it('打印时没进过视口的图表一并挂上，打印期间外壳写 data-motion="reduce"，打完撤掉、图表留着', async () => {
    const { entry, spinner } = stubAnimations()
    const wrapper = mountChart()
    await flushPromises()

    window.dispatchEvent(new Event('beforeprint'))
    await flushPromises()
    const shell = wrapper.find('[data-motion]')
    expect(shell.attributes('data-motion')).toBe('reduce')
    expect(shell.find('.chart').exists()).toBe(true)
    // 打印等不到动画播完：有限时长的一步播完，循环动画不碰（对它调 finish 会抛错）
    expect(entry.finish).toHaveBeenCalledTimes(1)
    expect(spinner.finish).not.toHaveBeenCalled()

    window.dispatchEvent(new Event('afterprint'))
    await nextTick()
    expect(wrapper.find('[data-motion]').exists()).toBe(false)
    expect(wrapper.find('.chart').exists()).toBe(true)
  })
})
