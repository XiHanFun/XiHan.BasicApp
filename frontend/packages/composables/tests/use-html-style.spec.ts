/**
 * useHtmlStyle 把图表配色写到 <html data-xh-chart-palette>、高对比度写到 <html data-contrast>
 *
 * 组件库按这两个属性整套换图表的分类色槽、描边与材质；多彩分类是不写属性时的缺省方案，选它就撤掉属性
 */
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, nextTick } from 'vue'
import { COLOR_VISION_FILTER_ID } from '~/composables/color-vision-filter'
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
  document.documentElement.style.filter = ''
  document.getElementById(COLOR_VISION_FILTER_ID)?.closest('svg')?.remove()
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

describe('色弱与灰度滤镜写到 <html>', () => {
  it('色弱挂色觉校正滤镜，灰度同时开着时灰度优先，都关时撤掉', async () => {
    const store = useAppStore()
    const wrapper = mount(Host)
    await nextTick()
    expect(document.documentElement.style.filter).toBe('')

    store.setColorWeaknessEnabled(true)
    await nextTick()
    // 样式序列化会把 url(#x) 规整成 url("#x")
    expect(document.documentElement.style.filter).toMatch(new RegExp(`^url\\("?#${COLOR_VISION_FILTER_ID}"?\\)$`))
    expect(document.getElementById(COLOR_VISION_FILTER_ID)).not.toBeNull()

    store.setGrayscaleEnabled(true)
    await nextTick()
    expect(document.documentElement.style.filter).toBe('grayscale(100%)')

    store.setGrayscaleEnabled(false)
    store.setColorWeaknessEnabled(false)
    await nextTick()
    expect(document.documentElement.style.filter).toBe('')
    wrapper.unmount()
  })
})
