/**
 * WorkflowJson ↔ 预设 4.x 引擎定义映射层（flow-workflow-json.ts）的单元测试。
 *
 * 锁定：两个方向的字段换算（名字与显示名、节点超时与重试的 TimeSpan、设计器画面数据的对象与 JSON 字符串、
 * 延时 / 遍历 / 子流程里预设沿用 4.x 叫法的属性键）、
 * 缺 id 补合法 id、去掉 4.x 才有的字段、往返稳定；经预设打开时坐标照存的还原，没有画面数据的定义按连线自动排布。
 */
import type { WorkflowJsonDefinition } from './flow-workflow-json'
import { fromWorkflowDefinition, toWorkflowDefinition } from '@xihan-flow/core/presets/xihan-workflow'
import { describe, expect, it } from 'vitest'
import { fromWorkflowJson, secondsToTimeSpan, timeSpanToSeconds, toWorkflowJson } from './flow-workflow-json'

/** 后端 get 返回的一份定义：框架序列化的写法（camelCase、不写 null、TimeSpan 文本），带设计器画面数据。 */
const stored: WorkflowJsonDefinition = {
  id: '0193f0aa00007000800000000000a001',
  name: 'leave',
  version: 3,
  displayName: '请假',
  description: '请假审批',
  category: '人事',
  status: 'Draft',
  enableCompensation: false,
  compensateOnFault: false,
  timeoutAction: 'Fault',
  createdAt: '2026-10-30T08:00:00+00:00',
  variables: [{ name: 'days', required: true, description: '天数' }],
  nodes: [
    { id: 'start', activityType: 'Start', displayName: '开始', properties: {}, continueOnError: false },
    {
      id: 'approve',
      activityType: 'UserTask',
      displayName: '主管审批',
      properties: { AssigneeRules: [{ type: 'Role', values: ['manager'] }] },
      retryPolicy: { maxAttempts: 3, firstDelay: '00:00:30', backoffFactor: 2 },
      timeout: '1.02:00:00',
      continueOnError: false,
    },
    { id: 'end', activityType: 'End', displayName: '结束', properties: { Outcome: 'approved' }, continueOnError: false },
  ],
  transitions: [
    { id: 't1', sourceNodeId: 'start', targetNodeId: 'approve', priority: 0, isDefault: false },
    { id: 't2', sourceNodeId: 'approve', targetNodeId: 'end', condition: 'outcome == \'approved\'', priority: 0, isDefault: false, displayName: '通过' },
  ],
  properties: {
    designerLayout: { start: { x: 60, y: 80 }, approve: { x: 320, y: 80 }, end: { x: 580, y: 80 } },
    owner: 'hr',
  },
}

describe('timeSpan 与秒数', () => {
  it('框架的常量格式与秒数互换：不满一天 hh:mm:ss，满一天 d.hh:mm:ss，小数七位', () => {
    expect(timeSpanToSeconds('01:00:00')).toBe(3600)
    expect(timeSpanToSeconds('1.02:03:04')).toBe(93_784)
    expect(timeSpanToSeconds('00:00:00.5000000')).toBe(0.5)
    expect(timeSpanToSeconds('2')).toBe(172_800)
    expect(secondsToTimeSpan(3600)).toBe('01:00:00')
    expect(secondsToTimeSpan(93_784)).toBe('1.02:03:04')
    expect(secondsToTimeSpan(0.5)).toBe('00:00:00.5000000')
  })

  it('不合法的文本与负数抛错，不静默当成没有', () => {
    expect(() => timeSpanToSeconds('soon')).toThrow(TypeError)
    expect(() => timeSpanToSeconds('25:00:00')).toThrow(TypeError)
    expect(() => secondsToTimeSpan(-1)).toThrow(TypeError)
  })
})

