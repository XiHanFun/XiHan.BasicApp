/**
 * 流程设计器上 BasicApp 的设计器扩展：只放应用语义，节点类型与属性都来自工作流引擎预设。
 *
 * - 受理人规则：预设登记的编辑方式由 AssigneeRulesEditor 提供（选人、选角色查应用自己的数据）。
 * - 流程元数据：编码、名称、分类、描述只读（在流程定义列表的「编辑」里改），编码一行带关联审批业务的提示。
 * - 连线条件：条件表达式加源节点常见办理结果的一键填入。
 * - 规则：见 workflow-rules.ts。
 */

import type { AppContext } from 'vue'
import type { WorkflowBusinessTypeDto } from '../../../../api'
import type { FlowDesignerExtension, FlowDocument, FlowEdge, FlowPropertyEditorContext } from '~/designers/flow'
import { h } from 'vue'
import { createVuePropertyEditor } from '~/designers'
import { XIHAN_WORKFLOW_ASSIGNEE_RULES } from '~/designers/flow'
import AssigneeRulesEditor from './AssigneeRulesEditor.vue'
import EdgeConditionEditor from './EdgeConditionEditor.vue'
import FlowMetadataField from './FlowMetadataField.vue'
import { createWorkflowRules, WORKFLOW_EXTENSION_ID, WORKFLOW_ISSUE_KEYS } from './workflow-rules'

/** 源节点类型 → 常见办理结果（连线条件一键填入） */
const OUTCOME_HINTS: Readonly<Record<string, readonly string[]>> = {
  UserTask: ['approved', 'rejected', 'timeout'],
  SubWorkflow: ['completed'],
}

export interface WorkflowExtensionOptions {
  /** 编辑器挂在这个应用上下文下，照常能用 i18n、Pinia 与应用组件 */
  appContext: AppContext | null
  /** 已登记的审批业务 */
  businessTypes: () => readonly WorkflowBusinessTypeDto[]
  /** 设计器里此刻的流程定义（响应式），编辑器据它给提示 */
  document: () => FlowDocument | undefined
  /** 问题说明与提示的文案 */
  t: (key: string, params?: Record<string, unknown>) => string
}

type Context = FlowPropertyEditorContext

function variableNames(value: unknown): string[] {
  if (!Array.isArray(value))
    return []
  return value.flatMap(item => (typeof item === 'object' && item !== null && typeof (item as { name?: unknown }).name === 'string')
    ? [((item as { name: string }).name).trim()]
    : []).filter(Boolean)
}

/** 表达式可引用的变量：关联业务声明的变量 + 定义里声明的启动变量 */
function knownVariables(options: WorkflowExtensionOptions): string[] {
  const data = options.document()?.data
  const code = typeof data?.code === 'string' ? data.code.trim() : ''
  const business = options.businessTypes().find(item => item.code === code)
  return [...new Set([...(business?.variables.map(variable => variable.name) ?? []), ...variableNames(data?.variables)])]
}

/** 编码一行的提示：关联了审批业务时先说业务要的结束结果，再说这几项去哪里改 */
function codeHints(options: WorkflowExtensionOptions, code: string): string[] {
  const business = options.businessTypes().find(item => item.code === code.trim())
  const readonlyTip = options.t('workflow.designer.metadata_readonly_tip')
  return business
    ? [options.t('workflow.designer.flow_business_tip', { name: business.name, outcomes: business.outcomes.join(' / ') }), readonlyTip]
    : [readonlyTip]
}

function sourceOutcomes(options: WorkflowExtensionOptions, context: Context): readonly string[] {
  if (context.target.length !== 1)
    return []
  const edge = context.target[0]!.value as unknown as FlowEdge
  const source = options.document()?.nodes.find(node => node.id === edge.source?.node)
  return source ? OUTCOME_HINTS[source.type] ?? [] : []
}

export function createWorkflowExtension(options: WorkflowExtensionOptions): FlowDesignerExtension {
  const editor = (render: (context: Context) => ReturnType<typeof h>) => createVuePropertyEditor(options.appContext, render)
  /** 流程元数据只显示当前值 */
  const metadata = (extra: (value: string) => Record<string, unknown> = () => ({})) => editor((context) => {
    const value = typeof context.value === 'string' ? context.value : ''
    return h(FlowMetadataField, { value, label: context.descriptor.label, ...extra(value) })
  })

  return {
    id: WORKFLOW_EXTENSION_ID,
    rules: createWorkflowRules({
      businessOutcomes: code => options.businessTypes().find(item => item.code === code)?.outcomes,
    }),
    issueText: (issue) => {
      const key = WORKFLOW_ISSUE_KEYS[issue.code]
      return key ? options.t(`workflow.designer.validate.${key}`) : undefined
    },
    editors: {
      byControl: {
        [XIHAN_WORKFLOW_ASSIGNEE_RULES]: editor(context => h(AssigneeRulesEditor, {
          'value': context.value,
          'label': context.descriptor.label ?? '',
          'variables': knownVariables(options),
          'onUpdate:value': (rules: unknown) => {
            context.setValue((rules ?? undefined) as Context['value'])
            context.commit()
          },
        })),
      },
      byTarget: {
        document: {
          'data.code': metadata(code => ({ hints: codeHints(options, code) })),
          'data.name': metadata(),
          'data.category': metadata(),
          'data.description': metadata(() => ({ multiline: true })),
        },
        edge: {
          'data.condition': editor(context => h(EdgeConditionEditor, {
            'value': typeof context.value === 'string' ? context.value : undefined,
            'label': context.descriptor.label,
            'readonly': context.readonly,
            'outcomes': sourceOutcomes(options, context),
            'onUpdate:value': (condition: string | undefined) => context.setValue(condition),
            'onCommit': () => context.commit(),
          })),
        },
      },
    },
  }
}
