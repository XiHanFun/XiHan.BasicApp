/**
 * packages/diagram/create-diagram.ts 的引擎无关逻辑。
 *
 * 职责边界：AntV X6 及其七个插件全部替身化，断言的是本包自己的决策——
 * 传给引擎的装配选项、端口模式与连线元数据的读写往返、运行态/高亮的写入时机、
 * 缩放钳位、对齐与分布的坐标计算、以及事件处理器的异常隔离。
 * 不验证 X6 自身的渲染行为（那属于引擎，不属于本包）。
 */
import type { DiagramApi, DiagramData } from './types'
import { afterEach, describe, expect, it, vi } from 'vitest'

const x6 = vi.hoisted(() => {
  interface Spec {
    [key: string]: unknown
    data?: Record<string, unknown>
    height?: number
    id?: string
    labels?: unknown[]
    ports?: { items: { group: string, id: string }[] }
    shape?: string
    source?: { cell: string }
    target?: { cell: string }
    width?: number
    x?: number
    y?: number
  }

  const state = {
    graph: null as unknown,
    plugins: [] as { name: string, options: Record<string, unknown> | undefined }[],
    keyBindings: new Map<string, () => boolean>(),
  }

  let sequence = 0

  function makeNode(spec: Spec) {
    let data: Record<string, unknown> = { ...spec.data }
    let x = spec.x ?? 0
    let y = spec.y ?? 0
    const width = spec.width ?? 100
    const height = spec.height ?? 40
    const node = {
      id: spec.id ?? `auto-node-${++sequence}`,
      shape: spec.shape ?? 'rect',
      setDataCalls: [] as { options: Record<string, unknown>, patch: Record<string, unknown> }[],
      isNode: () => true,
      isEdge: () => false,
      getData: () => data,
      setData(patch: Record<string, unknown>, options: Record<string, unknown> = {}) {
        node.setDataCalls.push({ patch, options })
        data = options.overwrite ? { ...patch } : { ...data, ...patch }
      },
      position: () => ({ x, y }),
      getPosition: () => ({ x, y }),
      getSize: () => ({ width, height }),
      setPosition(nextX: number, nextY: number) {
        x = nextX
        y = nextY
      },
      getPorts: () => spec.ports?.items ?? [],
    }
    return node
  }

  function makeEdge(spec: Spec) {
    let data: Record<string, unknown> = { ...spec.data }
    const edge = {
      id: spec.id ?? `auto-edge-${++sequence}`,
      labels: (spec.labels ?? []) as unknown[],
      attrs: (spec.attrs ?? {}) as Record<string, unknown>,
      isNode: () => false,
      isEdge: () => true,
      getData: () => data,
      setData(patch: Record<string, unknown>, options: Record<string, unknown> = {}) {
        data = options.overwrite ? { ...patch } : { ...data, ...patch }
      },
      getSourceCellId: () => spec.source?.cell ?? '',
      getTargetCellId: () => spec.target?.cell ?? '',
      setLabels(labels: unknown[]) {
        edge.labels = labels
      },
      setAttrs(attrs: Record<string, unknown>) {
        edge.attrs = attrs
      },
    }
    return edge
  }

  class FakeGraph {
    options: Record<string, unknown>
    cells: (ReturnType<typeof makeEdge> | ReturnType<typeof makeNode>)[] = []
    selected: unknown[] = []
    events = new Map<string, ((payload: unknown) => void)[]>()
    trace: string[] = []
    clipboard: unknown[] = []
    exported: { name: string, options: unknown }[] = []
    removedBatches: unknown[][] = []
    pasteOptions: unknown = null
    centeredCell: unknown = null
    centerContentCalls = 0
    disposeCalls = 0
    undoCalls = 0
    redoCalls = 0
    zoomValue = 1
    zoomToFitOptions: unknown[] = []

    constructor(options: Record<string, unknown>) {
      this.options = options
      state.graph = this
    }

    use(plugin: { name: string, options: Record<string, unknown> | undefined }) {
      state.plugins.push(plugin)
    }

    bindKey(keys: string[], handler: () => boolean) {
      for (const key of keys) {
        state.keyBindings.set(key, handler)
      }
    }

    addNode(spec: Spec) {
      const node = makeNode(spec)
      this.cells.push(node)
      return node
    }

    addEdge(spec: Spec) {
      const edge = makeEdge(spec)
      this.cells.push(edge)
      return edge
    }

    createEdge(spec: Spec) {
      return makeEdge(spec)
    }

    getCellById(id: string) {
      return this.cells.find(cell => cell.id === id)
    }

    getNodes() {
      return this.cells.filter(cell => cell.isNode()) as ReturnType<typeof makeNode>[]
    }

    getEdges() {
      return this.cells.filter(cell => cell.isEdge()) as ReturnType<typeof makeEdge>[]
    }

    clearCells() {
      this.cells = []
      this.trace.push('clearCells')
    }

    cleanHistory() {
      this.trace.push('cleanHistory')
    }

    removeCells(cells: { id: string }[]) {
      this.removedBatches.push(cells)
      const ids = new Set(cells.map(cell => cell.id))
      this.cells = this.cells.filter(cell => !ids.has(cell.id))
    }

    getSelectedCells() {
      return this.selected
    }

    select(cells: unknown) {
      this.selected = Array.isArray(cells) ? cells : [cells]
    }

    copy(cells: unknown[]) {
      this.clipboard = cells
    }

    paste(options: unknown) {
      this.pasteOptions = options
      return this.clipboard
    }

    isClipboardEmpty() {
      return this.clipboard.length === 0
    }

    undo() {
      this.undoCalls += 1
    }

    redo() {
      this.redoCalls += 1
    }

    zoom() {
      return this.zoomValue
    }

    zoomTo(value: number) {
      this.zoomValue = value
    }

    zoomToFit(options: unknown) {
      this.zoomToFitOptions.push(options)
      this.trace.push('zoomToFit')
    }

    centerContent() {
      this.centerContentCalls += 1
    }

    centerCell(cell: unknown) {
      this.centeredCell = cell
    }

    batchUpdate(run: () => void) {
      this.trace.push('batchUpdate')
      run()
    }

    clientToLocal(clientX: number, clientY: number) {
      return { x: clientX - 10, y: clientY - 20 }
    }

    exportPNG(name: string, options: unknown) {
      this.exported.push({ name, options })
    }

    on(event: string, handler: (payload: unknown) => void) {
      const list = this.events.get(event) ?? []
      list.push(handler)
      this.events.set(event, list)
    }

    emit(event: string, payload: unknown) {
      for (const handler of this.events.get(event) ?? []) {
        handler(payload)
      }
    }

    dispose() {
      this.disposeCalls += 1
    }
  }

  function pluginClass(name: string) {
    return class {
      name = name
      options: Record<string, unknown> | undefined
      constructor(options?: Record<string, unknown>) {
        this.options = options
      }
    }
  }

  return {
    state,
    FakeGraph,
    plugins: {
      Clipboard: pluginClass('Clipboard'),
      Export: pluginClass('Export'),
      History: pluginClass('History'),
      Keyboard: pluginClass('Keyboard'),
      MiniMap: pluginClass('MiniMap'),
      Selection: pluginClass('Selection'),
      Snapline: pluginClass('Snapline'),
    },
    makeNode,
    makeEdge,
  }
})

