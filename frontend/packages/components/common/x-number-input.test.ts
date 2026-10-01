/**
 * XNumberInput 的固定小数位、加减钮与前后缀。
 *
 * 组件库的数字字段没有 precision：整数字段要防住 1.5，价格要固定两位小数，
 * 回舍、补齐与缺省步长都由封装给；加减钮留空，皮肤才画得出字形。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { defineComponent, h, nextTick, ref } from 'vue'
import XNumberInput from './XNumberInput.vue'

/** 受控挂载：上抛的值写回 value，与页面上的 v-model 一致 */
function mountModel(initial: number | null, props: Record<string, unknown> = {}, slots: Record<string, () => unknown> = {}) {
  const model = ref<number | null>(initial)
  const emitted: Array<number | null> = []
  const wrapper = mount(defineComponent({
    setup: () => () => h(XNumberInput, {
      ...props,
      'value': model.value,
      'onUpdate:value': (next: number | null) => {
        emitted.push(next)
        model.value = next
      },
    }, slots),
  }), { attachTo: document.body })
  return { wrapper, model, emitted, input: () => wrapper.find('input') }
}

describe('xNumberInput precision', () => {
  it('上抛的值按位数回舍：整数字段打 1.5 提交的是 2', async () => {
    const { wrapper, model, input } = mountModel(null, { precision: 0 })
    await input().setValue('1.5')
    expect(model.value).toBe(2)
    // 输入途中框里的字不动，失焦再补成格式
    expect((input().element as HTMLInputElement).value).toBe('1.5')
    await input().trigger('blur')
    await nextTick()
    expect((input().element as HTMLInputElement).value).toBe('2')
    expect(model.value).toBe(2)
    wrapper.unmount()
  })

  it('外部写入的值按位数补齐显示', () => {
    const { wrapper, input } = mountModel(12.5, { precision: 2 })
    expect((input().element as HTMLInputElement).value).toBe('12.50')
    wrapper.unmount()
  })

  it('不写 step 时按最小一位步进，显示仍是固定位数', async () => {
    const { wrapper, model, input } = mountModel(12.5, { precision: 2 })
    await input().trigger('keydown', { key: 'ArrowUp' })
    await nextTick()
    expect(model.value).toBe(12.51)
    expect((input().element as HTMLInputElement).value).toBe('12.51')
    wrapper.unmount()
  })

  it('写了 step 时以 step 为准', async () => {
    const { wrapper, model, input } = mountModel(0.5, { precision: 2, step: 0.1 })
    await input().trigger('keydown', { key: 'ArrowUp' })
    expect(model.value).toBe(0.6)
    wrapper.unmount()
  })

  it('非法位数直接报错，不夹取', () => {
    expect(() => mount(XNumberInput, { props: { value: 1, precision: 1.5 } })).toThrow(RangeError)
  })
})

describe('xNumberInput 受控回显', () => {
  it('输入 -0 时负号不被值的格式化吃掉，接着能打成 -0.5', async () => {
    const { wrapper, model, input } = mountModel(null)
    await input().setValue('-')
    expect(model.value).toBeNull()
    expect((input().element as HTMLInputElement).value).toBe('-')

    await input().setValue('-0')
    await nextTick()
    expect(model.value).toBe(0)
    expect((input().element as HTMLInputElement).value).toBe('-0')

    await input().setValue('-0.5')
    expect(model.value).toBe(-0.5)
    wrapper.unmount()
  })

  it('清空上抛 null', async () => {
    const { wrapper, emitted, input } = mountModel(3)
    await input().setValue('')
    expect(emitted).toEqual([null])
    wrapper.unmount()
  })
})

describe('xNumberInput 部件', () => {
  it('加减钮留空，交给皮肤画字形；showButton=false 时不摆', () => {
    const { wrapper } = mountModel(1)
    const increment = wrapper.find('[data-part="increment-trigger"]')
    const decrement = wrapper.find('[data-part="decrement-trigger"]')
    expect(increment.exists()).toBe(true)
    expect(increment.element.childNodes).toHaveLength(0)
    expect(decrement.element.childNodes).toHaveLength(0)
    wrapper.unmount()

    const bare = mountModel(1, { showButton: false })
    expect(bare.wrapper.find('[data-part="increment-trigger"]').exists()).toBe(false)
    bare.wrapper.unmount()
  })

  it('前后缀进组件库的 prefix / suffix 部件', () => {
    const { wrapper } = mountModel(1, {}, { prefix: () => '¥', suffix: () => 'mm' })
    expect(wrapper.find('[data-part="prefix"]').text()).toBe('¥')
    expect(wrapper.find('[data-part="suffix"]').text()).toBe('mm')
    wrapper.unmount()
  })

  it('readOnly 与 invalid 交给根部件', () => {
    const { wrapper, input } = mountModel(1, { readOnly: true, invalid: true })
    expect(input().attributes('readonly')).toBeDefined()
    expect(wrapper.find('[data-part="control"]').attributes('data-invalid')).toBe('')
    expect(wrapper.find('[data-part="increment-trigger"]').attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })
})
