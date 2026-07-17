/**
 * 设计器图模型 ↔ 后端流程定义 JSON 的互转
 *
 * 图层面向 ~/diagram 的通用模型（DiagramData），不感知图引擎；
 * 后端格式为 WorkflowDefinitionJsonSerializer 稳定格式（camelCase）；
 * 设计器只改 code/name/description/category/enableCompensation/variables/nodes/transitions，
 * 其余字段（id/version/status/租户/时间）原样透传；
 * 画布坐标持久化在 extraProperties.designerLayout（{nodeId: {x,y}} 的 JSON 字符串）。
 */

import type { DiagramData, DiagramEdge, DiagramNode, DiagramNodeStatus, DiagramPortMode } from '~/diagram'

export const ACTIVITY_SHAPE = 'workflow-activity'

export interface DesignerRetryPolicy {
  maxAttempts: number
  firstDelaySeconds: number
  backoffFactor: number
}

export interface DesignerNodeData extends Record<string, unknown> {
  activityType: string
  name: string
  properties: Record<string, unknown>
  retryPolicy?: DesignerRetryPolicy | null
  timeoutSeconds?: number | null
  continueOnError?: boolean
  /** 设计器态：属性面板选中高亮 */
  __selected?: boolean
  /** 运行态：实例轨迹着色（由 ~/diagram 的 setNodeStatus 写入） */
  __status?: DiagramNodeStatus | null
  /** 高亮标记：校验错误等（由 ~/diagram 的 highlightNodes 写入） */
  __highlight?: boolean
}

export interface DesignerEdgeData extends Record<string, unknown> {
  condition?: string | null
  priority?: number
  isDefault?: boolean
  name?: string | null
}

export interface DefinitionVariableMeta {
  name: string
  required: boolean
  defaultValue?: string | null
  description?: string | null
}

export interface DefinitionMeta {
  code: string
  name: string
  description: string
  category: string
  enableCompensation: boolean
  variables: DefinitionVariableMeta[]
}

export interface ParsedDefinition {
  /** 原始定义对象（透传未编辑字段） */
  raw: Record<string, unknown>
  meta: DefinitionMeta
  data: DiagramData
}

const LAYOUT_KEY = 'designerLayout'

interface RawNode {
  id: string
  name?: string
  activityType: string
  properties?: Record<string, unknown>
  retryPolicy?: DesignerRetryPolicy | null
  timeoutSeconds?: number | null
  continueOnError?: boolean
}

interface RawTransition {
  id?: string
  name?: string | null
  sourceNodeId: string
  targetNodeId: string
  condition?: string | null
  priority?: number
  isDefault?: boolean
}

/** 活动类型 → 端口模式（开始无入口，结束/终止/故障无出口） */
export function portsOf(activityType: string): DiagramPortMode {
  if (activityType === 'Start')
    return 'out'
  if (['End', 'Terminate', 'Fault'].includes(activityType))
    return 'in'
  return 'both'
}

/** 连线展示标签：默认边 → 名称 → 条件 */
export function edgeLabel(data: DesignerEdgeData | undefined): string {
  if (!data)
    return ''
  if (data.isDefault)
    return 'default'
  return data.name || data.condition || ''
}

/** 自动布局：自开始节点 BFS 分层（x=层，y=层内序），孤立节点排在最下方 */
export function autoLayout(data: DiagramData): void {
  const outgoing = new Map<string, string[]>()
  for (const edge of data.edges) {
    const list = outgoing.get(edge.source) ?? []
    list.push(edge.target)
    outgoing.set(edge.source, list)
  }

  const start = data.nodes.find(node => (node.data as DesignerNodeData | undefined)?.activityType === 'Start') ?? data.nodes[0]
  const levelOf = new Map<string, number>()
  if (start) {
    const queue: { id: string, level: number }[] = [{ id: start.id, level: 0 }]
    while (queue.length > 0) {
      const { id, level } = queue.shift()!
      if (levelOf.has(id))
        continue
      levelOf.set(id, level)
      for (const next of outgoing.get(id) ?? []) {
        if (!levelOf.has(next))
          queue.push({ id: next, level: level + 1 })
      }
    }
  }

  const byLevel = new Map<number, string[]>()
  let maxLevel = 0
  for (const node of data.nodes) {
    const level = levelOf.get(node.id)
    if (level === undefined)
      continue
    const list = byLevel.get(level) ?? []
    list.push(node.id)
    byLevel.set(level, list)
    maxLevel = Math.max(maxLevel, level)
  }

  const positions = new Map<string, { x: number, y: number }>()
  for (const [level, ids] of byLevel) {
    ids.forEach((id, index) => {
      positions.set(id, { x: 60 + level * 260, y: 80 + index * 130 })
    })
  }

  let orphanIndex = 0
  for (const node of data.nodes) {
    if (!positions.has(node.id)) {
      positions.set(node.id, { x: 60 + orphanIndex * 260, y: 80 + (maxLevel + 2) * 130 })
      orphanIndex++
    }
  }

  for (const node of data.nodes) {
    const position = positions.get(node.id) ?? { x: 60, y: 80 }
    node.x = position.x
    node.y = position.y
  }
}