vi.mock('@antv/x6', () => ({ Graph: x6.FakeGraph }))
vi.mock('@antv/x6-plugin-clipboard', () => ({ Clipboard: x6.plugins.Clipboard }))
vi.mock('@antv/x6-plugin-export', () => ({ Export: x6.plugins.Export }))
vi.mock('@antv/x6-plugin-history', () => ({ History: x6.plugins.History }))
vi.mock('@antv/x6-plugin-keyboard', () => ({ Keyboard: x6.plugins.Keyboard }))
vi.mock('@antv/x6-plugin-minimap', () => ({ MiniMap: x6.plugins.MiniMap }))
vi.mock('@antv/x6-plugin-selection', () => ({ Selection: x6.plugins.Selection }))
vi.mock('@antv/x6-plugin-snapline', () => ({ Snapline: x6.plugins.Snapline }))

const { createDiagram } = await import('./create-diagram')
const { DIAGRAM_NODE_HIGHLIGHT_KEY, DIAGRAM_NODE_STATUS_KEY } = await import('./types')

type Graph = InstanceType<typeof x6.FakeGraph>

interface Harness {
  api: DiagramApi
  graph: Graph
}

function create(options: Parameters<typeof createDiagram>[1] = {}): Harness {
  x6.state.plugins = []
  x6.state.keyBindings = new Map()
  const api = createDiagram(document.createElement('div'), options)
  return { api, graph: x6.state.graph as Graph }
}

