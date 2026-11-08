/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/** 将 hex 颜色解析为 HSL 数值三元组 [h(0-360), s(0-100), l(0-100)] */
export function hexToHsl(hex: string): [number, number, number] {
  const r = Number.parseInt(hex.slice(1, 3), 16) / 255
  const g = Number.parseInt(hex.slice(3, 5), 16) / 255
  const b = Number.parseInt(hex.slice(5, 7), 16) / 255
  const max = Math.max(r, g, b)
  const min = Math.min(r, g, b)
  let h = 0
  let s = 0
  const l = (max + min) / 2
  if (max !== min) {
    const d = max - min
    s = l > 0.5
      ? d / (2 - max - min)
      : d / (max + min)
    switch (max) {
      case r:
        h = ((g - b) / d + (g < b
          ? 6
          : 0)) / 6
        break
      case g:
        h = ((b - r) / d + 2) / 6
        break
      default:
        h = ((r - g) / d + 4) / 6
    }
  }
  return [
    Math.round(h * 360),
    Math.round(s * 100),
    Math.round(l * 100),
  ]
}

/** Material You 改写的应用配色变量 */
export const MATERIAL_PALETTE_KEYS = [
  '--background-deep',
  '--background',
  '--card',
  '--popover',
  '--sidebar',
  '--header',
  '--foreground',
  '--muted',
  '--muted-foreground',
  '--accent',
  '--heavy',
  '--border',
  '--input',
  '--ring',
] as const

export type MaterialPaletteKey = (typeof MATERIAL_PALETTE_KEYS)[number]

/**
 * 从品牌色推导 Material You 色调，明暗自适应
 * 页面底、卡片、浮层、侧栏、顶栏、文字、边框与淡底都带品牌色相；彩度随品牌色的饱和度走、各有上限，灰色品牌色派生出的仍是灰
 * 明度贴着静态配色，字与底的对比不变；暗色不写 --input，静态的半透明白叠在任何底上都成立
 * 返回变量名 → "H S% L%"
 */
export function deriveMaterialPalette(hex: string, dark: boolean): Partial<Record<MaterialPaletteKey, string>> {
  const [h, s, l] = hexToHsl(hex)
  const clamp = (v: number) => Math.max(0, Math.min(100, Math.round(v)))
  /** 同色相的一档：彩度取品牌饱和度的 ratio 倍、不超过 cap */
  const tone = (ratio: number, cap: number, lightness: number) => `${h} ${clamp(Math.min(s * ratio, cap))}% ${lightness}%`

  if (dark) {
    return {
      '--background-deep': tone(0.35, 30, 8),
      '--background': tone(0.3, 26, 11.5),
      '--card': tone(0.3, 26, 11.5),
      '--popover': tone(0.28, 24, 14),
      '--sidebar': tone(0.32, 28, 10.5),
      '--header': tone(0.32, 28, 10.5),
      '--foreground': tone(0.12, 10, 95),
      '--muted': tone(0.3, 24, 16),
      '--muted-foreground': tone(0.2, 14, 66),
      '--accent': tone(0.45, 38, 20),
      '--heavy': tone(0.45, 34, 25),
      '--border': tone(0.35, 26, 23),
      '--ring': `${h} ${clamp(Math.min(s, 80))}% ${clamp(Math.max(l, 60))}%`,
    }
  }
  return {
    '--background-deep': tone(0.45, 40, 94),
    '--background': tone(0.45, 40, 99),
    '--card': tone(0.45, 40, 99),
    '--popover': tone(0.45, 40, 99),
    '--sidebar': tone(0.48, 42, 96.5),
    '--header': tone(0.48, 42, 96.5),
    '--foreground': tone(0.25, 22, 20),
    '--muted': tone(0.36, 32, 94.5),
    '--muted-foreground': tone(0.2, 16, 45),
    '--accent': tone(0.55, 50, 92.5),
    '--heavy': tone(0.5, 45, 87),
    '--border': tone(0.32, 28, 88),
    '--input': tone(0.32, 28, 88),
    '--ring': `${h} ${clamp(Math.min(s, 85))}% ${clamp(Math.max(Math.min(l, 55), 40))}%`,
  }
}

/**
 * 亮暗两套色调写成样式规则：:root 取亮色、.dark 取暗色
 * 不写根元素内联：侧栏、顶栏等带 .dark 类的区域会在自己身上重声明配色变量，根上的内联值传不进去
 * 选择器叠写一次抬高特指度，压过 variables.css 的 :root / .dark；根元素同时是 .dark 时靠暗色规则在后取胜
 */
export function materialPaletteCss(hex: string): string {
  const block = (selector: string, palette: Partial<Record<MaterialPaletteKey, string>>) => {
    const lines = Object.entries(palette).map(([name, value]) => `  ${name}: ${value};`)
    return `${selector} {\n${lines.join('\n')}\n}`
  }
  return [
    block(':root:root', deriveMaterialPalette(hex, false)),
    block('.dark.dark', deriveMaterialPalette(hex, true)),
  ].join('\n')
}
