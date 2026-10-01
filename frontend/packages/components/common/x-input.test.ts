/**
 * XInput 对外的事件与部件：clear / enter 两个事件、字数、后缀、原生 type 与多行行数。
 *
 * 调用方写 `@clear`、`show-count`、`#suffix` 都依赖这一层真的把它们接到组件库的部件上；
 * 没接时监听落成输入框上的死 DOM 事件、属性落成无意义的特性，页面上什么也不发生且不报错。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { h } from 'vue'
import XInput from './XInput.vue'

function mountInput(props: Record<string, unknown> = {}, slots: Record<string, () => unknown> = {}) {
  return mount(XInput, { props: { value: 'abc', ...props }, slots, attachTo: document.body })
}

describe('xInput clear 事件', () => {
  it('点清除钮先清值再报 clear', async () => {
    const wrapper = mountInput({ clearable: true })
    await wrapper.find('[data-part="clear-trigger"]').trigger('click')
    expect(wrapper.emitted('update:value')).toEqual([['']])
    expect(wrapper.emitted('clear')).toHaveLength(1)
    wrapper.unmount()
  })

  it('escape 清空同样报 clear；组合中与不可清空时不报', async () => {
    const wrapper = mountInput({ clearable: true })
    const input = wrapper.find('input')

    await input.trigger('keydown', { key: 'Escape', isComposing: true })
    expect(wrapper.emitted('clear')).toBeUndefined()

    await input.trigger('keydown', { key: 'Escape' })
    expect(wrapper.emitted('update:value')).toEqual([['']])
    expect(wrapper.emitted('clear')).toHaveLength(1)
    wrapper.unmount()

    const plain = mountInput()
    await plain.find('input').trigger('keydown', { key: 'Escape' })
    expect(plain.emitted('clear')).toBeUndefined()
    plain.unmount()

    const readOnly = mountInput({ clearable: true, readOnly: true })
    await readOnly.find('input').trigger('keydown', { key: 'Escape' })
    expect(readOnly.emitted('clear')).toBeUndefined()
    expect(readOnly.emitted('update:value')).toBeUndefined()
    readOnly.unmount()
  })

  it('密码档 Escape 清空也报 clear', async () => {
    const wrapper = mountInput({ type: 'password', clearable: true })
    await wrapper.find('input').trigger('keydown', { key: 'Escape' })
    expect(wrapper.emitted('update:value')).toEqual([['']])
    expect(wrapper.emitted('clear')).toHaveLength(1)
    wrapper.unmount()
  })
})

describe('xInput enter 事件', () => {
  it('回车按下即报；输入法组合中的回车（isComposing / keyCode 229）与长按连发不报', async () => {
    const wrapper = mountInput()
    const input = wrapper.find('input')

    await input.trigger('keydown', { key: 'Enter', isComposing: true })
    await input.trigger('keydown', { key: 'Enter', keyCode: 229 })
    expect(wrapper.emitted('enter')).toBeUndefined()

    await input.trigger('keydown', { key: 'Enter' })
    await input.trigger('keydown', { key: 'Enter', repeat: true })
    expect(wrapper.emitted('enter')).toHaveLength(1)
    wrapper.unmount()
  })

  it('密码档同样报，多行档的回车是换行不报', async () => {
    const password = mountInput({ type: 'password' })
    await password.find('input').trigger('keydown', { key: 'Enter' })
    expect(password.emitted('enter')).toHaveLength(1)
    password.unmount()

    const textarea = mountInput({ type: 'textarea' })
    await textarea.find('textarea').trigger('keydown', { key: 'Enter' })
    expect(textarea.emitted('enter')).toBeUndefined()
    textarea.unmount()
  })
})

describe('xInput 部件', () => {
  it('showCount 出字数部件，有上限时为「已用 / 上限」', () => {
    const wrapper = mountInput({ showCount: true, maxLength: 10 })
    const count = wrapper.find('[data-part="count"]')
    expect(count.exists()).toBe(true)
    expect(count.text()).toBe('3 / 10')
    expect(count.attributes('hidden')).toBeUndefined()

    expect(mountInput().find('[data-part="count"]').exists()).toBe(false)
  })

  it('suffix 插槽进组件库的 suffix 部件，与前缀同在盒内', () => {
    const wrapper = mountInput({}, { prefix: () => h('i', 'P'), suffix: () => h('i', 'S') })
    const control = wrapper.find('[data-part="control"]')
    expect(control.find('[data-part="prefix"]').text()).toBe('P')
    expect(control.find('[data-part="suffix"]').text()).toBe('S')
  })

  it('type 交给根部件，email / tel / url / search 落成原生 type；缺省 text', () => {
    expect(mountInput().find('input').attributes('type')).toBe('text')
    expect(mountInput({ type: 'email' }).find('input').attributes('type')).toBe('email')
    expect(mountInput({ type: 'search' }).find('input').attributes('type')).toBe('search')
    expect(mountInput({ type: 'password' }).find('input').attributes('type')).toBe('password')
  })

  // autosize 的量高要真实排版，jsdom 量不出行高，起始行数交给组件库那一路只能在浏览器里看
  it('多行档：rows 落到 textarea，没写时不发这个键；inputmode 落到输入框', () => {
    const fixed = mountInput({ type: 'textarea', rows: 4 })
    expect(fixed.find('textarea').attributes('rows')).toBe('4')
    expect(fixed.find('textarea').attributes('type')).toBeUndefined()

    expect(mountInput({ type: 'textarea' }).find('textarea').attributes('rows')).toBeUndefined()

    expect(mountInput({ inputmode: 'numeric' }).find('input').attributes('inputmode')).toBe('numeric')
  })
})