function pluginNames(): string[] {
  return x6.state.plugins.map(plugin => plugin.name).sort()
}

function pluginOptions(name: string): Record<string, unknown> | undefined {
  return x6.state.plugins.find(plugin => plugin.name === name)?.options
}

/** 让图里出现若干个「已选中」节点，用于对齐/分布/复制等命令 */
function selectNodes(graph: Graph, specs: { height?: number, id: string, width?: number, x: number, y: number }[]) {
  const nodes = specs.map(spec => graph.addNode(spec))
  graph.selected = nodes
  return nodes
}

const sampleData: DiagramData = {
  nodes: [
    { id: 'n1', shape: 'task', x: 10.4, y: 20.6 },
    { id: 'n2', shape: 'task', x: 200, y: 20, ports: 'in' },
  ],
  edges: [{ id: 'e1', source: 'n1', target: 'n2', label: '通过', dashed: true, data: { rule: 'ok' } }],
}

afterEach(() => {
  x6.state.graph = null
})

describe('画布装配选项', () => {
  it('默认开启 12 像素网格，显式关闭时传 false', () => {
    expect(create().graph.options.grid).toStrictEqual({ visible: true, size: 12 })
    expect(create({ grid: false }).graph.options.grid).toBe(false)
  })

  it('只读模式禁止拖动节点与连线，非只读不下发交互限制', () => {
    expect(create({ readonly: true }).graph.options.interacting)
      .toStrictEqual({ nodeMovable: false, edgeMovable: false })
    expect(create().graph.options.interacting).toBeUndefined()
  })

  it('滚轮缩放范围锁定在 0.2 到 2 之间', () => {
    expect(create().graph.options.mousewheel).toStrictEqual({ enabled: true, minScale: 0.2, maxScale: 2 })
  })

  it('连线规则禁止空连、连到节点本体、连到连线以及自环', () => {
    const connecting = create().graph.options.connecting as Record<string, unknown>

    expect(connecting.allowBlank).toBe(false)
    expect(connecting.allowNode).toBe(false)
    expect(connecting.allowEdge).toBe(false)
    expect(connecting.allowLoop).toBe(false)
    expect(connecting.snap).toBe(true)
  })
})

describe('连线合法性校验', () => {
  function validate(harness: Harness, args: Record<string, unknown>): boolean {
    const connecting = harness.graph.options.connecting as {
      validateConnection: (args: Record<string, unknown>) => boolean
    }
    return connecting.validateConnection(args)
  }

  const bothMagnets = {
    sourceCell: { id: 'n1' },
    targetCell: { id: 'n2' },
    sourceMagnet: {},
    targetMagnet: {},
  }

  it('只读画布一律不允许连线', () => {
    expect(validate(create({ readonly: true }), bothMagnets)).toBe(false)
  })

  it('起点或终点不是磁吸端口时不允许连线', () => {
    const harness = create()

    expect(validate(harness, { ...bothMagnets, sourceMagnet: null })).toBe(false)
    expect(validate(harness, { ...bothMagnets, targetMagnet: null })).toBe(false)
  })

  it('未配置业务校验器时默认放行', () => {
    expect(validate(create(), bothMagnets)).toBe(true)
  })

  it('业务校验器拒绝时不允许连线，并拿到源与目标节点 id', () => {
    const seen: [string, string][] = []
    const harness = create({
      connectionValidator: (source, target) => {
        seen.push([source, target])
        return false
      },
    })

    expect(validate(harness, bothMagnets)).toBe(false)
    expect(seen).toStrictEqual([['n1', 'n2']])
  })

  it('交互画出的新连线自带实线样式与空元数据槽位', () => {
    const harness = create()
    const connecting = harness.graph.options.connecting as { createEdge: () => { attrs: Record<string, unknown>, getData: () => Record<string, unknown> } }

    const edge = connecting.createEdge()

    expect((edge.attrs.line as { strokeDasharray: string }).strokeDasharray).toBe('')
    expect(edge.getData()).toStrictEqual({ __diagramEdgeMeta: {}, payload: {} })
  })
})