describe('fromWorkflowJson：WorkflowJson → 预设的 4.x 定义', () => {
  it('名字成编码、显示名成名称，节点超时与重试换成秒，画面数据进 extraProperties 的 JSON 字符串', () => {
    const legacy = fromWorkflowJson(stored)

    expect(legacy).toMatchObject({ code: 'leave', name: '请假', version: 3, category: '人事', status: 'Draft' })
    expect((legacy as Record<string, unknown>).displayName).toBeUndefined()
    expect(legacy.nodes?.[1]).toMatchObject({ id: 'approve', name: '主管审批', timeoutSeconds: 93_600, retryPolicy: { maxAttempts: 3, firstDelaySeconds: 30, backoffFactor: 2 } })
    expect((legacy.nodes?.[1] as Record<string, unknown> | undefined)?.timeout).toBeUndefined()
    expect(legacy.transitions?.[1]).toMatchObject({ id: 't2', name: '通过' })
    expect(JSON.parse(legacy.extraProperties?.designerLayout ?? 'null')).toEqual(stored.properties?.designerLayout)
    expect((legacy as Record<string, unknown>).properties).toEqual({ owner: 'hr' })
  })

  it('缺 id 的节点与连线补上框架认的 id，不交给预设补出含 -> 与 # 的 id', () => {
    const legacy = fromWorkflowJson({
      name: 'gen',
      nodes: [{ id: 'start', activityType: 'Start' }, { activityType: 'UserTask' }, { id: 'UserTask_1', activityType: 'UserTask' }],
      transitions: [{ sourceNodeId: 'start', targetNodeId: 'UserTask_1' }, { id: 'edge_1', sourceNodeId: 'UserTask_1', targetNodeId: 'start' }],
    })

    expect(legacy.nodes?.map(node => node.id)).toEqual(['start', 'UserTask_2', 'UserTask_1'])
    expect(legacy.transitions?.map(edge => edge.id)).toEqual(['edge_2', 'edge_1'])
    const document = fromWorkflowDefinition(legacy)
    expect(document.edges.every(edge => /^[A-Z0-9][\w.-]{0,63}$/i.test(edge.id))).toBe(true)
  })

  it('不是对象、nodes 不是数组、超时不是 TimeSpan 时抛 TypeError', () => {
    expect(() => fromWorkflowJson([] as unknown as WorkflowJsonDefinition)).toThrow(TypeError)
    expect(() => fromWorkflowJson({ nodes: {} as never })).toThrow(TypeError)
    expect(() => fromWorkflowJson({ nodes: [{ id: 'a', activityType: 'Delay', timeout: 'soon' }] })).toThrow(TypeError)
  })
})

describe('toWorkflowJson：预设的 4.x 定义 → WorkflowJson', () => {
  it('编码成名字、名称成显示名，秒数换成 TimeSpan，extraProperties 的画面数据解析回 properties 的对象', () => {
    const json = toWorkflowJson({
      code: 'expense',
      name: '报销',
      nodes: [
        { id: 'start', name: '', activityType: 'Start', properties: {} },
        { id: 'pay', name: '付款', activityType: 'Http', properties: { Url: 'https://pay' }, timeoutSeconds: 3600, retryPolicy: { maxAttempts: 2, firstDelaySeconds: 10 } },
      ],
      transitions: [{ id: 'edge_1', name: '去付款', sourceNodeId: 'start', targetNodeId: 'pay', priority: 0 }],
      extraProperties: { designerLayout: '{"start":{"x":0,"y":0},"pay":{"x":200,"y":0}}' },
    })

    expect(json).toMatchObject({ name: 'expense', displayName: '报销' })
    expect(json.code).toBeUndefined()
    expect(json.extraProperties).toBeUndefined()
    expect(json.nodes?.[0]).toEqual({ id: 'start', activityType: 'Start', properties: {} })
    expect(json.nodes?.[1]).toEqual({ id: 'pay', activityType: 'Http', displayName: '付款', properties: { Url: 'https://pay' }, retryPolicy: { maxAttempts: 2, firstDelay: '00:00:10' }, timeout: '01:00:00' })
    expect(json.transitions?.[0]).toEqual({ id: 'edge_1', sourceNodeId: 'start', targetNodeId: 'pay', priority: 0, displayName: '去付款' })
    expect(json.properties).toEqual({ designerLayout: { start: { x: 0, y: 0 }, pay: { x: 200, y: 0 } } })
  })

  it('去掉 4.x 才有的 creationTime、publishTime 与数字租户，框架的字符串租户留着', () => {
    const legacy = toWorkflowJson({ code: 'a', name: 'A', tenantId: 7, creationTime: '2025-01-01', publishTime: null, nodes: [], transitions: [] }) as Record<string, unknown>
    expect(Object.keys(legacy)).toEqual(['name', 'displayName', 'nodes', 'transitions'])

    const current = toWorkflowJson({ code: 'a', tenantId: 'acme' } as never) as Record<string, unknown>
    expect(current.tenantId).toBe('acme')
  })
})

