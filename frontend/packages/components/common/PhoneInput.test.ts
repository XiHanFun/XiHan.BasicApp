/**
 * PhoneInput 组件行为测试。
 *
 * 职责边界：对外只吐 E.164、回填时按国家拆开、号码不成立时不吐值并置 valid=false、
 * 父层经 v-model 把自己刚吐出的值原样传回来时不冲掉编辑中的号码、挂载与外部改值时也吐
 * valid、字段接线（id/aria-*）落在真正的号码输入框上。
 * 国家下拉的交互由组件库负责，这里不重复测。
 */
import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it } from 'vitest'
import { i18n } from '~/locales'
import PhoneInput from './PhoneInput.vue'

function mountInput(props: Record<string, unknown> = {}, attrs: Record<string, unknown> = {}) {
  return mount(PhoneInput, { props, attrs, global: { plugins: [i18n] } })
}

// setCountry 会把选择写入 localStorage（记住国家），测试之间必须互相隔离
beforeEach(() => {
  localStorage.clear()
})

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

  describe('父层经 v-model 回传自己刚吐出的值（回声）不冲掉编辑中的号码', () => {
    it('号码暂时不成立、吐出空字符串后，空字符串原样回传时保留正在输入的号码', async () => {
      const wrapper = mountInput({ value: '+886912345678' })
      await wrapper.vm.setNational('091234567')
      const echoed = wrapper.emitted('update:value')?.at(-1)?.[0]
      expect(echoed).toBe('')

      await wrapper.setProps({ value: echoed as string })

      expect(wrapper.vm.national).toBe('091234567')
    })

    it('吐出合法 E.164 后，原样回传时国家与号码保持不变', async () => {
      const wrapper = mountInput({ value: '' })
      await wrapper.vm.setCountry('TW')
      await wrapper.vm.setNational('0912345678')
      const echoed = wrapper.emitted('update:value')?.at(-1)?.[0]
      expect(echoed).toBe('+886912345678')

      await wrapper.setProps({ value: echoed as string })

      expect(wrapper.vm.national).toBe('0912345678')
      expect(wrapper.vm.country).toBe('TW')
    })

    it('外部把值换成另一个号码（非回声）时按新值重新解析', async () => {
      const wrapper = mountInput({ value: '+886912345678' })

      await wrapper.setProps({ value: '+8613800138000' })

      expect(wrapper.vm.country).toBe('CN')
      expect(wrapper.vm.national).toBe('13800138000')
    })

    it('外部把值重置为空字符串（非回声）时清空号码', async () => {
      const wrapper = mountInput({ value: '+8613800138000' })

      await wrapper.setProps({ value: '' })

      expect(wrapper.vm.national).toBe('')
    })
  })

  describe('valid 在挂载与外部改值时也要吐出，不只在用户编辑时', () => {
    it('挂载时传入合法 E.164 立即吐出 valid=true', () => {
      const wrapper = mountInput({ value: '+8613800138000' })

      expect(wrapper.emitted('valid')?.at(-1)).toEqual([true])
    })

    it('挂载时未传值立即吐出 valid=true', () => {
      const wrapper = mountInput({})

      expect(wrapper.emitted('valid')?.at(-1)).toEqual([true])
    })

    it('外部把值换成不成立的号码（非回声）时吐出 valid=false', async () => {
      const wrapper = mountInput({ value: '+886912345678' })

      await wrapper.setProps({ value: 'not-a-phone-number' })

      expect(wrapper.emitted('valid')?.at(-1)).toEqual([false])
    })
  })

  it('落在标签上的 id 落到真正的号码输入框，而不是外层包装的 div', () => {
    const wrapper = mountInput({ value: '' }, { id: 'phone-x' })

    expect(wrapper.find('[data-scope="text-field"][data-part="input"]').attributes('id')).toBe('phone-x')
    expect(wrapper.attributes('id')).toBeUndefined()
  })

  describe('存量非 E.164 写法（解析不了）时回填原始数字', () => {
    it('挂载时传入非空但解析不了的号码，按原始数字回填本地号码框，国家保持默认，valid=false', () => {
      const wrapper = mountInput({ value: '0912345678' })

      // 无法解析（没有国码信息），国家保持默认兜底而非被清空
      expect(wrapper.vm.country).toBe('CN')
      expect(wrapper.vm.national).toBe('0912345678')
      expect(wrapper.emitted('valid')?.at(-1)).toEqual([false])
    })

    it('外部把值换成非空但解析不了的号码（非回声）时，按原始数字回填', async () => {
      const wrapper = mountInput({ value: '+886912345678' })

      await wrapper.setProps({ value: '0912-345-678' })

      expect(wrapper.vm.national).toBe('0912345678')
      expect(wrapper.emitted('valid')?.at(-1)).toEqual([false])
    })
  })

  it('setCountry 记住选择，供下次默认使用', async () => {
    const wrapper = mountInput({ value: '' })
    await wrapper.vm.setCountry('JP')

    expect(localStorage.getItem('xihan_phone_country')).toBe('JP')
  })
})
