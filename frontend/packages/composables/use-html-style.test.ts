/**
 * useHtmlStyle 把图表配色写到 <html data-xh-chart-palette>。
 *
 * 组件库按这个属性整套换图表的分类色槽；多彩分类是不写属性时的缺省方案，选它就撤掉属性。
 */
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, nextTick } from 'vue'
import { useAppStore } from '~/stores'
import { useHtmlStyle } from './useHtmlStyle'

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
