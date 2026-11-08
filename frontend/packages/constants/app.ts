/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

// ==================== 主题模式 ====================

export const THEME_DARK = 'dark'
export const THEME_LIGHT = 'light'
export const THEME_AUTO = 'auto'

// ==================== 默认配置 ====================

export const DEFAULT_LOCALE = 'en-US'
/** 已上架语言（与 i18n 实例载入的语言包一一对应，由 i18n-instance.test.ts 守门） */
export const SUPPORTED_LOCALES = [
  'en-US',
  'zh-CN',
  'zh-TW',
  'ja-JP',
  'ko-KR',
  'de-DE',
  'es-ES',
  'fr-FR',
  'pt-BR',
  'ru-RU',
] as const
/** 浏览器语言全部不受支持时的退回语言：外语用户读英文比读中文容易 */
export const FALLBACK_LOCALE = 'en-US'
export const DEFAULT_THEME = THEME_LIGHT
export const DEFAULT_PAGE_SIZE = 20
export const TOKEN_EXPIRES_IN = 7 * 24 * 60 * 60 * 1000
export const DEFAULT_THEME_COLOR = '#5887f7'
export const DEFAULT_LAYOUT_MODE = 'side'
export const DEFAULT_UI_RADIUS = 0.25
export const DEFAULT_FONT_SIZE = 14

// ==================== 界面密度 ====================

/** 对应 XiHan.UI 的 data-density 轴：comfortable 控件 sm 28 / md 32 / lg 36，compact 收到 24 / 28 / 32 */
export const UI_DENSITY_COMFORTABLE = 'comfortable'
export const UI_DENSITY_COMPACT = 'compact'
export const UI_DENSITIES = [UI_DENSITY_COMFORTABLE, UI_DENSITY_COMPACT] as const
export type UiDensity = (typeof UI_DENSITIES)[number]
/** 缺省宽松：组件库的宽松档即字段 28 / 按钮 32 */
export const DEFAULT_UI_DENSITY: UiDensity = UI_DENSITY_COMFORTABLE

// ==================== 图表 ====================

/** 对应 XiHan.UI 的 data-xh-chart-palette：主题单色 / 多彩分类 / 柔和品牌 / 莫兰迪柔彩，整套换掉图表的分类色槽与色块内文字 */
export const CHART_PALETTES = [
  'monochrome',
  'categorical',
  'brand',
  'muted',
] as const
export type ChartPalette = (typeof CHART_PALETTES)[number]
/** 缺省多彩分类：组件库不写 data-xh-chart-palette 时就是这套 */
export const DEFAULT_CHART_PALETTE: ChartPalette = 'categorical'

// ==================== 主题颜色预设 ====================

export interface ThemeColorPreset {
  color: string
  nameKey: string
}

/** 默认色之外的预设：饱和、明亮而不刺眼的常见现代色，不分色系；其余颜色走自定义取色 */
export const THEME_COLOR_PRESETS: ThemeColorPreset[] = [
  { color: '#8B5CF6', nameKey: 'preference.appearance.color.preset.violet' },
  { color: '#EC4899', nameKey: 'preference.appearance.color.preset.rose' },
  { color: '#EF4444', nameKey: 'preference.appearance.color.preset.vermilion' },
  { color: '#F97316', nameKey: 'preference.appearance.color.preset.orange' },
  { color: '#F59E0B', nameKey: 'preference.appearance.color.preset.amber' },
  { color: '#10B981', nameKey: 'preference.appearance.color.preset.emerald' },
  { color: '#06B6D4', nameKey: 'preference.appearance.color.preset.cyan' },
]

export const ALL_THEME_COLORS = [
  DEFAULT_THEME_COLOR,
  ...THEME_COLOR_PRESETS.map(preset => preset.color),
]

// ==================== 底座项目 ====================

/** 页脚署名列出的底座项目：后端框架与前端组件库 */
export const FOUNDATION_PROJECTS = [
  { name: 'XiHan.Framework', url: 'https://github.com/XiHanFun/XiHan.Framework' },
  { name: 'XiHan.UI', url: 'https://github.com/XiHanFun/XiHan.UI' },
] as const
