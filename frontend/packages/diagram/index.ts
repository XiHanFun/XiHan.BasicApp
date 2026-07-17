/**
 * @xihan/diagram —— 通用图编辑封装（AntV X6）
 *
 * 定位：工作流/BPMN 设计器、ER 图、拓扑图、数据库设计器等图编辑场景的公共底座。
 * 应用侧只依赖本包导出的 DiagramApi / DiagramData / XDiagram / registerVueShape，
 * 不直接依赖 @antv/x6（引擎升级或替换只动本包）。
 */

export { createDiagram } from './create-diagram'
export {
  DIAGRAM_NODE_HIGHLIGHT_KEY,
  DIAGRAM_NODE_STATUS_KEY,
} from './types'
export type {
  DiagramAlign,
  DiagramApi,
  DiagramData,
  DiagramEdge,
  DiagramEdgeEventPayload,
  DiagramEventMap,
  DiagramNode,
  DiagramNodeEventPayload,
  DiagramNodeStatus,
  DiagramOptions,
  DiagramPoint,
  DiagramPortMode,
} from './types'
export { useDiagramNode } from './use-node'
export { DiagramTeleport, registerVueShape } from './vue-shape'
export type { VueShapeDefinition } from './vue-shape'
export { default as XDiagram } from './XDiagram.vue'
