/**
 * 流程设计器图模型 ↔ 后端定义 JSON 互转的单元测试。
 *
 * 职责边界：只测 transform.ts 的纯函数——端口模式、连线标签、自动布局、解析、序列化与本地校验。
 * 不涉及图引擎（~/diagram 只提供类型）、不涉及网络。
 */
import type { DefinitionMeta, DesignerEdgeData, DesignerNodeData } from './transform'
import type { DiagramData, DiagramEdge, DiagramNode } from '~/diagram'
import { describe, expect, it } from 'vitest'
import {
  ACTIVITY_SHAPE,
  autoLayout,
  edgeLabel,
  parseDefinition,
  portsOf,
  serializeDefinition,
  validateGraph,
} from './transform'

function node(id: string, activityType: string, x = 0, y = 0): DiagramNode {
  return {
    id,
    shape: ACTIVITY_SHAPE,
    x,
    y,
    ports: portsOf(activityType),
    data: {
      activityType,
      name: id,
      properties: {},
    } satisfies DesignerNodeData,
  }
}

function edge(source: string, target: string, data: DesignerEdgeData = {}): DiagramEdge {
  return { id: `${source}->${target}`, source, target, data }
}

function meta(overrides: Partial<DefinitionMeta> = {}): DefinitionMeta {
  return {
    code: 'order-approval',
    name: '订单审批',
    description: '',
    category: '',
    enableCompensation: false,
    variables: [],
    ...overrides,
  }
}

describe('portsOf 的端口模式', () => {
  it('开始节点只有出口', () => {
    expect(portsOf('Start')).toBe('out')
  })

  it('结束 / 终止 / 故障三类节点只有入口', () => {
    expect(portsOf('End')).toBe('in')
    expect(portsOf('Terminate')).toBe('in')
    expect(portsOf('Fault')).toBe('in')
  })

  it('其余活动双向连线，未知类型与空串按双向兜底', () => {
    expect(portsOf('UserTask')).toBe('both')
    expect(portsOf('未注册活动')).toBe('both')
    expect(portsOf('')).toBe('both')
  })

  it('活动类型区分大小写，start 不等于 Start', () => {
    expect(portsOf('start')).toBe('both')
  })
})

describe('edgeLabel 的展示优先级', () => {
  it('无连线数据时标签为空串', () => {
    expect(edgeLabel(undefined)).toBe('')
  })

  it('默认分支优先显示 default，压过名称与条件', () => {
    expect(edgeLabel({ isDefault: true, name: '通过', condition: 'amount > 0' })).toBe('default')
  })

  it('名称优先于条件', () => {
    expect(edgeLabel({ name: '通过', condition: 'amount > 0' })).toBe('通过')
  })

  it('名称为空时回退到条件表达式', () => {
    expect(edgeLabel({ name: '', condition: 'amount > 0' })).toBe('amount > 0')
    expect(edgeLabel({ name: null, condition: 'amount > 0' })).toBe('amount > 0')
  })

  it('两者都为空时返回空串而不是 undefined', () => {
    expect(edgeLabel({})).toBe('')
    expect(edgeLabel({ name: null, condition: null })).toBe('')
  })
})

