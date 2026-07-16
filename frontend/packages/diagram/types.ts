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
  zoomToFit: () => void
  undo: () => void
  redo: () => void
  on: <K extends keyof DiagramEventMap>(event: K, handler: (payload: DiagramEventMap[K]) => void) => void
  dispose: () => void
  /** 逃生门：直接拿 X6 Graph（仅限包内未覆盖的高级场景，业务代码慎用） */
  getGraph: () => Graph
}
