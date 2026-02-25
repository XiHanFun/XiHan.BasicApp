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

// ==================== 主题颜色预设 ====================

export interface ThemeColorPreset {
  color: string
  nameKey: string
}

export interface ThemeColorGroup {
  familyKey: string
  items: ThemeColorPreset[]
}

export const THEME_COLOR_GROUPS: ThemeColorGroup[] = [
  {
    familyKey: 'preference.appearance.color.family.red',
    items: [
      { color: '#C0446A', nameKey: 'preference.appearance.color.preset.yan_zhi_hong' },
      { color: '#C0392B', nameKey: 'preference.appearance.color.preset.zhu_sha_hong' },
      { color: '#8B1A3A', nameKey: 'preference.appearance.color.preset.jiang_zi_hong' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.orange',
    items: [
      { color: '#A0522D', nameKey: 'preference.appearance.color.preset.zhe_shi_zong' },
      { color: '#C45C26', nameKey: 'preference.appearance.color.preset.zhuan_wa_cheng' },
      { color: '#D4751A', nameKey: 'preference.appearance.color.preset.hu_po_cheng' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.yellow',
    items: [
      { color: '#D4A017', nameKey: 'preference.appearance.color.preset.jiang_huang_cheng' },
      { color: '#E8C97E', nameKey: 'preference.appearance.color.preset.xiang_ye_huang' },
      { color: '#F0C040', nameKey: 'preference.appearance.color.preset.teng_huang_se' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.green',
    items: [
      { color: '#7AB648', nameKey: 'preference.appearance.color.preset.song_hua_lv' },
      { color: '#5C8A6F', nameKey: 'preference.appearance.color.preset.zhu_qing_lv' },
      { color: '#2E8B57', nameKey: 'preference.appearance.color.preset.bi_yu_lv' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.cyan',
    items: [
      { color: '#48C0A3', nameKey: 'preference.appearance.color.preset.bi_bo_qing' },
      { color: '#1A6B56', nameKey: 'preference.appearance.color.preset.shi_qing_se' },
      { color: '#3DAA8A', nameKey: 'preference.appearance.color.preset.fei_cui_qing' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.blue',
    items: [
      { color: '#3A5A8C', nameKey: 'preference.appearance.color.preset.cang_qing_lan' },
      { color: '#5A7FA0', nameKey: 'preference.appearance.color.preset.shi_ban_lan' },
      { color: '#2A5CAA', nameKey: 'preference.appearance.color.preset.ji_lan_se' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.purple',
    items: [
      { color: '#8B7BA8', nameKey: 'preference.appearance.color.preset.ding_xiang_zi' },
      { color: '#9C7B9A', nameKey: 'preference.appearance.color.preset.ou_he_zi' },
      { color: '#6A4C8C', nameKey: 'preference.appearance.color.preset.qing_lian_zi' },
    ],
  },
]

export const ALL_THEME_COLORS = [
  DEFAULT_THEME_COLOR,
  ...THEME_COLOR_GROUPS.flatMap(g => g.items.map(i => i.color)),
]