describe('autoLayout 的分层布局', () => {
  it('自开始节点 BFS 分层：x 按层递进 260，y 按层内序递进 130', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('t', 'UserTask'), node('e', 'End')],
      edges: [edge('s', 't'), edge('t', 'e')],
    }
    autoLayout(data)

    expect(data.nodes.map(item => [item.id, item.x, item.y])).toEqual([
      ['s', 60, 80],
      ['t', 320, 80],
      ['e', 580, 80],
    ])
  })

  it('同层多个节点在 y 方向排开', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('a', 'UserTask'), node('b', 'UserTask')],
      edges: [edge('s', 'a'), edge('s', 'b')],
    }
    autoLayout(data)

    expect(data.nodes.map(item => [item.id, item.x, item.y])).toEqual([
      ['s', 60, 80],
      ['a', 320, 80],
      ['b', 320, 210],
    ])
  })

  it('从开始节点不可达的孤立节点统一排到最下方一行', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('a', 'UserTask'), node('x', 'Log'), node('y', 'Log')],
      edges: [edge('s', 'a')],
    }
    autoLayout(data)

    const positions = new Map(data.nodes.map(item => [item.id, [item.x, item.y]]))
    expect(positions.get('x')).toEqual([60, 470])
    expect(positions.get('y')).toEqual([320, 470])
  })

  it('没有开始节点时以第一个节点为布局起点', () => {
    const data: DiagramData = {
      nodes: [node('a', 'UserTask'), node('b', 'UserTask')],
      edges: [edge('a', 'b')],
    }
    autoLayout(data)

    expect(data.nodes.map(item => item.x)).toEqual([60, 320])
  })

  it('存在环时不会死循环，每个节点只定位一次', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('a', 'UserTask'), node('b', 'UserTask')],
      edges: [edge('s', 'a'), edge('a', 'b'), edge('b', 'a')],
    }
    autoLayout(data)

    expect(data.nodes.map(item => [item.id, item.x])).toEqual([['s', 60], ['a', 320], ['b', 580]])
  })

  it('空图不抛错', () => {
    const data: DiagramData = { nodes: [], edges: [] }

    expect(() => autoLayout(data)).not.toThrow()
    expect(data.nodes).toEqual([])
  })
})

describe('parseDefinition 的非法输入', () => {
  it('非法 JSON 直接抛错，交由调用方提示', () => {
    expect(() => parseDefinition('{')).toThrow()
  })

  it('顶层不是对象时抛出明确的类型错误', () => {
    expect(() => parseDefinition('[]')).toThrow(/JSON object/)
    expect(() => parseDefinition('null')).toThrow(/JSON object/)
    expect(() => parseDefinition('123')).toThrow(/JSON object/)
    expect(() => parseDefinition('"abc"')).toThrow(/JSON object/)
  })

  it('空对象解析为空图与空元数据，不抛错', () => {
    const parsed = parseDefinition('{}')

    expect(parsed.data).toEqual({ nodes: [], edges: [] })
    expect(parsed.meta).toEqual({
      code: '',
      name: '',
      description: '',
      category: '',
      enableCompensation: false,
      variables: [],
    })
  })
})