describe('插件装配', () => {
  it('默认装上选择、对齐线、历史、剪贴板、键盘与导出六个插件', () => {
    create()

    expect(pluginNames()).toStrictEqual(['Clipboard', 'Export', 'History', 'Keyboard', 'Selection', 'Snapline'])
  })

  it('按开关分别关掉选择、对齐线、历史与键盘，导出与剪贴板始终保留', () => {
    create({ selection: false, snapline: false, history: false, keyboard: false })

    expect(pluginNames()).toStrictEqual(['Clipboard', 'Export'])
  })

  it('只读画布下选择、键盘与剪贴板插件都以禁用态装入', () => {
    create({ readonly: true })

    expect(pluginOptions('Selection')?.enabled).toBe(false)
    expect(pluginOptions('Keyboard')?.enabled).toBe(false)
    expect(pluginOptions('Clipboard')?.enabled).toBe(false)
  })

  it('框选按 shift，多选叠加允许 ctrl/meta/shift，选中框不拦截点击', () => {
    create()

    expect(pluginOptions('Selection')).toMatchObject({
      rubberband: true,
      modifiers: 'shift',
      multipleSelectionModifiers: ['ctrl', 'meta', 'shift'],
      showNodeSelectionBox: true,
      pointerEvents: 'none',
    })
  })

  it('未给缩略图容器时不装小地图；给了才装并带上容器', () => {
    create({ minimap: true })
    expect(pluginNames().includes('MiniMap')).toBe(false)

    const container = document.createElement('div')
    create({ minimapContainer: container })
    expect(pluginOptions('MiniMap')?.container).toBe(container)
  })
})

describe('键盘命令', () => {
  function press(key: string): boolean | undefined {
    return x6.state.keyBindings.get(key)?.()
  }

  it('删除键有选中才删，空选中时不调用引擎删除', () => {
    const { graph } = create()
    expect(press('delete')).toBe(false)
    expect(graph.removedBatches).toHaveLength(0)

    const nodes = selectNodes(graph, [{ id: 'n1', x: 0, y: 0 }])
    press('backspace')

    expect(graph.removedBatches).toStrictEqual([nodes])
  })

  it('撤销与重做快捷键转调引擎，并返回 false 阻止浏览器默认行为', () => {
    const { graph } = create()

    expect(press('ctrl+z')).toBe(false)
    expect(press('meta+shift+z')).toBe(false)

    expect(graph.undoCalls).toBe(1)
    expect(graph.redoCalls).toBe(1)
  })

  it('复制键有选中才写剪贴板', () => {
    const { graph } = create()
    press('ctrl+c')
    expect(graph.clipboard).toStrictEqual([])

    const nodes = selectNodes(graph, [{ id: 'n1', x: 0, y: 0 }])
    press('ctrl+c')

    expect(graph.clipboard).toStrictEqual(nodes)
  })

  it('剪贴板为空时粘贴空转；非空时粘贴并选中，偏移 32 像素', () => {
    const { graph } = create()
    press('ctrl+v')
    expect(graph.pasteOptions).toBeNull()

    const nodes = selectNodes(graph, [{ id: 'n1', x: 0, y: 0 }])
    press('ctrl+c')
    graph.selected = []
    press('meta+v')

    expect(graph.pasteOptions).toStrictEqual({ offset: 32 })
    expect(graph.selected).toStrictEqual(nodes)
  })

  it('关闭键盘开关后不注册任何快捷键', () => {
    create({ keyboard: false })

    expect(x6.state.keyBindings.size).toBe(0)
  })
})

