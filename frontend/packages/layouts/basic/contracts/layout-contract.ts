import type { DropdownOption, MenuOption } from 'naive-ui'
import type { RouteRecordRaw } from 'vue-router'
import type { LayoutEventName } from '~/constants'
import type { useAppStore, useUserStore } from '~/stores'
import type { MenuMeta, MenuRoute } from '~/types'

export type LayoutMode =
  | 'side'
  | 'side-mixed'
  | 'top'
  | 'mix'
  | 'header-mix'
  | 'header-sidebar'
  | 'full'

export type HeaderMode = 'fixed' | 'static' | 'auto' | 'auto-scroll'

export interface LayoutBreadcrumbItem {
  title: string
  path: string
  icon?: string
  siblings: DropdownOption[]
}

export interface HeaderNavPropsContract {
  appStore: ReturnType<typeof useAppStore>
  layoutMode: string
  appTitle: string
  appLogo: string
  showTopMenu: boolean
  breadcrumbs: LayoutBreadcrumbItem[]
}

export interface HeaderToolbarPropsContract {
  appStore: ReturnType<typeof useAppStore>
  userStore: ReturnType<typeof useUserStore>
  isDark: boolean
  isFullscreen: boolean
  showPreferencesInHeader?: boolean
  timezoneOptions: DropdownOption[]
  localeOptions: Array<{ label: string; key: string }>
  userOptions: DropdownOption[]
}

export interface AppSidebarPropsContract {
  collapsed?: boolean
  expandOnHovering?: boolean
  floatingMode?: boolean
  floatingExpand?: boolean
  expandedWidth?: number
}

export interface SidebarMenuPropsContract {
  activeKey: string
  collapsed: boolean
  collapsedWidth?: number
  sidebarCollapsedShowTitle?: boolean
  noTopPadding?: boolean
  menuOptions: MenuOption[]
  navigationStyle: 'rounded' | 'plain'
  accordion?: boolean
}

export interface LayoutRouteMeta extends Partial<MenuMeta> {
  hidden?: boolean
  title?: string
  icon?: string
  openInNewWindow?: boolean
}

export type LayoutRouteRecord = RouteRecordRaw & {
  redirect?: RouteRecordRaw['redirect']
  children?: LayoutRouteRecord[]
  meta?: LayoutRouteMeta
}

export type LayoutMenuRoute = MenuRoute

export const LAYOUT_COMPAT_EVENTS: LayoutEventName[] = [
  'xihan-toggle-sidebar-request',
  'xihan-open-preference-drawer',
  'xihan-open-global-search',
  'xihan-lock-screen',
]

export const LAYOUT_CONTRACT_SNAPSHOT = {
  requiredAppStoreFields: [
    'layoutMode',
    'sidebarCollapsed',
    'sidebarWidth',
    'sidebarShow',
    'sidebarExpandOnHover',
    'sidebarCollapsedShowTitle',
    'headerShow',
    'headerMode',
    'navigationStyle',
    'navigationSplit',
    'navigationAccordion',
    'brandTitle',
    'brandLogo',
    'breadcrumbEnabled',
    'breadcrumbShowHome',
    'breadcrumbShowIcon',
    'breadcrumbHideOnlyOne',
    'breadcrumbStyle',
    'tabbarEnabled',
    'tabbarPersist',
    'tabbarDraggable',
    'tabbarShowIcon',
    'tabbarStyle',
    'widgetSidebarToggle',
    'widgetRefresh',
    'widgetThemeToggle',
    'widgetLanguageToggle',
    'widgetTimezone',
    'widgetFullscreen',
    'widgetNotification',
    'widgetLockScreen',
  ],
  requiredRouteMetaFields: [
    'title',
    'icon',
    'hidden',
    'keepAlive',
    'affixTab',
    'roles',
    'permissions',
  ],
  events: LAYOUT_COMPAT_EVENTS,
} as const
