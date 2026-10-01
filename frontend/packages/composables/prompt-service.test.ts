/**
 * 取值弹窗的字段结构：输入框套在 control 部件里才有盒，大写锁定提示是盒里的一格。
 *
 * 描边、底色与聚焦环画在 control 上（Field Chrome），直接把 input 放进根部件里得到的是一个无框的输入框。
 */
import type { VNodeChild } from 'vue'
import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { h, reactive } from 'vue'
import { prompt } from './prompt-service'

interface CapturedPrompt {
  body: (value: Record<string, string>) => VNodeChild
}

const captured: { options: CapturedPrompt | null } = { options: null }

vi.mock('./ui-service', () => ({
  dialogService: () => ({
    prompt: (options: CapturedPrompt) => {
      captured.options = options
      return Promise.resolve(null)
    },
  }),
}))

function renderBody(fields: Parameters<typeof prompt>[0]['fields']) {
  void prompt({ title: '修改用户名', fields })
  const value = reactive<Record<string, string>>(Object.fromEntries(fields.map(f => [f.key, f.value ?? ''])))
  return mount({ render: () => h('div', captured.options!.body(value) as never) })
}

describe('prompt 字段结构', () => {
  it('文本字段的输入框套在 control 部件里', () => {
    const wrapper = renderBody([{ key: 'userName', value: 'admin', placeholder: '新用户名' }])
    const control = wrapper.find('[data-scope="text-field"][data-part="control"]')
    expect(control.exists()).toBe(true)
    expect(control.attributes('data-xh-field-chrome')).toBe('')
    expect(control.find('input').attributes('aria-label')).toBe('新用户名')
  })

  it('密码字段的大写锁定提示在盒内、排在显隐钮前', () => {
    const wrapper = renderBody([{ key: 'password', type: 'password', placeholder: '当前密码' }])
    const control = wrapper.find('[data-scope="password-input"][data-part="control"]')
    const parts = [...control.element.children].map(el => el.getAttribute('data-part'))
    expect(parts).toEqual(['input', 'caps-lock-indicator', 'visibility-trigger'])
  })
})
