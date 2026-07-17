import type { Graph } from '@antv/x6'

/**
 * 通用图编辑模型（引擎无关的稳定契约）
 *
 * 消费方（工作流/BPMN 设计器、ER 图、拓扑图、数据库设计器等）只面向本模型与 DiagramApi 编程，
 * 不直接触碰 AntV X6 —— 引擎细节（插件、端口、序列化）全部收敛在本包内。
 */

export interface DiagramPoint {
  x: number
  y: number
}

/** 节点端口模式：in=仅入口（左）、out=仅出口（右）、both=双向、none=无端口 */
export type DiagramPortMode = 'in' | 'out' | 'both' | 'none'

/**
 * 节点运行态：由 setNodeStatus 合并写入节点保留键 `__status`，供消费方节点组件着色。
 * 图引擎不解释其语义（工作流实例轨迹、拓扑健康度等各自映射颜色）。
 */
export type DiagramNodeStatus = 'running' | 'completed' | 'faulted' | 'waiting' | 'canceled' | 'compensated'

/** 对齐方式（作用于当前选中的多个节点） */
export type DiagramAlign = 'left' | 'right' | 'top' | 'bottom' | 'center-horizontal' | 'center-vertical'

/**
 * 节点保留数据键（由本包 API 写入，消费方节点组件读取以呈现覆盖态；不属于业务数据）：
 * - `__status`  {@link DiagramNodeStatus} | null —— 运行态着色
 * - `__highlight` boolean —— 高亮标记（如校验错误红点）
 */
export const DIAGRAM_NODE_STATUS_KEY = '__status'
export const DIAGRAM_NODE_HIGHLIGHT_KEY = '__highlight'

export interface DiagramNode {
  id: string
  /** 已注册的形状名（registerVueShape 的 shape） */
  shape: string
  x: number
  y: number
  width?: number
  height?: number
  ports?: DiagramPortMode
  /** 业务数据（节点组件经 getNode().getData() 读取；updateNodeData 全量替换） */
  data?: Record<string, unknown>
}

export interface DiagramEdge {
  id: string
  source: string
  target: string
  /** 展示标签（空串/undefined 表示无标签） */
  label?: string
  /** 虚线样式（如工作流默认分支） */
  dashed?: boolean
  /** 业务数据 */
  data?: Record<string, unknown>
}

export interface DiagramData {
  nodes: DiagramNode[]
  edges: DiagramEdge[]
}

export interface DiagramOptions {
  /** 只读模式（禁用连线/移动/删除） */
  readonly?: boolean
  /** 网格（默认开） */
  grid?: boolean
  /** 对齐线（默认开） */
  snapline?: boolean
  /** 撤销重做（默认开） */
  history?: boolean
  /** 框选（Shift + 拖拽，默认开） */
  selection?: boolean
  /** 键盘（Del 删除、Ctrl+Z/Y、Ctrl+C/V，默认开） */
  keyboard?: boolean
  /** 连线合法性校验（返回 false 阻止本次连接） */
  connectionValidator?: (source: string, target: string) => boolean
  /** 缩略图导航（默认关）；开启后 XDiagram 会在画布角落渲染小地图 */
  minimap?: boolean
  /** 缩略图容器元素（由 XDiagram 在开启 minimap 时注入，业务无需关心） */
  minimapContainer?: HTMLElement
}

export interface DiagramNodeEventPayload {
  id: string
  data: Record<string, unknown>
}

export interface DiagramEdgeEventPayload {
  id: string
  source: string
  target: string
  label?: string
  dashed?: boolean
  data: Record<string, unknown>
}

export interface DiagramEventMap {
  'node:click': DiagramNodeEventPayload
  'edge:click': DiagramEdgeEventPayload
  'blank:click': void
  /** 交互画出的新连线（已带自动生成的 id） */
  'edge:connected': DiagramEdgeEventPayload
  /** 任意单元被移除（含键盘删除），供消费方同步选中态 */
  'cell:removed': { id: string }
  /** 右键节点（x/y 为浏览器坐标，供消费方定位上下文菜单） */
  'node:contextmenu': { id: string, x: number, y: number }
  /** 右键连线 */
  'edge:contextmenu': { id: string, x: number, y: number }
  /** 右键空白处 */
  'blank:contextmenu': { x: number, y: number }
}

export interface DiagramApi {
  /** 整图装载（覆盖现有内容） */
  load: (data: DiagramData) => void
  /** 导出当前图（含拖拽后的最新坐标） */
  toData: () => DiagramData
  addNode: (node: DiagramNode) => void
  addEdge: (edge: DiagramEdge) => void
  /** 全量替换节点业务数据（节点组件随之重渲染） */
  updateNodeData: (id: string, data: Record<string, unknown>) => void
  /** 更新连线标签/样式/数据 */
  updateEdge: (id: string, patch: { label?: string, dashed?: boolean, data?: Record<string, unknown> }) => void
  removeCells: (ids: string[]) => void
  /** 浏览器坐标 → 画布坐标（拖放定位） */
  clientToLocal: (clientX: number, clientY: number) => DiagramPoint

  // ── 运行态覆盖（合并写入节点保留键，不动业务数据） ──
  /** 设置单个节点运行态（传 null 清除）——驱动消费方节点组件着色 */
  setNodeStatus: (id: string, status: DiagramNodeStatus | null) => void
  /** 批量设置节点运行态（未列出的节点保持不变） */
  setNodeStatuses: (statuses: Record<string, DiagramNodeStatus | null>) => void
  /** 高亮一组节点（其余节点取消高亮）——校验错误标记等 */
  highlightNodes: (ids: string[]) => void
  /** 清除所有节点高亮 */
  clearHighlights: () => void
  /** 将指定节点居中滚动到视口 */
  scrollToNode: (id: string) => void

  // ── 视图与编辑命令（工具栏） ──
  zoomToFit: () => void
  zoomIn: () => void
  zoomOut: () => void
  /** 缩放到 100% 并居中内容 */
  zoomToActual: () => void
  /** 当前选中的节点 id 列表 */
  getSelectedNodeIds: () => string[]
  /** 对齐当前选中的多个节点 */
  align: (mode: DiagramAlign) => void
  /** 沿水平/垂直方向等距分布当前选中的多个节点 */
  distribute: (axis: 'horizontal' | 'vertical') => void
  /** 导出当前图为 PNG（触发浏览器下载） */
  exportPng: (fileName?: string) => void

  undo: () => void
  redo: () => void
  on: <K extends keyof DiagramEventMap>(event: K, handler: (payload: DiagramEventMap[K]) => void) => void
  dispose: () => void
  /** 逃生门：直接拿 X6 Graph（仅限包内未覆盖的高级场景，业务代码慎用） */
  getGraph: () => Graph
}
