/** 字典离线图标装载：后加载后再挂载SVG，切换和卸载使旧回调失效。 */
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import DictTagIcon from './DictTagIcon.vue'

const { load } = vi.hoisted(() => ({ load: vi.fn<(icon: string) => Promise<boolean>>() }))
vi.mock('~/iconify/offline', () => ({ ensureIconLoaded: load }))
vi.mock('~/iconify', () => ({ Icon: { props: ['icon'], template: '<svg :data-icon="icon" />' } }))

describe('字典图标异步显示', () => {
  it('旧图标迟到不能提前挂载尚未完成的新图标', async () => {
    let finishOld!: (value: boolean) => void
    let finishNew!: (value: boolean) => void
    load.mockImplementationOnce(() => new Promise((resolve) => {
      finishOld = resolve
    })).mockImplementationOnce(() => new Promise((resolve) => {
      finishNew = resolve
    }))
    const wrapper = mount(DictTagIcon, { props: { icon: 'ep:check' } })
    expect(wrapper.find('svg').exists()).toBe(false)
    await wrapper.setProps({ icon: 'carbon:checkmark' })
    finishOld(true)
    await flushPromises()
    expect(wrapper.find('svg').exists()).toBe(false)
    finishNew(true)
    await flushPromises()
    expect(wrapper.get('svg').attributes('data-icon')).toBe('carbon:checkmark')
    expect(wrapper.get('svg').attributes('aria-hidden')).toBe('true')
    wrapper.unmount()
  })

  it('不存在的图标不生成错误SVG，卸载后迟到结果不重新挂载', async () => {
    load.mockResolvedValueOnce(false)
    const wrapper = mount(DictTagIcon, { props: { icon: 'ep:missing' } })
    await flushPromises()
    expect(wrapper.find('svg').exists()).toBe(false)
    let finish!: (value: boolean) => void
    load.mockImplementationOnce(() => new Promise((resolve) => {
      finish = resolve
    }))
    await wrapper.setProps({ icon: 'ep:check' })
    wrapper.unmount()
    finish(true)
    await flushPromises()
    expect(wrapper.find('svg').exists()).toBe(false)
  })
})