/** 解析后端定义 JSON 为设计器图模型（JSON 非法时抛错，由调用方提示） */
export function parseDefinition(json: string): ParsedDefinition {
  const raw = JSON.parse(json) as Record<string, unknown>
  if (typeof raw !== 'object' || raw === null || Array.isArray(raw))
    throw new TypeError('definition must be a JSON object')

  const rawNodes = (raw.nodes as RawNode[] | undefined) ?? []
  const rawTransitions = (raw.transitions as RawTransition[] | undefined) ?? []
  const rawVariables = (raw.variables as Record<string, unknown>[] | undefined) ?? []
  const extraProperties = (raw.extraProperties as Record<string, string> | undefined) ?? {}

  let layout: Record<string, { x: number, y: number }> = {}
  try {
    layout = extraProperties[LAYOUT_KEY] ? JSON.parse(extraProperties[LAYOUT_KEY]) : {}
  }
  catch {
    layout = {}
  }

  const nodes: DiagramNode[] = rawNodes.map(rawNode => ({
    id: rawNode.id,
    shape: ACTIVITY_SHAPE,
    x: layout[rawNode.id]?.x ?? 0,
    y: layout[rawNode.id]?.y ?? 0,
    ports: portsOf(rawNode.activityType),
    data: {
      activityType: rawNode.activityType,
      name: rawNode.name ?? rawNode.id,
      properties: rawNode.properties ?? {},
      retryPolicy: rawNode.retryPolicy ?? null,
      timeoutSeconds: rawNode.timeoutSeconds ?? null,
      continueOnError: rawNode.continueOnError ?? false,
    } satisfies DesignerNodeData,
  }))

  const edges: DiagramEdge[] = rawTransitions.map((transition, index) => {
    const data: DesignerEdgeData = {
      condition: transition.condition ?? null,
      priority: transition.priority ?? 0,
      isDefault: transition.isDefault ?? false,
      name: transition.name ?? null,
    }
    return {
      id: transition.id || `${transition.sourceNodeId}->${transition.targetNodeId}#${index + 1}`,
      source: transition.sourceNodeId,
      target: transition.targetNodeId,
      label: edgeLabel(data),
      dashed: data.isDefault ?? false,
      data,
    }
  })

  const data: DiagramData = { nodes, edges }
  if (Object.keys(layout).length === 0 && nodes.length > 0)
    autoLayout(data)

  const meta: DefinitionMeta = {
    code: String(raw.code ?? ''),
    name: String(raw.name ?? ''),
    description: String(raw.description ?? ''),
    category: String(raw.category ?? ''),
    enableCompensation: Boolean(raw.enableCompensation ?? false),
    variables: rawVariables.map(variable => ({
      name: String(variable.name ?? ''),
      required: Boolean(variable.required ?? false),
      defaultValue: variable.defaultValue === null || variable.defaultValue === undefined ? null : String(variable.defaultValue),
      description: variable.description === null || variable.description === undefined ? null : String(variable.description),
    })),
  }

  return { raw, meta, data }
}

/** 把设计器图模型序列化回后端定义 JSON（透传未编辑字段） */
export function serializeDefinition(
  raw: Record<string, unknown>,
  meta: DefinitionMeta,
  data: DiagramData,
): string {
  const layout: Record<string, { x: number, y: number }> = {}
  for (const node of data.nodes)
    layout[node.id] = { x: Math.round(node.x), y: Math.round(node.y) }

  const rawNodes: RawNode[] = data.nodes.map((node) => {
    const nodeData = (node.data ?? {}) as DesignerNodeData
    const result: RawNode = {
      id: node.id,
      name: nodeData.name || node.id,
      activityType: nodeData.activityType,
      properties: nodeData.properties ?? {},
    }
    if (nodeData.retryPolicy)
      result.retryPolicy = nodeData.retryPolicy
    if (nodeData.timeoutSeconds !== null && nodeData.timeoutSeconds !== undefined)
      result.timeoutSeconds = nodeData.timeoutSeconds
    if (nodeData.continueOnError)
      result.continueOnError = true
    return result
  })

  const rawTransitions: RawTransition[] = data.edges.map((edge, index) => {
    const edgeData = (edge.data ?? {}) as DesignerEdgeData
    const result: RawTransition = {
      id: edge.id || `${edge.source}->${edge.target}#${index + 1}`,
      sourceNodeId: edge.source,
      targetNodeId: edge.target,
      priority: edgeData.priority ?? 0,
    }
    if (edgeData.name)
      result.name = edgeData.name
    if (edgeData.condition)
      result.condition = edgeData.condition
    if (edgeData.isDefault)
      result.isDefault = true
    return result
  })

  const extraProperties = {
    ...((raw.extraProperties as Record<string, string> | undefined) ?? {}),
    [LAYOUT_KEY]: JSON.stringify(layout),
  }

  const definition: Record<string, unknown> = {
    ...raw,
    code: meta.code.trim(),
    name: meta.name.trim(),
    description: meta.description.trim() || null,
    category: meta.category.trim() || null,
    enableCompensation: meta.enableCompensation,
    variables: meta.variables
      .filter(variable => variable.name.trim())
      .map(variable => ({
        name: variable.name.trim(),
        required: variable.required,
        defaultValue: variable.defaultValue?.toString().trim() || null,
        description: variable.description?.toString().trim() || null,
      })),
    nodes: rawNodes,
    transitions: rawTransitions,
    extraProperties,
  }

  return JSON.stringify(definition, null, 2)
}

