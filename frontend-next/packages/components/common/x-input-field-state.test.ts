/**
 * 封装控件的三态（disabled / readOnly / invalid）随外层 Field 走。
 *
 * XiHan.UI 的控件根按「本处写了 > 字段 > 表单」取值；封装若把这三个 prop 默认成 false，
 * 等于每一处都"写了"，字段校验出错、表单整体禁用都到不了画描边的 control 部件——
 * 登录页提交空表单时只剩字段组的红色竖条，输入盒本身不变红，就是这么来的。
 */
import { mount } from '@vue/test-utils'
import { XhFieldControl, XhFieldRoot } from '@xihan-ui/vue'
import { describe, expect, it } from 'vitest'
import { h } from 'vue'
import XInput from './XInput.vue'

function mountInField(fieldProps: Record<string, unknown>, inputProps: Record<string, unknown> = {}) {
  return mount({
    render: () => h(XhFieldRoot, fieldProps, () => h(XhFieldControl, null, () => h(XInput, { value: '', ...inputProps }))),
  })
}

function control(wrapper: ReturnType<typeof mount>) {
  return wrapper.find('[data-xh-field-chrome]')
}

describe('xInput 三态随字段走', () => {
  it('字段 invalid 时，画描边的 control 部件拿到 data-invalid', () => {
    const wrapper = mountInField({ invalid: true })
    expect(control(wrapper).attributes('data-invalid')).toBe('')
    expect(wrapper.find('input').attributes('aria-invalid')).toBe('true')
  })

  it('字段 disabled / readOnly 一样落到 control 与 input 上', () => {
    const disabled = mountInField({ disabled: true })
    expect(control(disabled).attributes('data-disabled')).toBe('')
    expect(disabled.find('input').attributes('disabled')).toBeDefined()

    const readOnly = mountInField({ readOnly: true })
    expect(control(readOnly).attributes('data-readonly')).toBe('')
    expect(readOnly.find('input').attributes('readonly')).toBeDefined()
  })

  it('本处显式写了以本处为准：字段 invalid 但控件写 invalid=false 就不变红', () => {
    const wrapper = mountInField({ invalid: true }, { invalid: false })
    expect(control(wrapper).attributes('data-invalid')).toBeUndefined()
  })

  it('不在字段里、也没写三态时，control 上没有任何状态标记', () => {
    const wrapper = mount(XInput, { props: { value: '' } })
    const el = control(wrapper)
    expect(el.attributes('data-invalid')).toBeUndefined()
    expect(el.attributes('data-disabled')).toBeUndefined()
    expect(el.attributes('data-readonly')).toBeUndefined()
  })

  it('密码档同样随字段走', () => {
    const wrapper = mountInField({ invalid: true }, { type: 'password' })
    expect(control(wrapper).attributes('data-invalid')).toBe('')
  })
})
