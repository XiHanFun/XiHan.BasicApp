// ==================== 主题模式 ====================

export const THEME_DARK = 'dark'
export const THEME_LIGHT = 'light'
export const THEME_AUTO = 'auto'

// ==================== 默认配置 ====================

export const DEFAULT_LOCALE = 'zh-CN'
export const DEFAULT_THEME = THEME_LIGHT
export const DEFAULT_PAGE_SIZE = 20
export const TOKEN_EXPIRES_IN = 7 * 24 * 60 * 60 * 1000
export const DEFAULT_THEME_COLOR = '#5887f7'
export const DEFAULT_LAYOUT_MODE = 'side'
export const DEFAULT_UI_RADIUS = 0.5
export const DEFAULT_FONT_SIZE = 14

export const LAYOUT_MODE_OPTIONS = [
  { label: '垂直', value: 'side' },
  { label: '双列菜单', value: 'side-mixed' },
  { label: '水平', value: 'top' },
  { label: '侧边导航', value: 'header-sidebar' },
  { label: '混合垂直', value: 'mix' },
  { label: '混合双列', value: 'header-mix' },
  { label: '内容全屏', value: 'full' },
]