/** 校验问题（level=error 阻断保存，warning 仅提示） */
export interface ValidationIssue {
  /** i18n 键后缀：workflow.designer.validate.{code} */
  code: string
  level: 'error' | 'warning'
  /** 关联节点（用于定位/高亮） */
  nodeId?: string
}

const TERMINAL_TYPES = new Set(['End', 'Terminate', 'Fault'])

/** 设计器本地结构校验（常驻问题面板 + 保存前把关；完整校验在后端发布时执行） */
export function validateGraph(meta: DefinitionMeta, data: DiagramData): ValidationIssue[] {
  const issues: ValidationIssue[] = []
  const push = (code: string, level: 'error' | 'warning', nodeId?: string) => issues.push({ code, level, nodeId })

  if (!meta.code.trim())
    push('code', 'error')
  if (!meta.name.trim())
    push('name', 'error')

  const nodes = data.nodes
  const dataOf = (node: DiagramNode) => (node.data ?? {}) as DesignerNodeData

  // 重复节点 id
  const ids = new Set<string>()
  for (const node of nodes) {
    if (ids.has(node.id))
      push('dup_node', 'error', node.id)
    ids.add(node.id)
  }

  // 开始节点数量
  const starts = nodes.filter(node => dataOf(node).activityType === 'Start')
  if (nodes.length > 0 && starts.length === 0)
    push('no_start', 'error')
  else if (starts.length > 1)
    starts.forEach(node => push('multi_start', 'error', node.id))

  // 至少一个结束/终止节点
  if (nodes.length > 0 && !nodes.some(node => ['End', 'Terminate'].includes(dataOf(node).activityType)))
    push('no_end', 'warning')

  // 悬空连线（端点不存在）
  if (data.edges.some(edge => !ids.has(edge.source) || !ids.has(edge.target)))
    push('dangling_edge', 'error')

  // 出/入度与出边表
  const outCount = new Map<string, number>()
  const inCount = new Map<string, number>()
  const outgoing = new Map<string, { target: string, data: DesignerEdgeData }[]>()
  for (const edge of data.edges) {
    outCount.set(edge.source, (outCount.get(edge.source) ?? 0) + 1)
    inCount.set(edge.target, (inCount.get(edge.target) ?? 0) + 1)
    const list = outgoing.get(edge.source) ?? []
    list.push({ target: edge.target, data: (edge.data ?? {}) as DesignerEdgeData })
    outgoing.set(edge.source, list)
  }

  for (const node of nodes) {
    const type = dataOf(node).activityType
    // 死路：非终止节点无出边
    if (!TERMINAL_TYPES.has(type) && (outCount.get(node.id) ?? 0) === 0)
      push('dead_end', 'warning', node.id)
    // 孤立入口：非开始节点无入边
    if (type !== 'Start' && (inCount.get(node.id) ?? 0) === 0)
      push('no_incoming', 'warning', node.id)
    // 独占网关无默认/兜底分支
    if (type === 'Decision') {
      const outs = outgoing.get(node.id) ?? []
      const conditional = outs.filter(out => out.data.condition && !out.data.isDefault)
      const hasFallback = outs.some(out => out.data.isDefault || !out.data.condition)
      if (conditional.length >= 1 && !hasFallback)
        push('decision_no_default', 'warning', node.id)
    }
  }

  // 从开始节点不可达
  const start = starts[0]
  if (start) {
    const reached = new Set<string>()
    const queue: string[] = [start.id]
    while (queue.length > 0) {
      const cur = queue.shift()
      if (cur === undefined || reached.has(cur))
        continue
      reached.add(cur)
      for (const out of outgoing.get(cur) ?? []) {
        if (!reached.has(out.target))
          queue.push(out.target)
      }
    }
    for (const node of nodes) {
      if (!reached.has(node.id))
        push('unreachable', 'warning', node.id)
    }
  }

  return issues
}
