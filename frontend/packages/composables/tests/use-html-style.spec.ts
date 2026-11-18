/**
 * useHtmlStyle 把图表配色写到 <html data-xh-chart-palette>、高对比度写到 <html data-contrast>
 *
 * 组件库按这两个属性整套换图表的分类色槽、描边与材质；多彩分类是不写属性时的缺省方案，选它就撤掉属性
 */
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, nextTick } from 'vue'
import { useHtmlStyle } from '~/composables/useHtmlStyle'
import { useAppStore } from '~/stores'

const Host = defineComponent({
  setup() {
    useHtmlStyle()
    return () => null
  },
})

beforeEach(() => {
  setActivePinia(createPinia())
})

afterEach(() => {
  delete document.documentElement.dataset.xhChartPalette
  delete document.documentElement.dataset.contrast
})

describe('图表配色写到 <html>', () => {
  it('选了别的方案就写属性，换回多彩分类就撤掉', async () => {
    const store = useAppStore()
    const wrapper = mount(Host)
    await nextTick()
    expect(document.documentElement.hasAttribute('data-xh-chart-palette')).toBe(false)

    store.setChartPalette('muted')
    await nextTick()
    expect(document.documentElement.getAttribute('data-xh-chart-palette')).toBe('muted')

    store.setChartPalette('monochrome')
    await nextTick()
    expect(document.documentElement.getAttribute('data-xh-chart-palette')).toBe('monochrome')

    store.setChartPalette('categorical')
    await nextTick()
    expect(document.documentElement.hasAttribute('data-xh-chart-palette')).toBe(false)
    wrapper.unmount()
  })
})

describe('高对比度写到 <html>', () => {
  it('打开写 more，关掉回到 default', async () => {
    const store = useAppStore()
    const wrapper = mount(Host)
    await nextTick()
    expect(document.documentElement.dataset.contrast).toBe('default')

    store.setHighContrastEnabled(true)
    await nextTick()
    expect(document.documentElement.dataset.contrast).toBe('more')

    store.setHighContrastEnabled(false)
    await nextTick()
    expect(document.documentElement.dataset.contrast).toBe('default')
    wrapper.unmount()
  })
})