describe('parseDefinition 的图与元数据还原', () => {
  it('节点缺省 name 时回落为节点 id，properties 缺省为空对象', () => {
    const parsed = parseDefinition(JSON.stringify({
      nodes: [{ id: 'n1', activityType: 'UserTask' }],
    }))

    expect(parsed.data.nodes[0]?.data).toEqual({
      activityType: 'UserTask',
      name: 'n1',
      properties: {},
      retryPolicy: null,
      timeoutSeconds: null,
      continueOnError: false,
    })
    expect(parsed.data.nodes[0]?.shape).toBe(ACTIVITY_SHAPE)
    expect(parsed.data.nodes[0]?.ports).toBe('both')
  })

  it('画布坐标来自 extraProperties.designerLayout，缺坐标的节点落在原点', () => {
    const parsed = parseDefinition(JSON.stringify({
      nodes: [{ id: 'n1', activityType: 'Start' }, { id: 'n2', activityType: 'End' }],
      extraProperties: { designerLayout: JSON.stringify({ n1: { x: 11, y: 22 } }) },
    }))

    expect(parsed.data.nodes.map(item => [item.id, item.x, item.y])).toEqual([
      ['n1', 11, 22],
      ['n2', 0, 0],
    ])
  })

  it('坐标 JSON 损坏时静默回退空布局并触发一次自动布局，而不是让整张图挤在原点', () => {
    const parsed = parseDefinition(JSON.stringify({
      nodes: [{ id: 'n1', activityType: 'Start' }, { id: 'n2', activityType: 'End' }],
      transitions: [{ sourceNodeId: 'n1', targetNodeId: 'n2' }],
      extraProperties: { designerLayout: '{坏掉的 JSON' },
    }))

    expect(parsed.data.nodes.map(item => [item.x, item.y])).toEqual([[60, 80], [320, 80]])
  })

  it('没有坐标记录时同样自动布局；节点为空则不布局也不抛错', () => {
    const laidOut = parseDefinition(JSON.stringify({
      nodes: [{ id: 'n1', activityType: 'Start' }],
    }))
    expect(laidOut.data.nodes[0]?.x).toBe(60)

    expect(parseDefinition('{"nodes":[]}').data.nodes).toEqual([])
  })

  it('连线缺省 id 时用「源->目标#序号」兜底，默认分支画成虚线', () => {
    const parsed = parseDefinition(JSON.stringify({
      nodes: [{ id: 'a', activityType: 'Start' }, { id: 'b', activityType: 'End' }],
      transitions: [
        { sourceNodeId: 'a', targetNodeId: 'b', isDefault: true },
        { id: 'given', sourceNodeId: 'a', targetNodeId: 'b', condition: 'x > 1' },
      ],
    }))

    expect(parsed.data.edges.map(item => [item.id, item.label, item.dashed])).toEqual([
      ['a->b#1', 'default', true],
      ['given', 'x > 1', false],
    ])
  })

  it('元数据字段强制转字符串，enableCompensation 强制转布尔', () => {
    const parsed = parseDefinition(JSON.stringify({
      code: 123,
      name: null,
      description: '说明',
      category: 0,
      enableCompensation: 1,
    }))

    expect(parsed.meta).toMatchObject({
      code: '123',
      name: '',
      description: '说明',
      category: '0',
      enableCompensation: true,
    })
  })

  it('变量的 defaultValue / description 只有 null 与 undefined 才归一为 null，空串保留', () => {
    const parsed = parseDefinition(JSON.stringify({
      variables: [
        { name: 'a', required: true, defaultValue: null },
        { name: 'b', defaultValue: '', description: '' },
        { name: 'c', defaultValue: 0 },
      ],
    }))

    expect(parsed.meta.variables).toEqual([
      { name: 'a', required: true, defaultValue: null, description: null },
      { name: 'b', required: false, defaultValue: '', description: '' },
      { name: 'c', required: false, defaultValue: '0', description: null },
    ])
  })

  it('原始定义对象整体挂在 raw 上，供序列化时透传未编辑字段', () => {
    const parsed = parseDefinition(JSON.stringify({ id: '99', version: 3, status: 'Published' }))

    expect(parsed.raw).toMatchObject({ id: '99', version: 3, status: 'Published' })
  })
})

