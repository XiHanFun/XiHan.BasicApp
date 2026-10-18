/**
 * 设计器上 BasicApp 自己的校验规则：预设覆盖不到、后端发布时同样拒绝的情况。
 *
 * - 受理人规则里 BasicApp 注册的类型（角色、部门、岗位、发起人主管等）参数要填全；
 *   框架内置的 User / Expression 由预设检查，这里不重复报。
 * - 关联了审批业务的流程：结束节点要声明业务认可的结果，终止节点让业务得不到结论。
 */

import type { FlowDocument, SchemaIssue, SchemaRule } from '~/designers/flow'
import { isRuleComplete, readRules, RULE_PROPERTY_KEYS } from './assignee-rules'

/** 规则所在扩展的 id；问题码以它的末段 workflow. 开头 */
export const WORKFLOW_EXTENSION_ID = '@xihan/workflow'

/** 问题码 → i18n 键后缀（workflow.designer.validate 下） */
export const WORKFLOW_ISSUE_KEYS: Readonly<Record<string, string>> = {
  'workflow.assignee-incomplete': 'assignee_incomplete',
  'workflow.business-end-outcome': 'business_end_outcome',
  'workflow.business-terminate': 'business_terminate',
}

/** 预设自己检查的规则类型 */
const PRESET_RULE_TYPES = new Set(['User', 'Starter', 'Expression'])

export interface WorkflowRuleOptions {
  /** 流程编码对应的审批业务允许的结果；没有关联业务时返回 undefined */
  businessOutcomes: (code: string) => readonly string[] | undefined
}

function nodeIssue(document: FlowDocument, index: number, code: string, severity: 'error' | 'warning'): SchemaIssue {
  return { code, path: ['nodes', index], target: { kind: 'node', id: document.nodes[index]!.id }, severity }
}

function propertiesOf(document: FlowDocument, index: number): Record<string, unknown> {
  const properties = document.nodes[index]!.data?.properties
  return typeof properties === 'object' && properties !== null && !Array.isArray(properties) ? properties as Record<string, unknown> : {}
}

export function createWorkflowRules(options: WorkflowRuleOptions): SchemaRule<FlowDocument, unknown>[] {
  const assigneeIncomplete: SchemaRule<FlowDocument, unknown> = document => document.nodes.flatMap((node, index) => {
    if (node.type !== 'UserTask')
      return []
    const properties = propertiesOf(document, index)
    const incomplete = RULE_PROPERTY_KEYS.some(key => readRules(properties[key])
      .some(rule => !PRESET_RULE_TYPES.has(rule.type) && !isRuleComplete(rule)))
    return incomplete ? [nodeIssue(document, index, 'workflow.assignee-incomplete', 'error')] : []
  })

  const businessOutcomes: SchemaRule<FlowDocument, unknown> = (document) => {
    const code = typeof document.data?.code === 'string' ? document.data.code.trim() : ''
    const outcomes = code ? options.businessOutcomes(code) : undefined
    if (!outcomes)
      return []
    return document.nodes.flatMap((node, index) => {
      if (node.type === 'Terminate')
        return [nodeIssue(document, index, 'workflow.business-terminate', 'warning')]
      const outcome = propertiesOf(document, index).Outcome
      if (node.type === 'End' && (typeof outcome !== 'string' || !outcomes.includes(outcome)))
        return [nodeIssue(document, index, 'workflow.business-end-outcome', 'warning')]
      return []
    })
  }

  return [assigneeIncomplete, businessOutcomes]
}
