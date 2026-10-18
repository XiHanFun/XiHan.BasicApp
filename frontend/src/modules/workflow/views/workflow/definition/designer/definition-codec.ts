/**
 * 后端流程定义 JSON ↔ 流程设计器的流程定义
 *
 * 互转交给工作流引擎预设：后端格式为 WorkflowDefinitionJsonSerializer 稳定格式（camelCase），
 * 坐标存 extraProperties.designerLayout（{nodeId: {x,y}} 的 JSON 字符串），没列出的字段原样往返。
 * 打开时把人工任务的旧版受理人写法（Assignees / AssigneesExpression / CcUserIds）并入受理人规则，保存即落为新写法。
 */

import type { FlowDocument, XihanWorkflowDefinition } from '~/designers/flow'
import { fromWorkflowDefinition, toWorkflowDefinition } from '~/designers/flow'
import { migrateLegacyAssignees } from './assignee-rules'

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

/** 人工任务节点的属性副本里并入旧版受理人写法；别的节点原样返回 */
function migrateNode(node: unknown): unknown {
  if (!isRecord(node) || node.activityType !== 'UserTask' || !isRecord(node.properties))
    return node
  const properties = { ...node.properties }
  return migrateLegacyAssignees(properties) ? { ...node, properties } : node
}

/** 解析后端定义 JSON 为流程定义；JSON 非法或结构不对时抛错，由调用方提示 */
export function openDefinition(json: string): FlowDocument {
  const raw: unknown = JSON.parse(json)
  if (!isRecord(raw))
    throw new TypeError('definition must be a JSON object')
  const nodes = Array.isArray(raw.nodes) ? raw.nodes.map(migrateNode) : raw.nodes
  return fromWorkflowDefinition({ ...raw, nodes } as unknown as XihanWorkflowDefinition)
}

/** 流程定义序列化回后端定义 JSON */
export function serializeDefinition(document: FlowDocument): string {
  return JSON.stringify(toWorkflowDefinition(document), null, 2)
}