describe('整图装载与导出', () => {
  it('装载按「清空 → 加内容 → 清历史 → 适配视口」的固定顺序执行', () => {
    const { api, graph } = create()

    api.load(sampleData)

    expect(graph.trace).toStrictEqual(['clearCells', 'cleanHistory', 'zoomToFit'])
    expect(graph.zoomToFitOptions[0]).toStrictEqual({ padding: 24, maxScale: 1 })
  })

  it('节点端口按模式生成：默认双向、in 只给入口、out 只给出口、none 不给端口', () => {
    const { api, graph } = create()

    api.load({
      nodes: [
        { id: 'both-default', shape: 's', x: 0, y: 0 },
        { id: 'in-only', shape: 's', x: 0, y: 0, ports: 'in' },
        { id: 'out-only', shape: 's', x: 0, y: 0, ports: 'out' },
        { id: 'none', shape: 's', x: 0, y: 0, ports: 'none' },
      ],
      edges: [],
    })

    const groups = graph.getNodes().map(node => node.getPorts().map(port => port.group))
    expect(groups).toStrictEqual([['in', 'out'], ['in'], ['out'], []])
  })

  it('导出时坐标四舍五入到整数，端口模式由实际端口反推', () => {
    const { api } = create()
    api.load(sampleData)

    const exported = api.toData()

    expect(exported.nodes).toStrictEqual([
      { id: 'n1', shape: 'task', x: 10, y: 21, ports: 'both', data: {} },
      { id: 'n2', shape: 'task', x: 200, y: 20, ports: 'in', data: {} },
    ])
  })

  it('导出连线时还原 label、dashed 与业务数据，且业务数据与元数据互不串味', () => {
    const { api } = create()
    api.load(sampleData)

    expect(api.toData().edges).toStrictEqual([
      { id: 'e1', source: 'n1', target: 'n2', label: '通过', dashed: true, data: { rule: 'ok' } },
    ])
  })

  it('虚线连线写入 6 4 的间隔，实线写空串', () => {
    const { api, graph } = create()

    api.addEdge({ id: 'dashed', source: 'a', target: 'b', dashed: true })
    api.addEdge({ id: 'solid', source: 'a', target: 'b' })

    const [dashed, solid] = graph.getEdges()
    expect((dashed?.attrs.line as { strokeDasharray: string }).strokeDasharray).toBe('6 4')
    expect((solid?.attrs.line as { strokeDasharray: string }).strokeDasharray).toBe('')
  })

  it('无标签连线的 labels 是空数组，而不是一个空文本标签', () => {
    const { api, graph } = create()

    api.addEdge({ id: 'no-label', source: 'a', target: 'b' })

    expect(graph.getEdges()[0]?.labels).toStrictEqual([])
  })

  it('装载空图也走完整流程，导出得到空节点空连线', () => {
    const { api, graph } = create()

    api.load({ nodes: [], edges: [] })

    expect(api.toData()).toStrictEqual({ nodes: [], edges: [] })
    expect(graph.trace).toStrictEqual(['clearCells', 'cleanHistory', 'zoomToFit'])
  })

  it('导出 PNG 默认文件名为 diagram.png，可被调用方覆盖', () => {
    const { api, graph } = create()

    api.exportPng()
    api.exportPng('流程图.png')

    expect(graph.exported.map(item => item.name)).toStrictEqual(['diagram.png', '流程图.png'])
    expect(graph.exported[0]?.options).toStrictEqual({ padding: 24, quality: 1 })
  })
})

describe('节点与连线的编辑命令', () => {
  it('更新节点数据是全量替换，旧字段不残留', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0, data: { label: '旧', color: 'red' } })

    api.updateNodeData('n1', { label: '新' })

    expect(graph.getCellById('n1')?.getData()).toStrictEqual({ label: '新' })
  })

  it('拿连线 id 去更新节点数据是空转，不会误写连线', () => {
    const { api, graph } = create()
    api.addEdge({ id: 'e1', source: 'a', target: 'b', data: { rule: 'ok' } })

    api.updateNodeData('e1', { label: '不该生效' })

    expect(graph.getCellById('e1')?.getData()).toMatchObject({ payload: { rule: 'ok' } })
  })

  it('只改标签时保留原来的虚线样式，只改样式时保留原标签', () => {
    const { api } = create()
    api.addEdge({ id: 'e1', source: 'a', target: 'b', label: '原标签', dashed: true })

    api.updateEdge('e1', { label: '新标签' })
    expect(api.toData().edges[0]).toMatchObject({ label: '新标签', dashed: true })

    api.updateEdge('e1', { dashed: false })
    expect(api.toData().edges[0]).toMatchObject({ label: '新标签', dashed: false })
  })

  it('不传业务数据时原数据保持不变，传了则整体替换', () => {
    const { api } = create()
    api.addEdge({ id: 'e1', source: 'a', target: 'b', data: { rule: 'ok', extra: 1 } })

    api.updateEdge('e1', { label: '只改标签' })
    expect(api.toData().edges[0]?.data).toStrictEqual({ rule: 'ok', extra: 1 })

    api.updateEdge('e1', { data: { rule: 'no' } })
    expect(api.toData().edges[0]?.data).toStrictEqual({ rule: 'no' })
  })

  it('更新不存在的连线是空转', () => {
    const { api } = create()

    expect(() => api.updateEdge('missing', { label: 'x' })).not.toThrow()
  })

  it('删除时过滤掉不存在的 id，一个都不存在则不调用引擎', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0 })

    api.removeCells(['missing-1', 'missing-2'])
    expect(graph.removedBatches).toHaveLength(0)

    api.removeCells(['n1', 'missing-1'])
    expect(graph.removedBatches[0]).toHaveLength(1)
    expect(graph.getNodes()).toHaveLength(0)
  })

  it('删除空 id 列表是空转', () => {
    const { api, graph } = create()

    api.removeCells([])

    expect(graph.removedBatches).toHaveLength(0)
  })

  it('浏览器坐标换算直接透传引擎结果', () => {
    const { api } = create()

    expect(api.clientToLocal(100, 200)).toStrictEqual({ x: 90, y: 180 })
  })

  it('销毁转调引擎销毁', () => {
    const { api, graph } = create()

    api.dispose()

    expect(graph.disposeCalls).toBe(1)
  })
})