describe('serializeDefinition 的落库形状', () => {
  it('未编辑字段原样透传，编辑字段被裁剪后覆盖', () => {
    const raw = { id: '99', version: 3, status: 'Draft', code: '旧编码' }
    const json = serializeDefinition(raw, meta({ code: '  new-code  ', name: '  新名字  ' }), {
      nodes: [],
      edges: [],
    })
    const result = JSON.parse(json) as Record<string, unknown>

    expect(result).toMatchObject({ id: '99', version: 3, status: 'Draft', code: 'new-code', name: '新名字' })
  })

  it('描述与分类裁剪后为空时写 null，而不是留空串', () => {
    const json = serializeDefinition({}, meta({ description: '   ', category: '' }), { nodes: [], edges: [] })
    const result = JSON.parse(json) as Record<string, unknown>

    expect(result.description).toBeNull()
    expect(result.category).toBeNull()
  })

  it('画布坐标四舍五入后写进 extraProperties.designerLayout，并保留原有其它扩展属性', () => {
    const json = serializeDefinition(
      { extraProperties: { owner: 'ops' } },
      meta(),
      { nodes: [node('a', 'Start', 10.4, 20.6)], edges: [] },
    )
    const result = JSON.parse(json) as { extraProperties: Record<string, string> }

    expect(result.extraProperties.owner).toBe('ops')
    expect(JSON.parse(result.extraProperties.designerLayout ?? '{}')).toEqual({ a: { x: 10, y: 21 } })
  })

  it('空名变量被剔除，其余变量名与默认值两端裁空白', () => {
    const json = serializeDefinition({}, meta({
      variables: [
        { name: '   ', required: false },
        { name: '  amount  ', required: true, defaultValue: '  100  ', description: '  金额  ' },
      ],
    }), { nodes: [], edges: [] })
    const result = JSON.parse(json) as { variables: unknown[] }

    expect(result.variables).toEqual([
      { name: 'amount', required: true, defaultValue: '100', description: '金额' },
    ])
  })

  it('节点的可选字段只在有值时写出：重试策略、超时、失败继续', () => {
    const bare = node('a', 'UserTask')
    const rich: DiagramNode = {
      ...node('b', 'UserTask'),
      data: {
        activityType: 'UserTask',
        name: '',
        properties: { Title: 'x' },
        retryPolicy: { maxAttempts: 3, firstDelaySeconds: 5, backoffFactor: 2 },
        timeoutSeconds: 0,
        continueOnError: true,
      } satisfies DesignerNodeData,
    }
    const json = serializeDefinition({}, meta(), { nodes: [bare, rich], edges: [] })
    const result = JSON.parse(json) as { nodes: Record<string, unknown>[] }

    expect(result.nodes[0]).toEqual({ id: 'a', name: 'a', activityType: 'UserTask', properties: {} })
    expect(result.nodes[1]).toEqual({
      id: 'b',
      name: 'b',
      activityType: 'UserTask',
      properties: { Title: 'x' },
      retryPolicy: { maxAttempts: 3, firstDelaySeconds: 5, backoffFactor: 2 },
      timeoutSeconds: 0,
      continueOnError: true,
    })
  })

  it('连线只写出有值的名称与条件，默认分支才写 isDefault，优先级缺省为 0', () => {
    const json = serializeDefinition({}, meta(), {
      nodes: [],
      edges: [
        edge('a', 'b'),
        { id: '', source: 'a', target: 'c', data: { name: '通过', condition: 'x > 1', isDefault: true, priority: 5 } },
      ],
    })
    const result = JSON.parse(json) as { transitions: Record<string, unknown>[] }

    expect(result.transitions[0]).toEqual({ id: 'a->b', sourceNodeId: 'a', targetNodeId: 'b', priority: 0 })
    expect(result.transitions[1]).toEqual({
      id: 'a->c#2',
      sourceNodeId: 'a',
      targetNodeId: 'c',
      priority: 5,
      name: '通过',
      condition: 'x > 1',
      isDefault: true,
    })
  })

  it('解析→序列化→再解析后图结构稳定，不会每存一次就漂移', () => {
    const source = JSON.stringify({
      code: 'flow',
      name: '流程',
      nodes: [{ id: 'a', activityType: 'Start' }, { id: 'b', activityType: 'End' }],
      transitions: [{ id: 't1', sourceNodeId: 'a', targetNodeId: 'b' }],
    })
    const first = parseDefinition(source)
    const second = parseDefinition(serializeDefinition(first.raw, first.meta, first.data))
    const third = parseDefinition(serializeDefinition(second.raw, second.meta, second.data))

    expect(second.data).toEqual(third.data)
    expect(second.meta).toEqual(third.meta)
  })
})

