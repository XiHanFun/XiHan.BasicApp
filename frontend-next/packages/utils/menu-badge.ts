/** 菜单标签的语气：存库取值就是组件库的 tone 词，后端只接受这六个 */
export const MENU_BADGE_TONES = ['neutral', 'brand', 'info', 'success', 'warning', 'danger'] as const

export type MenuBadgeTone = (typeof MENU_BADGE_TONES)[number]

/**
 * 菜单标签的语气。没配标签类型时，文字标签走 neutral、圆点走 danger——圆点本身就是「这里有新东西」的提醒。
 */
export function resolveMenuBadgeTone(badgeType: string | null | undefined, dot = false): MenuBadgeTone {
  return MENU_BADGE_TONES.find(tone => tone === badgeType) ?? (dot ? 'danger' : 'neutral')
}

const DOT_COLOR: Record<MenuBadgeTone, string> = {
  neutral: 'var(--xh-fg-muted)',
  brand: 'var(--xh-fg-brand)',
  info: 'var(--xh-fg-info)',
  success: 'var(--xh-fg-success)',
  warning: 'var(--xh-fg-warning)',
  danger: 'var(--xh-fg-danger)',
}

/** 圆点的填色：取语气的前景色，侧栏与菜单管理页同一套 */
export function menuBadgeDotColor(tone: MenuBadgeTone): string {
  return DOT_COLOR[tone]
}