describe('运行态与高亮', () => {
  it('设置运行态只合并保留键，业务数据原样保留', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0, data: { label: '任务' } })

    api.setNodeStatus('n1', 'running')

    expect(graph.getCellById('n1')?.getData()).toStrictEqual({ label: '任务', [DIAGRAM_NODE_STATUS_KEY]: 'running' })
  })

  it('传 null 清除运行态而不是删掉整块数据', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0, data: { label: '任务' } })
    api.setNodeStatus('n1', 'faulted')

    api.setNodeStatus('n1', null)

    expect(graph.getCellById('n1')?.getData()).toStrictEqual({ label: '任务', [DIAGRAM_NODE_STATUS_KEY]: null })
  })

  it('批量设置只影响列出的节点，未列出的保持原状，不存在的 id 跳过', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0 })
    api.addNode({ id: 'n2', shape: 's', x: 0, y: 0 })
    api.setNodeStatus('n2', 'completed')

    api.setNodeStatuses({ n1: 'running', missing: 'faulted' })

    expect(graph.getCellById('n1')?.getData()[DIAGRAM_NODE_STATUS_KEY]).toBe('running')
    expect(graph.getCellById('n2')?.getData()[DIAGRAM_NODE_STATUS_KEY]).toBe('completed')
  })

  it('给不存在的节点设置运行态是空转', () => {
    const { api } = create()

    expect(() => api.setNodeStatus('missing', 'running')).not.toThrow()
  })

  it('高亮只在状态确实变化时写入，重复高亮同一批不产生多余写', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0 })
    api.addNode({ id: 'n2', shape: 's', x: 0, y: 0 })
    const [first, second] = graph.getNodes()

    api.highlightNodes(['n1'])
    expect(first?.setDataCalls).toHaveLength(1)
    expect(second?.setDataCalls).toHaveLength(0)

    api.highlightNodes(['n1'])
    expect(first?.setDataCalls).toHaveLength(1)
  })

  it('高亮换到另一批节点时，老的取消、新的点亮', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0 })
    api.addNode({ id: 'n2', shape: 's', x: 0, y: 0 })
    api.highlightNodes(['n1'])

    api.highlightNodes(['n2'])

    expect(graph.getCellById('n1')?.getData()[DIAGRAM_NODE_HIGHLIGHT_KEY]).toBe(false)
    expect(graph.getCellById('n2')?.getData()[DIAGRAM_NODE_HIGHLIGHT_KEY]).toBe(true)
  })

  it('清除高亮等价于高亮空集合', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0 })
    api.highlightNodes(['n1'])

    api.clearHighlights()

    expect(graph.getCellById('n1')?.getData()[DIAGRAM_NODE_HIGHLIGHT_KEY]).toBe(false)
  })

  it('滚动定位到节点时居中该节点，不存在的 id 不动视口', () => {
    const { api, graph } = create()
    api.addNode({ id: 'n1', shape: 's', x: 0, y: 0 })

    api.scrollToNode('missing')
    expect(graph.centeredCell).toBeNull()

    api.scrollToNode('n1')
    expect(graph.centeredCell).toBe(graph.getCellById('n1'))
  })
})