describe('validateGraph 的本地校验', () => {
  function codesOf(metaInput: DefinitionMeta, data: DiagramData) {
    return validateGraph(metaInput, data).map(issue => issue.code)
  }

  it('编码与名称为空各报一条错误', () => {
    expect(codesOf(meta({ code: '  ', name: '' }), { nodes: [], edges: [] }))
      .toEqual(['code', 'name'])
  })

  it('空图在元数据齐全时没有任何问题——新建流程不该一上来就报错', () => {
    expect(validateGraph(meta(), { nodes: [], edges: [] })).toEqual([])
  })

  it('节点 id 重复逐个报错并带上定位用的 nodeId', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('s', 'End')],
      edges: [edge('s', 's')],
    }
    const issues = validateGraph(meta(), data)

    expect(issues).toContainEqual({ code: 'dup_node', level: 'error', nodeId: 's' })
  })

  it('缺开始节点报 no_start，多个开始节点逐个报 multi_start', () => {
    const noStart: DiagramData = { nodes: [node('a', 'UserTask')], edges: [] }
    expect(codesOf(meta(), noStart)).toContain('no_start')

    const multiStart: DiagramData = { nodes: [node('s1', 'Start'), node('s2', 'Start')], edges: [] }
    const codes = codesOf(meta(), multiStart)
    expect(codes.filter(code => code === 'multi_start')).toHaveLength(2)
  })

  it('缺结束/终止节点只报警告，不阻断保存', () => {
    const data: DiagramData = { nodes: [node('s', 'Start')], edges: [] }
    const issue = validateGraph(meta(), data).find(item => item.code === 'no_end')

    expect(issue?.level).toBe('warning')
  })

  it('故障节点（Fault）不算结束节点，仍然提示缺结束', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('f', 'Fault')],
      edges: [edge('s', 'f')],
    }

    expect(codesOf(meta(), data)).toContain('no_end')
  })

  it('连线端点不存在报 dangling_edge 且只报一次', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start')],
      edges: [edge('s', '不存在'), edge('也不存在', 's')],
    }
    const codes = codesOf(meta(), data)

    expect(codes.filter(code => code === 'dangling_edge')).toHaveLength(1)
  })

  it('非终止节点无出边报 dead_end，终止类节点不报', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('t', 'UserTask'), node('e', 'End')],
      edges: [edge('s', 't')],
    }
    const issues = validateGraph(meta(), data)

    expect(issues).toContainEqual({ code: 'dead_end', level: 'warning', nodeId: 't' })
    expect(issues.filter(item => item.code === 'dead_end' && item.nodeId === 'e')).toEqual([])
  })

  it('非开始节点无入边报 no_incoming，开始节点自身不报', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('x', 'End')],
      edges: [],
    }
    const issues = validateGraph(meta(), data)

    expect(issues).toContainEqual({ code: 'no_incoming', level: 'warning', nodeId: 'x' })
    expect(issues.filter(item => item.code === 'no_incoming' && item.nodeId === 's')).toEqual([])
  })

  it('判定网关有条件分支但没有默认或兜底分支时报 decision_no_default', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('d', 'Decision'), node('e1', 'End'), node('e2', 'End')],
      edges: [
        edge('s', 'd'),
        edge('d', 'e1', { condition: 'x > 1' }),
        edge('d', 'e2', { condition: 'x <= 1' }),
      ],
    }

    expect(codesOf(meta(), data)).toContain('decision_no_default')
  })

  it('判定网关只要有一条默认分支或无条件分支就不再报缺兜底', () => {
    const withDefault: DiagramData = {
      nodes: [node('s', 'Start'), node('d', 'Decision'), node('e1', 'End'), node('e2', 'End')],
      edges: [
        edge('s', 'd'),
        edge('d', 'e1', { condition: 'x > 1' }),
        edge('d', 'e2', { isDefault: true }),
      ],
    }
    expect(codesOf(meta(), withDefault)).not.toContain('decision_no_default')

    const withUnconditional: DiagramData = {
      nodes: [node('s', 'Start'), node('d', 'Decision'), node('e1', 'End'), node('e2', 'End')],
      edges: [
        edge('s', 'd'),
        edge('d', 'e1', { condition: 'x > 1' }),
        edge('d', 'e2'),
      ],
    }
    expect(codesOf(meta(), withUnconditional)).not.toContain('decision_no_default')
  })

  it('从开始节点不可达的节点逐个报 unreachable', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('e', 'End'), node('lost', 'Log')],
      edges: [edge('s', 'e'), edge('lost', 'lost')],
    }
    const issues = validateGraph(meta(), data)

    expect(issues).toContainEqual({ code: 'unreachable', level: 'warning', nodeId: 'lost' })
    expect(issues.filter(item => item.code === 'unreachable' && item.nodeId === 'e')).toEqual([])
  })

  it('一条最小可用流程（开始→审批→结束）没有任何 error 级问题', () => {
    const data: DiagramData = {
      nodes: [node('s', 'Start'), node('t', 'UserTask'), node('e', 'End')],
      edges: [edge('s', 't'), edge('t', 'e')],
    }
    const issues = validateGraph(meta(), data)

    expect(issues.filter(item => item.level === 'error')).toEqual([])
  })
})
