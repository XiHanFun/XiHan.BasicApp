/**
 * 图表进入视口才挂载（ChartInView）。
 *
 * 偏好「视口内动画」开着时，图表露出一半才挂上，入场动画在用户眼前播；关着照常立刻挂。
 * 挂过一次就一直挂着；打印时没进过视口的一并挂上，打印期间外壳写 data-motion="reduce"，组件库的图表据此直接画终态，
 * 减弱动效下仍留着的有限时长动画在挂上后一步播完。
 */
import { flushPromises, mount } from '@vue/test-utils'
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

describe('视口内动画关着', () => {
  it('图表立刻挂上，不建观察器', async () => {
    useAppStore().chartAnimateInView = false
    const wrapper = mountChart()
    await flushPromises()

    expect(wrapper.find('.chart').exists()).toBe(true)
    expect(FakeIntersectionObserver.instances).toHaveLength(0)
    wrapper.unmount()
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
    wrapper.unmount()
  })

  it('挂过一次就一直挂着：偏好从关换到开，已挂上的图表不撤回占位', async () => {
    const store = useAppStore()
    store.chartAnimateInView = false
    const wrapper = mountChart()
    await flushPromises()

    store.chartAnimateInView = true
    await nextTick()

    expect(wrapper.find('.chart').exists()).toBe(true)
    wrapper.unmount()
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
    wrapper.unmount()
  })
})
