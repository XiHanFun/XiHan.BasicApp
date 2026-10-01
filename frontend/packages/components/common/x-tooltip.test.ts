/**
 * 悬停说明 XTooltip 借被包的那个子节点当触发器（asChild）。
 *
 * 调用方常把 v-if 写在唯一的子节点上：条件为假时插槽只剩一个注释占位，
 * asChild 拿到 0 个可挂载节点直接抛错，整块页面跟着渲染失败。
 */
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { createCommentVNode, defineComponent, h, nextTick, ref } from 'vue'
import XTooltip from './XTooltip.vue'

/** 包一个按钮、按开关决定是否渲染它；条件为假时给出与模板 v-if 编译结果相同的注释占位 */
function mountWithToggle(initial: boolean) {
  const shown = ref(initial)
  const Host = defineComponent({
    setup: () => () => h(XTooltip, { content: '刷新' }, {
      default: () => [shown.value ? h('button', { type: 'button', class: 'target' }, 'R') : createCommentVNode('v-if', true)],
    }),
  })
  return { shown, wrapper: mount(Host, { attachTo: document.body }) }
}

afterEach(() => {
  document.body.innerHTML = ''
})

describe('xTooltip 子节点被 v-if 收掉', () => {
  it('没有可挂载的子节点时不装触发器，也不抛错', () => {
    const { wrapper } = mountWithToggle(false)
    expect(wrapper.find('.target').exists()).toBe(false)
    expect(wrapper.find('[data-scope="tooltip"]').exists()).toBe(false)
  })

  it('子节点出现后接上触发器，再次收掉时随之卸下', async () => {
    const { shown, wrapper } = mountWithToggle(false)

    shown.value = true
    await nextTick()
    const target = wrapper.find('.target')
    expect(target.exists()).toBe(true)
    expect(target.attributes('data-scope')).toBe('tooltip')

    shown.value = false
    await nextTick()
    expect(wrapper.find('.target').exists()).toBe(false)
  })
})
