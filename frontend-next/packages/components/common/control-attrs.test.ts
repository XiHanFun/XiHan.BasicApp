/**
 * 字段接线只落在真正的控件上，而且只落一次。
 *
 * XhFieldControl 默认把 id 与 aria-* 合并到唯一子节点上，封装经 attrs 转交给里面的输入框。
 * 封装若再从字段上下文另取一份，字段子树里的每个封装——弹窗里的搜索框、输入组、字段数组的每一行——
 * 都会拿到同一个 id：label 的 for 落空，读屏把它们都念成外层字段。
 */
import { mount } from '@vue/test-utils'
import { useFieldControl, XhButton, XhFieldControl, XhFieldLabel, XhFieldRoot, XhInputGroupRoot } from '@xihan-ui/vue'
import { describe, expect, it } from 'vitest'
import { defineComponent, h } from 'vue'
import XInput from './XInput.vue'
import XNumberInput from './XNumberInput.vue'

/** 字段控件的 id 取自标签的 for；断言整个文档里只有一个节点叫这个 id */
function controlIdOf(wrapper: ReturnType<typeof mount>): string {
  const id = wrapper.find('label').attributes('for')
  expect(id).toBeTruthy()
  return id!
}

function nodesWithId(id: string): Element[] {
  return [...document.querySelectorAll(`[id="${id}"]`)]
}

function mountField(control: () => unknown) {
  return mount({
    render: () => h(XhFieldRoot, null, () => [h(XhFieldLabel, null, () => '名称'), control()]),
  }, { attachTo: document.body })
}

/** 仿 IconPicker：外层 as-child=false，自己取接线绑到触发钮上，弹层里另有一个搜索框 */
const Composite = defineComponent({
  setup() {
    const fieldControl = useFieldControl()
    return () => h('div', [
      h(XhButton, { ...fieldControl.value, class: 'composite-trigger' }, () => '选择'),
      h(XInput, { value: '', class: 'composite-search' }),
    ])
  },
})

describe('control-attrs 字段接线', () => {
  it('封装做 XhFieldControl 的直接子节点：id 与名字落在输入框上', () => {
    const wrapper = mountField(() => h(XhFieldControl, null, () => h(XInput, { value: '' })))
    const id = controlIdOf(wrapper)
    const input = wrapper.find('input')
    expect(input.attributes('id')).toBe(id)
    expect(input.attributes('aria-labelledby')).toContain(wrapper.find('label').attributes('id'))
    expect(nodesWithId(id)).toHaveLength(1)
    wrapper.unmount()
  })

  it('数字输入同样落在输入框上', () => {
    const wrapper = mountField(() => h(XhFieldControl, null, () => h(XNumberInput, { value: 1 })))
    const id = controlIdOf(wrapper)
    expect(wrapper.find('input').attributes('id')).toBe(id)
    expect(nodesWithId(id)).toHaveLength(1)
    wrapper.unmount()
  })

  it('复合组件里嵌着的封装不再另取一份：id 只在复合组件选定的那个控件上', () => {
    const wrapper = mountField(() => h(XhFieldControl, { asChild: false }, () => h(Composite)))
    const id = controlIdOf(wrapper)
    expect(nodesWithId(id)).toEqual([wrapper.find('.composite-trigger').element])
    expect(wrapper.find('.composite-search input').attributes('id')).not.toBe(id)
    wrapper.unmount()
  })

  it('输入组夹在中间时接线停在组上，组里的输入框不重复这个 id', () => {
    const wrapper = mountField(() => h(XhFieldControl, null, () => h(XhInputGroupRoot, null, () => [
      h(XInput, { value: '' }),
      h(XhButton, null, () => '查询'),
    ])))
    const id = controlIdOf(wrapper)
    expect(nodesWithId(id)).toHaveLength(1)
    expect(wrapper.find('input').attributes('id')).not.toBe(id)
    wrapper.unmount()
  })

  it('经插槽载荷手动 v-bind 时，角色标记不转交到输入框上，盒只画一层', () => {
    const wrapper = mountField(() => h(XhFieldControl, { asChild: false }, {
      default: (wiring: Record<string, unknown>) => h(XInput, { value: '', ...wiring }),
    }))
    const id = controlIdOf(wrapper)
    const input = wrapper.find('input')
    expect(input.attributes('id')).toBe(id)
    expect(input.attributes('data-xh-field-chrome')).toBeUndefined()
    expect(input.attributes('data-scope')).toBe('text-field')
    expect(wrapper.findAll('[data-xh-field-chrome]')).toHaveLength(1)
    wrapper.unmount()
  })
})
