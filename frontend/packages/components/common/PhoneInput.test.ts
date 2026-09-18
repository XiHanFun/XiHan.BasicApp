/**
 * PhoneInput 组件行为测试。
 *
 * 职责边界：对外只吐 E.164、回填时按国家拆开、号码不成立时不吐值并置 valid=false。
 * 国家下拉的交互由组件库负责，这里不重复测。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import PhoneInput from './PhoneInput.vue'

function mountInput(props: Record<string, unknown> = {}) {
  return mount(PhoneInput, { props, global: { plugins: [i18n] } })
}

describe('phoneInput', () => {
  it('输入本地号码后按所选国家吐出 E.164', async () => {
    const wrapper = mountInput({ value: '' })
    await wrapper.vm.setCountry('TW')
    await wrapper.vm.setNational('0912345678')

    expect(wrapper.emitted('update:value')?.at(-1)).toEqual(['+886912345678'])
    expect(wrapper.emitted('valid')?.at(-1)).toEqual([true])
  })

  it('号码不成立时吐空值并标记无效', async () => {
    const wrapper = mountInput({ value: '' })
    await wrapper.vm.setCountry('TW')
    await wrapper.vm.setNational('0912')

    expect(wrapper.emitted('update:value')?.at(-1)).toEqual([''])
    expect(wrapper.emitted('valid')?.at(-1)).toEqual([false])
  })

  it('传入 E.164 时按国家与本地号码回填', () => {
    const wrapper = mountInput({ value: '+8613800138000' })

    expect(wrapper.vm.country).toBe('CN')
    expect(wrapper.vm.national).toBe('13800138000')
  })

  it('未传值时国家取浏览器地区（测试环境固定 zh-CN → CN）', () => {
    const wrapper = mountInput({})

    expect(wrapper.vm.country).toBe('CN')
  })
})
