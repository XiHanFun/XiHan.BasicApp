/**
 * Material You 动态取色：
 * 派生色调覆盖页面底、卡片、侧栏、顶栏、文字、边框与淡底，色相跟品牌色、彩度有上限、灰色品牌色仍派生出灰，明度贴着静态配色；
 * 根元素上开关、换明暗时按清单写入或撤掉内联值，不留上一档的残值
 */
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { effectScope, nextTick } from 'vue'
import { useTheme } from '~/hooks'
import { deriveMaterialPalette, MATERIAL_PALETTE_KEYS } from '~/hooks/material-palette'
import { useAppStore } from '~/stores'

function parts(value: string | undefined) {
  const [h, s, l] = value!.split(' ').map(v => Number.parseFloat(v))
  return { h: h!, s: s!, l: l! }
}

describe('派生色调', () => {
  it('亮色写满清单，暗色只少 --input', () => {
    expect(Object.keys(deriveMaterialPalette('#5887f7', false)).sort()).toEqual([...MATERIAL_PALETTE_KEYS].sort())
    expect(Object.keys(deriveMaterialPalette('#5887f7', true)).sort()).toEqual(MATERIAL_PALETTE_KEYS.filter(k => k !== '--input').sort())
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

describe('根元素上的写入与撤掉', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.removeAttribute('style')
    setActivePinia(createPinia())
  })

  it('开着写入；换到暗色撤掉 --input；关掉撤掉全部', async () => {
    const store = useAppStore()
    effectScope(true).run(() => useTheme())
    const root = document.documentElement.style
    await nextTick()
    for (const key of MATERIAL_PALETTE_KEYS)
      expect(root.getPropertyValue(key)).not.toBe('')

    store.setTheme('dark')
    await nextTick()
    expect(root.getPropertyValue('--input')).toBe('')
    expect(root.getPropertyValue('--card')).not.toBe('')

    store.setThemeDynamicColor(false)
    await nextTick()
    for (const key of MATERIAL_PALETTE_KEYS)
      expect(root.getPropertyValue(key)).toBe('')
  })
})
