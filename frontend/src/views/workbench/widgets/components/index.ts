import type { Component } from 'vue'
import { markRaw } from 'vue'
import ClockWidget from './ClockWidget.vue'
import QuickLinksWidget from './QuickLinksWidget.vue'
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

/** 小组件登记表：新增小组件只需在此追加一项。默认宽度按工作台默认布局（一行 2+3+3+4=12 栅格） */
export const WIDGETS: WidgetDef[] = [
  { key: 'clock', titleKey: 'workbench.widgets.clock.title', descKey: 'workbench.widgets.clock.desc', icon: 'lucide:clock', defaultSpan: 2, component: markRaw(ClockWidget) },
  { key: 'welcome', titleKey: 'workbench.widgets.welcome.title', descKey: 'workbench.widgets.welcome.desc', icon: 'lucide:sparkles', defaultSpan: 3, component: markRaw(WelcomeWidget) },
  { key: 'todo', titleKey: 'workbench.widgets.todo.title', descKey: 'workbench.widgets.todo.desc', icon: 'lucide:check-square', defaultSpan: 3, component: markRaw(TodoWidget) },
  { key: 'quickLinks', titleKey: 'workbench.widgets.quick_links.title', descKey: 'workbench.widgets.quick_links.desc', icon: 'lucide:zap', defaultSpan: 4, component: markRaw(QuickLinksWidget) },
]

export const WIDGET_MAP: Record<string, WidgetDef> = Object.fromEntries(WIDGETS.map(widget => [widget.key, widget]))

/** 看板项：小组件键 + 宽度 */
export interface BoardItem {
  key: string
  span: number
}

/** 默认看板 */
export const DEFAULT_BOARD: BoardItem[] = WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan }))
