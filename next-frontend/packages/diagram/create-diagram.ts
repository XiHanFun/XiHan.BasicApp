import type { Cell, Edge, Node } from '@antv/x6'
import type {
  DiagramAlign,
  DiagramApi,
  DiagramData,
  DiagramEdge,
  DiagramEdgeEventPayload,
  DiagramNode,
  DiagramNodeStatus,
  DiagramOptions,
} from './types'
import { Graph } from '@antv/x6'
import { Clipboard } from '@antv/x6-plugin-clipboard'
import { Export } from '@antv/x6-plugin-export'
import { History } from '@antv/x6-plugin-history'
import { Keyboard } from '@antv/x6-plugin-keyboard'
import { MiniMap } from '@antv/x6-plugin-minimap'
import { Selection } from '@antv/x6-plugin-selection'
import { Snapline } from '@antv/x6-plugin-snapline'
import { DIAGRAM_NODE_HIGHLIGHT_KEY, DIAGRAM_NODE_STATUS_KEY } from './types'

/** 连线保留数据键（label/dashed 的权威副本，业务 data 与其隔离） */
const EDGE_META_KEY = '__diagramEdgeMeta'

const EDGE_STROKE = '#94a3b8'

/** X6 鼠标事件的结构子集（取 clientX/Y 定位菜单，避免耦合 X6 内部事件类型） */
interface MouseEventLike { preventDefault: () => void, clientX: number, clientY: number }

function edgeAttrs(dashed: boolean | undefined) {
  return {
    line: {
      stroke: EDGE_STROKE,
      strokeWidth: 1.5,
      strokeDasharray: dashed ? '6 4' : '',
      targetMarker: { name: 'block', width: 10, height: 8 },
    },
  }
}

function edgeLabels(label: string | undefined) {
  return label
    ? [{ position: 0.5, attrs: { label: { text: label, fontSize: 11, fill: '#64748b' } } }]
    : []
}

function writeEdge(edge: Edge, patch: { label?: string, dashed?: boolean, data?: Record<string, unknown> }) {
  const previous = (edge.getData() ?? {}) as Record<string, unknown>
  const previousMeta = (previous[EDGE_META_KEY] ?? {}) as { label?: string, dashed?: boolean }
  const meta = {
    label: patch.label !== undefined ? patch.label : previousMeta.label,
    dashed: patch.dashed !== undefined ? patch.dashed : previousMeta.dashed,
  }
  const data = patch.data !== undefined ? patch.data : (previous.payload as Record<string, unknown> | undefined) ?? {}

  edge.setData({ [EDGE_META_KEY]: meta, payload: data }, { overwrite: true })
  edge.setLabels(edgeLabels(meta.label))
  edge.setAttrs(edgeAttrs(meta.dashed))
}

function readEdge(edge: Edge): DiagramEdgeEventPayload {
  const raw = (edge.getData() ?? {}) as Record<string, unknown>
  const meta = (raw[EDGE_META_KEY] ?? {}) as { label?: string, dashed?: boolean }
  return {
    id: edge.id,
    source: edge.getSourceCellId(),
    target: edge.getTargetCellId(),
    label: meta.label,
    dashed: meta.dashed,
    data: (raw.payload as Record<string, unknown> | undefined) ?? {},
  }
}

function toX6Node(node: DiagramNode) {
  const mode = node.ports ?? 'both'
  const items: { group: string, id: string }[] = []
  if (mode === 'in' || mode === 'both')
    items.push({ group: 'in', id: 'in' })
  if (mode === 'out' || mode === 'both')
    items.push({ group: 'out', id: 'out' })

  return {
    id: node.id,
    shape: node.shape,
    x: node.x,
    y: node.y,
    width: node.width,
    height: node.height,
    data: node.data ?? {},
    ports: { items },
  }
}

function readNode(node: Node): DiagramNode {
  const position = node.position()
  const ports = node.getPorts()
  const hasIn = ports.some(port => port.group === 'in')
  const hasOut = ports.some(port => port.group === 'out')
  return {
    id: node.id,
    shape: node.shape,
    x: Math.round(position.x),
    y: Math.round(position.y),
    ports: hasIn && hasOut ? 'both' : hasIn ? 'in' : hasOut ? 'out' : 'none',
    data: (node.getData() ?? {}) as Record<string, unknown>,
  }
}

