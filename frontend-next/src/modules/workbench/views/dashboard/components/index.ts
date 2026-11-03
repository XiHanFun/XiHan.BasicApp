import type { Component } from 'vue'
import type { WorkbenchListWidget } from '~/stores'
import { defineComponent, h, markRaw } from 'vue'
import { useWorkbenchWidgets } from '~/stores'
import AnnouncementWidget from './AnnouncementWidget.vue'
import DeliveriesWidget from './charts/DeliveriesWidget.vue'
import ExceptionTypesWidget from './charts/ExceptionTypesWidget.vue'
import JobRunsWidget from './charts/JobRunsWidget.vue'
import LoginFailuresWidget from './charts/LoginFailuresWidget.vue'
import LoginTrendWidget from './charts/LoginTrendWidget.vue'
import RequestHeatmapWidget from './charts/RequestHeatmapWidget.vue'
import RequestKpiWidget from './charts/RequestKpiWidget.vue'
import SessionDevicesWidget from './charts/SessionDevicesWidget.vue'
import StorageMixWidget from './charts/StorageMixWidget.vue'
import TopOperationsWidget from './charts/TopOperationsWidget.vue'
import ClockWidget from './ClockWidget.vue'
import FavoritesWidget from './FavoritesWidget.vue'
import RegisteredListWidget from './RegisteredListWidget.vue'
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
  /** 所属页面（模块登记的小组件）：调用方可见的页面里有它才显示，可选模块关掉时随之隐藏 */
  page?: string
}

function widget(key: string, i18nKey: string, icon: string, defaultSpan: number, component: Component, permission?: string): WidgetDef {
  return {
    key,
    titleKey: `workbench.widgets.${i18nKey}.title`,
    descKey: `workbench.widgets.${i18nKey}.desc`,
    icon,
    defaultSpan,
    component: markRaw(component),
    permission,
  }
}

/**
 * 小组件登记表：新增小组件只需在此追加一项
 * 数据小组件取后端 workbench.widget-data（各包经 IWorkbenchWidgetProvider 登记，方案 N2），不再用前端生成的示例数据（方案 6.6）；
 * 权限码与后端登记该小组件时声明的一致，前端据此决定显隐，后端取数时照样核对。今日统计与公告不限权限：
 * 前者只列调用方看得到的指标，后者是本人的收件箱
 */
export const WIDGETS: WidgetDef[] = [
  widget('welcome', 'welcome', 'lucide:sparkles', 4, WelcomeWidget),
  widget('clock', 'clock', 'lucide:clock', 4, ClockWidget),
  widget('favorites', 'favorites', 'lucide:star', 4, FavoritesWidget),
  widget('todo', 'todo', 'lucide:check-square', 3, TodoWidget),
  widget('announcement', 'announcement', 'lucide:megaphone', 9, AnnouncementWidget),
  widget('stats', 'stats', 'lucide:gauge', 2, StatsWidget),
  widget('request-kpi', 'request_kpi', 'lucide:activity', 4, RequestKpiWidget, 'auditing.access-logs.read'),
  widget('session-devices', 'session_devices', 'lucide:monitor-smartphone', 3, SessionDevicesWidget, 'identity.sessions.read'),
  widget('login-failures', 'login_failures', 'lucide:shield-alert', 3, LoginFailuresWidget, 'auditing.login-logs.read'),
  widget('login-trend', 'login_trend', 'lucide:trending-up', 8, LoginTrendWidget, 'auditing.login-logs.read'),
  widget('storage-mix', 'storage_mix', 'lucide:hard-drive', 4, StorageMixWidget, 'files.files.read'),
  widget('top-operations', 'top_operations', 'lucide:list-ordered', 8, TopOperationsWidget, 'auditing.access-logs.read'),
  widget('request-heatmap', 'request_heatmap', 'lucide:calendar-clock', 4, RequestHeatmapWidget, 'auditing.access-logs.read'),
  widget('job-runs', 'job_runs', 'lucide:timer', 4, JobRunsWidget, 'maintenance.job-runs.read'),
  widget('exception-types', 'exception_types', 'lucide:bug', 4, ExceptionTypesWidget, 'auditing.exception-logs.read'),
  widget('deliveries', 'deliveries', 'lucide:send', 4, DeliveriesWidget, 'notification.emails.read'),
]

/** 模块登记的列表型小组件（registerWorkbenchWidget）：同一个列表外观，排在内置小组件之后 */
function registered(widget: WorkbenchListWidget): WidgetDef {
  return {
    key: widget.key,
    titleKey: widget.titleKey,
    descKey: widget.descKey,
    icon: widget.icon,
    defaultSpan: widget.defaultSpan,
    component: markRaw(defineComponent({ name: 'RegisteredWidget', setup: () => () => h(RegisteredListWidget, { widget }) })),
    permission: widget.permission,
    page: widget.page,
  }
}

// 模块 setup 在挂载之前登记完，看板这一块按需加载时已经齐了
WIDGETS.push(...useWorkbenchWidgets().map(registered))

/**
 * 小组件对当前用户是否可见：声明了权限码按权限码门控，声明了所属页面按调用方可见的页面门控，都没声明人人可见
 * @param widget 小组件定义
 * @param hasPermission 权限判定
 * @param pages 调用方可见的页面编码
 * @returns 是否可见
 */
export function canShowWidget(widget: WidgetDef | undefined, hasPermission: (code: string) => boolean, pages: ReadonlySet<string>): boolean {
  return !!widget && (!widget.permission || hasPermission(widget.permission)) && (!widget.page || pages.has(widget.page))
}

export const WIDGET_MAP: Record<string, WidgetDef> = Object.fromEntries(WIDGETS.map(widget => [widget.key, widget]))

/** 看板项：小组件键 + 宽度 */
export interface BoardItem {
  key: string
  span: number
}

/**
 * 默认看板，每行凑满 12 栅格（与 4.x 同一套尺寸档）：
 * 欢迎(4) 时间(4) 收藏入口(4) / 便签待办(3) 公告轮播(9) /
 * 今日统计(2) 访问概况(4) 在线设备(3) 登录失败原因(3) / 登录趋势(8) 存储占用(4) /
 * 访问最多的操作(8) 访问时段(4) / 作业执行(4) 异常类型(4) 消息发送(4)
 * 模块登记的小组件接在最后（如工作流的「我的待办」），看不到的由 canShowWidget 滤掉
 */
export const DEFAULT_BOARD: BoardItem[] = WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan }))
