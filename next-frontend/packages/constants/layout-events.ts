export const LAYOUT_EVENT_TOGGLE_SIDEBAR_REQUEST = 'xihan-toggle-sidebar-request'
export const LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER = 'xihan-open-preference-drawer'
export const LAYOUT_EVENT_OPEN_GLOBAL_SEARCH = 'xihan-open-global-search'
export const LAYOUT_EVENT_LOCK_SCREEN = 'xihan-lock-screen'

export const LAYOUT_EVENT_NAMES = [
  LAYOUT_EVENT_TOGGLE_SIDEBAR_REQUEST,
  LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER,
  LAYOUT_EVENT_OPEN_GLOBAL_SEARCH,
  LAYOUT_EVENT_LOCK_SCREEN,
] as const

export type LayoutEventName = (typeof LAYOUT_EVENT_NAMES)[number]
