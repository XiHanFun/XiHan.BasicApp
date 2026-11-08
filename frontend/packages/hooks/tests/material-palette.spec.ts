/**
 * Material You 动态取色：
 * 派生色调覆盖页面底、卡片、侧栏、顶栏、文字、边框与淡底，色相跟品牌色、彩度有上限、灰色品牌色仍派生出灰，明度贴着静态配色；
 * 亮暗两套写成样式规则（带 .dark 类的侧栏、顶栏也取得到），开着写入、换主题色重算、关掉撤掉
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { effectScope, nextTick } from 'vue'
import { useTheme } from '~/hooks'
import { deriveMaterialPalette, MATERIAL_PALETTE_KEYS, materialPaletteCss } from '~/hooks/material-palette'
import { useAppStore } from '~/stores'

function parts(value: string | undefined) {
  const [h, s, l] = value!.split(' ').map(v => Number.parseFloat(v))
  return { h: h!, s: s!, l: l! }
}

describe('派生色调', () => {
  it('亮暗两套都写满清单：根元素同时匹配两条规则，少写一个就会漏进另一套的取值', () => {
    expect(Object.keys(deriveMaterialPalette('#5887f7', false)).sort()).toEqual([...MATERIAL_PALETTE_KEYS].sort())
    expect(Object.keys(deriveMaterialPalette('#5887f7', true)).sort()).toEqual([...MATERIAL_PALETTE_KEYS].sort())
  })

  it.each([
    ['#5887f7', 222],
    ['#22c55e', 142],
  ])('%s：每个变量都取品牌色相 %i', (hex, hue) => {
    for (const dark of [false, true]) {
      for (const value of Object.values(deriveMaterialPalette(hex, dark)))
        expect(parts(value).h).toBe(hue)
    }
  })

  it('灰色品牌色派生出的仍是灰', () => {
    for (const dark of [false, true]) {
      for (const value of Object.values(deriveMaterialPalette('#808080', dark)))
        expect(parts(value).s).toBe(0)
    }
  })

  it('彩度有上限：饱和度拉满的品牌色也只让边框带一点色', () => {
    expect(parts(deriveMaterialPalette('#0000ff', false)['--border']).s).toBe(28)
    expect(parts(deriveMaterialPalette('#0000ff', true)['--border']).s).toBe(26)
  })

  it('明度贴着静态配色：亮色的底都浅、字深，暗色反之', () => {
    const light = deriveMaterialPalette('#5887f7', false)
    const dark = deriveMaterialPalette('#5887f7', true)
    for (const key of [
      '--background-deep',
      '--background',
      '--card',
      '--popover',
      '--sidebar',
      '--header',
      '--muted',
    ] as const) {
      expect(parts(light[key]).l).toBeGreaterThanOrEqual(94)
      expect(parts(dark[key]).l).toBeLessThanOrEqual(16)
    }
    expect(parts(light['--foreground']).l).toBeLessThanOrEqual(20)
    expect(parts(dark['--foreground']).l).toBeGreaterThanOrEqual(95)
  })
})

describe('样式规则', () => {
  it('亮色规则在前、暗色规则在后，选择器都叠写一次以压过静态配色', () => {
    const css = materialPaletteCss('#5887f7')
    const light = css.indexOf(':root:root {')
    const dark = css.indexOf('.dark.dark {')

    expect(light).toBeGreaterThanOrEqual(0)
    expect(dark).toBeGreaterThan(light)
    expect(css.slice(dark)).toContain(`--sidebar: ${deriveMaterialPalette('#5887f7', true)['--sidebar']};`)
    expect(css.slice(dark)).toContain(`--input: ${deriveMaterialPalette('#5887f7', true)['--input']};`)
  })
})

describe('写入与撤掉', () => {
  const styleOf = () => document.getElementById('xh-material-palette')

  beforeEach(() => {
    localStorage.clear()
    document.documentElement.removeAttribute('style')
    styleOf()?.remove()
    setActivePinia(createPinia())
  })

  it('开着写成样式规则、不写根元素内联；换主题色跟着重算；关掉撤掉', async () => {
    const store = useAppStore()
    effectScope(true).run(() => useTheme())
    await nextTick()
    expect(styleOf()?.textContent).toBe(materialPaletteCss(store.themeColor))
    for (const key of MATERIAL_PALETTE_KEYS)
      expect(document.documentElement.style.getPropertyValue(key)).toBe('')

    store.setThemeColor('#22c55e')
    await nextTick()
    expect(styleOf()?.textContent).toBe(materialPaletteCss('#22c55e'))

    store.setThemeDynamicColor(false)
    await nextTick()
    expect(styleOf()).toBeNull()
  })
})
