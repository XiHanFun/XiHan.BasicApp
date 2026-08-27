/**
 * packages/constants/app.ts 契约测试。
 *
 * 职责边界：主题模式取值、默认配置字面量、主题色预设分组（色值/文案 key 的唯一性与格式）、
 * ALL_THEME_COLORS 的派生规则，以及页脚底座项目清单。
 * 这些值同时被持久化 key 与后端用户设置引用，改动即破坏历史数据的可读性。
 */
import { describe, expect, it } from 'vitest'
import {
  ALL_THEME_COLORS,
  DEFAULT_FONT_SIZE,
  DEFAULT_LAYOUT_MODE,
  DEFAULT_LOCALE,
  DEFAULT_PAGE_SIZE,
  DEFAULT_THEME,
  DEFAULT_THEME_COLOR,
  DEFAULT_UI_RADIUS,
  FOUNDATION_PROJECTS,
  THEME_AUTO,
  THEME_COLOR_GROUPS,
  THEME_DARK,
  THEME_LIGHT,
  TOKEN_EXPIRES_IN,
} from './app'

const allPresets = THEME_COLOR_GROUPS.flatMap(group => group.items)

describe('主题模式取值', () => {
  it('三种模式取值固定为 dark / light / auto', () => {
    expect(THEME_DARK).toBe('dark')
    expect(THEME_LIGHT).toBe('light')
    expect(THEME_AUTO).toBe('auto')
  })

  it('三种模式互不相同', () => {
    expect(new Set([THEME_DARK, THEME_LIGHT, THEME_AUTO]).size).toBe(3)
  })

  it('默认主题是亮色，而不是跟随系统', () => {
    expect(DEFAULT_THEME).toBe(THEME_LIGHT)
    expect(DEFAULT_THEME).not.toBe(THEME_AUTO)
  })
})

describe('默认配置', () => {
  it('默认语言为简体中文', () => {
    expect(DEFAULT_LOCALE).toBe('zh-CN')
  })

  it('默认分页大小为 20 且是正整数', () => {
    expect(DEFAULT_PAGE_SIZE).toBe(20)
    expect(Number.isInteger(DEFAULT_PAGE_SIZE)).toBe(true)
  })

  it('令牌有效期恰好是 7 天的毫秒数', () => {
    expect(TOKEN_EXPIRES_IN).toBe(604800000)
    expect(TOKEN_EXPIRES_IN).toBe(7 * 24 * 60 * 60 * 1000)
  })

  it('默认布局模式为侧边栏', () => {
    expect(DEFAULT_LAYOUT_MODE).toBe('side')
  })

  it('默认圆角与字号在合理区间，避免默认值本身就把界面撑坏', () => {
    expect(DEFAULT_UI_RADIUS).toBe(0.25)
    expect(DEFAULT_FONT_SIZE).toBe(14)
    expect(DEFAULT_UI_RADIUS).toBeGreaterThanOrEqual(0)
    expect(DEFAULT_FONT_SIZE).toBeGreaterThan(0)
  })
})

describe('主题色预设分组', () => {
  it('默认主题色是合法的 6 位十六进制色值', () => {
    expect(DEFAULT_THEME_COLOR).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it('共七个色系分组，每组三个预设色', () => {
    expect(THEME_COLOR_GROUPS).toHaveLength(7)
    expect(THEME_COLOR_GROUPS.map(group => group.items.length)).toEqual([3, 3, 3, 3, 3, 3, 3])
  })

  it('每个预设色都是合法的 6 位十六进制色值', () => {
    const invalid = allPresets.filter(item => !/^#[0-9a-f]{6}$/i.test(item.color))
    expect(invalid).toEqual([])
  })

  it('预设色值全局唯一，不出现两个同色不同名的条目', () => {
    const colors = allPresets.map(item => item.color.toLowerCase())
    expect(new Set(colors).size).toBe(colors.length)
  })

  it('预设文案 key 全局唯一，否则两个色块会显示同一个名字', () => {
    const nameKeys = allPresets.map(item => item.nameKey)
    expect(new Set(nameKeys).size).toBe(nameKeys.length)
  })

  it('分组文案 key 全局唯一', () => {
    const familyKeys = THEME_COLOR_GROUPS.map(group => group.familyKey)
    expect(new Set(familyKeys).size).toBe(familyKeys.length)
  })

  it('全部文案 key 落在 preference.appearance.color 命名空间下，供 i18n 统一维护', () => {
    const wrong = [
      ...THEME_COLOR_GROUPS.map(group => group.familyKey),
      ...allPresets.map(item => item.nameKey),
    ].filter(key => !key.startsWith('preference.appearance.color.'))

    expect(wrong).toEqual([])
  })

  it('分组 key 与预设 key 使用不同的子命名空间，避免层级互相覆盖', () => {
    expect(THEME_COLOR_GROUPS.every(group => group.familyKey.includes('.family.'))).toBe(true)
    expect(allPresets.every(item => item.nameKey.includes('.preset.'))).toBe(true)
  })
})

describe('全部主题色清单的派生', () => {
  it('首项为默认主题色，选色面板据此定位当前值', () => {
    expect(ALL_THEME_COLORS[0]).toBe(DEFAULT_THEME_COLOR)
  })

  it('长度等于默认色加上全部分组预设色', () => {
    expect(ALL_THEME_COLORS).toHaveLength(1 + allPresets.length)
    expect(ALL_THEME_COLORS).toHaveLength(22)
  })

  it('展开后的顺序与分组内顺序一致', () => {
    expect(ALL_THEME_COLORS.slice(1)).toEqual(allPresets.map(item => item.color))
  })

  it('全部色值互不重复，默认色不与任一预设色撞车', () => {
    const lowered = ALL_THEME_COLORS.map(color => color.toLowerCase())
    expect(new Set(lowered).size).toBe(lowered.length)
  })
})

describe('页脚底座项目', () => {
  it('列出后端框架与前端组件库两项', () => {
    expect(FOUNDATION_PROJECTS.map(project => project.name)).toEqual([
      'XiHan.Framework',
      'XiHan.UI',
    ])
  })

  it('全部链接为 https 的 GitHub 地址，避免页脚出现明文 http 外链', () => {
    const invalid = FOUNDATION_PROJECTS.filter(
      project => !project.url.startsWith('https://github.com/'),
    )
    expect(invalid).toEqual([])
  })

  it('项目名与链接均不重复', () => {
    expect(new Set(FOUNDATION_PROJECTS.map(p => p.name)).size).toBe(FOUNDATION_PROJECTS.length)
    expect(new Set(FOUNDATION_PROJECTS.map(p => p.url)).size).toBe(FOUNDATION_PROJECTS.length)
  })
})
