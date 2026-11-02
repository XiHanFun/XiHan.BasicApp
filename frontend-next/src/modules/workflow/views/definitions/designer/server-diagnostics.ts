/**
 * 发布失败时后端给的逐条问题：Problem Details 的字段级 errors，键是定义 JSON（WorkflowJson）里去掉 `$.` 的路径，
 * 如 `nodes[2].properties.Cron`、`transitions[0].condition`，定义级的问题是 `nodes`；值是已按界面语言翻译的文案。
 *
 * 按下标找到节点与连线的 ID（设计器里的节点、连线沿用定义里的 ID），交给设计器标出来；定位不到具体对象的算定义级问题。
 */

/** 一条落到设计器上的问题 */
export interface ServerDiagnostic {
  /** 落在哪个节点或连线上；定义级问题不给 */
  target?: { kind: 'node' | 'edge', id: string }
  /** 已翻译的文案 */
  message: string
}

const NODE_PATH = /^nodes\[(\d+)\]/
const TRANSITION_PATH = /^transitions\[(\d+)\]/

function idsOf(value: unknown): (string | undefined)[] {
  return Array.isArray(value)
    ? value.map(item => (typeof item === 'object' && item !== null && typeof (item as { id?: unknown }).id === 'string') ? (item as { id: string }).id : undefined)
    : []
}

function targetOf(path: string, nodes: (string | undefined)[], transitions: (string | undefined)[]): ServerDiagnostic['target'] {
  const node = NODE_PATH.exec(path)
  if (node) {
    const id = nodes[Number(node[1])]
    return id ? { kind: 'node', id } : undefined
  }
  const transition = TRANSITION_PATH.exec(path)
  if (transition) {
    const id = transitions[Number(transition[1])]
    return id ? { kind: 'edge', id } : undefined
  }
  return undefined
}

/**
 * 把字段级 errors 落到定义里的节点与连线上
 * @param definitionJson 发布的那份定义（WorkflowJson 文本）
 * @param errors Problem Details 的字段级 errors
 * @returns 每条文案一条问题；定义解析不了时都算定义级问题
 */
export function serverDiagnostics(definitionJson: string, errors: Readonly<Record<string, readonly string[]>>): ServerDiagnostic[] {
  let nodes: (string | undefined)[] = []
  let transitions: (string | undefined)[] = []
  try {
    const document = JSON.parse(definitionJson) as { nodes?: unknown, transitions?: unknown }
    nodes = idsOf(document?.nodes)
    transitions = idsOf(document?.transitions)
  }
  catch {
    // 定义解析不了时问题都挂在定义上
  }
  return Object.entries(errors).flatMap(([path, messages]) => {
    const target = targetOf(path, nodes, transitions)
    return messages.map(message => target ? { target, message } : { message })
  })
}