describe('经预设往返', () => {
  const open = (json: WorkflowJsonDefinition) => fromWorkflowDefinition(fromWorkflowJson(json))
  const save = (document: ReturnType<typeof open>) => toWorkflowJson(toWorkflowDefinition(document))

  it('坐标照 properties.designerLayout 还原，保存后字段与坐标都不变', () => {
    const document = open(stored)
    expect(document.nodes.map(node => [node.id, node.x, node.y])).toEqual([['start', 60, 80], ['approve', 320, 80], ['end', 580, 80]])

    const saved = save(document)
    expect(saved).toEqual(stored)
  })

  it('往返两次结果一致，不会每存一次就漂移', () => {
    const once = JSON.stringify(save(open(stored)))
    expect(JSON.stringify(save(open(JSON.parse(once) as WorkflowJsonDefinition)))).toBe(once)
  })

  it('没有画面数据的定义（后端种子与代码里建的）也能打开，按连线自动排布', () => {
    const document = open({
      name: 'demo-onboarding',
      displayName: 'Onboarding',
      nodes: [
        { id: 'start', activityType: 'Start' },
        { id: 'it', activityType: 'UserTask', properties: { AssigneeRules: [{ type: 'Role', values: ['admin'] }] } },
        { id: 'end', activityType: 'End' },
      ],
      transitions: [
        { id: 't1', sourceNodeId: 'start', targetNodeId: 'it' },
        { id: 't2', sourceNodeId: 'it', targetNodeId: 'end' },
      ],
    })
    expect(document.nodes.every(node => Number.isFinite(node.x) && Number.isFinite(node.y))).toBe(true)
    expect(new Set(document.nodes.map(node => node.x)).size).toBe(3)
    expect(save(document).properties).toMatchObject({ designerLayout: expect.any(Object) })
  })
})

describe('节点属性的叫法', () => {
  const document: WorkflowJsonDefinition = {
    id: 'a',
    name: 'onboarding',
    version: 1,
    nodes: [
      { id: 'wait', activityType: 'Delay', properties: { Duration: 60 } },
      { id: 'each', activityType: 'ForEach', properties: { Items: 'equipment', ItemVariable: 'itemName', DefinitionName: 'procurement', Parallel: true } },
      { id: 'sub', activityType: 'SubWorkflow', properties: { DefinitionName: 'expense', VariableExpressions: { amount: 'travelAmount' } } },
    ],
    transitions: [],
  }

  it('打开时换成预设的键，保存时换回框架的键，别的键原样', () => {
    const opened = fromWorkflowJson(document)
    expect(opened.nodes?.map(node => node.properties)).toEqual([
      { DurationSeconds: 60 },
      { ItemsExpression: 'equipment', ItemVariableName: 'itemName', DefinitionCode: 'procurement', Parallel: true },
      { DefinitionCode: 'expense', VariableExpressions: { amount: 'travelAmount' } },
    ])
    expect(toWorkflowJson(opened).nodes?.map(node => node.properties)).toEqual(document.nodes?.map(node => node.properties))
  })
})
