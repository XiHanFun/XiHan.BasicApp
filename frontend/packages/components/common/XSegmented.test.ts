/**
 * XSegmented 组件行为测试。
 *
 * 职责边界：选项带 icon 时渲染图标；iconOnly 时文字退成只给屏幕阅读器读（sr-only），
 * 可见的只剩图标，但每一段仍有可读名称。不带 icon 的既有用法不受影响。
 * 滑动指示器与选中逻辑由组件库负责，这里不重复测。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import XSegmented from './XSegmented.vue'

const options = [
  { value: 'a', label: '帳號', icon: 'lucide:user' },
  { value: 'b', label: '手機', icon: 'lucide:smartphone' },
]

// 泛型 SFC 的 props 型别 mount 推不出来，测试只关心渲染结果，这里直接放行
function mountSegmented(props: Record<string, unknown>) {
  return mount(XSegmented, { props: { value: 'a', ...props } as never })
}

describe('xSegmented', () => {
  it('选项带 icon 时每段都渲染图标，文字照常可见', () => {
    const wrapper = mountSegmented({ options })

    expect(wrapper.findAll('[data-segmented-icon]')).toHaveLength(2)
    const labels = wrapper.findAll('[data-segmented-label]')
    expect(labels.map(label => label.text())).toEqual(['帳號', '手機'])
    expect(labels.every(label => !label.classes('sr-only'))).toBe(true)
  })

  it('iconOnly 时文字只给屏幕阅读器读，每段仍保有可读名称', () => {
    const wrapper = mountSegmented({ options, iconOnly: true })

    const labels = wrapper.findAll('[data-segmented-label]')
    expect(labels.map(label => label.text())).toEqual(['帳號', '手機'])
    expect(labels.every(label => label.classes('sr-only'))).toBe(true)
  })

  it('选项不带 icon 的既有用法只渲染文字', () => {
    const wrapper = mountSegmented({ options: [{ value: 'a', label: '列表' }, { value: 'b', label: '卡片' }] })

    expect(wrapper.findAll('[data-segmented-icon]')).toHaveLength(0)
    expect(wrapper.text()).toContain('列表')
  })
})
