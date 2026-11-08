/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 流程实例页的状态、结果与运行轨迹着色规则（纯函数）
 *
 * 结果色调与标准结果放在这里、文案在 workflow.instances 下，
 * 审批中心（approvals/approval-labels）借用这里的色调、标准结果与状态 / 结果文案键
 */
import type { Tone } from '@xihan-ui/core'
import type { WorkflowInstanceStatus, WorkflowNodeInstanceStatus } from '~/contracts'
import type { FlowNodeStatus } from '~/designers/flow'

/** 标准办理结果（与后端 WorkflowUserTaskOutcomes 一致）；业务自定义的结果原样显示 */
export const STANDARD_OUTCOMES = ['approved', 'rejected', 'timeout'] as const
export type StandardOutcome = typeof STANDARD_OUTCOMES[number]

/** 实例状态色调 */
export function instanceStatusTone(status: WorkflowInstanceStatus): Tone {
  switch (status) {
    case 'Completed':
      return 'success'
    case 'Faulted':
      return 'danger'
    case 'Running':
      return 'info'
    case 'Suspended':
      return 'warning'
    default:
      return 'neutral'
  }
}

/** 节点执行状态色调 */
export function nodeStatusTone(status: WorkflowNodeInstanceStatus): Tone {
  switch (status) {
    case 'Completed':
      return 'success'
    case 'Faulted':
      return 'danger'
    case 'Suspended':
      return 'warning'
    case 'Running':
      return 'info'
    default:
      return 'neutral'
  }
}

/** 结果色调：通过绿、驳回红、超时黄、其余中性 */
export function outcomeTone(outcome?: string | null): Tone {
  if (outcome === 'approved')
    return 'success'
  if (outcome === 'rejected')
    return 'danger'
  if (outcome === 'timeout')
    return 'warning'
  return 'neutral'
}

/** 标准结果（有文案）；业务自定义结果与空值返回 null，调用方原样显示 */
export function standardOutcome(outcome?: string | null): StandardOutcome | null {
  return outcome && (STANDARD_OUTCOMES as readonly string[]).includes(outcome)
    ? outcome as StandardOutcome
    : null
}

/** 节点执行状态 → 流程图节点着色状态 */
const NODE_STATUS_MAP: Record<WorkflowNodeInstanceStatus, FlowNodeStatus> = {
  Running: 'running',
  Completed: 'completed',
  Faulted: 'faulted',
  Suspended: 'waiting',
  Canceled: 'canceled',
  Compensated: 'compensated',
}

/**
 * 定义节点 id → 运行态：取该节点最近一次执行的状态（执行历史按开始时间排），还在等待的补 waiting
 * @param nodes 节点执行历史
 * @param waits 待恢复的等待
 */
export function graphStatuses(
  nodes: readonly { nodeId: string, status: WorkflowNodeInstanceStatus }[],
  waits: readonly { nodeId: string }[],
): Record<string, FlowNodeStatus | null> {
  const result: Record<string, FlowNodeStatus | null> = {}
  for (const node of nodes)
    result[node.nodeId] = NODE_STATUS_MAP[node.status]
  for (const wait of waits) {
    if (result[wait.nodeId] === undefined)
      result[wait.nodeId] = 'waiting'
  }
  return result
}

/**
 * 定义文档（WorkflowJson）里节点 id → 显示名：执行历史只带节点 id，名字从实例所用的定义里取；文档读不出来时为空表
 * @param document 实例所用的定义文档
 */
export function nodeTitles(document: string | null | undefined): Record<string, string> {
  if (!document)
    return {}
  try {
    const raw: unknown = JSON.parse(document)
    const nodes = typeof raw === 'object' && raw !== null && Array.isArray((raw as { nodes?: unknown }).nodes)
      ? (raw as { nodes: unknown[] }).nodes
      : []
    return Object.fromEntries(nodes.flatMap((node) => {
      const { id, displayName } = (typeof node === 'object' && node !== null
        ? node
        : {}) as { id?: unknown, displayName?: unknown }
      return typeof id === 'string' && typeof displayName === 'string' && displayName !== ''
        ? [
            [id, displayName],
          ]
        : []
    }))
  }
  catch {
    return {}
  }
}