/**
 * 创建通用图编辑器（AntV X6 的唯一装配点）
 *
 * @param container 画布容器元素（尺寸由外层布局给定）
 * @param options 行为开关
 * @returns 引擎无关的编辑 API
 */
export function createDiagram(container: HTMLElement, options: DiagramOptions = {}): DiagramApi {
  const readonly = options.readonly ?? false

  const graph: Graph = new Graph({
    container,
    autoResize: true,
    grid: options.grid === false ? false : { visible: true, size: 12 },
    panning: { enabled: true },
    mousewheel: { enabled: true, minScale: 0.2, maxScale: 2 },
    interacting: readonly ? { nodeMovable: false, edgeMovable: false } : undefined,
    connecting: {
      snap: true,
      allowBlank: false,
      allowNode: false,
      allowEdge: false,
      allowLoop: false,
      highlight: true,
      connector: { name: 'smooth' },
      createEdge() {
        return graph.createEdge({
          attrs: edgeAttrs(false),
          data: { [EDGE_META_KEY]: {}, payload: {} },
          zIndex: 0,
        })
      },
      validateConnection({ sourceCell, targetCell, sourceMagnet, targetMagnet }) {
        if (readonly || !sourceMagnet || !targetMagnet || !sourceCell || !targetCell)
          return false
        return options.connectionValidator?.(sourceCell.id, targetCell.id) ?? true
      },
    },
    highlighting: {
      magnetAvailable: {
        name: 'stroke',
        args: { attrs: { stroke: '#4f83f0', strokeWidth: 2 } },
      },
    },
  })

  if (options.selection !== false) {
    graph.use(new Selection({
      enabled: !readonly,
      rubberband: true,
      // Shift 框选空白区域；Shift/Ctrl 点选叠加多选（对齐/分布需要）
      modifiers: 'shift',
      multipleSelectionModifiers: ['ctrl', 'meta', 'shift'],
      // 显示选中框给出多选反馈；pointerEvents:'none' 使其纯视觉、不拦截节点点击
      showNodeSelectionBox: true,
      pointerEvents: 'none',
    }))
  }
  if (options.snapline !== false)
    graph.use(new Snapline({ enabled: true }))
  if (options.history !== false)
    graph.use(new History({ enabled: true }))
  graph.use(new Clipboard({ enabled: !readonly }))

  if (options.keyboard !== false) {
    graph.use(new Keyboard({ enabled: !readonly }))
    graph.bindKey(['delete', 'backspace'], () => {
      const cells = graph.getSelectedCells()
      if (cells.length > 0)
        graph.removeCells(cells)
      return false
    })
    graph.bindKey(['ctrl+z', 'meta+z'], () => {
      graph.undo()
      return false
    })
    graph.bindKey(['ctrl+y', 'meta+shift+z'], () => {
      graph.redo()
      return false
    })
    graph.bindKey(['ctrl+c', 'meta+c'], () => {
      const cells = graph.getSelectedCells()
      if (cells.length > 0)
        graph.copy(cells)
      return false
    })
    graph.bindKey(['ctrl+v', 'meta+v'], () => {
      if (!graph.isClipboardEmpty())
        graph.select(graph.paste({ offset: 32 }))
      return false
    })
  }

  graph.use(new Export())
  if (options.minimapContainer) {
    graph.use(new MiniMap({
      container: options.minimapContainer,
      width: 180,
      height: 120,
      padding: 8,
    }))
  }

  const ZOOM_STEP = 0.2
  const ZOOM_MIN = 0.2
  const ZOOM_MAX = 2

  /** 当前选中的节点（无 Selection 插件时安全返回空） */
  function selectedNodes(): Node[] {
    if (typeof graph.getSelectedCells !== 'function')
      return []
    return graph.getSelectedCells().filter((cell): cell is Node => cell.isNode())
  }

  const api: DiagramApi = {
    load(data: DiagramData) {
      graph.clearCells()
      for (const node of data.nodes)
        graph.addNode(toX6Node(node))
      for (const edge of data.edges) {
        const created = graph.addEdge({
          id: edge.id,
          source: { cell: edge.source },
          target: { cell: edge.target },
          attrs: edgeAttrs(edge.dashed),
          labels: edgeLabels(edge.label),
          zIndex: 0,
          data: {
            [EDGE_META_KEY]: { label: edge.label, dashed: edge.dashed },
            payload: edge.data ?? {},
          },
        })
        void created
      }
      graph.cleanHistory()
      api.zoomToFit()
    },

    toData(): DiagramData {
      return {
        nodes: graph.getNodes().map(readNode),
        edges: graph.getEdges().map(readEdge),
      }
    },

    addNode(node: DiagramNode) {
      graph.addNode(toX6Node(node))
    },

    addEdge(edge: DiagramEdge) {
      const created = graph.addEdge({
        id: edge.id,
        source: { cell: edge.source },
        target: { cell: edge.target },
        attrs: edgeAttrs(edge.dashed),
        labels: edgeLabels(edge.label),
        zIndex: 0,
        data: { [EDGE_META_KEY]: { label: edge.label, dashed: edge.dashed }, payload: edge.data ?? {} },
      })
      void created
    },

    updateNodeData(id: string, data: Record<string, unknown>) {
      const cell = graph.getCellById(id)
      if (cell?.isNode())
        cell.setData(data, { overwrite: true, deep: false })
    },

    updateEdge(id: string, patch) {
      const cell = graph.getCellById(id)
      if (cell?.isEdge())
        writeEdge(cell, patch)
    },

    removeCells(ids: string[]) {
      const cells = ids
        .map(id => graph.getCellById(id))
        .filter((cell): cell is Cell => Boolean(cell))
      if (cells.length > 0)
        graph.removeCells(cells)
    },

    clientToLocal(clientX: number, clientY: number) {
      const point = graph.clientToLocal(clientX, clientY)
      return { x: point.x, y: point.y }
    },

    setNodeStatus(id: string, status: DiagramNodeStatus | null) {
      const cell = graph.getCellById(id)
      if (cell?.isNode())
        cell.setData({ [DIAGRAM_NODE_STATUS_KEY]: status }, { deep: false })
    },

    setNodeStatuses(statuses: Record<string, DiagramNodeStatus | null>) {
      for (const [id, status] of Object.entries(statuses)) {
        const cell = graph.getCellById(id)
        if (cell?.isNode())
          cell.setData({ [DIAGRAM_NODE_STATUS_KEY]: status }, { deep: false })
      }
    },

    highlightNodes(ids: string[]) {
      const set = new Set(ids)
      for (const node of graph.getNodes()) {
        const should = set.has(node.id)
        const current = Boolean((node.getData() as Record<string, unknown> | undefined)?.[DIAGRAM_NODE_HIGHLIGHT_KEY])
        // 只在变化时写入，避免无谓的节点重渲染
        if (should !== current)
          node.setData({ [DIAGRAM_NODE_HIGHLIGHT_KEY]: should }, { deep: false })
      }
    },

    clearHighlights() {
      api.highlightNodes([])
    },

    scrollToNode(id: string) {
      const cell = graph.getCellById(id)
      if (cell?.isNode())
        graph.centerCell(cell)
    },

    zoomToFit() {
      graph.zoomToFit({ padding: 24, maxScale: 1 })
    },

    zoomIn() {
      graph.zoomTo(Math.min(graph.zoom() + ZOOM_STEP, ZOOM_MAX))
    },

    zoomOut() {
      graph.zoomTo(Math.max(graph.zoom() - ZOOM_STEP, ZOOM_MIN))
    },

    zoomToActual() {
      graph.zoomTo(1)
      graph.centerContent()
    },

    getSelectedNodeIds() {
      return selectedNodes().map(node => node.id)
    },

    align(mode: DiagramAlign) {
      const nodes = selectedNodes()
      if (nodes.length < 2)
        return
      const rects = nodes.map(node => ({ node, pos: node.getPosition(), size: node.getSize() }))
      const left = Math.min(...rects.map(r => r.pos.x))
      const right = Math.max(...rects.map(r => r.pos.x + r.size.width))
      const top = Math.min(...rects.map(r => r.pos.y))
      const bottom = Math.max(...rects.map(r => r.pos.y + r.size.height))
      const centerX = rects.reduce((sum, r) => sum + r.pos.x + r.size.width / 2, 0) / rects.length
      const centerY = rects.reduce((sum, r) => sum + r.pos.y + r.size.height / 2, 0) / rects.length
      graph.batchUpdate(() => {
        for (const { node, pos, size } of rects) {
          let x = pos.x
          let y = pos.y
          if (mode === 'left')
            x = left
          else if (mode === 'right')
            x = right - size.width
          else if (mode === 'center-vertical')
            x = centerX - size.width / 2
          else if (mode === 'top')
            y = top
          else if (mode === 'bottom')
            y = bottom - size.height
          else if (mode === 'center-horizontal')
            y = centerY - size.height / 2
          node.setPosition(x, y)
        }
      })
    },

    distribute(axis: 'horizontal' | 'vertical') {
      const nodes = selectedNodes()
      if (nodes.length < 3)
        return
      const rects = nodes.map(node => ({ node, pos: node.getPosition(), size: node.getSize() }))
      const horizontal = axis === 'horizontal'
      const centerOf = (r: (typeof rects)[number]) => horizontal ? r.pos.x + r.size.width / 2 : r.pos.y + r.size.height / 2
      rects.sort((a, b) => centerOf(a) - centerOf(b))
      const first = rects[0]
      const last = rects[rects.length - 1]
      if (!first || !last)
        return
      const startC = centerOf(first)
      const endC = centerOf(last)
      const step = (endC - startC) / (rects.length - 1)
      graph.batchUpdate(() => {
        rects.forEach((r, index) => {
          if (index === 0 || index === rects.length - 1)
            return
          const target = startC + step * index
          if (horizontal)
            r.node.setPosition(target - r.size.width / 2, r.pos.y)
          else
            r.node.setPosition(r.pos.x, target - r.size.height / 2)
        })
      })
    },

    exportPng(fileName = 'diagram.png') {
      graph.exportPNG(fileName, { padding: 24, quality: 1 })
    },

    undo() {
      graph.undo()
    },

    redo() {
      graph.redo()
    },

    on(event, handler) {
      // 消费方回调抛出的异常若冒泡进 X6 事件分发，会破坏其交互状态（后续点击不再触发），
      // 故统一兜底：处理器异常只记录，不影响画布。
      const emit = (payload: unknown) => {
        try {
          (handler as (p: never) => void)(payload as never)
        }
        catch (error) {
          console.error(`[diagram] "${event}" 事件处理器异常：`, error)
        }
      }
      switch (event) {
        case 'node:click':
          graph.on('node:click', ({ node }: { node: Node }) => {
            emit({ id: node.id, data: (node.getData() ?? {}) as Record<string, unknown> })
          })
          break
        case 'edge:click':
          graph.on('edge:click', ({ edge }: { edge: Edge }) => emit(readEdge(edge)))
          break
        case 'blank:click':
          graph.on('blank:click', () => emit(undefined))
          break
        case 'edge:connected':
          graph.on('edge:connected', ({ edge, isNew }: { edge: Edge, isNew: boolean }) => {
            if (isNew)
              emit(readEdge(edge))
          })
          break
        case 'cell:removed':
          graph.on('cell:removed', ({ cell }: { cell: Cell }) => emit({ id: cell.id }))
          break
        case 'node:contextmenu':
          graph.on('node:contextmenu', ({ node, e }: { node: Node, e: MouseEventLike }) => {
            e.preventDefault()
            emit({ id: node.id, x: e.clientX, y: e.clientY })
          })
          break
        case 'edge:contextmenu':
          graph.on('edge:contextmenu', ({ edge, e }: { edge: Edge, e: MouseEventLike }) => {
            e.preventDefault()
            emit({ id: edge.id, x: e.clientX, y: e.clientY })
          })
          break
        case 'blank:contextmenu':
          graph.on('blank:contextmenu', ({ e }: { e: MouseEventLike }) => {
            e.preventDefault()
            emit({ x: e.clientX, y: e.clientY })
          })
          break
      }
    },

    dispose() {
      graph.dispose()
    },

    getGraph() {
      return graph
    },
  }

  return api
}
