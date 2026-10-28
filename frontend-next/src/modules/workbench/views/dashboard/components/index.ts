import type { Component } from 'vue'
import { markRaw } from 'vue'
import ClockWidget from './ClockWidget.vue'
import FavoritesWidget from './FavoritesWidget.vue'
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

function widget(key: string, i18nKey: string, icon: string, defaultSpan: number, component: Component): WidgetDef {
  return {
    key,
    titleKey: `workbench.widgets.${i18nKey}.title`,
    descKey: `workbench.widgets.${i18nKey}.desc`,
    icon,
    defaultSpan,
    component: markRaw(component),
  }
}

/**
 * 小组件登记表：新增小组件只需在此追加一项。
 * 6.0 只放数据真实或纯本地的小组件（欢迎、时钟、收藏夹、本机待办）；统计、公告与图表等业务数据小组件随后端 workbench.* 操作
 * （各包的 IWorkbenchWidgetProvider，方案 N2）加入，不再用前端生成的示例数据（方案 6.6）。
 */
export const WIDGETS: WidgetDef[] = [
  widget('welcome', 'welcome', 'lucide:sparkles', 4, WelcomeWidget),
  widget('clock', 'clock', 'lucide:clock', 4, ClockWidget),
  widget('favorites', 'favorites', 'lucide:star', 4, FavoritesWidget),
  widget('todo', 'todo', 'lucide:check-square', 4, TodoWidget),
]

export const WIDGET_MAP: Record<string, WidgetDef> = Object.fromEntries(WIDGETS.map(widget => [widget.key, widget]))

/** 看板项：小组件键 + 宽度 */
export interface BoardItem {
  key: string
  span: number
}

/** 默认看板，每行凑满 12 栅格：欢迎(4) 时间(4) 收藏入口(4) / 便签待办(4) */
export const DEFAULT_BOARD: BoardItem[] = WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan }))
