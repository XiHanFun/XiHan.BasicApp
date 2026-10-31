/** 共用图标选择器：已选非预载图标的回显、禁用和嵌套选择都走原受控事件。 */
import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import IconPicker from './IconPicker.vue'

vi.mock('vue-i18n', () => ({ useI18n: () => ({ t: (key: string) => key }) }))
vi.mock('./offline', () => ({
  ICON_SET_META: [{ prefix: 'lucide', name: 'Lucide' }],
  loadIconNames: async () => ['check', 'circle'],
  ensureIconLoaded: async (icon: string) => icon.includes(':'),
}))
vi.mock('@iconify/vue/offline', () => ({ Icon: { props: ['icon'], template: '<svg :data-icon="icon" />' } }))

describe('图标选择器', () => {
  it('已有图标可回显且触发钮有名称，禁用时不能打开', async () => {
    const wrapper = mount(IconPicker, { props: { modelValue: 'ep:check', placeholder: '选择字典图标', disabled: true } })
    await flushPromises()
    expect(wrapper.get('.icon-picker-trigger').attributes('aria-label')).toBe('选择字典图标')
    expect(wrapper.get('svg').attributes('data-icon')).toBe('ep:check')
    expect(wrapper.get('.icon-picker-trigger').attributes('disabled')).toBeDefined()
    await wrapper.get('.icon-picker-trigger').trigger('click')
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
    wrapper.unmount()
  })

  it('菜单原有选择流程仍提交完整图标ID', async () => {
    const wrapper = mount(IconPicker, { attachTo: document.body })
    await wrapper.get('.icon-picker-trigger').trigger('click')
    await flushPromises()
    const option = document.querySelector<HTMLButtonElement>('button[aria-label="lucide:check"]')
    expect(option).not.toBeNull()
    option!.click()
    await flushPromises()
    expect(wrapper.emitted('update:modelValue')).toEqual([['lucide:check']])
    wrapper.unmount()
  })
})
