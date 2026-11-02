/**
 * 工作台小组件登记点。
 * 职责：可选功能模块把自己的看板小组件登记进来，工作台看板与内置小组件一起列出——
 * 工作台不依赖任何具体功能模块，删除模块目录即小组件消失（与壳层扩展同一个做法）。
 * 登记的是后端 `workbench.widget-data` 的列表型小组件：看板用同一个列表外观渲染，模块只给键、文案与跳转。
 */
import { shallowReactive } from 'vue'

/** 一个模块登记的列表型小组件 */
export interface WorkbenchListWidget {
  /** 看板上的键（与内置小组件不重名，也是看板布局里存的键） */
  key: string
  /** 后端小组件键（`workbench.widget-data` 的 key，形态 List：值是总数，条目是最近几条） */
  dataKey: string
  /** 标题文案键 */
  titleKey: string
  /** 添加面板里的说明文案键 */
  descKey: string
  /** 没有条目时的文案键 */
  emptyKey: string
  /** 图标（iconify） */
  icon: string
  /** 缺省宽度（12 栅格） */
  defaultSpan: number
  /** 所属页面：调用方可见的页面里有它才显示（可选模块关掉时后端不下发它的页面，小组件随之隐藏） */
  page: string
  /** 标题栏「查看全部」跳转的路由 */
  moreLink: string
  /** 可选权限码：声明后仅拥有该权限的用户可见 */
  permission?: string
}

const widgets = shallowReactive<WorkbenchListWidget[]>([])

/**
 * 登记一个看板小组件；模块 setup 钩子（app.mount 之前）调用。
 * @param widget 小组件定义。
 */
export function registerWorkbenchWidget(widget: WorkbenchListWidget): void {
  widgets.push(widget)
}

/**
 * 读取全部已登记的看板小组件；工作台看板消费。
 * @returns 小组件列表（登记顺序）。
 */
export function useWorkbenchWidgets(): readonly WorkbenchListWidget[] {
  return widgets
}