describe('视口缩放', () => {
  it('放大与缩小按 0.2 步长', () => {
    const { api, graph } = create()

    api.zoomIn()
    expect(graph.zoomValue).toBeCloseTo(1.2, 10)

    api.zoomOut()
    expect(graph.zoomValue).toBeCloseTo(1, 10)
  })

  it('放大封顶 2 倍，缩小封底 0.2 倍', () => {
    const { api, graph } = create()

    graph.zoomValue = 1.9
    api.zoomIn()
    expect(graph.zoomValue).toBe(2)

    graph.zoomValue = 0.3
    api.zoomOut()
    expect(graph.zoomValue).toBe(0.2)
  })

  it('已在极值时继续缩放保持不变', () => {
    const { api, graph } = create()

    graph.zoomValue = 2
    api.zoomIn()
    expect(graph.zoomValue).toBe(2)

    graph.zoomValue = 0.2
    api.zoomOut()
    expect(graph.zoomValue).toBe(0.2)
  })

  it('回到实际尺寸会同时把缩放置 1 并居中内容', () => {
    const { api, graph } = create()
    graph.zoomValue = 0.4

    api.zoomToActual()

    expect(graph.zoomValue).toBe(1)
    expect(graph.centerContentCalls).toBe(1)
  })
})

describe('对齐与分布', () => {
  it('选中数量不足 2 时对齐直接返回，不开批量事务', () => {
    const { api, graph } = create()
    selectNodes(graph, [{ id: 'n1', x: 5, y: 5 }])

    api.align('left')

    expect(graph.trace.includes('batchUpdate')).toBe(false)
  })

  it('左对齐取最小 x，右对齐让右边缘齐平', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [
      { id: 'n1', x: 10, y: 0, width: 100 },
      { id: 'n2', x: 50, y: 0, width: 40 },
    ])

    api.align('left')
    expect(nodes.map(node => node.getPosition().x)).toStrictEqual([10, 10])

    api.align('right')
    expect(nodes.map(node => node.getPosition().x)).toStrictEqual([10, 70])
  })

  it('上对齐取最小 y，下对齐让下边缘齐平', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [
      { id: 'n1', x: 0, y: 10, height: 60 },
      { id: 'n2', x: 0, y: 40, height: 20 },
    ])

    api.align('top')
    expect(nodes.map(node => node.getPosition().y)).toStrictEqual([10, 10])

    api.align('bottom')
    expect(nodes.map(node => node.getPosition().y)).toStrictEqual([10, 50])
  })

  it('垂直居中把各节点中心对到公共中心 x 上', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [
      { id: 'n1', x: 0, y: 0, width: 100 },
      { id: 'n2', x: 100, y: 0, width: 100 },
    ])

    api.align('center-vertical')

    expect(nodes.map(node => node.getPosition().x)).toStrictEqual([50, 50])
  })

  it('水平居中把各节点中心对到公共中心 y 上', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [
      { id: 'n1', x: 0, y: 0, height: 40 },
      { id: 'n2', x: 0, y: 100, height: 40 },
    ])

    api.align('center-horizontal')

    expect(nodes.map(node => node.getPosition().y)).toStrictEqual([50, 50])
  })

  it('选中数量不足 3 时分布直接返回', () => {
    const { api, graph } = create()
    selectNodes(graph, [{ id: 'n1', x: 0, y: 0 }, { id: 'n2', x: 100, y: 0 }])

    api.distribute('horizontal')

    expect(graph.trace.includes('batchUpdate')).toBe(false)
  })

  it('水平分布只挪中间节点，首尾保持原位', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [
      { id: 'n1', x: 0, y: 0, width: 100 },
      { id: 'n2', x: 30, y: 0, width: 100 },
      { id: 'n3', x: 400, y: 0, width: 100 },
    ])

    api.distribute('horizontal')

    expect(nodes.map(node => node.getPosition().x)).toStrictEqual([0, 200, 400])
  })

  it('垂直分布按中心等距排开，与选中顺序无关（内部按中心排序）', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [
      { id: 'bottom', x: 0, y: 400, height: 40 },
      { id: 'middle', x: 0, y: 10, height: 40 },
      { id: 'top', x: 0, y: 0, height: 40 },
    ])

    api.distribute('vertical')

    expect(nodes.map(node => ({ id: node.id, y: node.getPosition().y })))
      .toStrictEqual([{ id: 'bottom', y: 400 }, { id: 'middle', y: 200 }, { id: 'top', y: 0 }])
  })

  it('选中里混入连线时只对节点做对齐', () => {
    const { api, graph } = create()
    const nodes = selectNodes(graph, [{ id: 'n1', x: 10, y: 0 }, { id: 'n2', x: 90, y: 0 }])
    graph.selected = [...nodes, graph.addEdge({ id: 'e1', source: { cell: 'n1' }, target: { cell: 'n2' } })]

    api.align('left')

    expect(api.getSelectedNodeIds()).toStrictEqual(['n1', 'n2'])
    expect(nodes.map(node => node.getPosition().x)).toStrictEqual([10, 10])
  })
})

