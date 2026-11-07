/**
 * 字段名后面的问号提示（XFieldLabel / XHelpTip）与编辑弹窗缺省初始焦点的选择器
 *
 * 字段说明不放在字段下方，收进字段名后面的问号里；问号是按钮，不能落在 label 里；
 * 弹窗打开时初始焦点要越过问号，落到第一个真正的控件上
 */
import { mount } from '@vue/test-utils'
import { XhFieldControl, XhFieldRoot } from '@xihan-ui/vue'
import { afterEach, describe, expect, it } from 'vitest'
import { defineComponent, h } from 'vue'
import { FIRST_CONTROL_SELECTOR } from './edit-modal-focus'
import XFieldLabel from './XFieldLabel.vue'

function mountField(help: null | string | undefined) {
  const Host = defineComponent({
    setup: () => () => h(XhFieldRoot, null, () => [
      h(XFieldLabel, { help }, () => '编码'),
      h(XhFieldControl, null, () => [
        h('input', { name: 'code' }),
      ]),
    ]),
  })
  return mount(Host, { attachTo: document.body })
}

afterEach(() => {
  document.body.innerHTML = ''
})

describe('xFieldLabel', () => {
  it('给了说明：字段名后面出一枚问号，名字就是这段说明，问号不在 label 里', () => {
    const wrapper = mountField('小写字母开头')
    const label = wrapper.get('label')
    const tip = wrapper.get('[data-help-tip]')

    expect(label.text()).toBe('编码')
    expect(tip.element.tagName).toBe('BUTTON')
    expect(tip.attributes('aria-label')).toBe('小写字母开头')
    expect(tip.element.closest('label')).toBeNull()
    // 字段名与问号并排在同一行里，这一行排在控件之前
    const row = label.element.parentElement!
    expect(row.contains(tip.element)).toBe(true)
    expect(row.compareDocumentPosition(wrapper.get('input').element) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    // label 照常指向控件
    expect(label.attributes('for')).toBe(wrapper.get('input').attributes('id'))
    wrapper.unmount()
  })

  it('没给说明、说明为空：只有字段名，不出问号', () => {
    for (const help of [undefined, null, '']) {
      const wrapper = mountField(help)
      expect(wrapper.get('label').text()).toBe('编码')
      expect(wrapper.find('[data-help-tip]').exists()).toBe(false)
      wrapper.unmount()
    }
  })
})

describe('编辑弹窗缺省的初始焦点', () => {
  function pick(html: string): string | null {
    document.body.innerHTML = `<div data-scope="dialog" data-part="content">${html}</div>`
    const picked = document.querySelector('[data-part="content"]')!.querySelector(FIRST_CONTROL_SELECTOR)
    return picked?.getAttribute('data-id') ?? null
  }

  it('越过关闭钮与字段名后面的问号，落到第一个真正的控件上', () => {
    expect(pick(`
      <button data-scope="dialog" data-part="close-trigger" data-id="close"></button>
      <span><label>名称</label><button data-help-tip data-id="tip"></button></span>
      <input data-id="name" />
      <button data-id="save"></button>
    `)).toBe('name')
  })

  it('首个控件是下拉的触发钮时落在它上面；禁用、隐藏输入与不进 tab 序列的节点都越过', () => {
    expect(pick(`
      <button data-scope="dialog" data-part="drag-trigger" data-id="drag"></button>
      <input type="hidden" data-id="hidden" />
      <select tabindex="-1" data-id="native"></select>
      <input disabled data-id="locked" />
      <button role="combobox" data-id="kind"></button>
      <input data-id="code" />
    `)).toBe('kind')
  })

  it('别的组件里同名的部件不受影响：只越过对话框自己的关闭钮', () => {
    expect(pick(`
      <button data-scope="tag" data-part="close-trigger" data-id="tag-close"></button>
      <input data-id="code" />
    `)).toBe('tag-close')
  })

  it('没有可落的控件时什么也不选，交回组件库兜底', () => {
    expect(pick('<a href="#" data-id="link">文档</a><button data-help-tip data-id="tip"></button>')).toBeNull()
  })
})
