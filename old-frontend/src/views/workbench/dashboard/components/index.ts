import type { Component } from 'vue'
import { markRaw } from 'vue'
import AnnouncementWidget from './AnnouncementWidget.vue'
import ClockWidget from './ClockWidget.vue'
import FavoritesWidget from './FavoritesWidget.vue'
import StatsWidget from './StatsWidget.vue'
import TodoWidget from './TodoWidget.vue'
import WelcomeWidget from './WelcomeWidget.vue'

/** 小组件定义：键、i18n 标题/描述键、图标、默认宽度（12 栅格）、组件、可选权限码 */
export interface WidgetDef {
  key: string
  titleKey: string
  descKey: string
  icon: string
  defaultSpan: number
  component: Component
  /** 可选权限码：声明后仅拥有该权限的用户可见/可添加此小组件（缺省人人可见） */
  permission?: string
}

/** 小组件登记表：新增小组件只需在此追加一项 */
export const WIDGETS: WidgetDef[] = [
  { key: 'clock', titleKey: 'workbench.widgets.clock.title', descKey: 'workbench.widgets.clock.desc', icon: 'lucide:clock', defaultSpan: 2, component: markRaw(ClockWidget) },
  { key: 'welcome', titleKey: 'workbench.widgets.welcome.title', descKey: 'workbench.widgets.welcome.desc', icon: 'lucide:sparkles', defaultSpan: 4, component: markRaw(WelcomeWidget) },
  { key: 'stats', titleKey: 'workbench.widgets.stats.title', descKey: 'workbench.widgets.stats.desc', icon: 'lucide:gauge', defaultSpan: 3, component: markRaw(StatsWidget), permission: 'saas:user-statistics:read' },
  { key: 'favorites', titleKey: 'workbench.widgets.favorites.title', descKey: 'workbench.widgets.favorites.desc', icon: 'lucide:star', defaultSpan: 3, component: markRaw(FavoritesWidget) },
  { key: 'todo', titleKey: 'workbench.widgets.todo.title', descKey: 'workbench.widgets.todo.desc', icon: 'lucide:check-square', defaultSpan: 3, component: markRaw(TodoWidget) },
  { key: 'announcement', titleKey: 'workbench.widgets.announcement.title', descKey: 'workbench.widgets.announcement.desc', icon: 'lucide:megaphone', defaultSpan: 6, component: markRaw(AnnouncementWidget) },
]

export const WIDGET_MAP: Record<string, WidgetDef> = Object.fromEntries(WIDGETS.map(widget => [widget.key, widget]))

/** 看板项：小组件键 + 宽度 */
export interface BoardItem {
  key: string
  span: number
}

/** 默认看板：时间(2) 欢迎(4) 今日统计(3) 收藏入口(3) / 便签待办(3) 公告轮播(6) */
export const DEFAULT_BOARD: BoardItem[] = WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan }))