describe('事件订阅', () => {
  it('节点点击带出 id 与业务数据', () => {
    const { api, graph } = create()
    const payloads: unknown[] = []
    api.on('node:click', payload => payloads.push(payload))

    graph.emit('node:click', { node: x6.makeNode({ id: 'n1', data: { label: '任务' } }) })

    expect(payloads).toStrictEqual([{ id: 'n1', data: { label: '任务' } }])
  })

  it('连线点击带出源、目标、标签与业务数据', () => {
    const { api, graph } = create()
    api.addEdge({ id: 'e1', source: 'n1', target: 'n2', label: '通过', dashed: true, data: { rule: 'ok' } })
    const payloads: unknown[] = []
    api.on('edge:click', payload => payloads.push(payload))

    graph.emit('edge:click', { edge: graph.getEdges()[0] })

    expect(payloads).toStrictEqual([
      { id: 'e1', source: 'n1', target: 'n2', label: '通过', dashed: true, data: { rule: 'ok' } },
    ])
  })

  it('只有交互新建的连线才触发 edge:connected，程序装载的不触发', () => {
    const { api, graph } = create()
    api.addEdge({ id: 'e1', source: 'n1', target: 'n2' })
    let fired = 0
    api.on('edge:connected', () => {
      fired += 1
    })

    graph.emit('edge:connected', { edge: graph.getEdges()[0], isNew: false })
    expect(fired).toBe(0)

    graph.emit('edge:connected', { edge: graph.getEdges()[0], isNew: true })
    expect(fired).toBe(1)
  })

  it('空白点击的载荷是 undefined', () => {
    const { api, graph } = create()
    const payloads: unknown[] = []
    api.on('blank:click', payload => payloads.push(payload))

    graph.emit('blank:click', {})

    expect(payloads).toStrictEqual([undefined])
  })

  it('单元被移除只带 id', () => {
    const { api, graph } = create()
    const payloads: unknown[] = []
    api.on('cell:removed', payload => payloads.push(payload))

    graph.emit('cell:removed', { cell: { id: 'n1' } })

    expect(payloads).toStrictEqual([{ id: 'n1' }])
  })

  it('三类右键菜单都会阻止浏览器默认菜单并给出浏览器坐标', () => {
    const { api, graph } = create()
    const prevented: string[] = []
    const payloads: unknown[] = []
    api.on('node:contextmenu', payload => payloads.push(payload))
    api.on('edge:contextmenu', payload => payloads.push(payload))
    api.on('blank:contextmenu', payload => payloads.push(payload))
    const event = (tag: string) => ({
      preventDefault: () => prevented.push(tag),
      clientX: 120,
      clientY: 240,
    })

    graph.emit('node:contextmenu', { node: { id: 'n1' }, e: event('node') })
    graph.emit('edge:contextmenu', { edge: { id: 'e1' }, e: event('edge') })
    graph.emit('blank:contextmenu', { e: event('blank') })

    expect(prevented).toStrictEqual(['node', 'edge', 'blank'])
    expect(payloads).toStrictEqual([
      { id: 'n1', x: 120, y: 240 },
      { id: 'e1', x: 120, y: 240 },
      { x: 120, y: 240 },
    ])
  })

  it('消费方回调抛异常被就地记录，不冒泡进 X6 破坏后续交互', () => {
    const errors = vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const { api, graph } = create()
    api.on('blank:click', () => {
      throw new Error('业务回调炸了')
    })

    expect(() => graph.emit('blank:click', {})).not.toThrow()
    expect(() => graph.emit('blank:click', {})).not.toThrow()
    expect(errors).toHaveBeenCalledTimes(2)
    expect(String(errors.mock.calls[0]?.[0])).toContain('blank:click')
  })

  it('未订阅的事件名不注册任何引擎监听', () => {
    const { graph } = create()

    expect(graph.events.size).toBe(0)
  })
})
