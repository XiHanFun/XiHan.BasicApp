/**
 * 离线 Icon 的按需装入
 * 锁定：图标所在的图标集没装时先装、装好后显示，属性照常落到图标上；未收录的图标集不加载、显示默认插槽
 * 用真的离线 Icon 与 ep 图标集（体积小），每个用例重新导入一份模块，已装的图标集不串到别的用例
 */
import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

type IconModule = typeof import('./icon')
type OfflineModule = typeof import('./offline')

let Icon: IconModule['Icon']
let isIconSetLoaded: OfflineModule['isIconSetLoaded']

beforeEach(async () => {
  vi.resetModules()
  ;({ Icon } = await import('./icon'))
  ;({ isIconSetLoaded } = await import('./offline'))
})

describe('icon 按需装入图标集', () => {
  it('图标集没装时先装，装好后显示，宽度与类名落到图标上', async () => {
    expect(isIconSetLoaded('ep')).toBe(false)

    const wrapper = mount(Icon, {
      props: { icon: 'ep:edit' },
      attrs: { width: 14, class: 'menu-icon' },
    })
    expect(wrapper.find('svg').exists()).toBe(false)

    await vi.waitFor(() => expect(wrapper.find('svg').exists()).toBe(true))
    expect(isIconSetLoaded('ep')).toBe(true)
    expect(wrapper.find('svg').attributes('width')).toBe('14')
    expect(wrapper.find('svg').classes()).toContain('menu-icon')
  })

  it('未收录的图标集不加载，显示默认插槽', () => {
    const wrapper = mount(Icon, {
      props: { icon: 'unknown:thing' },
      slots: {
        default: () => 'fallback',
      },
    })

    expect(wrapper.text()).toBe('fallback')
    expect(isIconSetLoaded('unknown')).toBe(false)
  })
})
