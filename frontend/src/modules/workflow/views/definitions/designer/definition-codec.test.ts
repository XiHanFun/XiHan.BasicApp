/**
 * 后端定义文档（WorkflowJson）↔ 流程设计器流程定义的单元测试
 *
 * 职责边界：只测 BasicApp 这一层——存过的定义照原坐标打开、往返不丢字段、受理人旧写法的迁移
 * WorkflowJson 与预设写法的逐项换算由 ~/designers 的映射层测试覆盖，预设自身的互转规则由流程设计器仓库的测试覆盖
 */
import { describe, expect, it } from 'vitest'
import { openDefinition, serializeDefinition } from './definition-codec'

/** 设计器保存过的一份定义：坐标在 properties.designerLayout，id / 版本 / 状态 / 租户等由后端带着 */
const saved = JSON.stringify({
  id: '0193f0aa00007000800000000000d001',
  name: 'leave',
  version: 3,
  displayName: '请假',
  status: 'Draft',
  enableCompensation: false,
  tenantId: 'acme',
  createdAt: '2026-10-30T08:00:00+00:00',
  variables: [
    { name: 'days', required: true, description: '天数' },
  ],
  nodes: [
    {
      id: 'start',
      activityType: 'Start',
      displayName: '开始',
      properties: {},
      continueOnError: false,
    },
    {
      id: 'approve',
      activityType: 'UserTask',
      displayName: '主管审批',
      properties: {
        AssigneeRules: [
          {
            type: 'Role',
            values: ['manager'],
          },
        ],
      },
      timeout: '01:00:00',
      continueOnError: false,
    },
    {
      id: 'gate',
      activityType: 'Decision',
      displayName: '天数',
      properties: {},
      continueOnError: false,
    },
    {
      id: 'end',
      activityType: 'End',
      displayName: '结束',
      properties: { Outcome: 'approved' },
      continueOnError: false,
    },
  ],
  transitions: [
    {
      id: 't1',
      sourceNodeId: 'start',
      targetNodeId: 'approve',
      priority: 0,
      isDefault: false,
    },
    {
      id: 't2',
      sourceNodeId: 'approve',
      targetNodeId: 'gate',
      condition: 'outcome == \'approved\'',
      priority: 0,
      isDefault: false,
    },
    {
      id: 't3',
      sourceNodeId: 'gate',
      targetNodeId: 'end',
      priority: 1,
      isDefault: true,
    },
  ],
  properties: {
    designerLayout: {
      start: { x: 60, y: 80 },
      approve: { x: 320, y: 80 },
      gate: { x: 580, y: 80 },
      end: { x: 840, y: 80 },
    },
    owner: 'hr',
  },
})

describe('openDefinition 打开存过的定义', () => {
  it('坐标照 designerLayout 原样还原，不重新排布', () => {
    const document = openDefinition(saved)
    expect(document.nodes.map(node => [node.id, node.x, node.y])).toEqual([
      ['start', 60, 80],
      ['approve', 320, 80],
      ['gate', 580, 80],
      ['end', 840, 80],
    ])
  })

  it('节点类型、显示名、属性与超时落到流程定义的对应位置', () => {
    const approve = openDefinition(saved).nodes.find(node => node.id === 'approve')!
    expect(approve.type).toBe('UserTask')
    expect(approve.label).toBe('主管审批')
    expect(approve.data).toMatchObject({
      properties: {
        AssigneeRules: [
          {
            type: 'Role',
            values: ['manager'],
          },
        ],
      },
      timeoutSeconds: 3600,
    })
  })

  it('没有坐标的定义（后端种子与代码里建的）也能打开，节点都有坐标，缺 id 的连线补上合法 id', () => {
    const document = openDefinition(JSON.stringify({
      name: 'permission-request',
      displayName: '权限申请',
      nodes: [
        { id: 'start', activityType: 'Start' },
        {
          id: 'admin',
          activityType: 'UserTask',
          properties: {
            AssigneeRules: [
              {
                type: 'Role',
                values: ['admin'],
              },
            ],
          },
        },
        { id: 'end', activityType: 'End' },
      ],
      transitions: [
        { sourceNodeId: 'start', targetNodeId: 'admin' },
        { sourceNodeId: 'admin', targetNodeId: 'end' },
      ],
    }))
    expect(document.nodes.every(node => Number.isFinite(node.x) && Number.isFinite(node.y))).toBe(true)
    expect(new Set(document.nodes.map(node => node.x)).size).toBe(3)
    expect(document.edges.map(edge => edge.id)).toEqual(['edge_1', 'edge_2'])
  })

  it('新建时的空白定义打开为空画布', () => {
    const document = openDefinition('{"name":"expense","displayName":"报销","category":null,"variables":[],"nodes":[],"transitions":[]}')
    expect(document.nodes).toEqual([])
    expect(document.edges).toEqual([])
    expect(document.data).toMatchObject({ code: 'expense', name: '报销' })
  })

  it('非法 JSON 与顶层不是对象时抛错，交由调用方提示', () => {
    expect(() => openDefinition('{')).toThrow()
    expect(() => openDefinition('[]')).toThrow(TypeError)
    expect(() => openDefinition('null')).toThrow(TypeError)
  })
})

describe('serializeDefinition 写回 WorkflowJson', () => {
  it('打开再保存，后端字段、节点、连线与坐标都不变', () => {
    const before = JSON.parse(saved)
    const after = JSON.parse(serializeDefinition(openDefinition(saved)))
    expect(after).toMatchObject({
      id: before.id,
      name: 'leave',
      displayName: '请假',
      version: 3,
      status: 'Draft',
      tenantId: 'acme',
      variables: before.variables,
    })
    expect(after.code).toBeUndefined()
    expect(after.nodes).toEqual(before.nodes)
    expect(after.transitions.map((item: { id: string }) => item.id)).toEqual(['t1', 't2', 't3'])
    expect(after.transitions[2]).toMatchObject({ isDefault: true, priority: 1 })
    expect(after.properties.owner).toBe('hr')
    expect(after.properties.designerLayout).toEqual(before.properties.designerLayout)
  })

  it('往返两次结果一致，不会每存一次就漂移', () => {
    const once = serializeDefinition(openDefinition(saved))
    expect(serializeDefinition(openDefinition(once))).toBe(once)
  })
})

describe('openDefinition 的受理人旧写法迁移', () => {
  it('人工任务的 Assignees / AssigneesExpression / CcUserIds 并入规则后删除', () => {
    const document = openDefinition(JSON.stringify({
      name: 'c',
      displayName: 'n',
      nodes: [
        {
          id: 'task',
          activityType: 'UserTask',
          properties: {
            Assignees: ['1', '2'],
            AssigneesExpression: 'starterLeader',
            CcUserIds: ['9'],
          },
        },
      ],
      transitions: [],
    }))
    expect(document.nodes[0]!.data?.properties).toEqual({
      AssigneeRules: [
        {
          type: 'User',
          values: ['1', '2'],
        },
        {
          type: 'Expression',
          values: ['starterLeader'],
        },
      ],
      CcRules: [
        {
          type: 'User',
          values: ['9'],
        },
      ],
    })
  })

  it('别的节点上的同名属性原样保留', () => {
    const document = openDefinition(JSON.stringify({
      name: 'c',
      displayName: 'n',
      nodes: [
        {
          id: 'log',
          activityType: 'Log',
          properties: {
            Assignees: ['1'],
          },
        },
      ],
      transitions: [],
    }))
    expect(document.nodes[0]!.data?.properties).toEqual({
      Assignees: ['1'],
    })
  })
})
