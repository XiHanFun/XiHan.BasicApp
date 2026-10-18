/**
 * 后端定义 JSON ↔ 流程设计器流程定义的单元测试。
 *
 * 职责边界：只测 BasicApp 这一层——存量定义（X6 版设计器存的）照原坐标打开、往返不丢字段、旧版受理人写法的迁移。
 * 预设自身的互转规则由流程设计器仓库的测试覆盖。
 */
import { describe, expect, it } from 'vitest'
import { openDefinition, serializeDefinition } from './definition-codec'

/** X6 版设计器保存过的一份定义：坐标在 extraProperties.designerLayout，id / 版本 / 状态等由后端带着 */
const savedByX6 = JSON.stringify({
  id: 'd-1',
  code: 'leave',
  name: '请假',
  version: 3,
  status: 'draft',
  tenantId: 7,
  description: null,
  enableCompensation: false,
  variables: [{ name: 'days', required: true, defaultValue: null, description: '天数' }],
  nodes: [
    { id: 'start', name: '开始', activityType: 'Start', properties: {} },
    { id: 'approve', name: '主管审批', activityType: 'UserTask', properties: { AssigneeRules: [{ type: 'Role', values: ['manager'] }] }, timeoutSeconds: 3600 },
    { id: 'gate', name: '天数', activityType: 'Decision', properties: {} },
    { id: 'end', name: '结束', activityType: 'End', properties: { Outcome: 'approved' } },
  ],
  transitions: [
    { id: 't1', sourceNodeId: 'start', targetNodeId: 'approve', priority: 0 },
    { id: 't2', sourceNodeId: 'approve', targetNodeId: 'gate', condition: 'outcome == \'approved\'', priority: 0 },
    { id: 't3', sourceNodeId: 'gate', targetNodeId: 'end', priority: 1, isDefault: true },
  ],
  extraProperties: {
    designerLayout: JSON.stringify({ start: { x: 60, y: 80 }, approve: { x: 320, y: 80 }, gate: { x: 580, y: 80 }, end: { x: 840, y: 80 } }),
    owner: 'hr',
  },
})

describe('openDefinition 打开存量定义', () => {
  it('坐标照 designerLayout 原样还原，不重新排布', () => {
    const document = openDefinition(savedByX6)
    expect(document.nodes.map(node => [node.id, node.x, node.y])).toEqual([
      ['start', 60, 80],
      ['approve', 320, 80],
      ['gate', 580, 80],
      ['end', 840, 80],
    ])
  })

  it('节点类型、名称与属性落到流程定义的对应位置', () => {
    const approve = openDefinition(savedByX6).nodes.find(node => node.id === 'approve')!
    expect(approve.type).toBe('UserTask')
    expect(approve.label).toBe('主管审批')
    expect(approve.data).toMatchObject({ properties: { AssigneeRules: [{ type: 'Role', values: ['manager'] }] }, timeoutSeconds: 3600 })
  })

  it('没有坐标的定义（代码里建的模板）也能打开，节点都有坐标', () => {
    const document = openDefinition(JSON.stringify({
      code: 'permission-request',
      name: '权限申请',
      nodes: [
        { id: 'start', activityType: 'Start' },
        { id: 'admin', activityType: 'UserTask', properties: { AssigneeRules: [{ type: 'Role', values: ['admin'] }] } },
        { id: 'end', activityType: 'End' },
      ],
      transitions: [
        { sourceNodeId: 'start', targetNodeId: 'admin' },
        { sourceNodeId: 'admin', targetNodeId: 'end' },
      ],
    }))
    expect(document.nodes.every(node => Number.isFinite(node.x) && Number.isFinite(node.y))).toBe(true)
    expect(new Set(document.nodes.map(node => node.x)).size).toBe(3)
  })

  it('新建时的空定义打开为空画布', () => {
    const document = openDefinition('{"code":"","name":"","nodes":[],"transitions":[]}')
    expect(document.nodes).toEqual([])
    expect(document.edges).toEqual([])
  })

  it('非法 JSON 与顶层不是对象时抛错，交由调用方提示', () => {
    expect(() => openDefinition('{')).toThrow()
    expect(() => openDefinition('[]')).toThrow(TypeError)
    expect(() => openDefinition('null')).toThrow(TypeError)
  })
})

describe('serializeDefinition 写回后端格式', () => {
  it('打开再保存，后端字段、节点、连线与坐标都不变', () => {
    const before = JSON.parse(savedByX6)
    const after = JSON.parse(serializeDefinition(openDefinition(savedByX6)))
    expect(after).toMatchObject({ id: 'd-1', code: 'leave', version: 3, status: 'draft', tenantId: 7, variables: before.variables })
    expect(after.nodes).toEqual(before.nodes)
    expect(after.transitions.map((item: { id: string }) => item.id)).toEqual(['t1', 't2', 't3'])
    expect(after.transitions[2]).toMatchObject({ isDefault: true, priority: 1 })
    expect(after.extraProperties.owner).toBe('hr')
    expect(JSON.parse(after.extraProperties.designerLayout)).toEqual(JSON.parse(before.extraProperties.designerLayout))
  })

  it('往返两次结果一致，不会每存一次就漂移', () => {
    const once = serializeDefinition(openDefinition(savedByX6))
    expect(serializeDefinition(openDefinition(once))).toBe(once)
  })
})

describe('openDefinition 的旧版受理人迁移', () => {
  it('人工任务的 Assignees / AssigneesExpression / CcUserIds 并入规则后删除', () => {
    const document = openDefinition(JSON.stringify({
      code: 'c',
      name: 'n',
      nodes: [{ id: 'task', activityType: 'UserTask', properties: { Assignees: ['1', '2'], AssigneesExpression: 'starterLeader', CcUserIds: ['9'] } }],
      transitions: [],
    }))
    expect(document.nodes[0]!.data?.properties).toEqual({
      AssigneeRules: [{ type: 'User', values: ['1', '2'] }, { type: 'Expression', values: ['starterLeader'] }],
      CcRules: [{ type: 'User', values: ['9'] }],
    })
  })

  it('别的节点上的同名属性原样保留', () => {
    const document = openDefinition(JSON.stringify({
      code: 'c',
      name: 'n',
      nodes: [{ id: 'log', activityType: 'Log', properties: { Assignees: ['1'] } }],
      transitions: [],
    }))
    expect(document.nodes[0]!.data?.properties).toEqual({ Assignees: ['1'] })
  })
})
