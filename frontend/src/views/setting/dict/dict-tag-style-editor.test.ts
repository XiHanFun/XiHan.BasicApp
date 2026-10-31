/** 字典样式编辑的受控契约：隐藏保留选择、清除恢复配色、重新打开与提交锁定。 */
import type { DictTagStyle } from '~/types/dictionary'
import { mount } from '@vue/test-utils'
import { XhSwitch } from '@xihan-ui/vue'
import { describe, expect, it, vi } from 'vitest'
import { defineComponent, ref } from 'vue'
import { createI18n } from 'vue-i18n'
import setting from '@/locales/langs/zh-CN/setting'
import DictTagStyleEditor from './DictTagStyleEditor.vue'

vi.mock('~/iconify', () => ({
  Icon: { props: ['icon'], template: '<svg :data-icon="icon" />' },
  IconPicker: { name: 'IconPicker', props: ['modelValue', 'disabled'], emits: ['update:modelValue'], template: '<button :disabled="disabled">选择图标</button>' },
}))

/** @param initial 初始服务端样式。 @returns 使用真实父级v-model的编辑器，避免只断言一次emit而漏掉回显回退。 */
function createEditor(initial: DictTagStyle) {
  const value = ref<DictTagStyle>(initial)
  const disabled = ref(false)
  const wrapper = mount(defineComponent({
    components: { DictTagStyleEditor },
    setup: () => ({ value, disabled }),
    template: '<DictTagStyleEditor v-model="value" label="已完成" :disabled="disabled" />',
  }), { global: { plugins: [createI18n({ legacy: false, locale: 'zh-CN', missingWarn: false, fallbackWarn: false, messages: { 'zh-CN': { setting } } })] } })
  return { wrapper, value, disabled }
}

describe('字典图标编辑', () => {
  it('关闭、重新开启和换配色不丢失自定义选择；清除只重置图标', async () => {
    const { wrapper, value } = createEditor({ tone: 'success', icon: 'ep:check', textColor: '#112233' })
    const toggle = wrapper.findComponent(XhSwitch)
    toggle.vm.$emit('update:checked', false)
    await wrapper.vm.$nextTick()
    expect(value.value).toMatchObject({ showIcon: false, icon: 'ep:check', textColor: '#112233' })
    expect(wrapper.findComponent({ name: 'IconPicker' }).props('disabled')).toBe(true)
    toggle.vm.$emit('update:checked', true)
    await wrapper.vm.$nextTick()
    expect(value.value.icon).toBe('ep:check')
    const reset = wrapper.findAll('button').find(button => button.text() === '恢复默认图标')!
    await reset.trigger('click')
    expect(value.value).toMatchObject({ icon: null, tone: 'success', textColor: '#112233' })
    expect(wrapper.text()).toContain('跟随当前配色的默认图标')
    wrapper.unmount()
  })

  it('已保存的隐藏配置重开仍隐藏；保存中禁用图标选择且默认恢复完整重置', async () => {
    const { wrapper, value, disabled } = createEditor({ showIcon: false, icon: 'carbon:checkmark', tone: 'info' })
    expect(wrapper.findComponent(XhSwitch).props('checked')).toBe(false)
    const restore = wrapper.findAll('button').find(button => button.text() === '恢复默认样式')!
    await restore.trigger('click')
    expect(value.value).toEqual({})
    expect(wrapper.findComponent(XhSwitch).props('checked')).toBe(true)
    const picker = wrapper.findComponent({ name: 'IconPicker' })
    picker.vm.$emit('update:modelValue', 'ep:check')
    await wrapper.vm.$nextTick()
    expect(value.value.icon).toBe('ep:check')
    disabled.value = true
    await wrapper.vm.$nextTick()
    expect(picker.props('disabled')).toBe(true)
    expect(wrapper.findComponent(XhSwitch).props('disabled')).toBe(true)
    wrapper.unmount()
  })
})
