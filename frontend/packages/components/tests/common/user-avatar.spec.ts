/**
 * 用户头像（UserAvatar.vue）的回退字：
 * 取名字的前两个字，头像里放不下第二个就只放一个（顶栏 24px 的头像放两个汉字会撑出圆外）；拉丁字母窄，小头像也放两个；没名字给「?」
 */
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import UserAvatar from '~/components/common/UserAvatar.vue'

function fallbackOf(props: { name?: null | string, size?: number }): string {
  const wrapper = mount(UserAvatar, { props })
  const text = wrapper.get('[data-scope="avatar"][data-part="fallback"]').text()
  wrapper.unmount()
  return text
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('用户头像的回退字', () => {
  it('汉字名：小头像只放一个字，放得下才放两个', () => {
    expect(fallbackOf({ name: '超级管理员', size: 24 })).toBe('超')
    expect(fallbackOf({ name: '超级管理员', size: 28 })).toBe('超级')
    expect(fallbackOf({ name: '超级管理员', size: 40 })).toBe('超级')
  })

  it('拉丁字母窄：小头像也放两个；名字前后的空白不算', () => {
    expect(fallbackOf({ name: '  superadmin ', size: 24 })).toBe('su')
  })

  it('没有名字给问号', () => {
    expect(fallbackOf({ name: '   ', size: 24 })).toBe('?')
    expect(fallbackOf({ name: null })).toBe('?')
  })
})
