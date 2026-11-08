/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 后端定义文档（框架 的 WorkflowJson）↔ 流程设计器的流程定义
 *
 * 预设只认它自己的定义格式：先经映射层（~/designers/flow 的 fromWorkflowJson / toWorkflowJson）换成预设的写法，
 * 再交给工作流引擎预设互转。坐标存 properties.designerLayout（{nodeId: {x,y}}），没列出的字段原样往返
 * 打开时把人工任务受理人的旧写法（Assignees / AssigneesExpression / CcUserIds）并入受理人规则，保存即落为新写法
 */

import type { FlowDocument, WorkflowJsonDefinition, XihanWorkflowDefinition } from '~/designers/flow'
import { fromWorkflowDefinition, fromWorkflowJson, toWorkflowDefinition, toWorkflowJson } from '~/designers/flow'
import { migrateLegacyAssignees } from './assignee-rules'

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/** 人工任务节点的属性副本里并入受理人的旧写法；别的节点原样返回 */
function migrateNode(node: unknown): unknown {
  if (!isRecord(node) || node.activityType !== 'UserTask' || !isRecord(node.properties))
    return node
  const properties = { ...node.properties }
  return migrateLegacyAssignees(properties)
    ? { ...node, properties }
    : node
}

/** 解析后端定义文档为流程定义；JSON 非法或结构不对时抛错，由调用方提示 */
export function openDefinition(json: string): FlowDocument {
  const raw: unknown = JSON.parse(json)
  if (!isRecord(raw))
    throw new TypeError('definition must be a JSON object')
  const legacy = fromWorkflowJson(raw as WorkflowJsonDefinition)
  const nodes = Array.isArray(legacy.nodes)
    ? legacy.nodes.map(migrateNode)
    : legacy.nodes
  return fromWorkflowDefinition({ ...legacy, nodes } as XihanWorkflowDefinition)
}

/** 流程定义序列化回后端定义文档 */
export function serializeDefinition(document: FlowDocument): string {
  return JSON.stringify(toWorkflowJson(toWorkflowDefinition(document)), null, 2)
}
