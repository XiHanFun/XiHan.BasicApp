/**
 * 菜单标签的语气：存库取值就是组件库的 tone 词；没配时文字标签走 neutral、圆点走 danger。
 */
import { describe, expect, it } from 'vitest'
import { MENU_BADGE_TONES, menuBadgeDotColor, resolveMenuBadgeTone } from './menu-badge'

describe('resolveMenuBadgeTone', () => {
  it('存库取值原样作语气', () => {
    for (const tone of MENU_BADGE_TONES)
      expect(resolveMenuBadgeTone(tone)).toBe(tone)
  })

  it('没配标签类型：文字标签 neutral，圆点 danger', () => {
    expect(resolveMenuBadgeTone(undefined)).toBe('neutral')
    expect(resolveMenuBadgeTone(null)).toBe('neutral')
    expect(resolveMenuBadgeTone(undefined, true)).toBe('danger')
  })

  it('配了圆点也按存库语气着色', () => {
    expect(resolveMenuBadgeTone('success', true)).toBe('success')
  })
})

describe('menuBadgeDotColor', () => {
  it('每种语气都取组件库的前景色令牌', () => {
    for (const tone of MENU_BADGE_TONES)
      expect(menuBadgeDotColor(tone)).toMatch(/^var\(--xh-fg-[a-z]+\)$/)
    expect(menuBadgeDotColor('neutral')).toBe('var(--xh-fg-muted)')
  })
})
